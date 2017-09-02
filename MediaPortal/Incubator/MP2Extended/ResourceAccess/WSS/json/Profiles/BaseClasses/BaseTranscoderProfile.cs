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
using MediaPortal.Plugins.MP2Extended.WSS.Profiles;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles.BaseClasses
{
  class BaseTranscoderProfile
  {
    internal WebTranscoderProfile TranscoderProfile(KeyValuePair<string, EndPointProfile> profile)
    {
      int bandwith = 0;
      string mime = "video/MP2T";
      int maxHeight = profile.Value.MediaTranscoding.VideoSettings.MaxHeight;
      int maxWidth = Convert.ToInt32((double)maxHeight * (16.0 / 9.0));
      string transport = "http";
      long audioBitrate = 512;
      long videoBitRate = 10000;
      if (profile.Value.MediaTranscoding.VideoTargets.Count > 0)
      {
        if (profile.Value.MediaTranscoding.VideoTargets[0].Target.MaxVideoBitrate > 0)
        {
          videoBitRate = profile.Value.MediaTranscoding.VideoTargets[0].Target.MaxVideoBitrate;
        }
        if (profile.Value.MediaTranscoding.VideoTargets[0].Target.AudioBitrate > 0)
        {
          audioBitrate = profile.Value.MediaTranscoding.VideoTargets[0].Target.AudioBitrate;
        }
        if (videoBitRate > 0 && audioBitrate > 0)
        {
          bandwith = Convert.ToInt32(videoBitRate + audioBitrate);
        }
        if (profile.Value.MediaTranscoding.VideoTargets[0].Target.MaxVideoHeight > maxHeight)
        {
          maxHeight = profile.Value.MediaTranscoding.VideoTargets[0].Target.MaxVideoHeight;
        }
        if (profile.Value.MediaTranscoding.VideoTargets[0].Target.AspectRatio > 0)
        {
          maxWidth = Convert.ToInt32((float)maxHeight * profile.Value.MediaTranscoding.VideoTargets[0].Target.AspectRatio);
        }
        else
        {
          maxWidth = Convert.ToInt32((double)maxHeight * (16.0 / 9.0));
        }

        if (profile.Value.MediaTranscoding.VideoTargets[0].Target.VideoContainerType == VideoContainer.Hls)
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
