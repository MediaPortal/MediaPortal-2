using System;
using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.WSS.Profiles;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles.BaseClasses
{
  class BaseTranscoderProfile
  {
    internal WebTranscoderProfile TranscoderProfile(KeyValuePair<string, EndPointProfile> profile)
    {
      int bandwith = 0;
      string mime = "video/MP2T";
      int maxHeight = profile.Value.Settings.Video.MaxHeight;
      int maxWidth = Convert.ToInt32((double)maxHeight * (16.0 / 9.0));
      string transport = "http";
      long audioBitrate = 512;
      long videoBitRate = 10000;
      if (profile.Value.MediaTranscoding.Video.Count > 0)
      {
        if (profile.Value.MediaTranscoding.Video[0].Target.MaxVideoBitrate > 0)
        {
          videoBitRate = profile.Value.MediaTranscoding.Video[0].Target.MaxVideoBitrate;
        }
        if (profile.Value.MediaTranscoding.Video[0].Target.AudioBitrate > 0)
        {
          audioBitrate = profile.Value.MediaTranscoding.Video[0].Target.AudioBitrate;
        }
        if (videoBitRate > 0 && audioBitrate > 0)
        {
          bandwith = Convert.ToInt32(videoBitRate + audioBitrate);
        }
        if (profile.Value.MediaTranscoding.Video[0].Target.MaxVideoHeight > maxHeight)
        {
          maxHeight = profile.Value.MediaTranscoding.Video[0].Target.MaxVideoHeight;
        }
        if (profile.Value.MediaTranscoding.Video[0].Target.AspectRatio > 0)
        {
          maxWidth = Convert.ToInt32((float)maxHeight * profile.Value.MediaTranscoding.Video[0].Target.AspectRatio);
        }
        else
        {
          maxWidth = Convert.ToInt32((double)maxHeight * (16.0 / 9.0));
        }

        if (profile.Value.MediaTranscoding.Video[0].Target.VideoContainerType == Transcoding.Service.VideoContainer.Hls)
        {
          transport = "httplive";
          mime = "application/x-mpegURL";
        }

        //Impossible to guess the right MIME without knowing source. Could be a live stream or direct profile f.ex.
        //List<string> profiles = ProfileMime.ResolveVideoProfile(profile.Value.MediaTranscoding.Video[0].Target.VideoContainerType, profile.Value.MediaTranscoding.Video[0].Target.VideoCodecType,
        //  profile.Value.MediaTranscoding.Video[0].Target.AudioCodecType, profile.Value.MediaTranscoding.Video[0].Target.EncodingProfileType, profile.Value.MediaTranscoding.Video[0].Target.LevelMinimum,
        //   0, maxWidth, maxHeight, profile.Value.MediaTranscoding.Video[0].Target.MaxVideoBitrate, profile.Value.MediaTranscoding.Video[0].Target.AudioBitrate, Transcoding.Service.Timestamp.None);
        //ProfileMime.FindCompatibleMime(profile.Value, profiles, ref mime);
      }

      WebTranscoderProfile webTranscoderProfile = new WebTranscoderProfile
      {
        Bandwidth = bandwith,
        Description = profile.Value.Name,
        HasVideoStream = true,
        MIME = mime,
        MaxOutputHeight = maxHeight,
        MaxOutputWidth = maxWidth,
        Name = profile.Value.Name,
        Targets = profile.Value.Targets,
        Transport = transport
      };

      return webTranscoderProfile;
    }
  }
}
