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
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Extensions.MediaServer.ResourceAccess;

namespace MediaPortal.Extensions.MediaServer.DLNA
{
  public class DlnaMediaItem
  {
    private object _streamSync = new object();
    private List<Guid> _streams = new List<Guid>();

    public DlnaMediaItem(MediaItem item, EndPointSettings client, bool live)
    {
      Client = client;
      MediaSource = item;
      LastUpdated = DateTime.Now;
      TranscodingParameter = null;
      IsSegmented = false;
      IsLive = live;
    }

    public async Task Initialize(int? edition = null)
    {
      bool sourceIsLive = false;

      var infos = await MediaAnalyzer.ParseMediaItemAsync(MediaSource, edition);
      if (infos == null || infos.Count == 0)
      {
        Logger.Warn("MediaServer: Mediaitem {0} couldn't be analyzed", MediaSource.MediaItemId);
        return;
      }

      if (MediaSource.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        IsAudio = true;
      }
      else if (MediaSource.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
      {
        IsImage = true;
      }
      else if (MediaSource.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        IsVideo = true;
      }
      else
      {
        Logger.Warn("MediaServer: Mediaitem {0} contains no required aspect information", MediaSource.MediaItemId);
        return;
      }

      IList<MetadataContainer> media = infos.First().Value;
      if (MediaSource.HasEditions && !edition.HasValue)
      {
        //Find best matching audio edition
        int currentPriority = -1;
        var preferredAudioLang = Client.PreferredAudioLanguages.ToList();
        foreach (var info in infos)
        {
          var mc = info.Value.First();
          for (int idx = 0; idx < mc.Audio.Count; idx++)
          {
            for (int priority = 0; priority < preferredAudioLang.Count; priority++)
            {
              if (preferredAudioLang[priority].Equals(mc.Audio[idx].Language, StringComparison.InvariantCultureIgnoreCase) == true)
              {
                if (currentPriority == -1 || priority < currentPriority)
                {
                  currentPriority = priority;
                  media = info.Value;
                }
              }
            }
          }
        }
      }
      var firstMedia = media.First();

      if (MediaServerPlugin.Settings.TranscodingAllowed == true)
      {
        string transcodeId = MediaSource.MediaItemId.ToString() + "_" + Client.Profile.ID;
        if (sourceIsLive) transcodeId = Guid.NewGuid().ToString() + "_" + Client.Profile.ID;

        if (IsAudio)
        {
          AudioTranscoding audio = TranscodeProfileManager.GetAudioTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            firstMedia, IsLive, transcodeId);
          TranscodingParameter = audio;
        }
        else if (IsImage)
        {
          ImageTranscoding image = TranscodeProfileManager.GetImageTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            firstMedia, transcodeId);
          TranscodingParameter = image;
        }
        else if (IsVideo)
        {
          VideoTranscoding video = TranscodeProfileManager.GetVideoTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            media, Client.PreferredAudioLanguages, IsLive, transcodeId);
          if (video != null)
          {
            if (video.TargetVideoContainer == VideoContainer.Hls)
            {
              IsSegmented = true;
            }
            if (MediaServerPlugin.Settings.HardcodedSubtitlesAllowed == false)
            {
              video.TargetSubtitleSupport = SubtitleSupport.None;
            }
          }
          TranscodingParameter = video;
        }
      }

