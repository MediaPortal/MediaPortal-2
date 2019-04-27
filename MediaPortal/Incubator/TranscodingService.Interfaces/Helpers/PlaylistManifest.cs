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
using System.Globalization;
using System.IO;
using System.Text;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using MediaPortal.Common;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Helpers
{
  public class PlaylistManifest
  {
    public const string URL_PLACEHOLDER = "[URL]";
    public const string PLAYLIST_MANIFEST_FILE_NAME = "manifest.m3u8";
    public const string PLAYLIST_FOLDER_SUFFIX = "_mptf";

    public static Task<Stream> CreatePlaylistManifestAsync(VideoTranscoding video, SubtitleStream sub)
    {
      if (ServiceRegistration.IsRegistered<IMediaConverter>())
      {
        IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>();
        TranscodedVideoMetadata metaData = converter.GetTranscodedVideoMetadata(video);

        double bitrate = 10000000;
        if (metaData.TargetVideoBitrate.HasValue && metaData.TargetAudioBitrate.HasValue)
        {
          bitrate += metaData.TargetVideoBitrate.Value;
          bitrate += metaData.TargetAudioBitrate.Value;
          bitrate = bitrate * 1024; //Bitrate in bits/s
        }

        int width = 1920;
        int height = 1080;
        if (metaData.TargetVideoMaxHeight.HasValue && metaData.TargetVideoMaxWidth.HasValue)
        {
          width = metaData.TargetVideoMaxHeight.Value;
          height = metaData.TargetVideoMaxWidth.Value;
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
          codec += ((metaData.TargetLevel ?? 0) * 10).ToString("0");

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
        using (StringWriter writer = new StringWriter(manifestBuilder))
        {
          writer.WriteLine("#EXTM3U");
          writer.WriteLine();
          if (sub != null)
          {
            CultureInfo culture = new CultureInfo(sub.Language);
            writer.WriteLine(string.Format("#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"{0}\",DEFAULT=YES,AUTOSELECT=YES,FORCED=NO,LANGUAGE=\"{1}\",URI=\"{2}\"",
            culture.DisplayName, culture.TwoLetterISOLanguageName.ToLowerInvariant(), URL_PLACEHOLDER + converter.HLSSubtitlePlayListName));
            writer.WriteLine();
          }
          writer.WriteLine(string.Format("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH={0},RESOLUTION={1},CODECS=\"{2}\"{3}",
          bitrate.ToString("0"), width + "x" + height, codec, sub != null ? ",SUBTITLES=\"subs\"" : ""));
          writer.WriteLine(URL_PLACEHOLDER + converter.HLSMediaPlayListName);
          writer.WriteLine();
        }

        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(manifestBuilder.ToString()));
        memStream.Position = 0;
        return Task.FromResult<Stream>(memStream);
      }
      return Task.FromResult<Stream>(null);
    }

    public static Task<Stream> CreateVideoPlaylistAsync(VideoTranscoding video, long startSegment)
    {
      if (ServiceRegistration.IsRegistered<IMediaConverter>())
      {
        IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>();
        StringBuilder palylistBuilder = new StringBuilder();

        using (StringWriter writer = new StringWriter(palylistBuilder))
        {
          writer.WriteLine("#EXTM3U");
          writer.WriteLine("#EXT-X-VERSION:3");
          writer.WriteLine("#EXT-X-ALLOW-CACHE:NO");
          writer.WriteLine("#EXT-X-TARGETDURATION:" + converter.HLSSegmentTimeInSeconds);
          writer.WriteLine("#EXT-X-MEDIA-SEQUENCE:0");

          double remainingDuration = video.SourceMediaTotalDuration.TotalSeconds;
          remainingDuration -= (Convert.ToDouble(startSegment) * Convert.ToDouble(converter.HLSSegmentTimeInSeconds));
          while (remainingDuration > 0)
          {
            double segmentTime = remainingDuration >= converter.HLSSegmentTimeInSeconds ? converter.HLSSegmentTimeInSeconds : remainingDuration;
            writer.WriteLine("#EXTINF:" + segmentTime.ToString("0.000000", CultureInfo.InvariantCulture) + ",");
            writer.WriteLine(URL_PLACEHOLDER + startSegment.ToString("00000") + ".ts");

            startSegment++;
            remainingDuration -= converter.HLSSegmentTimeInSeconds;
          }

          writer.WriteLine("#EXT-X-ENDLIST");
        }

        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(palylistBuilder.ToString()));
        memStream.Position = 0;
        return Task.FromResult<Stream>(memStream);
      }
      return Task.FromResult<Stream>(null);
    }

    public static Task<Stream> CreateSubsPlaylistAsync(VideoTranscoding video, long startSegment)
    {
      if (ServiceRegistration.IsRegistered<IMediaConverter>())
      {
        IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>();
        StringBuilder palylistBuilder = new StringBuilder();

        using (StringWriter writer = new StringWriter(palylistBuilder))
        {
          writer.WriteLine("#EXTM3U");
          writer.WriteLine("#EXT-X-VERSION:3");
          writer.WriteLine("#EXT-X-ALLOW-CACHE:NO");
          writer.WriteLine("#EXT-X-TARGETDURATION:" + converter.HLSSegmentTimeInSeconds);
          writer.WriteLine("#EXT-X-MEDIA-SEQUENCE:0");
          writer.WriteLine();

          double remainingDuration = video.SourceMediaTotalDuration.TotalSeconds;
          remainingDuration -= (Convert.ToDouble(startSegment) * Convert.ToDouble(converter.HLSSegmentTimeInSeconds));
          while (remainingDuration > 0)
          {
            double segmentTime = remainingDuration >= converter.HLSSegmentTimeInSeconds ? converter.HLSSegmentTimeInSeconds : remainingDuration;
            writer.WriteLine("#EXTINF:" + segmentTime.ToString("0.000000", CultureInfo.InvariantCulture) + ",");
            writer.WriteLine(URL_PLACEHOLDER + "playlist" + startSegment.ToString("0") + ".vtt");
            writer.WriteLine();

            startSegment++;
            remainingDuration -= converter.HLSSegmentTimeInSeconds;
          }

          writer.WriteLine("#EXT-X-ENDLIST");
        }

        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(palylistBuilder.ToString()));
        memStream.Position = 0;
        return Task.FromResult<Stream>(memStream);
      }
      return Task.FromResult<Stream>(null);
    }

    public static async Task<Stream> CorrectPlaylistUrlsAsync(string baseUrl, string playlist)
    {
      if (ServiceRegistration.IsRegistered<IMediaConverter>())
      {
        IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>();
        if (baseUrl == null) baseUrl = "";
        StringBuilder palylistBuilder = new StringBuilder();

        using (var streamReader = new StreamReader(playlist, Encoding.UTF8))
        using (var streamWriter = new StringWriter(palylistBuilder))
        {
          string line = await streamReader.ReadLineAsync().ConfigureAwait(false);
          while (line != null)
          {
            //Fix ffmpeg adding 1 second to the target time
            if (line.Contains("#EXT-X-TARGETDURATION:"))
              line = line.Replace("#EXT-X-TARGETDURATION:" + (converter.HLSSegmentTimeInSeconds + 1), "#EXT-X-TARGETDURATION:" + converter.HLSSegmentTimeInSeconds);
            //Replace URL
            if (line.Contains(URL_PLACEHOLDER))
              line = line.Replace(URL_PLACEHOLDER, baseUrl);
            await streamWriter.WriteLineAsync(line).ConfigureAwait(false);
            line = await streamReader.ReadLineAsync().ConfigureAwait(false);
          }
        }

        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(palylistBuilder.ToString()));
        memStream.Position = 0;
        return memStream;
      }
      return null;
    }
  }
}
