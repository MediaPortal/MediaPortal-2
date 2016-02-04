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
using MediaPortal.Plugins.Transcoding.Service.Objects;
using MediaPortal.Plugins.Transcoding.Service.Metadata;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.Base
{
  public class PlaylistManifest
  {
    public const string URL_PLACEHOLDER = "[URL]";
    public const string PLAYLIST_FOLDER_SUFFIX = "_mptf";

    internal static byte[] CreatePlaylistManifest(VideoTranscoding video, Subtitle sub)
    {
      TranscodedVideoMetadata metaData = MediaConverter.GetTranscodedVideoMetadata(video);

      double bitrate = 10000000;
      if(metaData.TargetVideoBitrate > 0 && metaData.TargetAudioBitrate > 0)
      {
        bitrate += metaData.TargetVideoBitrate;
        bitrate += metaData.TargetAudioBitrate;
        bitrate = bitrate * 1024; //Bitrate in bits/s
      }

      int width = 1920;
      int height = 1080;
      if(metaData.TargetVideoMaxHeight > 0 && metaData.TargetVideoMaxWidth > 0)
      {
        width = metaData.TargetVideoMaxHeight;
        height = metaData.TargetVideoMaxWidth;
      }

      string codec = "avc1.66.30,mp4a.40.2"; //H264 Baseline 3.0 and AAC
      if(metaData.TargetVideoCodec == VideoCodec.H264)
      {
        codec = "avc1.";
        if(metaData.TargetProfile == EncodingProfile.Baseline)
          codec += "66.";
        else if(metaData.TargetProfile == EncodingProfile.Main)
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
          if(metaData.TargetAudioCodec == AudioCodec.Aac)
            codec += "2";
          else if(metaData.TargetAudioCodec == AudioCodec.Mp3)
            codec += "34";
          else //HE-ACC
            codec += "5";
        }
      }

      StringBuilder manifestBuilder = new StringBuilder();
      manifestBuilder.AppendLine("#EXTM3U");
      manifestBuilder.AppendLine();
      if(sub != null)
      {
        CultureInfo culture = new CultureInfo(sub.Language);
        manifestBuilder.AppendLine(string.Format("#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"{0}\",DEFAULT=YES,AUTOSELECT=YES,FORCED=NO,LANGUAGE=\"{1}\",URI=\"{2}\"",
          culture.DisplayName, culture.TwoLetterISOLanguageName.ToLowerInvariant(), URL_PLACEHOLDER + MediaConverter.PLAYLIST_SUBTITLE_FILE_NAME));
        manifestBuilder.AppendLine();
      }
      manifestBuilder.AppendLine(string.Format("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH={0},RESOLUTION={1},CODECS=\"{2}\"{3}", 
        bitrate.ToString("0"),width + "x" + height,codec, sub != null ? ",SUBTITLES=\"subs\"" : ""));
      manifestBuilder.AppendLine(URL_PLACEHOLDER + MediaConverter.PLAYLIST_FILE_NAME);
      manifestBuilder.AppendLine();

      return Encoding.UTF8.GetBytes(manifestBuilder.ToString());
    }

    internal static byte[] CreateVideoPlaylist(VideoTranscoding video, long startSegment)
    {
      StringBuilder palylistBuilder = new StringBuilder();

      palylistBuilder.AppendLine("#EXTM3U");
      palylistBuilder.AppendLine("#EXT-X-VERSION:3");
      palylistBuilder.AppendLine("#EXT-X-ALLOW-CACHE:NO");
      palylistBuilder.AppendLine("#EXT-X-TARGETDURATION:" + MediaConverter.HLSSegmentTimeInSeconds);
      palylistBuilder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

      double remainingDuration = video.SourceDuration.TotalSeconds;
      remainingDuration -= (Convert.ToDouble(startSegment) * Convert.ToDouble(MediaConverter.HLSSegmentTimeInSeconds));
      while (remainingDuration > 0)
      {
        double segmentTime = remainingDuration >= MediaConverter.HLSSegmentTimeInSeconds ? MediaConverter.HLSSegmentTimeInSeconds : remainingDuration;
        palylistBuilder.AppendLine("#EXTINF:" + segmentTime.ToString("0.000000", CultureInfo.InvariantCulture) + ",");
        palylistBuilder.AppendLine(URL_PLACEHOLDER + startSegment.ToString("00000") + ".ts");

        startSegment++;
        remainingDuration -= MediaConverter.HLSSegmentTimeInSeconds;
      }

      palylistBuilder.AppendLine("#EXT-X-ENDLIST");

      return Encoding.UTF8.GetBytes(palylistBuilder.ToString());
    }

    internal static byte[] CreateSubsPlaylist(VideoTranscoding video, long startSegment)
    {
      StringBuilder palylistBuilder = new StringBuilder();

      palylistBuilder.AppendLine("#EXTM3U");
      palylistBuilder.AppendLine("#EXT-X-VERSION:3");
      palylistBuilder.AppendLine("#EXT-X-ALLOW-CACHE:NO");
      palylistBuilder.AppendLine("#EXT-X-TARGETDURATION:" + MediaConverter.HLSSegmentTimeInSeconds);
      palylistBuilder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
      palylistBuilder.AppendLine();

      double remainingDuration = video.SourceDuration.TotalSeconds;
      remainingDuration -= (Convert.ToDouble(startSegment) * Convert.ToDouble(MediaConverter.HLSSegmentTimeInSeconds));
      while (remainingDuration > 0)
      {
        double segmentTime = remainingDuration >= MediaConverter.HLSSegmentTimeInSeconds ? MediaConverter.HLSSegmentTimeInSeconds : remainingDuration;
        palylistBuilder.AppendLine("#EXTINF:" + segmentTime.ToString("0.000000", CultureInfo.InvariantCulture) + ",");
        palylistBuilder.AppendLine(URL_PLACEHOLDER + "playlist" + startSegment.ToString("0") + ".vtt");
        palylistBuilder.AppendLine();

        startSegment++;
        remainingDuration -= MediaConverter.HLSSegmentTimeInSeconds;
      }

      palylistBuilder.AppendLine("#EXT-X-ENDLIST");

      return Encoding.UTF8.GetBytes(palylistBuilder.ToString());
    }

    internal static byte[] CorrectPlaylistUrls(string baseUrl, string playlist)
    {
      if (baseUrl == null) baseUrl = "";
      StringBuilder urlReplace = new StringBuilder(File.ReadAllText(playlist, Encoding.UTF8), 100 + (30 + baseUrl.Length) * 800);
      //Fix ffmpeg adding 1 second to the target time
      urlReplace.Replace("#EXT-X-TARGETDURATION:" + (MediaConverter.HLSSegmentTimeInSeconds + 1), "#EXT-X-TARGETDURATION:" + MediaConverter.HLSSegmentTimeInSeconds);
      urlReplace.Replace(URL_PLACEHOLDER, baseUrl);
      return Encoding.UTF8.GetBytes(urlReplace.ToString());
    }

    internal static string GetPlaylistFolderFromTranscodeFile(string cachePath, string transcodingFile)
    {
      string folderTranscodeId = Path.GetFileNameWithoutExtension(transcodingFile).Replace(".", "_") + PLAYLIST_FOLDER_SUFFIX;
      return Path.Combine(cachePath, folderTranscodeId);
    }

    internal static void CreatePlaylistFiles(TranscodeData data)
    {
      if (Directory.Exists(data.WorkPath) == false)
      {
        Directory.CreateDirectory(data.WorkPath);
      }
      if (data.SegmentPlaylistData != null)
      {
        string playlist = Path.Combine(data.WorkPath, MediaConverter.PLAYLIST_FILE_NAME);
        string tempPlaylist = playlist + ".tmp";
        File.WriteAllBytes(tempPlaylist, data.SegmentPlaylistData);
        File.Move(tempPlaylist, playlist);
        if (data.SegmentSubsPlaylistData != null)
        {
          playlist = Path.Combine(data.WorkPath, MediaConverter.PLAYLIST_SUBTITLE_FILE_NAME);
          tempPlaylist = playlist + ".tmp";
          File.WriteAllBytes(playlist, data.SegmentSubsPlaylistData);
          File.Move(tempPlaylist, playlist);
        }
      }
      if (data.SegmentPlaylist != null && data.SegmentManifestData != null)
      {
        string tempManifest = data.SegmentPlaylist + ".tmp";
        File.WriteAllBytes(tempManifest, data.SegmentManifestData);
        File.Move(tempManifest, data.SegmentPlaylist);
      }

      //No need to keep data so free used memory
      data.SegmentManifestData = null;
      data.SegmentPlaylistData = null;
      data.SegmentSubsPlaylistData = null;
    }
  }
}
