#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.Transcoding.Interfaces.SlimTv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Common.PathManager;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using MediaPortal.Plugins.Transcoding.Interfaces.MetaData;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders
{
  public abstract class BaseMediaAnalyzer : IMediaAnalyzer
  {
    #region Constants

    protected const string LIVE_MEDIAINFO_KEY = "MediaInfo";
    protected static readonly string DEFAULT_ANALYSIS_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\Transcoding\");

    #endregion

    protected int _analyzerMaximumThreads;
    protected int _analyzerTimeout;
    protected long _analyzerStreamTimeout;
    protected ILogger _logger = null;
    protected readonly Dictionary<float, long> _h264MaxDpbMbs = new Dictionary<float, long>();
    protected SlimTvHandler _slimTvHandler = new SlimTvHandler();
    protected ICollection<string> _audioExtensions = new List<string>();
    protected ICollection<string> _videoExtensions = new List<string>();
    protected ICollection<string> _imageExtensions = new List<string>();

    public BaseMediaAnalyzer()
    {
      _analyzerMaximumThreads = TranscodingServicePlugin.Settings.AnalyzerMaximumThreads;
      _analyzerTimeout = TranscodingServicePlugin.Settings.AnalyzerTimeout * 2;
      _analyzerStreamTimeout = TranscodingServicePlugin.Settings.AnalyzerStreamTimeout;
      _logger = ServiceRegistration.Get<ILogger>();
      _audioExtensions = new List<string>(TranscodingServicePlugin.Settings.AudioFileExtensions);
      _videoExtensions = new List<string>(TranscodingServicePlugin.Settings.VideoFileExtensions);
      _imageExtensions = new List<string>(TranscodingServicePlugin.Settings.ImageFileExtensions);

      _h264MaxDpbMbs.Add(1F, 396);
      _h264MaxDpbMbs.Add(1.1F, 396);
      _h264MaxDpbMbs.Add(1.2F, 900);
      _h264MaxDpbMbs.Add(1.3F, 2376);
      _h264MaxDpbMbs.Add(2F, 2376);
      _h264MaxDpbMbs.Add(2.1F, 4752);
      _h264MaxDpbMbs.Add(2.2F, 8100);
      _h264MaxDpbMbs.Add(3F, 8100);
      _h264MaxDpbMbs.Add(3.1F, 18000);
      _h264MaxDpbMbs.Add(3.2F, 20480);
      _h264MaxDpbMbs.Add(4F, 32768);
      _h264MaxDpbMbs.Add(4.1F, 32768);
      _h264MaxDpbMbs.Add(4.2F, 34816);
      _h264MaxDpbMbs.Add(5F, 110400);
      _h264MaxDpbMbs.Add(5.1F, 184320);
      _h264MaxDpbMbs.Add(5.2F, 184320);
    }

    protected bool HasAudioExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return _audioExtensions.Contains(ext);
    }

    protected bool HasVideoExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return _videoExtensions.Contains(ext);
    }

    protected bool HasImageExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return _imageExtensions.Contains(ext);
    }

    public abstract MetadataContainer ParseMediaStream(IResourceAccessor MediaResource);

    private void CopyAspects(MediaItem SourceMediaItem, MediaItem DestinationMediaItem)
    {
      foreach (IList<MediaItemAspect> aspectList in SourceMediaItem.Aspects.Values.ToList())
      {
        foreach (MediaItemAspect aspectData in aspectList.ToList())
        {
          if (aspectData is SingleMediaItemAspect)
          {
            MediaItemAspect.SetAspect(DestinationMediaItem.Aspects, (SingleMediaItemAspect)aspectData);
          }
          else if (aspectData is MultipleMediaItemAspect)
          {
            MediaItemAspect.AddOrUpdateAspect(DestinationMediaItem.Aspects, (MultipleMediaItemAspect)aspectData);
          }
        }
      }
    }

    private MetadataContainer ParseSlimTvItem(LiveTvMediaItem LiveMedia)
    {
      if (LiveMedia.AdditionalProperties.ContainsKey(LIVE_MEDIAINFO_KEY))
      {
        return (MetadataContainer)LiveMedia.AdditionalProperties[LIVE_MEDIAINFO_KEY];
      }
      else //Not been tuned for transcode aspects yet
      {
        MediaItem liveMediaItem = new MediaItem(LiveMedia.MediaItemId, LiveMedia.Aspects); //Preserve current aspects
        IChannel channel = (IChannel)LiveMedia.AdditionalProperties[LiveTvMediaItem.CHANNEL];
        MetadataContainer container = ParseChannelStream(channel.ChannelId, out liveMediaItem);
        if (container == null) return null;
        CopyAspects(liveMediaItem, LiveMedia);
        LiveMedia.AdditionalProperties.Add(LIVE_MEDIAINFO_KEY, container);
        return container;
      }
    }

    public MetadataContainer ParseMediaItem(MediaItem Media)
    {
      MetadataContainer info = null;

      //Check for live items
      if (Media.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        if (Media.IsLiveRadioItem())
        {
          info = ParseSlimTvItem((LiveTvMediaItem)Media);
          if (info != null)
          {
            info.Metadata.Live = true;
            info.Metadata.Size = 0;
          }
          return info;
        }
      }
      else if (Media.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        if (Media.IsLiveTvItem())
        {
          info = ParseSlimTvItem((LiveTvMediaItem)Media);
          if (info != null)
          {
            info.Metadata.Live = true;
            info.Metadata.Size = 0;
          }
          return info;
        }
      }

      //Analyze media
      IResourceAccessor mia = Media.GetResourceLocator().CreateAccessor();
      info = ParseMediaStream(mia);
      if (info == null)
      {
        _logger.Warn("MediaAnalyzer: Mediaitem {0} could not be parsed for information", Media.MediaItemId);
      }
      else
      {
        info.Metadata.Source = mia;
      }
      return info;
    }

    protected void SaveAnalysis(IResourceAccessor accessor, MetadataContainer analysis)
    {
      string filePath = DEFAULT_ANALYSIS_CACHE_PATH;
      if (accessor is ILocalFsResourceAccessor file)
      {
        filePath = Path.Combine(filePath, $"{file.ResourceName}.analysis");
      }
      else if (accessor is INetworkResourceAccessor link)
      {
        filePath = Path.Combine(filePath, $"{link.ResourceName}.analysis");
      }
      else
      {
        return;
      }
      if (!File.Exists(filePath))
      {
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
          Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        string fileContents = JsonConvert.SerializeObject(analysis);
        File.WriteAllText(filePath, fileContents, Encoding.UTF8);
      }
    }

    protected MetadataContainer LoadAnalysis(IResourceAccessor accessor)
    {
      string filePath = DEFAULT_ANALYSIS_CACHE_PATH;
      if (accessor is ILocalFsResourceAccessor file)
      {
        filePath = Path.Combine(filePath, $"{file.ResourceName}.analysis");
      }
      else if (accessor is INetworkResourceAccessor link)
      {
        filePath = Path.Combine(filePath, $"{link.ResourceName}.analysis");
      }
      if (File.Exists(filePath))
      {
        MetadataContainer info = JsonConvert.DeserializeObject<MetadataContainer>(File.ReadAllText(filePath, Encoding.UTF8));
        info.Metadata.Source = accessor;
        return info;
      }
      return null;
    }

    public MetadataContainer ParseChannelStream(int ChannelId, out MediaItem ChannelMediaItem)
    {
      MetadataContainer info = null;
      ChannelMediaItem = null;
      string identifier = "MediaAnalyzer_" + ChannelId;
      if (_slimTvHandler.StartTuning(identifier, ChannelId, out ChannelMediaItem))
      {
        try
        {
          if (ChannelMediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
          {
            //Create media item with channel GUID
            string channelGuid = "{54560000-0000-0000-0000-" + ChannelId.ToString("000000000000") + "}";
            LiveTvMediaItem liveTvMediaItem = new LiveTvMediaItem(new Guid(channelGuid), ChannelMediaItem.Aspects);
            foreach (KeyValuePair<string, object> props in ((LiveTvMediaItem)ChannelMediaItem).AdditionalProperties)
            {
              liveTvMediaItem.AdditionalProperties.Add(props.Key, props.Value);
            }
            ChannelMediaItem = liveTvMediaItem;
          }
          else if (ChannelMediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
          {
            //Create media item with channel GUID
            string channelGuid = "{5244494F-0000-0000-0000-" + ChannelId.ToString("000000000000") + "}";
            LiveTvMediaItem liveRadioMediaItem = new LiveTvMediaItem(new Guid(channelGuid), ChannelMediaItem.Aspects);
            foreach (KeyValuePair<string, object> props in ((LiveTvMediaItem)ChannelMediaItem).AdditionalProperties)
            {
              liveRadioMediaItem.AdditionalProperties.Add(props.Key, props.Value);
            }
            ChannelMediaItem = liveRadioMediaItem;
          }

          IResourceAccessor ra = _slimTvHandler.GetAnalysisAccessor(ChannelId);
          info = ParseMediaStream(ra);
          if (info == null) return null;
        }
        finally
        {
          _slimTvHandler.EndTuning(identifier);
        }
      }
      return info;
    }
  }
}
