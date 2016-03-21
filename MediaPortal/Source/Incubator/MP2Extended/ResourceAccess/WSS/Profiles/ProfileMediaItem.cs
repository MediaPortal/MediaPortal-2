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
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces.Aspects;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles
{
  public class ProfileMediaItem
  {
    private List<Guid> _streams = new List<Guid>();
    private string _clientId = null;

    public ProfileMediaItem(string clientId, MediaItem item, EndPointSettings client, bool live)
    {
      _clientId = clientId;

      Client = client;
      MediaSource = item;
      LastUpdated = DateTime.Now;
      TranscodingParameter = null;
      IsSegmented = false;
      IsLive = live;
      MetadataContainer info = null;
      bool sourceIsLive = false;

      info = MediaAnalyzer.ParseMediaItem(item);
      if(info == null)
      {
        Logger.Warn("MP2Extended: Mediaitem {0} couldn't be analyzed", item.MediaItemId);
        return;
      }

      if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        IsAudio = true;
        if (item.Aspects.ContainsKey(TranscodeItemAudioAspect.ASPECT_ID) == false)
        {
          Logger.Warn("MP2Extended: Mediaitem {0} contains no transcoding audio information", item.MediaItemId);
          return;
        }
      }
      else if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
      {
        IsImage = true;
        if (item.Aspects.ContainsKey(TranscodeItemImageAspect.ASPECT_ID) == false)
        {
          Logger.Warn("MP2Extended: Mediaitem {0} contains no transcoding image information", item.MediaItemId);
          return;
        }
      }
      else if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        IsVideo = true;
        if (item.Aspects.ContainsKey(TranscodeItemVideoAspect.ASPECT_ID) == false)
        {
          Logger.Warn("MP2Extended: Mediaitem {0} contains no transcoding video information", item.MediaItemId);
          return;
        }
      }
      else
      {
        Logger.Warn("MP2Extended: Mediaitem {0} contains no required aspect information", item.MediaItemId);
        return;
      }

      if (MP2Extended.Settings.TranscodingAllowed == true)
      {
        string transcodeId = MediaSource.MediaItemId.ToString() + "_" + Client.Profile.ID;
        if (sourceIsLive) transcodeId = Guid.NewGuid().ToString() + "_" + Client.Profile.ID;

        if (IsAudio)
        {
          AudioTranscoding audio = TranscodeProfileManager.GetAudioTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, client.Profile.ID,
            info, live, transcodeId);
          TranscodingParameter = audio;
        }
        else if (IsImage)
        {
          ImageTranscoding image = TranscodeProfileManager.GetImageTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, client.Profile.ID,
            info, transcodeId);
          TranscodingParameter = image;
        }
        else if (IsVideo)
        {
          VideoTranscoding video = TranscodeProfileManager.GetVideoTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, client.Profile.ID,
            info, client.PreferredAudioLanguages, live, transcodeId);
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
        string transcodeId = MediaSource.MediaItemId.ToString() + "_" + Client.Profile.ID;
        if (sourceIsLive) transcodeId = Guid.NewGuid().ToString() + "_" + Client.Profile.ID;

        if (sourceIsLive == true)
        {
          if (IsVideo)
            TranscodingParameter = TranscodeProfileManager.GetLiveVideoTranscoding(info, client.PreferredAudioLanguages, transcodeId);
          else if (IsAudio)
            TranscodingParameter = TranscodeProfileManager.GetLiveAudioTranscoding(info, transcodeId);
        }
        else if (IsVideo)
        {
          VideoTranscoding video = TranscodeProfileManager.GetVideoSubtitleTranscoding(ProfileManager.TRANSCODE_PROFILE_SECTION, client.Profile.ID,
            info, live, transcodeId);
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

      AssignWebMetadata(info);
    }

    private void AssignWebMetadata(MetadataContainer info)
    {
      if (info == null) return;
      List<string> profileList = new List<string>();
      if (TranscodingParameter == null)
      {
        WebMetadata = info;
      }
      else
      {
        if (IsImage)
        {
          ImageTranscoding image = (ImageTranscoding)TranscodingParameter;
          TranscodedImageMetadata metadata = MediaConverter.GetTranscodedImageMetadata(image);
          WebMetadata = new MetadataContainer();
          WebMetadata.Metadata.Mime = info.Metadata.Mime;
          WebMetadata.Metadata.ImageContainerType = metadata.TargetImageCodec;
          WebMetadata.Metadata.Size = 0;
          if (Client.EstimateTransodedSize == true)
          {
            WebMetadata.Metadata.Size = info.Metadata.Size;
          }
          WebMetadata.Image.Height = metadata.TargetMaxHeight;
          WebMetadata.Image.Orientation = metadata.TargetOrientation;
          WebMetadata.Image.PixelFormatType = metadata.TargetPixelFormat;
          WebMetadata.Image.Width = metadata.TargetMaxWidth;
        }
        else if (IsAudio)
        {
          AudioTranscoding audio = (AudioTranscoding)TranscodingParameter;
          TranscodedAudioMetadata metadata = MediaConverter.GetTranscodedAudioMetadata(audio);
          WebMetadata = new MetadataContainer();
          WebMetadata.Metadata.Mime = info.Metadata.Mime;
          WebMetadata.Metadata.AudioContainerType = metadata.TargetAudioContainer;
          WebMetadata.Metadata.Bitrate = 0;
          if (metadata.TargetAudioBitrate > 0)
          {
            WebMetadata.Metadata.Bitrate = metadata.TargetAudioBitrate;
          }
          //else if(info.Audio[0].Bitrate > 0)
          //{
          //  DlnaMetadata.Metadata.Bitrate = info.Audio[0].Bitrate;
          //}
          WebMetadata.Metadata.Duration = info.Metadata.Duration;
          WebMetadata.Metadata.Size = 0;
          if (Client.EstimateTransodedSize == true)
          {
            double audiobitrate = Convert.ToDouble(WebMetadata.Metadata.Bitrate);
            double bitrate = 0;
            if (audiobitrate > 0)
            {
              bitrate = audiobitrate * 1024; //Bitrate in bits/s
            }
            if (bitrate > 0 && WebMetadata.Metadata.Duration > 0)
            {
              WebMetadata.Metadata.Size = Convert.ToInt64((bitrate * WebMetadata.Metadata.Duration) / 8.0);
            }
          }
          AudioStream audioStream = new AudioStream();
          audioStream.Bitrate = metadata.TargetAudioBitrate;
          audioStream.Channels = metadata.TargetAudioChannels;
          audioStream.Codec = metadata.TargetAudioCodec;
          audioStream.Frequency = metadata.TargetAudioFrequency;
          WebMetadata.Audio.Add(audioStream);
        }
        else if (IsVideo)
        {
          VideoTranscoding video = (VideoTranscoding)TranscodingParameter;
          TranscodedVideoMetadata metadata = MediaConverter.GetTranscodedVideoMetadata(video);
          int selectedAudio = 0;
          for (int stream = 0; stream < info.Audio.Count; stream++)
          {
            if (video.SourceAudioStreamIndex == info.Audio[stream].StreamIndex)
            {
              selectedAudio = stream;
              break;
            }
          }

          WebMetadata = new MetadataContainer();
          WebMetadata.Metadata.Mime = info.Metadata.Mime;
          WebMetadata.Metadata.VideoContainerType = metadata.TargetVideoContainer;
          WebMetadata.Metadata.Bitrate = 0;
          if (metadata.TargetAudioBitrate > 0 && metadata.TargetVideoBitrate > 0)
          {
            WebMetadata.Metadata.Bitrate = metadata.TargetAudioBitrate + metadata.TargetVideoBitrate;
          }
          //else if (metadata.TargetAudioBitrate > 0 && info.Video.Bitrate > 0)
          //{
          //  DlnaMetadata.Metadata.Bitrate = metadata.TargetAudioBitrate + info.Video.Bitrate;
          //}
          //else if (info.Audio[selectedAudio].Bitrate > 0 && metadata.TargetVideoBitrate > 0)
          //{
          //  DlnaMetadata.Metadata.Bitrate = info.Audio[selectedAudio].Bitrate + metadata.TargetVideoBitrate;
          //}
          //else if (info.Audio[selectedAudio].Bitrate > 0 && info.Video.Bitrate > 0)
          //{
          //  DlnaMetadata.Metadata.Bitrate = info.Audio[selectedAudio].Bitrate + info.Video.Bitrate;
          //}
          WebMetadata.Metadata.Duration = info.Metadata.Duration;
          WebMetadata.Metadata.Size = 0;
          if (Client.EstimateTransodedSize == true)
          {
            double videobitrate = Convert.ToDouble(WebMetadata.Metadata.Bitrate);
            double bitrate = 0;
            if (videobitrate > 0)
            {
              bitrate = videobitrate * 1024; //Bitrate in bits/s
            }
            if (bitrate > 0 && WebMetadata.Metadata.Duration > 0)
            {
              WebMetadata.Metadata.Size = Convert.ToInt64((bitrate * WebMetadata.Metadata.Duration) / 8.0);
            }
          }

          AudioStream audioStream = new AudioStream();
          audioStream.Bitrate = metadata.TargetAudioBitrate;
          audioStream.Channels = metadata.TargetAudioChannels;
          audioStream.Codec = metadata.TargetAudioCodec;
          audioStream.Frequency = metadata.TargetAudioFrequency;
          WebMetadata.Audio.Add(audioStream);

          WebMetadata.Video.AspectRatio = metadata.TargetVideoAspectRatio;
          WebMetadata.Video.Bitrate = metadata.TargetVideoBitrate;
          WebMetadata.Video.Codec = metadata.TargetVideoCodec;
          WebMetadata.Video.Framerate = metadata.TargetVideoFrameRate;
          WebMetadata.Video.HeaderLevel = metadata.TargetLevel;
          WebMetadata.Video.ProfileType = metadata.TargetProfile;
          WebMetadata.Video.RefLevel = metadata.TargetLevel;
          WebMetadata.Video.Height = metadata.TargetVideoMaxHeight;
          WebMetadata.Video.PixelAspectRatio = metadata.TargetVideoPixelAspectRatio;
          WebMetadata.Video.PixelFormatType = metadata.TargetVideoPixelFormat;
          WebMetadata.Video.TimestampType = metadata.TargetVideoTimestamp;
          WebMetadata.Video.Width = metadata.TargetVideoMaxWidth;
        }
      }

      if (IsImage)
      {
        profileList = ProfileMime.ResolveImageProfile(WebMetadata.Metadata.ImageContainerType, WebMetadata.Image.Width, WebMetadata.Image.Height);
      }
      else if (IsAudio)
      {
        profileList = ProfileMime.ResolveAudioProfile(WebMetadata.Metadata.AudioContainerType, WebMetadata.Audio[0].Codec, WebMetadata.Audio[0].Bitrate, WebMetadata.Audio[0].Frequency, WebMetadata.Audio[0].Channels);
      }
      else if (IsVideo)
      {
        profileList = ProfileMime.ResolveVideoProfile(WebMetadata.Metadata.VideoContainerType, WebMetadata.Video.Codec, WebMetadata.Audio[0].Codec, WebMetadata.Video.ProfileType, WebMetadata.Video.HeaderLevel,
          WebMetadata.Video.Framerate, WebMetadata.Video.Width, WebMetadata.Video.Height, WebMetadata.Video.Bitrate, WebMetadata.Audio[0].Bitrate, WebMetadata.Video.TimestampType);
      }

      string mime = info.Metadata.Mime;
      ProfileMime.FindCompatibleMime(Client, profileList, ref mime);
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
        return MediaConverter.IsTranscodeRunning(_clientId, TranscodingParameter.TranscodeId);
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
        MediaConverter.StopTranscode(_clientId, TranscodingParameter.TranscodeId);
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
    public MetadataContainer WebMetadata { get; private set; }
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
        if (WebMetadata != null && WebMetadata.IsVideo == true)
        {
          if (WebMetadata.Metadata.VideoContainerType == VideoContainer.Unknown)
          {
            return false;
          }
          else if (WebMetadata.Metadata.VideoContainerType == VideoContainer.Mp4)
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
    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