      if (TranscodingParameter == null)
      {
        string transcodeId = MediaSource.MediaItemId.ToString() + "_" + Client.Profile.ID;
        if (sourceIsLive) transcodeId = Guid.NewGuid().ToString() + "_" + Client.Profile.ID;

        if (sourceIsLive == true)
        {
          if (IsVideo)
            TranscodingParameter = TranscodeProfileManager.GetLiveVideoTranscoding(firstMedia, Client.PreferredAudioLanguages, transcodeId);
          else if (IsAudio)
            TranscodingParameter = TranscodeProfileManager.GetLiveAudioTranscoding(firstMedia, transcodeId);
        }
        else if (IsVideo)
        {
          VideoTranscoding video = TranscodeProfileManager.GetVideoSubtitleTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            media, IsLive, transcodeId);
          if (video != null)
          {
            if (video.TargetVideoContainer == VideoContainer.Hls)
            {
              IsSegmented = true;
            }
            if (MediaServerPlugin.Settings.HardcodedSubtitlesAllowed == false)
            {
              video.TargetSubtitleSupport = SubtitleSupport.None;
            }
          }
          TranscodingParameter = video;
        }
      }

      AssignDlnaMetadata(media);
    }

    private void AssignDlnaMetadata(IList<MetadataContainer> infos)
    {
      if (infos == null || infos.Count == 0)
        return;

      MetadataContainer info = infos.First();
      List<string> profileList = new List<string>();
      if (TranscodingParameter == null)
      {
        //Stacked media should always have a transcoding parameter so take first
        DlnaMetadata = info;
      }
      else
      {
        if (info.IsImage)
        {
          ImageTranscoding image = (ImageTranscoding)TranscodingParameter;
          TranscodedImageMetadata metadata = MediaConverter.GetTranscodedImageMetadata(image);
          DlnaMetadata = new MetadataContainer
          {
            Metadata = new MetadataStream
            {
              Mime = info.Metadata.Mime,
              ImageContainerType = metadata.TargetImageCodec,
              Size = Client.EstimateTransodedSize ? info.Metadata.Size : 0,
            },
            Image = new ImageStream
            {
              Height = metadata.TargetMaxHeight,
              Orientation = metadata.TargetOrientation,
              PixelFormatType = metadata.TargetPixelFormat,
              Width = metadata.TargetMaxWidth
            }
          };
        }
        else if (info.IsAudio)
        {
          AudioTranscoding audio = (AudioTranscoding)TranscodingParameter;
          TranscodedAudioMetadata metadata = MediaConverter.GetTranscodedAudioMetadata(audio);
          DlnaMetadata = new MetadataContainer
          {
            Metadata = new MetadataStream
            {
              Mime = info.Metadata.Mime,
              AudioContainerType = metadata.TargetAudioContainer,
              Bitrate = metadata.TargetAudioBitrate > 0 ? metadata.TargetAudioBitrate : null,
              Duration = info.Metadata.Duration,
              Size = Client.EstimateTransodedSize ? (metadata.TargetAudioBitrate > 0 ? Convert.ToInt64((metadata.TargetAudioBitrate * 1024 * info.Metadata.Duration) / 8.0) : (long?)null) : null,
            }
          };

          AudioStream audioStream = new AudioStream();
          audioStream.Bitrate = metadata.TargetAudioBitrate;
          audioStream.Channels = metadata.TargetAudioChannels;
          audioStream.Codec = metadata.TargetAudioCodec;
          audioStream.Frequency = metadata.TargetAudioFrequency;
          DlnaMetadata.Audio.Add(audioStream);
        }
        else if (info.IsVideo)
        {
          VideoTranscoding video = (VideoTranscoding)TranscodingParameter;
          TranscodedVideoMetadata metadata = MediaConverter.GetTranscodedVideoMetadata(video);
          int selectedAudio = 0;
          for (int stream = 0; stream < info.Audio.Count; stream++)
          {
            if (video.FirstAudioStreamIndex == info.Audio[stream].StreamIndex)
            {
              selectedAudio = stream;
              break;
            }
          }

          DlnaMetadata = new MetadataContainer
          {
            Metadata = new MetadataStream
            {
              Mime = info.Metadata.Mime,
              VideoContainerType = metadata.TargetVideoContainer,
              Bitrate = metadata.TargetAudioBitrate > 0 && metadata.TargetVideoBitrate > 0 ? metadata.TargetAudioBitrate + metadata.TargetVideoBitrate : null,
              Duration = infos.All(i => i.Metadata?.Duration > 0) ? infos.Sum(i => i.Metadata.Duration) : null,
              Size = Client.EstimateTransodedSize ? (metadata.TargetAudioBitrate > 0 && infos.All(i => i.Metadata?.Duration > 0) ? Convert.ToInt64((metadata.TargetAudioBitrate * 1024 * infos.Sum(i => i.Metadata.Duration)) / 8.0) : (long?)null) : null,
            },
            Video = new VideoStream
            {
              AspectRatio = metadata.TargetVideoAspectRatio,
              Bitrate = metadata.TargetVideoBitrate,
              Codec = metadata.TargetVideoCodec,
              Framerate = metadata.TargetVideoFrameRate,
              HeaderLevel = metadata.TargetLevel,
              ProfileType = metadata.TargetProfile,
              RefLevel = metadata.TargetLevel,
              Height = metadata.TargetVideoMaxHeight,
              PixelAspectRatio = metadata.TargetVideoPixelAspectRatio,
              PixelFormatType = metadata.TargetVideoPixelFormat,
              TimestampType = metadata.TargetVideoTimestamp,
              Width = metadata.TargetVideoMaxWidth,
            }
          };

          AudioStream audioStream = new AudioStream();
          audioStream.Bitrate = metadata.TargetAudioBitrate;
          audioStream.Channels = metadata.TargetAudioChannels;
          audioStream.Codec = metadata.TargetAudioCodec;
          audioStream.Frequency = metadata.TargetAudioFrequency;
          DlnaMetadata.Audio.Add(audioStream);
        }
      }

      if (info.IsImage)
      {
        profileList = DlnaProfiles.ResolveImageProfile(DlnaMetadata.Metadata.ImageContainerType, DlnaMetadata.Image.Width, DlnaMetadata.Image.Height);
      }
      else if (info.IsAudio)
      {
        profileList = DlnaProfiles.ResolveAudioProfile(DlnaMetadata.Metadata.AudioContainerType, DlnaMetadata.FirstAudioStream.Codec, DlnaMetadata.FirstAudioStream.Bitrate, DlnaMetadata.FirstAudioStream.Frequency, DlnaMetadata.FirstAudioStream.Channels);
      }
      else if (info.IsVideo)
      {
        profileList = DlnaProfiles.ResolveVideoProfile(DlnaMetadata.Metadata.VideoContainerType, DlnaMetadata.Video.Codec, DlnaMetadata.Audio.Count > 0 ? DlnaMetadata.FirstAudioStream.Codec : AudioCodec.Unknown, DlnaMetadata.Video.ProfileType, DlnaMetadata.Video.HeaderLevel,
          DlnaMetadata.Video.Framerate, DlnaMetadata.Video.Width, DlnaMetadata.Video.Height, DlnaMetadata.Video.Bitrate, DlnaMetadata.FirstAudioStream?.Bitrate, DlnaMetadata.Video.TimestampType);
      }

      string profile = "";
      string mime = info.Metadata.Mime;
      if (DlnaProfiles.TryFindCompatibleProfile(Client, profileList, ref profile, ref mime))
      {
        DlnaMime = mime;
        DlnaProfile = profile;
      }
    }

    public bool IsTranscoding
    {
      get
      {
        if (TranscodingParameter == null)
        {
          return false;
        }
        return MediaConverter.IsTranscodeRunningAsync(Client.ClientId.ToString(), TranscodingParameter.TranscodeId).Result;
      }
    }

    public bool StartTrancoding()
    {
      LastUpdated = DateTime.Now;
      return true;
    }

    public void StopTranscoding()
    {
      if (TranscodingParameter != null)
      {
        MediaConverter.StopTranscodeAsync(Client.ClientId.ToString(), TranscodingParameter.TranscodeId).Wait();
      }
    }

    public Guid StartStreaming()
    {
      Guid ret = Guid.NewGuid();
      lock(_streamSync)
        _streams.Add(ret);
      return ret;
    }
    public void StopStreaming()
    {
      lock (_streamSync)
        _streams.Clear();
    }
    public void StopStreaming(Guid streamId)
    {
      lock (_streamSync)
        _streams.Remove(streamId);
    }
    public bool IsStreamActive(Guid streamId)
    {
      lock (_streamSync)
        return _streams.Contains(streamId);
    }
    public bool IsStreaming
    {
      get
      {
        lock (_streamSync)
          return _streams.Count > 0;
      }
    }
    public TranscodeContext TranscodingContext { get; set; }
    public string DlnaProfile { get; set; }
    public string DlnaMime { get; set; }
    public MetadataContainer DlnaMetadata { get; private set; }
    public EndPointSettings Client { get; private set; }
    public MediaItem MediaSource { get; private set; }
    public bool IsSegmented { get; private set; }
    public bool IsLive { get; private set; }
    public bool IsStreamable
    {
      get
      {
        if (IsTranscoded == false || IsTranscoding == false)
        {
          return true;
        }
        if (DlnaMetadata != null && DlnaMetadata.IsVideo == true)
        {
          if (DlnaMetadata.Metadata.VideoContainerType == VideoContainer.Unknown)
          {
            return false;
          }
          else if (DlnaMetadata.Metadata.VideoContainerType == VideoContainer.Mp4)
          {
            return false;
          }
        }
        return true;
      }
    }
    public BaseTranscoding TranscodingParameter { get; private set; }
    public BaseTranscoding SubtitleTranscodingParameter { get; private set; }
    public bool IsImage { get; private set; }
    public bool IsAudio { get; private set; }
    public bool IsVideo { get; private set; }
    public bool IsTranscoded
    {
      get
      {
        return TranscodingParameter != null;
      }
    }
    public DateTime LastUpdated { get; set; }
    internal static IMediaConverter MediaConverter
    {
      get { return ServiceRegistration.Get<IMediaConverter>(); }
    }
    internal static IMediaAnalyzer MediaAnalyzer
    {
      get { return ServiceRegistration.Get<IMediaAnalyzer>(); }
    }
    internal static ITranscodeProfileManager TranscodeProfileManager
    {
      get { return ServiceRegistration.Get<ITranscodeProfileManager>(); }
    }
    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
