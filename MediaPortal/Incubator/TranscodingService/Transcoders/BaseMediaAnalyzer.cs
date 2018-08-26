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
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Extensions.TranscodingService.Interfaces.SlimTv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Common.PathManager;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using MediaPortal.Extensions.TranscodingService.Interfaces.MetaData;
using System.Threading.Tasks;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders
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

    public abstract Task<MetadataContainer> ParseMediaStreamAsync(IResourceAccessor MediaResource);

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

    private async Task<MetadataContainer> ParseSlimTvItemAsync(LiveTvMediaItem LiveMedia)
    {
      try
      {
        if (LiveMedia.AdditionalProperties.ContainsKey(LIVE_MEDIAINFO_KEY))
        {
          return (MetadataContainer)LiveMedia.AdditionalProperties[LIVE_MEDIAINFO_KEY];
        }
        else //Not been tuned for transcode aspects yet
        {
          LiveTvMediaItem liveMediaItem = new LiveTvMediaItem(LiveMedia.MediaItemId, LiveMedia.Aspects); //Preserve current aspects
          IChannel channel = (IChannel)LiveMedia.AdditionalProperties[LiveTvMediaItem.CHANNEL];
          var container = await ParseChannelStreamAsync(channel.ChannelId, liveMediaItem).ConfigureAwait(false);
          if (container == null) return null;
          CopyAspects(liveMediaItem, LiveMedia);
          LiveMedia.AdditionalProperties.Add(LIVE_MEDIAINFO_KEY, container);
          return container;
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Live mediaitem {0} could not be parsed", ex, LiveMedia.MediaItemId);
      }
      return null;
    }

    public async Task<IDictionary<int, IList<MetadataContainer>>> ParseMediaItemAsync(MediaItem Media, int? editionId = null)
    {
      try
      {
        if (Media.IsStub)
          return null;

        IDictionary<int, IList<MetadataContainer>> mediaContainers = new Dictionary<int, IList<MetadataContainer>>();

        //Check for live items
        if (Media.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        {
          if (Media.IsLiveRadioItem() && Media is LiveTvMediaItem ltmi)
          {
            MetadataContainer info = await ParseSlimTvItemAsync(ltmi).ConfigureAwait(false);
            if (info != null)
            {
              info.Metadata.Live = true;
              info.Metadata.Size = 0;
            }

            mediaContainers.Add(-1, new MetadataContainer[] { info });
            return mediaContainers;
          }
        }
        else if (Media.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
        {
          if (Media.IsLiveTvItem() && Media is LiveTvMediaItem ltmi)
          {
            MetadataContainer info = await ParseSlimTvItemAsync(ltmi).ConfigureAwait(false);
            if (info != null)
            {
              info.Metadata.Live = true;
              info.Metadata.Size = 0;
            }
            mediaContainers.Add(-1, new MetadataContainer[] { info });
            return mediaContainers;
          }
        }

        IList<MultipleMediaItemAspect> providerAspects;
        if (!MediaItemAspect.TryGetAspects(Media.Aspects, ProviderResourceAspect.Metadata, out providerAspects))
        {
          return null;
        }

        IDictionary<int, ResourceLocator> resources = null;
        if (Media.HasEditions)
        {
          IEnumerable<int> praIdxs = null;
          if (editionId.HasValue)
            praIdxs = Media.Editions.First(e => e.Key == editionId.Value).Value.PrimaryResourceIndexes;
          else
            praIdxs = Media.Editions.SelectMany(e => e.Value.PrimaryResourceIndexes).Distinct();

          resources = providerAspects.Where(pra => praIdxs.Contains(pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX))).
              ToDictionary(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX), pra => new ResourceLocator(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_SYSTEM_ID), ResourcePath.Deserialize(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH))));
        }
        else
        {
          resources = providerAspects.Where(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY).
              ToDictionary(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX), pra => new ResourceLocator(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_SYSTEM_ID), ResourcePath.Deserialize(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH))));
        }

        //Analyze media
        foreach (var res in resources)
        {
          IResourceAccessor mia = res.Value.CreateAccessor();
          MetadataContainer info = await ParseMediaStreamAsync(mia).ConfigureAwait(false);
          if (info == null)
          {
            _logger.Error("MediaAnalyzer: Mediaitem {0} could not be parsed for information", Media.MediaItemId);
            return null;
          }
          else
          {
            info.Metadata.Source = mia;

            //Add external subtitles (embedded ones should already be included)
            IList<MultipleMediaItemAspect> subtitleAspects;
            if (MediaItemAspect.TryGetAspects(Media.Aspects, SubtitleAspect.Metadata, out subtitleAspects))
            {
              IResourceAccessor ra = null;
              info.Subtitles.AddRange(subtitleAspects.Where(sa => sa.GetAttributeValue<int>(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX) == res.Key && sa.GetAttributeValue<bool>(SubtitleAspect.ATTR_INTERNAL) == false &&
                ResourcePath.Deserialize(providerAspects.FirstOrDefault(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) == sa.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX))?.
                    GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH) ?? "").TryCreateLocalResourceAccessor(out ra) && ra is ILocalFsResourceAccessor).
                Select(sa => new SubtitleStream
                {
                  StreamIndex = -1,
                  CharacterEncoding = sa.GetAttributeValue<string>(SubtitleAspect.ATTR_SUBTITLE_ENCODING),
                  Codec = SubtitleHelper.GetSubtitleCodec(sa.GetAttributeValue<string>(SubtitleAspect.ATTR_SUBTITLE_FORMAT)),
                  Default = sa.GetAttributeValue<bool>(SubtitleAspect.ATTR_DEFAULT),
                  Language = sa.GetAttributeValue<string>(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE),
                  IsPartial = resources.Count > 1,
                  Source = (ra as ILocalFsResourceAccessor).LocalFileSystemPath
                }));
            }
          }
          int edition = -1;
          if (Media.HasEditions)
            edition = Media.Editions.First(e => e.Value.PrimaryResourceIndexes.Contains(res.Key)).Key;
          if (!mediaContainers.ContainsKey(edition))
            mediaContainers.Add(edition, new List<MetadataContainer>());

          mediaContainers[edition].Add(info);
        }
        return mediaContainers;
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Mediaitem {0} could not be parsed", ex, Media.MediaItemId);
      }
      return null;
    }

    private string GetResourceCategory(string resourceName)
    {
      if (HasAudioExtension(resourceName))
      {
        return "Audio";
      }
      else if (HasImageExtension(resourceName))
      {
        return "Image";
      }
      else if (HasVideoExtension(resourceName))
      {
        return "Video";
      }
      return "";
    }

    protected async Task SaveAnalysisAsync(IResourceAccessor accessor, MetadataContainer analysis)
    {
      try
      {
        string filePath = DEFAULT_ANALYSIS_CACHE_PATH;
        if (accessor is ILocalFsResourceAccessor file)
        {
          filePath = Path.Combine(filePath, GetResourceCategory(file.ResourceName));
          filePath = Path.Combine(filePath, $"{file.ResourceName}.analysis");
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
          using (var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8))
            await streamWriter.WriteAsync(fileContents).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Error saving analysis", ex);
      }
    }

    protected async Task<MetadataContainer> LoadAnalysisAsync(IResourceAccessor accessor)
    {
      try
      {
        string filePath = DEFAULT_ANALYSIS_CACHE_PATH;
        if (accessor is ILocalFsResourceAccessor file)
        {
          filePath = Path.Combine(filePath, GetResourceCategory(file.ResourceName));
          filePath = Path.Combine(filePath, $"{file.ResourceName}.analysis");
        }
        if (File.Exists(filePath))
        {
          MetadataContainer info = null;
          using (var streamReader = new StreamReader(filePath, Encoding.UTF8))
            info = JsonConvert.DeserializeObject<MetadataContainer>(await streamReader.ReadToEndAsync().ConfigureAwait(false));
          info.Metadata.Source = accessor;
          return info;
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Error loading analysis", ex);
      }
      return null;
    }

    public async Task<MetadataContainer> ParseChannelStreamAsync(int ChannelId, LiveTvMediaItem ChannelMediaItem)
    {
      MetadataContainer info = null;
      try
      {
        string identifier = "MediaAnalyzer_" + ChannelId;
        var result = await _slimTvHandler.StartTuningAsync(identifier, ChannelId).ConfigureAwait(false);
        try
        {
          if (result.Success)
          {
            CopyAspects(result.LiveMediaItem, ChannelMediaItem);
            foreach (KeyValuePair<string, object> props in ((LiveTvMediaItem)result.LiveMediaItem).AdditionalProperties)
            {
              ChannelMediaItem.AdditionalProperties[props.Key] = props.Value;
            }

            IResourceAccessor ra = await _slimTvHandler.GetAnalysisAccessorAsync(ChannelId).ConfigureAwait(false);
            info = await ParseMediaStreamAsync(ra).ConfigureAwait(false);
            if (info == null)
              return null;
          }
        }
        finally
        {
          await _slimTvHandler.EndTuningAsync(identifier).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Error parsing channel {0}", ex, ChannelId);
      }
      return info;
    }
  }
}
