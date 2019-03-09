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

using MediaInfoLib;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor.Settings;
using MediaPortal.Utilities;
using MediaPortal.Utilities.SystemAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for video files. Supports several formats.
  /// </summary>
  public class VideoMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the video metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "F2D86BE4-07E6-40F2-9D12-C0076861CAB8";

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// Default mimetype is being used if actual mimetype detection fails.
    /// </summary>
    private const string DEFAULT_MIMETYPE = "video/unknown";

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static ICollection<string> VIDEO_FILE_EXTENSIONS = new HashSet<string>();
    protected static ICollection<string> SUBTITLE_FILE_EXTENSIONS = new HashSet<string>();
    protected static ICollection<string> SUBTITLE_FOLDERS = new HashSet<string>();
    protected static Regex REGEXP_MULTIFILE = null;
    protected static Regex REGEXP_STEREOSCOPICFILE = null;
    protected static Regex REGEXP_SAMPLEFILE = null;
    protected static string GROUP_FILE = "file";
    protected static string GROUP_MEDIA = "media";
    protected static string GROUP_DISC = "disc";
    protected static string GROUP_STEREO = "stereo";
    protected static string GROUP_STEREO_MODE = "mode";
    protected static long MAX_SAMPLE_VIDEO_SIZE = 0;
    protected static readonly IList<Regex> REGEXP_CLEANUPS = new List<Regex>
      {
        // Removing "disc n" from name, this can be used in future to detect multipart titles!
        new Regex(@"(\s|-|_)*(Disc|Disk|CD|DVD|File)\s*\d{1,2}", RegexOptions.IgnoreCase),
        new Regex(@"\s*(Blu-ray|BD|3D|�|�)", RegexOptions.IgnoreCase), 
        // If source is an ISO or ZIP medium, remove the extensions for lookup
        new Regex(@".(iso|zip)$", RegexOptions.IgnoreCase),
        new Regex(@"(\s|-)*$", RegexOptions.IgnoreCase),
        // Common tags regex from MovingPictures
        new Regex(@"(([\(\{\[]|\b)((576|720|1080)[pi]|dvd([r59]|rip|scr)|(avc)?hd|wmv|ntsc|pal|mpeg|dsr|r[1-5]|bd[59]|dts|ac3|blu(-)?ray|[hp]dtv|stv|hddvd|xvid|divx|x264|dxva)([\]\)\}]|\b)(-[^\s]+$)?)", RegexOptions.IgnoreCase),
        // Can be extended
      };

    protected MetadataExtractorMetadata _metadata;
    protected SettingsChangeWatcher<VideoMetadataExtractorSettings> _settingWatcher;

    #endregion

    #region Ctor

    static VideoMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);
      VideoMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the video extensions for which this <see cref="VideoMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(VideoMetadataExtractorSettings settings)
    {
      VIDEO_FILE_EXTENSIONS = new HashSet<string>(settings.VideoFileExtensions.Select(e => e.ToLowerInvariant()));
      SUBTITLE_FILE_EXTENSIONS = new HashSet<string>(settings.SubtitleFileExtensions.Select(e => e.ToLowerInvariant()));
      SUBTITLE_FOLDERS = new HashSet<string>(settings.SubtitleFolders);
      REGEXP_MULTIFILE = settings.MultiPartVideoRegex.Regex;
      REGEXP_STEREOSCOPICFILE = settings.StereoVideoRegex.Regex;
      MAX_SAMPLE_VIDEO_SIZE = settings.MaxSampleSize * 1024 * 1024; //Convert to bytes
      REGEXP_SAMPLEFILE = settings.SampleVideoRegex.Regex;
    }

    public VideoMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Video metadata extractor", MetadataExtractorPriority.Core, true,
          MEDIA_CATEGORIES, new MediaItemAspectMetadata[]
              {
                MediaAspect.Metadata,
                VideoStreamAspect.Metadata,
                VideoAudioStreamAspect.Metadata,
                SubtitleAspect.Metadata,
                ThumbnailLargeAspect.Metadata
              });

      _settingWatcher = new SettingsChangeWatcher<VideoMetadataExtractorSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;

      LoadSettings();
    }

    #endregion

    #region Settings

    public static bool CacheOfflineFanArt { get; private set; }
    public static bool CacheLocalFanArt { get; private set; }

    private void LoadSettings()
    {
      CacheOfflineFanArt = _settingWatcher.Settings.CacheOfflineFanArt;
      CacheLocalFanArt = _settingWatcher.Settings.CacheLocalFanArt;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Returns the information if the specified file name (or path) has a file extension which is
    /// supposed to be supported by this metadata extractor.
    /// </summary>
    /// <param name="fileName">Relative or absolute file path to check.</param>
    /// <returns><c>true</c>, if the file's extension is supposed to be supported, else <c>false</c>.</returns>
    protected static bool HasVideoExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return VIDEO_FILE_EXTENSIONS.Contains(ext);
    }

    protected static bool HasSubtitleExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return SUBTITLE_FILE_EXTENSIONS.Contains(ext);
    }

    protected MediaInfoWrapper ReadMediaInfo(IFileSystemResourceAccessor mediaItemAccessor)
    {
      MediaInfoWrapper result = new MediaInfoWrapper();

      ILocalFsResourceAccessor localFsResourceAccessor = mediaItemAccessor as ILocalFsResourceAccessor;
      if (ReferenceEquals(localFsResourceAccessor, null))
      {
        Stream stream = null;
        try
        {
          stream = mediaItemAccessor.OpenRead();
          if (stream != null)
            result.Open(stream);
        }
        finally
        {
          if (stream != null)
            stream.Close();
        }
      }
      else
      {
        using (localFsResourceAccessor.EnsureLocalFileSystemAccess())
          result.Open(localFsResourceAccessor.LocalFileSystemPath);
      }

      return result;
    }

    public class VideoResult
    {
      protected bool _isDVD;
      protected string _title;
      protected string _mimeType;
      protected DateTime? _mediaDate;

      protected int? _streamId;
      protected float? _ar;
      protected float? _frameRate;
      protected int? _width;
      protected int? _height;
      protected long? _playTime;
      protected long? _vidBitRate;
      protected int _audioStreamCount;
      protected int _subStreamCount;
      protected long _fileSize;
      protected List<string> _vidCodecs = new List<string>();
      protected List<int?> _audStreamIds = new List<int?>();
      protected List<string> _audCodecs = new List<string>();
      protected List<long?> _audBitRates = new List<long?>();
      protected List<int?> _audChannels = new List<int?>();
      protected List<long?> _audSampleRates = new List<long?>();
      protected List<string> _audioLanguages = new List<string>();
      protected List<int?> _subStreamIds = new List<int?>();
      protected List<string> _subCodecs = new List<string>();
      protected List<bool> _subDefaults = new List<bool>();
      protected List<bool> _subForceds = new List<bool>();
      protected List<string> _subLanguages = new List<string>();

      public VideoResult(string videoTitle, MediaInfoWrapper mainInfo)
      {
        _title = videoTitle;
        AddMediaInfo(mainInfo);
      }

      public static VideoResult CreateDVDInfo(string dvdTitle, MediaInfoWrapper videoTsInfo)
      {
        VideoResult result = new VideoResult(dvdTitle, videoTsInfo) { IsDVD = true, MimeType = "video/dvd" };
        return result;
      }

      public static VideoResult CreateFileInfo(string fileName, MediaInfoWrapper fileInfo)
      {
        return new VideoResult(CleanupTitle(fileName), fileInfo);
      }

      protected static string CleanupTitle(string title)
      {
        foreach (Regex regex in REGEXP_CLEANUPS)
          title = regex.Replace(title, "");
        while (title.Contains(".."))
          title = title.Replace("..", "."); //Remove leftover periods
        return BaseInfo.CleanupWhiteSpaces(title);
      }

      public void AddMediaInfo(MediaInfoWrapper mediaInfo)
      {
        // This method will be called at least one time, for video DVDs it will be called multiple times for the different
        // .ifo files. The first time this method is called, the given media info instance is the "major" instance, i.e.
        // in case of a video DVD, it is the video_ts.ifo file.
        // We will collect most of our interesting attributes by taking the first one which is available. All others will then be
        // ignored. Only for some attributes, all values will be collected.
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

      public void UpdateMetadata(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, ILocalFsResourceAccessor lfsra, int partNum, int partSet, bool refresh, bool reimport)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);

        if (!refresh)
        {
          //VideoAspect required to mark this media item as a video
          SingleMediaItemAspect videoAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, VideoAspect.Metadata);
          videoAspect.SetAttribute(VideoAspect.ATTR_ISDVD, IsDVD);

          if (!reimport)
          {
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, _title);
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, lfsra.LastChanged);
          }

          MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, _mimeType);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, lfsra.Size);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, lfsra.CanonicalLocalResourcePath.Serialize());

          int streamId = 0;
          MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(extractedAspectData, VideoStreamAspect.Metadata);
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, streamId++);

          Match match = REGEXP_STEREOSCOPICFILE.Match(lfsra.LocalFileSystemPath);
          string videoType = VideoStreamAspect.GetVideoType(match.Groups[GROUP_STEREO_MODE].Value, match.Groups[GROUP_STEREO].Value, _height, _width);
          if (!string.IsNullOrWhiteSpace(videoType))
            videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, videoType);
          if (videoType == VideoStreamAspect.TYPE_SBS || videoType == VideoStreamAspect.TYPE_HSBS)
          {
            _width = _width.Value / 2;
            _ar = (float)_width.Value / (float)_height.Value;
          }
          else if (videoType == VideoStreamAspect.TYPE_TAB || videoType == VideoStreamAspect.TYPE_HTAB)
          {
            _height = _height.Value / 2;
            _ar = (float)_width.Value / (float)_height.Value;
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

          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOENCODING, StringUtils.Join(", ", _vidCodecs));
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, _audioStreamCount);
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, partNum);
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET, partSet);

          for (int i = 0; i < _audioStreamCount; i++)
          {
            MultipleMediaItemAspect audioAspect = MediaItemAspect.CreateAspect(extractedAspectData, VideoAudioStreamAspect.Metadata);
            audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, 0);
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
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, 0);
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, streamId++);
            if (_subCodecs[i] != null)
            {
              string subType = SubtitleAspect.GetSubtitleType(_subCodecs[i]);
              if (!string.IsNullOrWhiteSpace(subType))
                subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, subType);
            }
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_DEFAULT, _subDefaults[i]);
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_FORCED, _subForceds[i]);
            subtitleAspect.SetAttribute(SubtitleAspect.ATTR_INTERNAL, true);
            if (_subLanguages[i] != null)
              subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, _subLanguages[i].ToUpperInvariant());
          }
        }
        else
        {
          int providerIndex = -1;
          IList<MultipleMediaItemAspect> providerResourceAspects = new List<MultipleMediaItemAspect>();
          if (MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out providerResourceAspects))
          {
            for (int idx = 0; idx < providerResourceAspects.Count; idx++)
            {
              if (providerResourceAspects[idx].GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH) == lfsra.CanonicalLocalResourcePath.Serialize())
              {
                providerIndex = idx;
                providerResourceAspects[idx].SetAttribute(ProviderResourceAspect.ATTR_SIZE, lfsra.Size);
                break;
              }
            }
          }

          if (providerIndex >= 0)
          {
            IList<MultipleMediaItemAspect> videoStreamAspects = new List<MultipleMediaItemAspect>();
            if (MediaItemAspect.TryGetAspects(extractedAspectData, VideoStreamAspect.Metadata, out videoStreamAspects))
            {
              for (int idx = 0; idx < providerResourceAspects.Count; idx++)
              {
                if (videoStreamAspects[idx].GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX) == providerResourceAspects[providerIndex].GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX))
                {
                  if (_ar.HasValue)
                    videoStreamAspects[idx].SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, _ar.Value);
                  if (_frameRate.HasValue)
                    videoStreamAspects[idx].SetAttribute(VideoStreamAspect.ATTR_FPS, _frameRate.Value);
                  if (_width.HasValue)
                    videoStreamAspects[idx].SetAttribute(VideoStreamAspect.ATTR_WIDTH, _width.Value);
                  if (_height.HasValue)
                    videoStreamAspects[idx].SetAttribute(VideoStreamAspect.ATTR_HEIGHT, _height.Value);
                  // MediaInfo returns milliseconds, we need seconds
                  if (_playTime.HasValue)
                    videoStreamAspects[idx].SetAttribute(VideoStreamAspect.ATTR_DURATION, _playTime.Value / 1000);
                  if (_vidBitRate.HasValue)
                    videoStreamAspects[idx].SetAttribute(VideoStreamAspect.ATTR_VIDEOBITRATE, _vidBitRate.Value / 1000); // We store kbit/s
                  break;
                }
              }
            }
          }
        }
      }

      public bool IsDVD
      {
        get { return _isDVD; }
        set { _isDVD = value; }
      }

      public string MimeType
      {
        get { return _mimeType; }
        set { _mimeType = value; }
      }
    }

    protected async Task ExtractMatroskaTagsAsync(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      try
      {
        // Calling EnsureLocalFileSystemAccess not necessary; only string operation
        string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(lfsra.LocalFileSystemPath)).ToLower();
        if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
          return;

        Stopwatch sw = new Stopwatch();
        sw.Start();

        // Try to get extended information out of matroska files)
        MatroskaBinaryReader mkvReader = new MatroskaBinaryReader(lfsra);
        // Add keys to be extracted to tags dictionary, matching results will returned as value
        Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultVideoTags;
        await mkvReader.ReadTagsAsync(tagsToExtract).ConfigureAwait(false);
        bool assignedValue = false;

        // Read title
        string title = string.Empty;
        IList<string> tags = tagsToExtract[MatroskaConsts.TAG_SIMPLE_TITLE];
        if (tags != null)
          title = tags.FirstOrDefault();
        if (!string.IsNullOrEmpty(title))
        {
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);
          assignedValue = true;
        }

        // Read release date
        int year;
        string yearCandidate = null;
        tags = tagsToExtract[MatroskaConsts.TAG_EPISODE_YEAR] ?? tagsToExtract[MatroskaConsts.TAG_SEASON_YEAR];
        if (tags != null)
          yearCandidate = (tags.FirstOrDefault() ?? string.Empty).Substring(0, 4);

        if (int.TryParse(yearCandidate, out year))
        {
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 1, 1));
          assignedValue = true;
        }

        IList<MultipleMediaItemAspect> videoStreamAspects;
        if (MediaItemAspect.TryGetAspects(extractedAspectData, VideoStreamAspect.Metadata, out videoStreamAspects))
        {
          string stereoType = videoStreamAspects[0].GetAttributeValue<string>(VideoStreamAspect.ATTR_VIDEO_TYPE);
          if (string.IsNullOrEmpty(stereoType) || stereoType == VideoStreamAspect.TYPE_SD ||
            stereoType == VideoStreamAspect.TYPE_HD || stereoType == VideoStreamAspect.TYPE_UHD)
          {
            int? height = videoStreamAspects[0].GetAttributeValue<int?>(VideoStreamAspect.ATTR_HEIGHT);
            int? width = videoStreamAspects[0].GetAttributeValue<int?>(VideoStreamAspect.ATTR_WIDTH);

            MatroskaConsts.StereoMode mode = await mkvReader.ReadStereoModeAsync().ConfigureAwait(false);
            if (mode == MatroskaConsts.StereoMode.AnaglyphCyanRed || mode == MatroskaConsts.StereoMode.AnaglyphGreenMagenta)
            {
              videoStreamAspects[0].SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_ANAGLYPH);
              assignedValue = true;
            }
            else if (mode == MatroskaConsts.StereoMode.SBSLeftEyeFirst || mode == MatroskaConsts.StereoMode.SBSRightEyeFirst)
            {
              //If it was not detected as SBS by resolution it's probably Half SBS
              videoStreamAspects[0].SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HSBS);
              if (width.HasValue && height.HasValue)
              {
                width = width.Value / 2;
                float ar = (float)width.Value / (float)height.Value;
                videoStreamAspects[0].SetAttribute(VideoStreamAspect.ATTR_WIDTH, width.Value);
                videoStreamAspects[0].SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, ar);
                assignedValue = true;
              }
            }
            else if (mode == MatroskaConsts.StereoMode.TABLeftEyeFirst || mode == MatroskaConsts.StereoMode.TABRightEyeFirst)
            {
              //If it was not detected as TAB by resolution it's probably Half TAB
              videoStreamAspects[0].SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HTAB);
              if (width.HasValue && height.HasValue)
              {
                height = height.Value / 2;
                float ar = (float)width.Value / (float)height.Value;
                videoStreamAspects[0].SetAttribute(VideoStreamAspect.ATTR_HEIGHT, height.Value);
                videoStreamAspects[0].SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, ar);
                assignedValue = true;
              }
            }
            else if (mode == MatroskaConsts.StereoMode.FieldSequentialModeLeftEyeFirst || mode == MatroskaConsts.StereoMode.FieldSequentialModeRightEyeFirst)
            {
              videoStreamAspects[0].SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_MVC);
              assignedValue = true;
            }
          }
        }

        sw.Stop();
        ServiceRegistration.Get<ILogger>().Debug("VideoMetadataExtractor: Completed reading {1}matroska tags from resource '{0}' (Time: {2} ms)", lfsra.CanonicalLocalResourcePath, assignedValue ? "and assigning " : "", sw.ElapsedMilliseconds);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("VideoMetadataExtractor: Exception reading matroska tags from resource '{0}' (Text: '{1}')", e, lfsra.CanonicalLocalResourcePath, e.Message);
      }
    }

    protected void ExtractMp4Tags(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      try
      {
        // Calling EnsureLocalFileSystemAccess not necessary; only string operation
        string extensionUpper = StringUtils.TrimToEmpty(Path.GetExtension(lfsra.LocalFileSystemPath)).ToUpper();
        bool assignedValue = false;

        // Try to get extended information out of MP4 files)
        if (extensionUpper != ".MP4" && extensionUpper != ".M4V") return;

        Stopwatch sw = new Stopwatch();
        sw.Start();

        using (lfsra.EnsureLocalFileSystemAccess())
        {
          TagLib.File mp4File = TagLib.File.Create(lfsra.LocalFileSystemPath);
          if (ReferenceEquals(mp4File, null) || ReferenceEquals(mp4File.Tag, null))
            return;

          TagLib.Tag tag = mp4File.Tag;

          string title = tag.Title;
          if (!string.IsNullOrEmpty(title))
          {
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);
            assignedValue = true;
          }
          title = tag.TitleSort;
          if (!string.IsNullOrEmpty(title))
          {
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, title);
            assignedValue = true;
          }

          int year = (int)tag.Year;
          if (year != 0)
          {
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 1, 1));
            assignedValue = true;
          }

          sw.Stop();
          ServiceRegistration.Get<ILogger>().Debug("VideoMetadataExtractor: Completed reading {1}mp4 tags from resource '{0}' (Time: {2} ms)", lfsra.CanonicalLocalResourcePath, assignedValue ? "and assigning " : "", sw.ElapsedMilliseconds);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("VideoMetadataExtractor: Exception reading mp4 tags from resource '{0}' (Text: '{1}')", e, lfsra.CanonicalLocalResourcePath, e.Message);
      }
    }

    protected string GetSubtitleFormat(string subtitleSource)
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

    protected string GetSubtitleEncoding(string subtitleSource, string subtitleLanguage)
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

      //Detect encoding from file
      Ude.CharsetDetector cdet = new Ude.CharsetDetector();
      cdet.Feed(buffer, 0, buffer.Length);
      cdet.DataEnd();
      if (cdet.Charset != null && cdet.Confidence >= 0.1)
      {
        return Encoding.GetEncoding(cdet.Charset).BodyName.ToUpperInvariant();
      }

      //Use windows encoding
      return Encoding.Default.BodyName.ToUpperInvariant();
    }

    protected string GetSubtitleLanguage(string subtitleSource, bool imageBased)
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

    protected bool IsImageBasedSubtitle(string subtitleFormat)
    {
      if (subtitleFormat == SubtitleAspect.FORMAT_DVBTEXT)
        return true;
      if (subtitleFormat == SubtitleAspect.FORMAT_VOBSUB)
        return true;
      if (subtitleFormat == SubtitleAspect.FORMAT_PGS)
        return true;

      return false;
    }

    protected string GetSubtitleMime(string subtitleFormat)
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

    protected void FindExternalSubtitles(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      try
      {
        IList<MultipleMediaItemAspect> providerResourceAspects;
        if (!MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out providerResourceAspects))
          return;

        int newResourceIndex = -1;
        foreach (MultipleMediaItemAspect providerResourceAspect in providerResourceAspects)
        {
          int resouceIndex = providerResourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
          if (newResourceIndex < resouceIndex)
          {
            newResourceIndex = resouceIndex;
          }
        }
        newResourceIndex++;

        using (lfsra.EnsureLocalFileSystemAccess())
        {
          foreach (MultipleMediaItemAspect mmia in providerResourceAspects)
          {
            string accessorPath = (string)mmia.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
            ResourcePath resourcePath = ResourcePath.Deserialize(accessorPath);

            if (!HasVideoExtension(resourcePath.FileName))
              continue;

            string filePath = LocalFsResourceProviderBase.ToDosPath(resourcePath);
            if (string.IsNullOrEmpty(filePath))
              continue;

            List<string> subs = new List<string>();
            int videoResouceIndex = (int)mmia.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
            string[] subFiles = Directory.GetFiles(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "*.*");
            if (subFiles != null)
              subs.AddRange(subFiles);
            foreach (string folder in SUBTITLE_FOLDERS)
            {
              if (string.IsNullOrEmpty(Path.GetPathRoot(folder)) && Directory.Exists(Path.Combine(Path.GetDirectoryName(filePath), folder))) //Is relative path
                subFiles = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(filePath), folder), Path.GetFileNameWithoutExtension(filePath) + "*.*");
              else if (Directory.Exists(folder)) //Is absolute path
                subFiles = Directory.GetFiles(folder, Path.GetFileNameWithoutExtension(filePath) + "*.*");

              if (subFiles != null)
                subs.AddRange(subFiles);
            }
            foreach (string subFile in subFiles)
            {
              if (!HasSubtitleExtension(subFile))
                continue;

              LocalFsResourceAccessor fsra = new LocalFsResourceAccessor((LocalFsResourceProvider)lfsra.ParentProvider, LocalFsResourceProviderBase.ToProviderPath(subFile));

              //Check if already exists
              bool exists = false;
              foreach (MultipleMediaItemAspect providerResourceAspect in providerResourceAspects)
              {
                string subAccessorPath = (string)providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                ResourcePath subResourcePath = ResourcePath.Deserialize(subAccessorPath);
                if (subResourcePath.Equals(fsra.CanonicalLocalResourcePath))
                {
                  //Already exists
                  exists = true;
                  break;
                }
              }
              if (exists)
                continue;

              string subFormat = GetSubtitleFormat(subFile);
              if (!string.IsNullOrEmpty(subFormat))
              {
                MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, newResourceIndex);
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_SECONDARY);
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, GetSubtitleMime(subFormat));
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, fsra.Size);
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());

                MultipleMediaItemAspect subtitleResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, SubtitleAspect.Metadata);
                subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, newResourceIndex);
                subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, videoResouceIndex);
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
                newResourceIndex++;
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("VideoMetadataExtractor: Exception finding external subtitles for resource '{0}' (Text: '{1}')", e, lfsra.CanonicalLocalResourcePath, e.Message);
      }
    }

    protected bool IsSampleFile(IFileSystemResourceAccessor fsra)
    {
      try
      {
        bool match = (fsra.Size < MAX_SAMPLE_VIDEO_SIZE);
        if (match)
        {
          match = REGEXP_SAMPLEFILE.Match(fsra.ResourcePathName).Success;
        }
        return match;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("VideoMetadataExtractor: Exception checking if sample for resource '{0}' (Text: '{1}')", e, fsra.CanonicalLocalResourcePath, e.Message);
        return false;
      }
    }

    protected void UpdateSetName(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, int partNum)
    {
      IList<MultipleMediaItemAspect> videoStreamAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspectData, VideoStreamAspect.Metadata, out videoStreamAspects))
      {
        string title = null;
        if (!MediaItemAspect.TryGetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, out title))
          title = ProviderPathHelper.GetFileNameWithoutExtension(lfsra.ResourceName);

        string stereoType = videoStreamAspects[0].GetAttributeValue<string>(VideoStreamAspect.ATTR_VIDEO_TYPE);
        int? height = videoStreamAspects[0].GetAttributeValue<int?>(VideoStreamAspect.ATTR_HEIGHT);
        int? width = videoStreamAspects[0].GetAttributeValue<int?>(VideoStreamAspect.ATTR_WIDTH);

        List<string> suffixes = new List<string>();
        if (partNum > 0)
          suffixes.Add("#");
        if (!string.IsNullOrEmpty(stereoType))
          suffixes.Add(stereoType);
        if (height.HasValue && width.HasValue)
          suffixes.Add($"{width.Value}x{height.Value}");

        videoStreamAspects[0].SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET_NAME, title + (suffixes.Count > 0 ? " (" + string.Join(", ", suffixes) + ")" : ""));
      }
    }

    /// <summary>
    /// Helper method that contains overrides for video detection for certain formats, because video count information is not always correct.
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns><c>true</c> if this file should be treated as video.</returns>
    protected bool IsWorkaroundRequired(string filePath)
    {
      return filePath.ToLowerInvariant().EndsWith(".wtv");
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public async Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra == null)
          return false;

        bool isReimport = extractedAspectData.ContainsKey(ReimportAspect.ASPECT_ID);
        VideoResult result = null;
        if (!fsra.IsFile && fsra.ResourceExists("VIDEO_TS"))
        {
          IFileSystemResourceAccessor fsraVideoTs = fsra.GetResource("VIDEO_TS");
          if (fsraVideoTs != null && fsraVideoTs.ResourceExists("VIDEO_TS.IFO"))
          {
            int multipart = -1;
            int multipartSet = -1;
            Match match = REGEXP_MULTIFILE.Match(fsra.ResourcePathName);
            if (match.Groups[GROUP_DISC].Length > 0)
            {
              int.TryParse(match.Groups[GROUP_DISC].Value, out multipart);
            }

            // Video DVD
            using (MediaInfoWrapper videoTsInfo = ReadMediaInfo(fsraVideoTs.GetResource("VIDEO_TS.IFO")))
            {
              if (!videoTsInfo.IsValid || videoTsInfo.GetVideoCount() == 0)
                return false; // Invalid video_ts.ifo file
              result = VideoResult.CreateDVDInfo(fsra.ResourceName, videoTsInfo);
            }
            // Iterate over all video files; MediaInfo finds different audio/video metadata for each .ifo file
            ICollection<IFileSystemResourceAccessor> files = fsraVideoTs.GetFiles();
            if (files != null)
              foreach (IFileSystemResourceAccessor file in files)
              {
                string lowerPath = (file.ResourcePathName ?? string.Empty).ToLowerInvariant();
                if (!lowerPath.EndsWith(".ifo") || lowerPath.EndsWith("video_ts.ifo"))
                  continue;
                using (MediaInfoWrapper mediaInfo = ReadMediaInfo(file))
                {
                  // Before we start evaluating the file, check if it is a video at all
                  if (mediaInfo.IsValid && mediaInfo.GetVideoCount() == 0)
                    continue;
                  result.AddMediaInfo(mediaInfo);
                }
              }

            if (result != null)
            {
              using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
              {
                ILocalFsResourceAccessor lfsra = rah.LocalFsResourceAccessor;
                if (lfsra != null)
                {
                  result.UpdateMetadata(extractedAspectData, lfsra, multipart, multipartSet, false, isReimport);
                  UpdateSetName(lfsra, extractedAspectData, multipart);
                }
                return true;
              }
            }
          }
        }
        else if (fsra.IsFile)
        {
          string filePath = fsra.ResourcePathName;
          string mediaTitle = DosPathHelper.GetFileNameWithoutExtension(fsra.ResourceName);
          if (HasVideoExtension(filePath))
          {
            if (IsSampleFile(fsra))
              return false;

            int multipart = -1;
            int multipartSet = -1;
            Match match = REGEXP_MULTIFILE.Match(filePath);
            if (match.Groups[GROUP_DISC].Length > 0)
            {
              int.TryParse(match.Groups[GROUP_DISC].Value, out multipart);
            }

            using (MediaInfoWrapper fileInfo = ReadMediaInfo(fsra))
            {
              // Before we start evaluating the file, check if it is a video at all
              if (!fileInfo.IsValid || (fileInfo.GetVideoCount() == 0 && !IsWorkaroundRequired(filePath)))
                return false;

              result = VideoResult.CreateFileInfo(mediaTitle, fileInfo);
            }

            using (Stream stream = fsra.OpenRead())
              result.MimeType = MimeTypeDetector.GetMimeType(stream, DEFAULT_MIMETYPE);

            if (result != null)
            {
              using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
              {
                ILocalFsResourceAccessor lfsra = rah.LocalFsResourceAccessor;
                if (lfsra != null)
                {
                  result.UpdateMetadata(extractedAspectData, lfsra, multipart, multipartSet, false, isReimport);
                  if (!isReimport) //Ignore tags for reimports because they might be the cause of the wrong match
                  {
                    try
                    {
                      await ExtractMatroskaTagsAsync(lfsra, extractedAspectData).ConfigureAwait(false);
                      ExtractMp4Tags(lfsra, extractedAspectData);
                    }
                    catch (Exception ex)
                    {
                      ServiceRegistration.Get<ILogger>().Debug("VideoMetadataExtractor: Exception reading tags for '{0}'", ex, lfsra.CanonicalLocalResourcePath);
                    }
                  }
                  UpdateSetName(lfsra, extractedAspectData, multipart);

                  //Initial add of all subtitles because they have been skipped during import
                  if (!forceQuickMode)
                    FindExternalSubtitles(lfsra, extractedAspectData);

                  return true;
                }
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("VideoMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", e, mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
      if (fsra == null)
        return false;

      if (!fsra.IsFile && fsra.ResourceExists("VIDEO_TS"))
      {
        using (IFileSystemResourceAccessor fsraVideoTs = fsra.GetResource("VIDEO_TS"))
        {
          if (fsraVideoTs != null && fsraVideoTs.ResourceExists("VIDEO_TS.IFO"))
          {
            // Video DVD
            return true;
          }
        }
      }
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
    }

    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      return Task.FromResult<IList<MediaItemSearchResult>>(null);
    }

    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }

    #endregion
  }
}
