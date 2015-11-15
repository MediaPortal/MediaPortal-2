using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.Base
{
  public class PlaylistManifest
  {
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

        codec += ",mp4a.40.";
        if(metaData.TargetAudioCodec == AudioCodec.Aac)
          codec += "2";
        else if(metaData.TargetAudioCodec == AudioCodec.Mp3)
          codec += "34";
        else //HE-ACC
          codec += "5";
      }

      string baseUrl = video.HlsBaseUrl != null ? video.HlsBaseUrl : "";
      string manifest = "#EXTM3U";
      manifest += "\n";
      manifest += "\n";
      if(sub != null)
      {
        CultureInfo culture = new CultureInfo(sub.Language);
        manifest += string.Format("#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"{0}\",DEFAULT=YES,AUTOSELECT=YES,FORCED=NO,LANGUAGE=\"{1}\",URI=\"{2}\"",
          culture.DisplayName, culture.TwoLetterISOLanguageName.ToLowerInvariant(), baseUrl + MediaConverter.PLAYLIST_SUBTITLE_FILE_NAME);
        manifest += "\n";
        manifest += "\n";
      }
      manifest += string.Format("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH={0},RESOLUTION={1},CODECS=\"{2}\"{3}", 
        bitrate.ToString("0"),width + "x" + height,codec, sub != null ? ",SUBTITLES=\"subs\"" : "");
      manifest += "\n";
      manifest += baseUrl + MediaConverter.PLAYLIST_FILE_NAME;
      manifest += "\n";

      return Encoding.UTF8.GetBytes(manifest);
    }
  }
}
