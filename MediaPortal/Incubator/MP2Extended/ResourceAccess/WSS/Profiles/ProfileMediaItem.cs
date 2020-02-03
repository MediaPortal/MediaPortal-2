#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles;
using System.Linq;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles
{
  public class ProfileMediaItem
  {
    public const int EDITION_OFFSET = 1000;

    private List<Guid> _streams = new List<Guid>();
    private string _clientId = null;
    private int _edition;

    public ProfileMediaItem(string clientId, EndPointProfile profile, bool live)
    {
      _clientId = clientId;

      Profile = profile;
      LastUpdated = DateTime.Now;
      TranscodingParameter = null;
      IsSegmented = false;
      IsLive = live;
    }

    public async Task Initialize(Guid? userId, MediaItem item, int? edition, int? audioId = null, int? subtitleId = null)
    {
      MediaItemId = item.MediaItemId;

      var info = await MediaAnalyzer.ParseMediaItemAsync(item);
      if (info == null)
      {
        Logger.Warn("MP2Extended: Mediaitem {0} couldn't be analyzed", item.MediaItemId);
        return;
      }

      int? audioStreamId = null;
      List<string> preferredAudioLang = new List<string>();
      List<string> preferredSubtitleLang = new List<string>();
      await ResourceAccessUtils.AddPreferredLanguagesAsync(userId, preferredAudioLang, preferredSubtitleLang);

      if (audioId.HasValue)
      {
        if (audioId.Value >= EDITION_OFFSET)
        {
          edition = (audioId.Value / EDITION_OFFSET) - 1;
          audioStreamId = audioId.Value - ((audioId.Value / EDITION_OFFSET) * EDITION_OFFSET);
        }
        else
        {
          audioStreamId = audioId;
        }
      }

      if (item.HasEditions && !edition.HasValue)
      {
        //Find edition with best matching audio that doesn't require transcoding
        int currentPriority = -1;
        bool currentRequiresTranscoding = false;
        foreach (var checkEdition in info.Metadata.Keys)
        {
          for (int idx = 0; idx < info.Audio[checkEdition].Count; idx++)
          {
            VideoTranscoding video = TranscodeProfileManager.GetVideoTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Profile.ID,
              info, checkEdition, preferredAudioLang, IsLive, "");

            bool requiresTranscoding = video != null;

            for (int priority = 0; priority < preferredAudioLang.Count; priority++)
            {
              if (preferredAudioLang[priority].Equals(info.Audio[checkEdition][idx].Language, StringComparison.InvariantCultureIgnoreCase) == true)
              {
                if (currentPriority == -1 || priority < currentPriority || (!requiresTranscoding && currentRequiresTranscoding && priority == currentPriority))
                {
                  currentPriority = priority;
                  currentRequiresTranscoding = requiresTranscoding;
                  audioStreamId = info.Audio[checkEdition][idx].StreamIndex;
                  _edition = checkEdition;
                }
              }
            }
          }
        }
      }
      else
      {
        //Assign first edition
        _edition = info.Metadata.Min(m => m.Key);
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
        Logger.Warn("MP2Extended: Mediaitem {0} contains no required aspect information", item.MediaItemId);
        return;
      }

      string transcodeId = item.MediaItemId.ToString() + "_" + Profile.ID;
      if (IsLive)
        transcodeId = Guid.NewGuid().ToString() + "_" + Profile.ID;

      if (MP2Extended.Settings.TranscodingAllowed)
      {
        if (IsAudio)
        {
          AudioTranscoding audio = TranscodeProfileManager.GetAudioTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Profile.ID,
            info, _edition, IsLive, transcodeId);
          TranscodingParameter = audio;
        }
        else if (IsImage)
        {
          ImageTranscoding image = TranscodeProfileManager.GetImageTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Profile.ID,
            info, _edition, transcodeId);
          TranscodingParameter = image;
        }
        else if (IsVideo)
        {
          VideoTranscoding video;
          if (audioStreamId.HasValue)
          {
            video = TranscodeProfileManager.GetVideoTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Profile.ID,
              info, _edition, audioStreamId.Value, subtitleId, IsLive, transcodeId);
          }
          else
          {
            video = TranscodeProfileManager.GetVideoTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Profile.ID,
              info, _edition, preferredAudioLang, IsLive, transcodeId);
          }

          if (video != null)
          {
            if (video.TargetVideoContainer == VideoContainer.Hls)
            {
              IsSegmented = true;
            }
            if (MP2Extended.Settings.HardcodedSubtitlesAllowed == false)
            {
              video.TargetSubtitleSupport = SubtitleSupport.None;
            }
          }
          TranscodingParameter = video;
        }
      }

      if (TranscodingParameter == null)
      {
        if (IsLive)
        {
          if (IsVideo)
          {
            if (audioStreamId.HasValue)
              TranscodingParameter = TranscodeProfileManager.GetLiveVideoTranscoding(info, audioStreamId.Value, transcodeId);
            else
              TranscodingParameter = TranscodeProfileManager.GetLiveVideoTranscoding(info, preferredAudioLang, transcodeId);
          }
          else if (IsAudio)
            TranscodingParameter = TranscodeProfileManager.GetLiveAudioTranscoding(info, transcodeId);
        }
        else if (IsVideo)
        {
          VideoTranscoding video = TranscodeProfileManager.GetVideoSubtitleTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, Profile.ID,
            info, _edition, IsLive, transcodeId);
          if (video != null)
          {
            if (video.TargetVideoContainer == VideoContainer.Hls)
            {
              IsSegmented = true;
            }
            if (MP2Extended.Settings.HardcodedSubtitlesAllowed == false)
            {
              video.TargetSubtitleSupport = SubtitleSupport.None;
            }
          }
          TranscodingParameter = video;
        }
      }

      AssignWebMetadata(info, _edition);
    }

    private void AssignWebMetadata(MetadataContainer info, int edition)
    {
      if (info == null) return;
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
            Size = EstimateTransodedSize ? info.Metadata[edition].Size : 0,
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
            Size = EstimateTransodedSize ? (metadata.TargetAudioBitrate > 0 ? Convert.ToInt64((metadata.TargetAudioBitrate * 1024 * info.Metadata[edition].Duration) / 8.0) : (long?)null) : null,
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
            Size = EstimateTransodedSize ? (metadata.TargetAudioBitrate > 0 && info.Metadata[edition].Duration > 0 ? Convert.ToInt64((metadata.TargetAudioBitrate * 1024 * info.Metadata[edition].Duration) / 8.0) : (long?)null) : null,
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
        profileList = ProfileMime.ResolveImageProfile(Metadata.ImageContainerType, Image.Width ?? 0, Image.Height ?? 0);
      }
      else if (IsAudio)
      {
        profileList = ProfileMime.ResolveAudioProfile(Metadata.AudioContainerType, Audio[0].Codec, Audio[0].Bitrate ?? 0, Audio[0].Frequency ?? 0, Audio[0].Channels ?? 2);
      }
      else if (IsVideo)
      {
        profileList = ProfileMime.ResolveVideoProfile(Metadata.VideoContainerType, Video.Codec, Audio[0].Codec, Video.ProfileType, Video.HeaderLevel ?? 0,
          Video.Framerate ?? 0, Video.Width ?? 0, Video.Height ?? 0, Video.Bitrate ?? 0, Audio[0].Bitrate ?? 0, Video.TimestampType);
      }

      string mime = info.Metadata[edition].Mime;
      ProfileMime.FindCompatibleMime(Profile, profileList, ref mime);
      Mime = mime;
    }

    public bool IsTranscoding
    {
      get
      {
        if (TranscodingParameter == null)
        {
          return false;
        }
        return MediaConverter.IsTranscodeRunningAsync(_clientId, TranscodingParameter.TranscodeId).Result;
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
        MediaConverter.StopTranscodeAsync(_clientId, TranscodingParameter.TranscodeId);
      }
    }

    public Guid StartStreaming()
    {
      Guid ret = Guid.NewGuid();
      _streams.Add(ret);
      return ret;
    }
    public void StopStreaming()
    {
      _streams.Clear();
    }
    public void StopStreaming(Guid streamId)
    {
      _streams.Remove(streamId);
    }
    public bool IsStreamActive(Guid streamId)
    {
      return _streams.Contains(streamId);
    }
    public bool IsStreaming
    {
      get
      {
        return _streams.Count > 0;
      }
    }

    public string Mime { get; set; }
    public string SegmentDir { get; set; }
    public bool EstimateTransodedSize { get; set; } = true;
    public EndPointProfile Profile { get; private set; }

    public Guid MediaItemId { get; private set; }
    public MetadataStream Metadata { get; private set; }
    public ImageStream Image { get; private set; }
    public VideoStream Video { get; private set; }
    public List<AudioStream> Audio { get; private set; }
    public Dictionary<int, List<SubtitleStream>> Subtitles { get; private set; }

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
        if (Metadata != null && Video != null && Audio.Count > 0 && Video.Codec != VideoCodec.Unknown)
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

    private ITranscodeProfileManager TranscodeProfileManager
    {
      get { return ServiceRegistration.Get<ITranscodeProfileManager>(); }
    }
    private IMediaConverter MediaConverter
    {
      get { return ServiceRegistration.Get<IMediaConverter>(); }
    }
    private IMediaAnalyzer MediaAnalyzer
    {
      get { return ServiceRegistration.Get<IMediaAnalyzer>(); }
    }
    private ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
