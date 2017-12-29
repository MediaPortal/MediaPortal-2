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

            string stereoscopic = null;
            if (!string.IsNullOrEmpty(video.StereoMode))
            {
              if (video.StereoMode.StartsWith("H", StringComparison.InvariantCultureIgnoreCase))
              {
                if (video.StereoMode.EndsWith("SBS", StringComparison.InvariantCultureIgnoreCase))
                {
                  stereoscopic = VideoStreamAspect.TYPE_HSBS;
                }
                else if (video.StereoMode.EndsWith("TAB", StringComparison.InvariantCultureIgnoreCase))
                {
                  stereoscopic = VideoStreamAspect.TYPE_HTAB;
                }
                else if (video.StereoMode.EndsWith("OU", StringComparison.InvariantCultureIgnoreCase))
                {
                  stereoscopic = VideoStreamAspect.TYPE_HTAB;
                }
              }
              else
              {
                if (video.StereoMode.EndsWith("SBS", StringComparison.InvariantCultureIgnoreCase))
                {
                  stereoscopic = VideoStreamAspect.TYPE_SBS;
                }
                else if (video.StereoMode.EndsWith("TAB", StringComparison.InvariantCultureIgnoreCase))
                {
                  stereoscopic = VideoStreamAspect.TYPE_TAB;
                }
                else if (video.StereoMode.EndsWith("OU", StringComparison.InvariantCultureIgnoreCase))
                {
                  stereoscopic = VideoStreamAspect.TYPE_TAB;
                }
              }
              if (video.StereoMode.EndsWith("MVC", StringComparison.InvariantCultureIgnoreCase))
              {
                stereoscopic = VideoStreamAspect.TYPE_MVC;
              }
              else if (video.StereoMode.EndsWith("ANAGLYPH", StringComparison.InvariantCultureIgnoreCase))
              {
                stereoscopic = VideoStreamAspect.TYPE_ANAGLYPH;
              }
            }

            int full3DTABMinHeight = 720 * 2;
            int full3DSBSMinWidth = 1280 * 2;
            if (video.Height.HasValue && video.Width.HasValue)
            {
              if (((double)video.Width.Value / (float)video.Height.Value >= 2.5) && (video.Width.Value >= full3DSBSMinWidth)) // we have Full HD SBS 
              {
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_SBS);
                video.Width = video.Width.Value / 2;
                video.Aspect = (decimal)video.Width.Value / (decimal)video.Height.Value;
              }
              else if (((double)video.Width.Value / (float)video.Height.Value <= 1.5) && (video.Height.Value >= full3DTABMinHeight)) // we have Full HD TAB
              {
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_TAB);
                video.Height = video.Height.Value / 2;
                video.Aspect = (decimal)video.Width.Value / (decimal)video.Height.Value;
              }
              else if (stereoscopic == VideoStreamAspect.TYPE_SBS || stereoscopic == VideoStreamAspect.TYPE_HSBS)
              {
                //Cannot be full SBS because of resolution
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HSBS);
                video.Width = video.Width.Value / 2;
                video.Aspect = (decimal)video.Width.Value / (decimal)video.Height.Value;
              }
              else if (stereoscopic == VideoStreamAspect.TYPE_TAB || stereoscopic == VideoStreamAspect.TYPE_HTAB)
              {
                //Cannot be full TAB because of resolution
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HTAB);
                video.Height = video.Height.Value / 2;
                video.Aspect = (decimal)video.Width.Value / (decimal)video.Height.Value;
              }
              else if (stereoscopic != null)
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, stereoscopic);
              else if (video.Height.Value > 2000)
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_UHD);
              else if (video.Height.Value > 700)
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HD);
              else
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_SD);
            }
            else if (video.Height.HasValue)
            {
              if (stereoscopic != null)
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, stereoscopic);
              else if (video.Height.Value > 2000)
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_UHD);
              else if (video.Height.Value > 700)
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HD);
              else
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_SD);
            }
            else
            {
              if (stereoscopic != null)
                videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, stereoscopic);
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
            if (!string.IsNullOrEmpty(stereoscopic))
              suffixes.Add(stereoscopic);
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
