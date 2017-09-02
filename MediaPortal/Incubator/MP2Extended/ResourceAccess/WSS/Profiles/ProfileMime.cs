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
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles
{
  public static class ProfileMime
  {
    public static List<string> ResolveImageProfile(ImageContainer container, int width, int height)
    {
      List<string> valuesProfiles = new List<string>();
      if (container == ImageContainer.Jpeg)
      {
        valuesProfiles.Add("JPEG");
      }
      else if (container == ImageContainer.Png)
      {
        valuesProfiles.Add("PNG");
      }
      else if (container == ImageContainer.Gif)
      {
        valuesProfiles.Add("GIF");
      }
      else if (container == ImageContainer.Raw)
      {
        valuesProfiles.Add("RAW");
      }
      else
      {
        throw new Exception("Image does not match any supported web profile");
      }
      return valuesProfiles;
    }

    public static List<string> ResolveAudioProfile(AudioContainer container, AudioCodec audioCodec, long bitrate, long frequency, int channels)
    {
      List<string> valuesProfiles = new List<string>();
      if (container == AudioContainer.Rtsp)
      {
        valuesProfiles.Add("RTSP");
      }
      else if (container == AudioContainer.Rtp)
      {
        valuesProfiles.Add("RTP");
      }
      else if (container == AudioContainer.Ac3)
      {
        valuesProfiles.Add("AC3");
      }
      else if (container == AudioContainer.Asf)
      {
        valuesProfiles.Add("WMA");
      }
      else if (container == AudioContainer.Mp3)
      {
        valuesProfiles.Add("MP3");
      }
      else if (container == AudioContainer.Mp2)
      {
        valuesProfiles.Add("MP2");
      }
      else if (container == AudioContainer.Lpcm)
      {
        if (frequency != 0 && channels != 0)
        {
          if (frequency == 44100 && channels == 1)
          {
            valuesProfiles.Add("LPCM16_44_MONO");
          }
          else if (frequency == 44100 && channels == 2)
          {
            valuesProfiles.Add("LPCM16_44_STEREO");
          }
          else if (frequency == 48000 && channels == 1)
          {
            valuesProfiles.Add("LPCM16_48_MONO");
          }
          else if (frequency == 48000 && channels == 2)
          {
            valuesProfiles.Add("LPCM16_48_STEREO");
          }
          else
          {
            throw new Exception("Unsupported LPCM format. Only 44.1 / 48 kHz and Mono / Stereo formats are allowed.");
          }
        }
        else
        {
          valuesProfiles.Add("LPCM16_48_STEREO");
        }
      }
      else if (container == AudioContainer.Mp4)
      {
        valuesProfiles.Add("AAC");
      }
      else if (container == AudioContainer.Adts)
      {
        valuesProfiles.Add("AAC");
      }
      else if (container == AudioContainer.Flac)
      {
        valuesProfiles.Add("FLAC");
      }
      else if (container == AudioContainer.Ogg)
      {
        valuesProfiles.Add("OGG");
      }
      else
      {
        throw new Exception("Audio does not match any supported web profile");
      }
      return valuesProfiles;
    }

    public static List<string> ResolveVideoProfile(VideoContainer container, VideoCodec videoCodec, AudioCodec audioCodec, EncodingProfile h264Profile, float h264Level, float fps, int width, int height, long videoBitrate, long audioBitrate, Timestamp timestampType)
    {
      List<string> valuesProfiles = new List<string>();
      if (container == VideoContainer.Rtsp)
      {
        valuesProfiles.Add("RTSP");
      }
      else if (container == VideoContainer.Rtp)
      {
        valuesProfiles.Add("RTP");
      }
      else if (container == VideoContainer.Asf)
      {
        if ((videoCodec == VideoCodec.Wmv && audioCodec == AudioCodec.Unknown) ||
          (videoCodec == VideoCodec.Wmv && audioCodec == AudioCodec.Mp3) ||
          (audioCodec == AudioCodec.Wma || audioCodec == AudioCodec.WmaPro))
        {
          if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Wma)
          {
            valuesProfiles.Add("WMV");
          }
        }
        else if (videoCodec == VideoCodec.Vc1)
        {
          valuesProfiles.Add("VC1_ASF");
        }
        else if (videoCodec == VideoCodec.Mpeg1 || videoCodec == VideoCodec.Mpeg2)
        {
          valuesProfiles.Add("DVR_MS");
        }
        else
        {
          throw new Exception("ASF video file does not match any supported web profile");
        }
      }
      else if (container == VideoContainer.Avi)
      {
        valuesProfiles.Add("AVI");
      }
      else if (container == VideoContainer.Matroska)
      {
        valuesProfiles.Add("MATROSKA");
      }
      else if (container == VideoContainer.Mp4)
      {
        if (videoCodec == VideoCodec.H264)
        {
          valuesProfiles.Add("AVC_MP4");
        }
        else if (videoCodec == VideoCodec.MsMpeg4 || videoCodec == VideoCodec.Mpeg4)
        {
          valuesProfiles.Add("MPEG4_P2_MP4");
        }
        else if (videoCodec == VideoCodec.H263 && audioCodec == AudioCodec.Aac)
        {
          valuesProfiles.Add("MPEG4_H263_MP4");
        }
        else
        {
          throw new Exception("MP4 video file does not match any supported web profile");
        }
      }
      else if (container == VideoContainer.Mpeg2Ps)
      {
        valuesProfiles.Add("MPEG_PS");
      }
      else if (container == VideoContainer.Mpeg1)
      {
        valuesProfiles.Add("MPEG1");
      }
      else if (container == VideoContainer.Mpeg2Ts || container == VideoContainer.M2Ts)
      {
        if (videoCodec == VideoCodec.Mpeg2)
        {
          valuesProfiles.Add("MPEG_TS");
        }
        else if (videoCodec == VideoCodec.H264)
        {
          valuesProfiles.Add("AVC_TS");
        }
        else if (videoCodec == VideoCodec.Vc1)
        {
          valuesProfiles.Add("VC1_TS");
        }
        else if (videoCodec == VideoCodec.MsMpeg4 || videoCodec == VideoCodec.Mpeg4)
        {
          valuesProfiles.Add("MPEG4_P2_TS");
        }
        else
        {
          throw new Exception("MPEG2TS video file does not match any supported web profile");
        }
      }
      else if (container == VideoContainer.Flv)
      {
        valuesProfiles.Add("FLV");
      }
      else if (container == VideoContainer.Wtv)
      {
        valuesProfiles.Add("WTV");
      }
      else if (container == VideoContainer.Gp3)
      {
        if (videoCodec == VideoCodec.H264)
        {
          valuesProfiles.Add("AVC_3GPP");
        }
        else if (videoCodec == VideoCodec.MsMpeg4 || videoCodec == VideoCodec.Mpeg4)
        {
          valuesProfiles.Add("MPEG4_P2_3GPP");
        }
        else if (videoCodec == VideoCodec.H263)
        {
          valuesProfiles.Add("MPEG4_H263_3GPP");
        }
        else
        {
          throw new Exception("3GP video file does not match any supported web profile");
        }
      }
      else if (container == VideoContainer.RealMedia)
      {
        valuesProfiles.Add("REAL_VIDEO");
      }
      else if (container == VideoContainer.Ogg)
      {
        valuesProfiles.Add("OGV");
      }
      else if (container == VideoContainer.Hls)
      {
        if (videoCodec == VideoCodec.H264 && (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Aac || audioCodec == AudioCodec.Mp3))
        {
          valuesProfiles.Add("HLS");
        }
        else
        {
          throw new Exception("HLS video file does not match any supported web profile");
        }
      }
      else
      {
        throw new Exception("Video does not match any supported web profile");
      }
      return valuesProfiles;
    }

    public static bool FindCompatibleMime(EndPointSettings client, List<string> resolvedList, ref string Mime)
    {
      return FindCompatibleMime(client.Profile, resolvedList, ref Mime);
    }

    public static bool FindCompatibleMime(EndPointProfile profile, List<string> resolvedList, ref string Mime)
    {
      foreach (MediaMimeMapping map in profile.MediaMimeMap.Values)
      {
        if (resolvedList.Contains(map.MappedMediaFormat) == true)
        {
          Mime = map.MIME;
          return true;
        }
      }
      foreach (MediaMimeMapping map in profile.MediaMimeMap.Values)
      {
        if (string.IsNullOrEmpty(map.MIMEName) == false)
        {
          List<string> renamedMaps = new List<string>();
          renamedMaps.AddRange(map.MIMEName.Split(','));
          foreach (string submap in renamedMaps)
          {
            if (resolvedList.Contains(submap) == true)
            {
              Mime = map.MIME;
              return true;
            }
          }
        }
      }
      return false;
    }
  }
}
