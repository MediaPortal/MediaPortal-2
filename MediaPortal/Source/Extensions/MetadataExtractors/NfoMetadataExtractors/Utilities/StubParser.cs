#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Utilities
{
  public class StubParser
  {
    public static void ParseFileInfo(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, HashSet<StreamDetailsStub> FileInfo, string title, decimal? fps = null)
    {
      int streamId = 0;
      if (FileInfo != null && FileInfo.Count > 0)
      {
        if (FileInfo.First().VideoStreams != null && FileInfo.First().VideoStreams.Count > 0)
        {
          foreach (var video in FileInfo.First().VideoStreams)
          {
            MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(extractedAspectData, VideoStreamAspect.Metadata);
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, streamId++);

            string videoType = VideoStreamAspect.GetVideoType(null, video.StereoMode, video.Height, video.Width);
            if (!string.IsNullOrEmpty(videoType))
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, videoType);
            if (videoType == VideoStreamAspect.TYPE_SBS || videoType == VideoStreamAspect.TYPE_HSBS)
            {
              video.Width = video.Width.Value / 2;
              video.Aspect = (decimal)video.Width.Value / (decimal)video.Height.Value;
            }
            else if (videoType == VideoStreamAspect.TYPE_TAB || videoType == VideoStreamAspect.TYPE_HTAB)
            {
              video.Height = video.Height.Value / 2;
              video.Aspect = (decimal)video.Width.Value / (decimal)video.Height.Value;
            }

            if (video.Aspect.HasValue)
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, Convert.ToSingle(video.Aspect.Value));
            if (fps.HasValue)
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_FPS, Convert.ToSingle(fps.Value));
            if (video.Width.HasValue)
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_WIDTH, video.Width.Value);
            if (video.Height.HasValue)
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, video.Height.Value);
            if (video.Duration.HasValue)
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_DURATION, Convert.ToInt64(video.Duration.Value.TotalSeconds));
            if (video.Bitrate.HasValue)
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOBITRATE, video.Bitrate.Value / 1000); // We store kbit/s

            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOENCODING, video.Codec);
            if (FileInfo.First().AudioStreams != null && FileInfo.First().AudioStreams.Count > 0)
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, FileInfo.First().AudioStreams.Count);
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, -1);
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET, -1);

            List<string> suffixes = new List<string>();
            if (!string.IsNullOrEmpty(videoType))
              suffixes.Add(videoType);
            if (video.Height.HasValue && video.Width.HasValue)
              suffixes.Add($"{video.Width.Value}x{video.Height.Value}");
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET_NAME, title + (suffixes.Count > 0 ? " (" + string.Join(", ", suffixes) + ")" : ""));
          }
        }

        if (FileInfo.First().AudioStreams != null && FileInfo.First().AudioStreams.Count > 0)
        {
          foreach (var audio in FileInfo.First().AudioStreams)
          {
            MultipleMediaItemAspect audioAspect = MediaItemAspect.CreateAspect(extractedAspectData, VideoAudioStreamAspect.Metadata);
            audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, 0);
            audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, streamId++);
            if (audio.Codec != null)
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOENCODING, audio.Codec);
            if (audio.Bitrate != null)
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOBITRATE, audio.Bitrate.Value / 1000); // We store kbit/s
            if (audio.Channels != null)
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, audio.Channels.Value);
            if (audio.Language != null)
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE, ParseLanguage(audio.Language));
          }
        }

        if (FileInfo.First().SubtitleStreams != null && FileInfo.First().SubtitleStreams.Count > 0)
        {
          foreach (var subtitle in FileInfo.First().SubtitleStreams)
          {
            MultipleMediaItemAspect subtitleAspect = MediaItemAspect.CreateAspect(extractedAspectData, SubtitleAspect.Metadata);
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, 0);
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, 0);
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, streamId++);
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_INTERNAL, true);
            if (subtitle.Language != null)
              subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, ParseLanguage(subtitle.Language));
          }
        }
      }
    }

    public static string ParseLanguage(string language)
    {
      foreach (CultureInfo cultureInfo in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
      {
        if (cultureInfo.EnglishName == language || cultureInfo.NativeName == language)
          return cultureInfo.TwoLetterISOLanguageName;
      }

      try
      {
        CultureInfo cultureInfo = new CultureInfo(language);
        return cultureInfo.TwoLetterISOLanguageName;
      }
      catch (CultureNotFoundException)
      {
        try
        {
          if (language.Contains("/"))
          {
            language = language.Substring(0, language.IndexOf("/")).Trim();

            CultureInfo cultureInfo = new CultureInfo(language);
            return cultureInfo.TwoLetterISOLanguageName;
          }
          return null;
        }
        catch
        {
          return null;
        }
      }
    }
  }
}
