using MediaInfoLib;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles;
using MediaPortal.Extensions.TranscodingService.Interfaces.Settings;
using MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.TranscodingService
{
  static class Program
  {
    #region File Extensions

    private readonly static List<string> DEFAULT_VIDEO_FILE_EXTENSIONS = new List<string>
      {
          ".mkv",
          ".mk3d",
          ".ogm",
          ".avi",
          ".wmv",
          ".mpg",
          ".mp4",
          ".m4v",
          ".ts",
          ".flv",
          ".m2ts",
          ".mts",
          ".mov",
          ".wtv",
          ".dvr-ms",
          ".divx",
          ".mpeg",
          ".m2p",
          ".qt",
          ".rm"
      };

    private readonly static List<string> DEFAULT_SUBTITLE_FILE_EXTENSIONS = new List<string>
      {
        ".srt",
        ".smi",
        ".ass",
        ".ssa",
        ".sub",
        ".vtt",
        ".idx",
      };

    private readonly static List<string> DEFAULT_AUDIO_FILE_EXTENSIONS = new List<string>
      {
          ".ape",
          ".flac",
          ".mp3",
          ".ogg",
          ".wv",
          ".wav",
          ".wma",
          ".mp4",
          ".m4a",
          ".m4p",
          ".mpc",
          ".mp+",
          ".mpp",
          ".dsf",
          ".dff",
      };

    private readonly static List<string> DEFAULT_IMAGE_FILE_EXTENSIONS = new List<string>
      {
          ".jpg",
          ".jpeg",
          ".png",
          ".bmp",
          ".gif",
          ".tga",
          ".tiff",
          ".tif",
      };

    private static bool HasVideoExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return DEFAULT_VIDEO_FILE_EXTENSIONS.Contains(ext);
    }

    private static bool HasSubtitleExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return DEFAULT_SUBTITLE_FILE_EXTENSIONS.Contains(ext);
    }

    private static bool HasAudioExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return DEFAULT_AUDIO_FILE_EXTENSIONS.Contains(ext);
    }

    private static bool HasImageExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return DEFAULT_IMAGE_FILE_EXTENSIONS.Contains(ext);
    }

    #endregion

    #region Metadata

    private class MediaResult
    {
      private bool _isDVD = false;
      private string _title;

      private int? _streamId;
      private float? _ar;
      private float? _frameRate;
      private int? _width;
      private int? _height;
      private long? _playTime;
      private long? _vidBitRate;
      private int _audioStreamCount;
      private int _subStreamCount;
      private long _fileSize;
      private List<string> _vidCodecs = new List<string>();
      private List<int?> _audStreamIds = new List<int?>();
      private List<string> _audCodecs = new List<string>();
      private List<long?> _audBitRates = new List<long?>();
      private List<int?> _audChannels = new List<int?>();
      private List<long?> _audSampleRates = new List<long?>();
      private List<string> _audioLanguages = new List<string>();
      private List<int?> _subStreamIds = new List<int?>();
      private List<string> _subCodecs = new List<string>();
      private List<bool> _subDefaults = new List<bool>();
      private List<bool> _subForceds = new List<bool>();
      private List<string> _subLanguages = new List<string>();

      public MediaResult(string title)
      {
        _title = title;
      }

      public void AddMediaInfo(MediaInfoWrapper mediaInfo)
      {
        for (int i = 0; i < mediaInfo.GetVideoCount(); i++)
        {
          if (!_streamId.HasValue)
            _streamId = mediaInfo.GetVideoStreamID(i);
          if (!_ar.HasValue)
            _ar = mediaInfo.GetAR(i);
          if (!_frameRate.HasValue)
            _frameRate = mediaInfo.GetFramerate(i);
          if (!_width.HasValue)
            _width = mediaInfo.GetWidth(i);
          if (!_height.HasValue)
            _height = mediaInfo.GetHeight(i);
          if (!_playTime.HasValue)
          {
            long? time = mediaInfo.GetPlaytime(i);
            if (time.HasValue && time > 1000)
              _playTime = time.Value;
          }
          if (!_vidBitRate.HasValue)
            _vidBitRate = mediaInfo.GetVidBitrate(i);
          string vidCodec = mediaInfo.GetVidCodec(i);
          if (!string.IsNullOrEmpty(vidCodec) && !_vidCodecs.Contains(vidCodec))
            _vidCodecs.Add(vidCodec);
        }

        _audioStreamCount = mediaInfo.GetAudioCount();
        for (int i = 0; i < _audioStreamCount; i++)
        {
          int? audSteam = mediaInfo.GetAudioStreamID(i);
          if (_audStreamIds.Count <= i) _audStreamIds.Add(null);
          if (audSteam.HasValue)
          {
            if (_audStreamIds[i] == null)
              _audStreamIds[i] = audSteam.Value;
          }

          long? audBitrate = mediaInfo.GetAudioBitrate(i);
          if (_audBitRates.Count <= i) _audBitRates.Add(null);
          if (audBitrate.HasValue)
          {
            if (_audBitRates[i] == null)
              _audBitRates[i] = audBitrate.Value;
          }

          string audCodec = mediaInfo.GetAudioCodec(i);
          if (_audCodecs.Count <= i) _audCodecs.Add(null);
          if (!string.IsNullOrEmpty(audCodec))
          {
            if (_audCodecs[i] == null)
              _audCodecs[i] = audCodec;
          }

          string audLang = mediaInfo.GetAudioLanguage(i);
          if (_audioLanguages.Count <= i) _audioLanguages.Add(null);
          if (!string.IsNullOrEmpty(audLang))
          {
            if (_audioLanguages[i] == null)
              _audioLanguages[i] = audLang;
          }

          int? audChannels = mediaInfo.GetAudioChannels(i);
          if (_audChannels.Count <= i) _audChannels.Add(null);
          if (audChannels.HasValue)
          {
            if (_audChannels[i] == null)
              _audChannels[i] = audChannels.Value;
          }

          long? audSampleRate = mediaInfo.GetAudioSampleRate(i);
          if (_audSampleRates.Count <= i) _audSampleRates.Add(null);
          if (audSampleRate.HasValue)
          {
            if (_audSampleRates[i] == null)
              _audSampleRates[i] = audSampleRate.Value;
          }
        }

        _subStreamCount = mediaInfo.GetSubtitleCount();
        for (int i = 0; i < _subStreamCount; i++)
        {
          int? subSteam = mediaInfo.GetSubtitleStreamID(i);
          if (_subStreamIds.Count <= i) _subStreamIds.Add(null);
          if (subSteam.HasValue)
          {
            if (_subStreamIds[i] == null)
              _subStreamIds[i] = subSteam.Value;
          }

          string subCodec = mediaInfo.GetSubtitleFormat(i);
          if (_subCodecs.Count <= i) _subCodecs.Add(null);
          if (!string.IsNullOrEmpty(subCodec))
          {
            if (_subCodecs[i] == null)
              _subCodecs[i] = subCodec;
          }

          string subLang = mediaInfo.GetSubtitleLanguage(i);
          if (_subLanguages.Count <= i) _subLanguages.Add(null);
          if (!string.IsNullOrEmpty(subLang))
          {
            if (_subLanguages[i] == null)
              _subLanguages[i] = subLang;
          }

          bool? subDefault = mediaInfo.GetSubtitleDefault(i);
          if (_subDefaults.Count <= i) _subDefaults.Add(false);
          if (subDefault.HasValue)
          {
            if (_subDefaults[i] == false)
              _subDefaults[i] = subDefault.Value;
          }

          bool? subForced = mediaInfo.GetSubtitleForced(i);
          if (_subForceds.Count <= i) _subForceds.Add(false);
          if (subForced.HasValue)
          {
            if (_subForceds[i] == false)
              _subForceds[i] = subForced.Value;
          }
        }
      }

      public void UpdateVideoMetadata(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, ILocalFsResourceAccessor lfsra, int resIdx, int partNum, int partSet)
      {
        //VideoAspect required to mark this media item as a video
        SingleMediaItemAspect videoAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, VideoAspect.Metadata);
        videoAspect.SetAttribute(VideoAspect.ATTR_ISDVD, _isDVD);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, _title);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, lfsra.LastChanged);

        int streamId = 0;
        MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(extractedAspectData, VideoStreamAspect.Metadata);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, resIdx);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, streamId++);

        if (_height.HasValue && _width.HasValue)
        {
          if (_height.Value > 2000)
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_UHD);
          else if (_height.Value > 700)
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HD);
          else
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_SD);
        }
        else if (_height.HasValue)
        {
          if (_height.Value > 2000)
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_UHD);
          else if (_height.Value > 700)
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HD);
          else
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_SD);
        }

        if (_ar.HasValue)
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, _ar.Value);
        if (_frameRate.HasValue)
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_FPS, _frameRate.Value);
        if (_width.HasValue)
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_WIDTH, _width.Value);
        if (_height.HasValue)
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, _height.Value);
        // MediaInfo returns milliseconds, we need seconds
        if (_playTime.HasValue)
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_DURATION, _playTime.Value / 1000);
        if (_vidBitRate.HasValue)
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOBITRATE, _vidBitRate.Value / 1000); // We store kbit/s

        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOENCODING, string.Join(", ", _vidCodecs));
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, _audioStreamCount);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, partNum);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET, partSet);

        for (int i = 0; i < _audioStreamCount; i++)
        {
          MultipleMediaItemAspect audioAspect = MediaItemAspect.CreateAspect(extractedAspectData, VideoAudioStreamAspect.Metadata);
          audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, resIdx);
          audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, streamId++);
          if (_audCodecs[i] != null)
            audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOENCODING, _audCodecs[i]);
          if (_audBitRates[i] != null)
            audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOBITRATE, _audBitRates[i].Value / 1000); // We store kbit/s
          if (_audChannels[i] != null)
            audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, _audChannels[i].Value);
          if (_audSampleRates[i] != null)
            audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE, _audSampleRates[i].Value);
          if (_audioLanguages[i] != null)
            audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE, _audioLanguages[i]);
        }

        for (int i = 0; i < _subStreamCount; i++)
        {
          MultipleMediaItemAspect subtitleAspect = MediaItemAspect.CreateAspect(extractedAspectData, SubtitleAspect.Metadata);
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, 0);
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, resIdx);
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, streamId++);
          if (_subCodecs[i] != null)
          {
            if (_subCodecs[i].Equals("VOBSUB", StringComparison.InvariantCultureIgnoreCase))
              subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, SubtitleAspect.FORMAT_VOBSUB);
            else if (_subCodecs[i].Equals("PGS", StringComparison.InvariantCultureIgnoreCase))
              subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, SubtitleAspect.FORMAT_PGS);
            else if (_subCodecs[i].Equals("ASS", StringComparison.InvariantCultureIgnoreCase))
              subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, SubtitleAspect.FORMAT_ASS);
            else if (_subCodecs[i].Equals("SSA", StringComparison.InvariantCultureIgnoreCase))
              subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, SubtitleAspect.FORMAT_SSA);
            else if (_subCodecs[i].Equals("UTF-8", StringComparison.InvariantCultureIgnoreCase))
              subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, SubtitleAspect.FORMAT_SRT);
            else if (_subCodecs[i].IndexOf("TELETEXT", StringComparison.InvariantCultureIgnoreCase) >= 0)
              subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, SubtitleAspect.FORMAT_TELETEXT);
            else if (_subCodecs[i].IndexOf("DVB", StringComparison.InvariantCultureIgnoreCase) >= 0)
              subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, SubtitleAspect.FORMAT_DVBTEXT);
          }
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_DEFAULT, _subDefaults[i]);
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_FORCED, _subForceds[i]);
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_INTERNAL, true);
          if (_subLanguages[i] != null)
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, _subLanguages[i].ToUpperInvariant());
        }
      }

      public void UpdateAudioMetadata(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, ILocalFsResourceAccessor lfsra)
      {
        SingleMediaItemAspect audioAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, AudioAspect.Metadata);
        audioAspect.SetAttribute(AudioAspect.ATTR_ISCD, _isDVD);
        audioAspect.SetAttribute(AudioAspect.ATTR_TRACK, 1);
        audioAspect.SetAttribute(AudioAspect.ATTR_TRACKNAME, _title);
        if (_audCodecs[0] != null)
          audioAspect.SetAttribute(AudioAspect.ATTR_ENCODING, _audCodecs[0]);
        if (_audBitRates[0] != null)
          audioAspect.SetAttribute(AudioAspect.ATTR_BITRATE, Convert.ToInt32(_audBitRates[0].Value / 1000)); // We store kbit/s
        if (_audChannels[0] != null)
          audioAspect.SetAttribute(AudioAspect.ATTR_CHANNELS, _audChannels[0].Value);
        if (_audSampleRates[0] != null)
          audioAspect.SetAttribute(AudioAspect.ATTR_SAMPLERATE, _audSampleRates[0].Value);
        audioAspect.SetAttribute(AudioAspect.ATTR_DISCID, 1);
        audioAspect.SetAttribute(AudioAspect.ATTR_NUMTRACKS, 1);
        audioAspect.SetAttribute(AudioAspect.ATTR_ALBUM, "Transcode Album");
        audioAspect.SetCollectionAttribute(AudioAspect.ATTR_ALBUMARTISTS, new string[] { "Transcode Artist" });

        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, _title);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, lfsra.LastChanged);
      }
    }

    #endregion

    #region Subtitle Metadata

    private static string GetSubtitleFormat(string subtitleSource)
    {
      if (string.Compare(Path.GetExtension(subtitleSource), ".srt", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_SRT;
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".smi", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_SMI;
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".ass", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_ASS;
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".ssa", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_SSA;
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".sub", true, CultureInfo.InvariantCulture) == 0)
      {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(subtitleSource), Path.GetFileNameWithoutExtension(subtitleSource) + ".idx")) == true)
        {
          //Only the idx file should be imported
          return null;
        }
        else
        {
          string subContent = File.ReadAllText(subtitleSource);
          if (subContent.Contains("[INFORMATION]")) return SubtitleAspect.FORMAT_SUBVIEW;
          else if (subContent.Contains("}{")) return SubtitleAspect.FORMAT_MICRODVD;
        }
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".idx", true, CultureInfo.InvariantCulture) == 0)
      {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(subtitleSource), Path.GetFileNameWithoutExtension(subtitleSource) + ".sub")) == true)
        {
          return SubtitleAspect.FORMAT_VOBSUB;
        }
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".vtt", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_WEBVTT;
      }
      return null;
    }

    private static string GetSubtitleEncoding(string subtitleSource, string subtitleLanguage)
    {
      if (string.IsNullOrEmpty(subtitleSource))
      {
        return null;
      }

      byte[] buffer = File.ReadAllBytes(subtitleSource);

      //Use byte order mark if any
      if (buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0XFE && buffer[3] == 0XFF)
        return "UTF-32";
      else if (buffer[0] == 0XFF && buffer[1] == 0XFE && buffer[2] == 0x00 && buffer[3] == 0x00)
        return "UTF-32";
      else if (buffer[0] == 0XFE && buffer[1] == 0XFF)
        return "UNICODEBIG";
      else if (buffer[0] == 0XFF && buffer[1] == 0XFE)
        return "UNICODELITTLE";
      else if (buffer[0] == 0XEF && buffer[1] == 0XBB && buffer[2] == 0XBF)
        return "UTF-8";
      else if (buffer[0] == 0X2B && buffer[1] == 0X2F && buffer[2] == 0x76)
        return "UTF-7";

      //Detect encoding from language
      if (string.IsNullOrEmpty(subtitleLanguage) == false)
      {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        foreach (CultureInfo culture in cultures)
        {
          if (culture.TwoLetterISOLanguageName.ToUpperInvariant() == subtitleLanguage.ToUpperInvariant())
          {
            return Encoding.GetEncoding(culture.TextInfo.ANSICodePage).BodyName.ToUpperInvariant();
          }
        }
      }

      //Use windows encoding
      return Encoding.Default.BodyName.ToUpperInvariant();
    }

    private static string GetSubtitleLanguage(string subtitleSource, bool imageBased)
    {
      if (string.IsNullOrEmpty(subtitleSource))
      {
        return null;
      }

      CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);

      //Language from file name
      string[] tags = subtitleSource.ToUpperInvariant().Split('.');
      if (tags.Length > 2)
      {
        tags = tags.Where((t, index) => index > 0 && index < tags.Length - 1).ToArray(); //Ignore first element (title) and last element (extension)
        foreach (CultureInfo culture in cultures)
        {
          string languageName = culture.EnglishName;
          if (culture.IsNeutralCulture == false)
          {
            languageName = culture.Parent.EnglishName;
          }
          if (tags.Contains(languageName.ToUpperInvariant()) ||
            tags.Contains(culture.ThreeLetterISOLanguageName.ToUpperInvariant()) ||
            tags.Contains(culture.ThreeLetterWindowsLanguageName.ToUpperInvariant()) ||
            tags.Contains(culture.TwoLetterISOLanguageName.ToUpperInvariant()))
          {
            return culture.TwoLetterISOLanguageName;
          }
        }
      }

      //Language from file encoding
      if (!imageBased)
      {
        string encoding = GetSubtitleEncoding(subtitleSource, null);
        if (encoding != null)
        {
          switch (encoding.ToUpperInvariant())
          {
            case "US-ASCII":
              return "EN";

            case "WINDOWS-1253":
              return "EL";
            case "ISO-8859-7":
              return "EL";

            case "WINDOWS-1254":
              return "TR";

            case "WINDOWS-1255":
              return "HE";
            case "ISO-8859-8":
              return "HE";

            case "WINDOWS-1256":
              return "AR";
            case "ISO-8859-6":
              return "AR";

            case "WINDOWS-1258":
              return "VI";
            case "VISCII":
              return "VI";

            case "WINDOWS-31J":
              return "JA";
            case "EUC-JP":
              return "JA";
            case "Shift_JIS":
              return "JA";
            case "ISO-2022-JP":
              return "JA";

            case "X-MSWIN-936":
              return "ZH";
            case "GB18030":
              return "ZH";
            case "X-EUC-CN":
              return "ZH";
            case "GBK":
              return "ZH";
            case "GB2312":
              return "ZH";
            case "X-WINDOWS-950":
              return "ZH";
            case "X-MS950-HKSCS":
              return "ZH";
            case "X-EUC-TW":
              return "ZH";
            case "BIG5":
              return "ZH";
            case "BIG5-HKSCS":
              return "ZH";

            case "EUC-KR":
              return "KO";
            case "ISO-2022-KR":
              return "KO";

            case "TIS-620":
              return "TH";
            case "ISO-8859-11":
              return "TH";

            case "KOI8-R":
              return "RU";
            case "KOI7":
              return "RU";

            case "KOI8-U":
              return "UK";
          }
        }
      }

      return null;
    }

    private static bool IsImageBasedSubtitle(string subtitleFormat)
    {
      if (subtitleFormat == SubtitleAspect.FORMAT_DVBTEXT)
        return true;
      if (subtitleFormat == SubtitleAspect.FORMAT_VOBSUB)
        return true;
      if (subtitleFormat == SubtitleAspect.FORMAT_PGS)
        return true;

      return false;
    }

    private static string GetSubtitleMime(string subtitleFormat)
    {
      if (subtitleFormat == SubtitleAspect.FORMAT_SRT)
        return "text/srt";
      if (subtitleFormat == SubtitleAspect.FORMAT_MICRODVD)
        return "text/microdvd";
      if (subtitleFormat == SubtitleAspect.FORMAT_SUBVIEW)
        return "text/plain";
      if (subtitleFormat == SubtitleAspect.FORMAT_ASS)
        return "text/x-ass";
      if (subtitleFormat == SubtitleAspect.FORMAT_SSA)
        return "text/x-ssa";
      if (subtitleFormat == SubtitleAspect.FORMAT_SMI)
        return "smi/caption";
      if (subtitleFormat == SubtitleAspect.FORMAT_WEBVTT)
        return "text/vtt";
      if (subtitleFormat == SubtitleAspect.FORMAT_PGS)
        return "image/pgs";
      if (subtitleFormat == SubtitleAspect.FORMAT_VOBSUB)
        return "image/vobsub";
      if (subtitleFormat == SubtitleAspect.FORMAT_DVBTEXT)
        return "image/vnd.dvb.subtitle";

      return null;
    }

    private static void FindExternalSubtitles(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> mediaAspectData, ref int currentResourceIndex)
    {
      int videoResourceIndex = currentResourceIndex;

      string filePath = lfsra.LocalFileSystemPath;
      if (string.IsNullOrEmpty(filePath))
        return;

      List<string> subs = new List<string>();
      string[] subFiles = Directory.GetFiles(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "*.*");
      if (subFiles != null)
        subs.AddRange(subFiles);

      foreach (string subFile in subFiles)
      {
        if (!HasSubtitleExtension(subFile))
          continue;

        LocalFsResourceAccessor fsra = new LocalFsResourceAccessor((LocalFsResourceProvider)lfsra.ParentProvider, LocalFsResourceProviderBase.ToProviderPath(subFile));

        string subFormat = GetSubtitleFormat(subFile);
        if (!string.IsNullOrEmpty(subFormat))
        {
          MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(mediaAspectData, ProviderResourceAspect.Metadata);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, ++currentResourceIndex);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_SECONDARY);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, GetSubtitleMime(subFormat));
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, fsra.Size);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());

          MultipleMediaItemAspect subtitleResourceAspect = MediaItemAspect.CreateAspect(mediaAspectData, SubtitleAspect.Metadata);
          subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, currentResourceIndex);
          subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, videoResourceIndex);
          subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, -1); //External subtitle
          subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, subFormat);
          subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_INTERNAL, false);
          subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_DEFAULT, subFile.ToLowerInvariant().Contains(".default."));
          subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_FORCED, subFile.ToLowerInvariant().Contains(".forced."));

          bool imageBased = IsImageBasedSubtitle(subFormat);
          string language = GetSubtitleLanguage(subFile, imageBased);
          if (language != null) subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, language);
          if (imageBased == false)
          {
            string encoding = GetSubtitleEncoding(subFile, language);
            if (encoding != null) subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_ENCODING, encoding);
          }
          else
          {
            subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_ENCODING, SubtitleAspect.BINARY_ENCODING);
          }
        }
      }
    }

    #endregion

    private static MediaItem GetVideoMediaItem(params string[] mediaFiles)
    {
      MediaItem video = new MediaItem(Guid.NewGuid());
      int idx = -1;
      int primaryCount = 0;
      foreach (var file in mediaFiles)
      {
        if (!HasVideoExtension(file))
          continue;

        primaryCount++;
      }

      int setNo = primaryCount > 1 ? 1 : -1;
      int partNo = primaryCount > 1 ? 1 : -1;
      foreach (var file in mediaFiles)
      {
        var path = LocalFsResourceProviderBase.ToResourcePath(file);
        if (!path.TryCreateLocalResourceAccessor(out IResourceAccessor localFsResource))
          continue;

        var lfsra = (ILocalFsResourceAccessor)localFsResource;
        if (!HasVideoExtension(file))
          continue;

        MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(video.Aspects, ProviderResourceAspect.Metadata);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, ++idx);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, MimeDetector.GetFileMime((ILocalFsResourceAccessor)localFsResource, "video/unknown"));
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, lfsra.Size);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, lfsra.CanonicalLocalResourcePath.Serialize());

        string title = "Transcode Video Test";
        using (MediaInfoWrapper fileInfo = new MediaInfoWrapper())
        {
          fileInfo.Open(lfsra.LocalFileSystemPath);
          if (!fileInfo.IsValid || (fileInfo.GetVideoCount() == 0))
            return null;

          MediaResult result = new MediaResult(title);
          result.AddMediaInfo(fileInfo);
          result.UpdateVideoMetadata(video.Aspects, lfsra, idx, partNo, setNo);
        }

        FindExternalSubtitles(lfsra, video.Aspects, ref idx);

        partNo = primaryCount > 1 ? partNo + 1 : -1;
      }

      return video;
    }

    private static MediaItem GetAudioMediaItem(string mediaFile)
    {
      MediaItem audio = new MediaItem(Guid.NewGuid());
      var path = LocalFsResourceProviderBase.ToResourcePath(mediaFile);
      if (!path.TryCreateLocalResourceAccessor(out IResourceAccessor localFsResource))
        return audio;

      var lfsra = (ILocalFsResourceAccessor)localFsResource;
      if (!HasAudioExtension(mediaFile))
        return audio;

      MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(audio.Aspects, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, MimeDetector.GetFileMime((ILocalFsResourceAccessor)localFsResource, "audio/unknown"));
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, lfsra.Size);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, lfsra.CanonicalLocalResourcePath.Serialize());

      string title = "Transcode Audio Test";
      using (MediaInfoWrapper fileInfo = new MediaInfoWrapper())
      {
        fileInfo.Open(lfsra.LocalFileSystemPath);
        if (!fileInfo.IsValid || (fileInfo.GetAudioCount() == 0))
          return null;

        MediaResult result = new MediaResult(title);
        result.AddMediaInfo(fileInfo);
        result.UpdateAudioMetadata(audio.Aspects, lfsra);
      }

      return audio;
    }

    private static MediaItem GetImageMediaItem(string mediaFile)
    {
      MediaItem image = new MediaItem(Guid.NewGuid());
      var path = LocalFsResourceProviderBase.ToResourcePath(mediaFile);
      if (!path.TryCreateLocalResourceAccessor(out IResourceAccessor localFsResource))
        return image;

      var lfsra = (ILocalFsResourceAccessor)localFsResource;
      if (!HasImageExtension(mediaFile))
        return image;

      MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(image.Aspects, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, MimeDetector.GetFileMime((ILocalFsResourceAccessor)localFsResource, "image/unknown"));
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, lfsra.Size);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, lfsra.CanonicalLocalResourcePath.Serialize());

      using (var img = System.Drawing.Image.FromFile(mediaFile))
      {
        MediaItemAspect imageAspect = MediaItemAspect.GetOrCreateAspect(image.Aspects, ImageAspect.Metadata);
        imageAspect.SetAttribute(ImageAspect.ATTR_WIDTH, img.Width);
        imageAspect.SetAttribute(ImageAspect.ATTR_HEIGHT, img.Height);
        imageAspect.SetAttribute(ImageAspect.ATTR_ORIENTATION, 0);
      }

      string title = "Transcode Image Test";
      MediaItemAspect.SetAttribute(image.Aspects, MediaAspect.ATTR_TITLE, title);
      MediaItemAspect.SetAttribute(image.Aspects, MediaAspect.ATTR_ISVIRTUAL, false);
      MediaItemAspect.SetAttribute(image.Aspects, MediaAspect.ATTR_RECORDINGTIME, lfsra.LastChanged);

      return image;
    }

    private static async Task TranscodeAsync(MediaItem mi, string profileFileName, string profileName, string fileId)
    {
      if (mi == null)
        return;

      string profileSection = "Test";

      FFMpegMediaAnalyzer fFMpegMediaAnalyzer = new FFMpegMediaAnalyzer();
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var settings = settingsManager.Load<TranscodingServiceSettings>();
      TranscodeProfileManager profileManager = new TranscodeProfileManager();
      profileManager.SubtitleFont = settings.SubtitleFont;
      profileManager.SubtitleFontSize = settings.SubtitleFontSize;
      profileManager.SubtitleColor = settings.SubtitleColor;
      profileManager.SubtitleBox = settings.SubtitleBox;
      profileManager.ForceSubtitles = settings.ForceSubtitles;
      await profileManager.LoadTranscodeProfilesAsync(profileSection, profileFileName);

      FFMpegMediaConverter fFMpegMediaConverter = new FFMpegMediaConverter();
      if (mi.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        var containers = await fFMpegMediaAnalyzer.ParseMediaItemAsync(mi, null);
        var transcodeInfo = profileManager.GetVideoTranscoding(profileSection, profileName, containers, new string[] { "EN" }, false, fileId);
        if (transcodeInfo == null)
        {
          Console.WriteLine("No transoding needed!");
          return;
        }
        var transcodedMetadata = fFMpegMediaConverter.GetTranscodedVideoMetadata(transcodeInfo);
        using (var context = await fFMpegMediaConverter.GetMediaStreamAsync("VideoTranscodeTestId", transcodeInfo, 0, 0, true))
        {
          await context.WaitForCompleteAsync();
          if (context.Failed || context.Aborted)
            Console.WriteLine("Tanscoding failed");
          else
            Console.WriteLine("Transcoding complete");
        }
      }
      else if (mi.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        var containers = await fFMpegMediaAnalyzer.ParseMediaItemAsync(mi, null);
        var transcodeInfo = profileManager.GetAudioTranscoding(profileSection, profileName, containers.First(), false, fileId);
        if (transcodeInfo == null)
        {
          Console.WriteLine("No transoding needed!");
          return;
        }
        var transcodedMetadata = fFMpegMediaConverter.GetTranscodedAudioMetadata(transcodeInfo);
        using (var context = await fFMpegMediaConverter.GetMediaStreamAsync("AudioTranscodeTestId", transcodeInfo, 0, 0, true))
        {
          await context.WaitForCompleteAsync();
          if (context.Failed || context.Aborted)
            Console.WriteLine("Tanscoding failed");
          else
            Console.WriteLine("Transcoding complete");
        }
      }
      else if (mi.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
      {
        var containers = await fFMpegMediaAnalyzer.ParseMediaItemAsync(mi, null);
        var transcodeInfo = profileManager.GetImageTranscoding(profileSection, profileName, containers.First(), fileId);
        if (transcodeInfo == null)
        {
          Console.WriteLine("No transoding needed!");
          return;
        }
        var transcodedMetadata = fFMpegMediaConverter.GetTranscodedImageMetadata(transcodeInfo);
        using (var context = await fFMpegMediaConverter.GetMediaStreamAsync("ImageTranscodeTestId", transcodeInfo, 0, 0, true))
        {
          await context.WaitForCompleteAsync();
          if (context.Failed || context.Aborted)
            Console.WriteLine("Tanscoding failed");
          else
            Console.WriteLine("Transcoding complete");
        }
      }
      else
      {
        Console.WriteLine("Unknown media type");
      }
    }

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
      try
      {
        ServiceRegistration.Set<IPathManager>(new PathManager());
        ServiceRegistration.Get<IPathManager>().SetPath("DATA", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"));
        ServiceRegistration.Get<IPathManager>().SetPath("CONFIG", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"));
        ServiceRegistration.Get<IPathManager>().SetPath("LOG", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log"));

        ServiceRegistration.Set<ILocalization>(new NoLocalization());
        ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));
        ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

        ServiceRegistration.Set<IMediaAccessor>(new TestMediaAccessor());
        ServiceRegistration.Set<IImpersonationService>(new TestImpersonationService());
        ServiceRegistration.Set<ISystemResolver>(new TestSystemResolverService());

        if (args.Length >= 4)
        {
          MediaItem mi = null;
          string[] files = args.Skip(3).ToArray();
          if (args[0] == "video" && args.Length >= 4)
          {
            mi = GetVideoMediaItem(files);
          }
          else if (args[0] == "audio" && args.Length == 4)
          {
            mi = GetAudioMediaItem(args[3]);
          }
          else if (args[0] == "image" && args.Length == 4)
          {
            mi = GetImageMediaItem(args[3]);
          }
          else
          {
            Usage();
            Console.ReadKey();
            Environment.Exit(0);
            return;
          }

          string profileFileName = args[1];
          if (!Path.IsPathRooted(profileFileName))
            profileFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"..\..\..\{profileFileName}");

          TranscodeAsync(mi, profileFileName, args[2], Path.GetFileNameWithoutExtension(files.First())).Wait();
        }
        else
        {
          Usage();
        }

        Console.ReadKey();
      }
      catch (Exception e)
      {
        Console.Error.WriteLine("Error running test:\n{0}", e);
        Environment.Exit(1);
      }

      Environment.Exit(0);
    }

    static void Usage()
    {
      Console.WriteLine("Usage: Test.TranscodingService video <transcode profiles file> <profile name> <video file 1> <video file n>");
      Console.WriteLine("Usage: Test.TranscodingService audio <transcode profiles file> <profile name> <audio file>");
      Console.WriteLine("Usage: Test.TranscodingService image <transcode profiles file> <profile name> <image file>");
      Environment.Exit(1);
    }
  }
}
