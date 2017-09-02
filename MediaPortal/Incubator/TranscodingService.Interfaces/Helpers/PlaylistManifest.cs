#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Globalization;
using System.IO;
using System.Text;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;
using MediaPortal.Common;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Helpers
{
  public class PlaylistManifest
  {
    public const string URL_PLACEHOLDER = "[URL]";
    public const string PLAYLIST_MANIFEST_FILE_NAME = "manifest.m3u8";

    public static byte[] CreatePlaylistManifest(VideoTranscoding video, SubtitleStream sub)
    {
      if (ServiceRegistration.IsRegistered<IMediaConverter>())
      {
        IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>();
        TranscodedVideoMetadata metaData = converter.GetTranscodedVideoMetadata(video);

        double bitrate = 10000000;
        if (metaData.TargetVideoBitrate > 0 && metaData.TargetAudioBitrate > 0)
        {
          bitrate += metaData.TargetVideoBitrate;
          bitrate += metaData.TargetAudioBitrate;
          bitrate = bitrate * 1024; //Bitrate in bits/s
        }

        int width = 1920;
        int height = 1080;
        if (metaData.TargetVideoMaxHeight > 0 && metaData.TargetVideoMaxWidth > 0)
        {
          width = metaData.TargetVideoMaxHeight;
          height = metaData.TargetVideoMaxWidth;
        }

        string codec = "avc1.66.30,mp4a.40.2"; //H264 Baseline 3.0 and AAC
        if (metaData.TargetVideoCodec == VideoCodec.H264)
        {
          codec = "avc1.";
          if (metaData.TargetProfile == EncodingProfile.Baseline)
            codec += "66.";
          else if (metaData.TargetProfile == EncodingProfile.Main)
            codec += "77.";
          else //High
            codec += "100.";
          codec += (metaData.TargetLevel * 10).ToString("0");

          if (metaData.TargetAudioCodec == AudioCodec.Ac3)
          {
            codec += ",ac-3";
          }
          else
          {
            codec += ",mp4a.40.";
            if (metaData.TargetAudioCodec == AudioCodec.Aac)
              codec += "2";
            else if (metaData.TargetAudioCodec == AudioCodec.Mp3)
              codec += "34";
            else //HE-ACC
              codec += "5";
          }
        }

        StringBuilder manifestBuilder = new StringBuilder();
        manifestBuilder.AppendLine("#EXTM3U");
        manifestBuilder.AppendLine();
        if (sub != null)
        {
          CultureInfo culture = new CultureInfo(sub.Language);
          manifestBuilder.AppendLine(string.Format("#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"{0}\",DEFAULT=YES,AUTOSELECT=YES,FORCED=NO,LANGUAGE=\"{1}\",URI=\"{2}\"",
            culture.DisplayName, culture.TwoLetterISOLanguageName.ToLowerInvariant(), URL_PLACEHOLDER + converter.HLSSubtitlePlayListName));
          manifestBuilder.AppendLine();
        }
        manifestBuilder.AppendLine(string.Format("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH={0},RESOLUTION={1},CODECS=\"{2}\"{3}",
          bitrate.ToString("0"), width + "x" + height, codec, sub != null ? ",SUBTITLES=\"subs\"" : ""));
        manifestBuilder.AppendLine(URL_PLACEHOLDER + converter.HLSMediaPlayListName);
        manifestBuilder.AppendLine();

        return Encoding.UTF8.GetBytes(manifestBuilder.ToString());
      }
      return null;
    }

    public static byte[] CreateVideoPlaylist(VideoTranscoding video, long startSegment)
    {
      if (ServiceRegistration.IsRegistered<IMediaConverter>())
      {
        IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>();
        StringBuilder palylistBuilder = new StringBuilder();

        palylistBuilder.AppendLine("#EXTM3U");
        palylistBuilder.AppendLine("#EXT-X-VERSION:3");
        palylistBuilder.AppendLine("#EXT-X-ALLOW-CACHE:NO");
        palylistBuilder.AppendLine("#EXT-X-TARGETDURATION:" + converter.HLSSegmentTimeInSeconds);
        palylistBuilder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

        double remainingDuration = video.SourceDuration.TotalSeconds;
        remainingDuration -= (Convert.ToDouble(startSegment) * Convert.ToDouble(converter.HLSSegmentTimeInSeconds));
        while (remainingDuration > 0)
        {
          double segmentTime = remainingDuration >= converter.HLSSegmentTimeInSeconds ? converter.HLSSegmentTimeInSeconds : remainingDuration;
          palylistBuilder.AppendLine("#EXTINF:" + segmentTime.ToString("0.000000", CultureInfo.InvariantCulture) + ",");
          palylistBuilder.AppendLine(URL_PLACEHOLDER + startSegment.ToString("00000") + ".ts");

          startSegment++;
          remainingDuration -= converter.HLSSegmentTimeInSeconds;
        }

        palylistBuilder.AppendLine("#EXT-X-ENDLIST");

        return Encoding.UTF8.GetBytes(palylistBuilder.ToString());
      }
      return null;
    }

    public static byte[] CreateSubsPlaylist(VideoTranscoding video, long startSegment)
    {
      if (ServiceRegistration.IsRegistered<IMediaConverter>())
      {
        IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>();
        StringBuilder palylistBuilder = new StringBuilder();

        palylistBuilder.AppendLine("#EXTM3U");
        palylistBuilder.AppendLine("#EXT-X-VERSION:3");
        palylistBuilder.AppendLine("#EXT-X-ALLOW-CACHE:NO");
        palylistBuilder.AppendLine("#EXT-X-TARGETDURATION:" + converter.HLSSegmentTimeInSeconds);
        palylistBuilder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
        palylistBuilder.AppendLine();

        double remainingDuration = video.SourceDuration.TotalSeconds;
        remainingDuration -= (Convert.ToDouble(startSegment) * Convert.ToDouble(converter.HLSSegmentTimeInSeconds));
        while (remainingDuration > 0)
        {
          double segmentTime = remainingDuration >= converter.HLSSegmentTimeInSeconds ? converter.HLSSegmentTimeInSeconds : remainingDuration;
          palylistBuilder.AppendLine("#EXTINF:" + segmentTime.ToString("0.000000", CultureInfo.InvariantCulture) + ",");
          palylistBuilder.AppendLine(URL_PLACEHOLDER + "playlist" + startSegment.ToString("0") + ".vtt");
          palylistBuilder.AppendLine();

          startSegment++;
          remainingDuration -= converter.HLSSegmentTimeInSeconds;
        }

        palylistBuilder.AppendLine("#EXT-X-ENDLIST");

        return Encoding.UTF8.GetBytes(palylistBuilder.ToString());
      }
      return null;
    }

    public static byte[] CorrectPlaylistUrls(string baseUrl, string playlist)
    {
      if (ServiceRegistration.IsRegistered<IMediaConverter>())
      {
        IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>();

        if (baseUrl == null) baseUrl = "";
        StringBuilder urlReplace = new StringBuilder(File.ReadAllText(playlist, Encoding.UTF8), 100 + (30 + baseUrl.Length) * 800);
        //Fix ffmpeg adding 1 second to the target time
        urlReplace.Replace("#EXT-X-TARGETDURATION:" + (converter.HLSSegmentTimeInSeconds + 1), "#EXT-X-TARGETDURATION:" + converter.HLSSegmentTimeInSeconds);
        urlReplace.Replace(URL_PLACEHOLDER, baseUrl);
        return Encoding.UTF8.GetBytes(urlReplace.ToString());
      }
      return null;
    }
  }
}
