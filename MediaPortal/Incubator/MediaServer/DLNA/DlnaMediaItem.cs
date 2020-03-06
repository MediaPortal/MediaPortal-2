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
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;

namespace MediaPortal.Extensions.MediaServer.DLNA
{
  public class DlnaMediaItem
  {
    private object _streamSync = new object();
    private List<Guid> _streams = new List<Guid>();
    private int _edition = -1;

    public DlnaMediaItem(EndPointSettings client)
    {
      Client = client;
      LastUpdated = DateTime.Now;
      TranscodingParameter = null;
      IsSegmented = false;
    }

    public async Task Initialize(MediaItem item, int? edition = null)
    {
      IsLive = false;
      MediaItemId = item.MediaItemId;
      if (MediaItemAspect.TryGetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, out string title))
        MediaItemTitle = title;
      else
        MediaItemTitle = "?";
      
      var info = await MediaAnalyzer.ParseMediaItemAsync(item, edition);
      if (info == null)
      {
        Logger.Warn("MediaServer: Mediaitem {0} couldn't be analyzed", item.MediaItemId);
        return;
      }

      if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        IsAudio = true;
      }
      else if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
      {
        IsImage = true;
      }
      else if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        IsVideo = true;
      }
      else
      {
        Logger.Warn("MediaServer: Mediaitem {0} contains no required aspect information", item.MediaItemId);
        return;
      }

      if (item.HasEditions && !edition.HasValue)
      {
        //Find edition with best matching audio that doesn't require transcoding
        int currentPriority = -1;
        bool currentRequiresTranscoding = false;
        var preferredAudioLang = Client.PreferredAudioLanguages.ToList();
        foreach (var checkEdition in info.Metadata.Keys)
        {
          for (int idx = 0; idx < info.Audio[checkEdition].Count; idx++)
          {
            VideoTranscoding video = TranscodeProfileManager.GetVideoTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
              info, checkEdition, Client.PreferredAudioLanguages, IsLive, "");

            bool requiresTranscoding = video != null;

            for (int priority = 0; priority < preferredAudioLang.Count; priority++)
            {
              if (preferredAudioLang[priority].Equals(info.Audio[checkEdition][idx].Language, StringComparison.InvariantCultureIgnoreCase) == true)
              {
                if (currentPriority == -1 || priority < currentPriority || (!requiresTranscoding && currentRequiresTranscoding && priority == currentPriority))
                {
                  currentPriority = priority;
                  currentRequiresTranscoding = requiresTranscoding;
                  edition = checkEdition;
                }
              }
            }
          }
        }
      }
      if (!edition.HasValue)
      {
        //Assign first edition
        _edition = info.Metadata.Min(m => m.Key);
      }
      else
      {
        _edition = edition.Value;
      }

      string transcodeId = item.MediaItemId.ToString() + "_" + Client.Profile.ID;
      if (MediaServerPlugin.Settings.TranscodingAllowed)
      {
        if (IsAudio)
        {
          AudioTranscoding audio = TranscodeProfileManager.GetAudioTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            info, _edition, IsLive, transcodeId);
          TranscodingParameter = audio;
        }
        else if (IsImage)
        {
          ImageTranscoding image = TranscodeProfileManager.GetImageTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            info, _edition, transcodeId);
          TranscodingParameter = image;
        }
        else if (IsVideo)
        {
          VideoTranscoding video = TranscodeProfileManager.GetVideoTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            info, _edition, Client.PreferredAudioLanguages, IsLive, transcodeId);
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
        if (IsVideo)
        {
          VideoTranscoding video = TranscodeProfileManager.GetVideoSubtitleTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            info, _edition, IsLive, transcodeId);
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
          SubtitleTranscodingParameter = video;
        }
      }

      //Assign some extra meta data
      DateTime? aspectDate;
      if (MediaItemAspect.TryGetAttribute(item.Aspects, ImporterAspect.ATTR_DATEADDED, out aspectDate))
        MediaItemAddedDate = aspectDate;
      if (MediaItemAspect.TryGetAttribute(item.Aspects, MediaAspect.ATTR_RECORDINGTIME, out aspectDate))
        MediaItemRecordingDate = aspectDate;

      if (IsVideo)
      {
        if (MediaItemAspect.TryGetAspects(item.Aspects, VideoStreamAspect.Metadata, out var videoStreamAspects))
        {
          MediaItemWidth = videoStreamAspects.FirstOrDefault()?.GetAttributeValue<int?>(VideoStreamAspect.ATTR_WIDTH);
          MediaItemHeight = videoStreamAspects.FirstOrDefault()?.GetAttributeValue<int?>(VideoStreamAspect.ATTR_HEIGHT);
        }
      }
      else if (IsImage)
      {
        if (MediaItemAspect.TryGetAspect(item.Aspects, ImageAspect.Metadata, out var imageAspect))
        {
          MediaItemImageMake = imageAspect?.GetAttributeValue<string>(ImageAspect.ATTR_MAKE);
          MediaItemImageMake = imageAspect?.GetAttributeValue<string>(ImageAspect.ATTR_MODEL);
          MediaItemImageMake = imageAspect?.GetAttributeValue<string>(ImageAspect.ATTR_FNUMBER);
          MediaItemImageMake = imageAspect?.GetAttributeValue<string>(ImageAspect.ATTR_ISO_SPEED);
          MediaItemImageMake = imageAspect?.GetAttributeValue<string>(ImageAspect.ATTR_EXPOSURE_TIME);
          MediaItemImageOrientation = imageAspect?.GetAttributeValue<int?>(ImageAspect.ATTR_ORIENTATION);
          MediaItemWidth = imageAspect?.GetAttributeValue<int?>(ImageAspect.ATTR_WIDTH);
          MediaItemHeight = imageAspect?.GetAttributeValue<int?>(ImageAspect.ATTR_HEIGHT);
        }
      }

      AssignDlnaMetadata(info, _edition);
    }

    public async Task<LiveTvMediaItem> InitializeChannel(int channelId)
    {
      IsLive = true;
      MediaItemId = Guid.Empty;
      MediaItemTitle = "Channel " + channelId;

      LiveTvMediaItem item = new LiveTvMediaItem(Guid.Empty);
      var info = await MediaAnalyzer.ParseChannelStreamAsync(channelId, item);
      if (info == null)
      {
        Logger.Warn("MediaServer: Channel {0} couldn't be analyzed", channelId);
        return null;
      }

      if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        IsAudio = true;
      }
      else if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        IsVideo = true;
      }
      else
      {
        Logger.Warn("MediaServer: Channel {0} contains no required aspect information", channelId);
        return null;
      }

      if (MediaItemAspect.TryGetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, out string title))
        MediaItemTitle = title;
      _edition = info.Metadata.Min(m => m.Key);

      string transcodeId = Guid.NewGuid().ToString() + "_" + Client.Profile.ID;
      if (MediaServerPlugin.Settings.TranscodingAllowed)
      {
        if (IsAudio)
        {
          AudioTranscoding audio = TranscodeProfileManager.GetAudioTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            info, _edition, IsLive, transcodeId);
          TranscodingParameter = audio;
        }
        else if (IsVideo)
        {
          VideoTranscoding video = TranscodeProfileManager.GetVideoTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Client.Profile.ID,
            info, _edition, Client.PreferredAudioLanguages, IsLive, transcodeId);
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
        if (IsVideo)
          TranscodingParameter = TranscodeProfileManager.GetLiveVideoTranscoding(info, Client.PreferredAudioLanguages, transcodeId);
        else if (IsAudio)
          TranscodingParameter = TranscodeProfileManager.GetLiveAudioTranscoding(info, transcodeId);
      }

      AssignDlnaMetadata(info, _edition);

      return item;
    }

    private void AssignDlnaMetadata(MetadataContainer info, int edition)
    {
      if (info == null)
        return;

      List<string> profileList = new List<string>();
      if (TranscodingParameter == null)
      {
        Metadata = info.Metadata[edition];
        Video = info.Video[edition];
        Audio = info.Audio[edition];
        Image = info.Image[edition];
        Subtitles = info.Subtitles[edition];
      }
      else
      {
        if (IsImage)
        {
          ImageTranscoding image = (ImageTranscoding)TranscodingParameter;
          TranscodedImageMetadata metadata = MediaConverter.GetTranscodedImageMetadata(image);
          Metadata = new MetadataStream
          {
            Mime = info.Metadata[edition].Mime,
            ImageContainerType = metadata.TargetImageCodec,
            Size = Client.EstimateTransodedSize ? info.Metadata[edition].Size : 0,
          };
          Image = new ImageStream
          {
            Height = metadata.TargetMaxHeight,
            Orientation = metadata.TargetOrientation,
            PixelFormatType = metadata.TargetPixelFormat,
            Width = metadata.TargetMaxWidth
          };
        }
        else if (IsAudio)
        {
          AudioTranscoding audio = (AudioTranscoding)TranscodingParameter;
          TranscodedAudioMetadata metadata = MediaConverter.GetTranscodedAudioMetadata(audio);
          Metadata = new MetadataStream
          {
            Mime = info.Metadata[edition].Mime,
            AudioContainerType = metadata.TargetAudioContainer,
            Bitrate = metadata.TargetAudioBitrate > 0 ? metadata.TargetAudioBitrate : null,
            Duration = info.Metadata[edition].Duration,
            Size = Client.EstimateTransodedSize ? (metadata.TargetAudioBitrate > 0 ? Convert.ToInt64((metadata.TargetAudioBitrate * 1024 * info.Metadata[edition].Duration) / 8.0) : (long?)null) : null,
          };
          AudioStream audioStream = new AudioStream();
          audioStream.Bitrate = metadata.TargetAudioBitrate;
          audioStream.Channels = metadata.TargetAudioChannels;
          audioStream.Codec = metadata.TargetAudioCodec;
          audioStream.Frequency = metadata.TargetAudioFrequency;
          Audio = new List<AudioStream> { audioStream };
        }
        else if (IsVideo)
        {
          VideoTranscoding video = (VideoTranscoding)TranscodingParameter;
          TranscodedVideoMetadata metadata = MediaConverter.GetTranscodedVideoMetadata(video);
          Metadata = new MetadataStream
          {
            Mime = info.Metadata[edition].Mime,
            VideoContainerType = metadata.TargetVideoContainer,
            Bitrate = metadata.TargetAudioBitrate > 0 && metadata.TargetVideoBitrate > 0 ? metadata.TargetAudioBitrate + metadata.TargetVideoBitrate : null,
            Duration = info.Metadata[edition].Duration,
            Size = Client.EstimateTransodedSize ? (metadata.TargetAudioBitrate > 0 && info.Metadata[edition].Duration > 0 ? Convert.ToInt64((metadata.TargetAudioBitrate * 1024 * info.Metadata[edition].Duration) / 8.0) : (long?)null) : null,
          };
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
          };
          AudioStream audioStream = new AudioStream();
          audioStream.Bitrate = metadata.TargetAudioBitrate;
          audioStream.Channels = metadata.TargetAudioChannels;
          audioStream.Codec = metadata.TargetAudioCodec;
          audioStream.Frequency = metadata.TargetAudioFrequency;
          Audio = new List<AudioStream> { audioStream };
        }
      }

      if (IsImage)
      {
        profileList = DlnaProfiles.ResolveImageProfile(Metadata.ImageContainerType, Image.Width, Image.Height);
      }
      else if (IsAudio)
      {
        var audio = Audio.FirstOrDefault();
        profileList = DlnaProfiles.ResolveAudioProfile(Metadata.AudioContainerType, audio?.Codec ?? AudioCodec.Unknown, audio?.Bitrate, audio?.Frequency, audio?.Channels);
      }
      else if (IsVideo)
      {
        var audio = Audio.FirstOrDefault();
        profileList = DlnaProfiles.ResolveVideoProfile(Metadata.VideoContainerType, Video.Codec, audio?.Codec ?? AudioCodec.Unknown, Video.ProfileType, Video.HeaderLevel,
          Video.Framerate, Video.Width, Video.Height, Video.Bitrate, audio?.Bitrate, Video.TimestampType);
      }

      string profile = "";
      string mime = info.Metadata[edition].Mime;
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
    public string DlnaProfile { get; set; }
    public string DlnaMime { get; set; }
    public EndPointSettings Client { get; private set; }

    public Guid MediaItemId { get; private set; }
    public string MediaItemTitle { get; private set; }
    public DateTime? MediaItemAddedDate { get; private set; }
    public DateTime? MediaItemRecordingDate { get; private set; }
    public int? MediaItemWidth { get; private set; }
    public int? MediaItemHeight { get; private set; }
    public int? MediaItemImageOrientation { get; private set; }
    public string MediaItemImageMake { get; private set; }
    public string MediaItemImageModel { get; private set; }
    public string MediaItemImageFNumber { get; private set; }
    public string MediaItemImageIsoSpeed { get; private set; }
    public string MediaItemImageExposureTime { get; private set; }

    public MetadataStream Metadata { get; private set; }
    public ImageStream Image { get; private set; }
    public VideoStream Video { get; private set; }
    public List<AudioStream> Audio { get; private set; }
    public Dictionary<int, List<SubtitleStream>> Subtitles { get; private set; } = new Dictionary<int, List<SubtitleStream>>();

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
        if (Metadata != null && IsVideo)
        {
          if (Metadata.VideoContainerType == VideoContainer.Unknown)
          {
            return false;
          }
          else if (Metadata.VideoContainerType == VideoContainer.Mp4)
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

    private static IMediaConverter MediaConverter
    {
      get { return ServiceRegistration.Get<IMediaConverter>(); }
    }
    private static IMediaAnalyzer MediaAnalyzer
    {
      get { return ServiceRegistration.Get<IMediaAnalyzer>(); }
    }
    private static ITranscodeProfileManager TranscodeProfileManager
    {
      get { return ServiceRegistration.Get<ITranscodeProfileManager>(); }
    }
    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
