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

using MediaPortal.Common.Settings;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Settings
{
  public enum Transcoder
  {
    None = -1,
    FFMpeg
  };

  public enum HWAcceleration
  {
    None,
    Auto,
    DirectX11,
    DXVA2,
    Intel,
    Nvidia,
    Amd
  };

  public class TranscodingServiceSettings
  {
    private readonly string DEFAULT_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TranscodeCache\");
    private readonly static string[] DEFAULT_AUDIO_FILE_EXTENSIONS = new string[]
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
    // Don't add .ifo here because they are processed while processing the video DVD directory
    private readonly static string[] DEFAULT_VIDEO_FILE_EXTENSIONS = new string[]
    {
      ".mkv",
      ".mk3d",
      ".webm",
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
    };
    private readonly static string[] DEFAULT_IMAGE_FILE_EXTENSIONS = new string[]
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

    protected string[] _audioFileExtensions;
    protected string[] _videoFileExtensions;
    protected string[] _imageFileExtensions;

    public TranscodingServiceSettings()
    {
      CacheEnabled = false;
      CacheMaximumSizeInGB = 0; //GB
      CacheMaximumAgeInDays = 30; //Days
      CachePath = DEFAULT_CACHE_PATH;
      AnalyzerMaximumThreads = 0; //Auto
      AnalyzerTimeout = 30000;
      AnalyzerStreamTimeout = 10000000;
      TranscoderMaximumThreads = 0; //Auto
      TranscoderTimeout = 5000;
      HLSSegmentTimeInSeconds = 10;
      SubtitleDefaultEncoding = "UTF-8";
      HardwareAcceleration = HWAcceleration.Auto;
      Transcoder = Transcoder.FFMpeg;
      AudioFileExtensions = DEFAULT_AUDIO_FILE_EXTENSIONS;
      VideoFileExtensions = DEFAULT_VIDEO_FILE_EXTENSIONS;
      ImageFileExtensions = DEFAULT_IMAGE_FILE_EXTENSIONS;
      SubtitleColor = null;
      SubtitleFontSize = null;
      SubtitleBox = false;
      SubtitleFont = null;
      ForceSubtitles = true;
      EnableAnalysisOfImportedMedia = false;
      EnableCleanupOrphanedAnalysis = true;
      CleanupOrphanedAnalysisIntervalHours = 24;
    }

    /// <summary>
    /// After new media has been imported schedule an analysis of the new media item
    /// </summary>
    [Setting(SettingScope.Global)]
    public bool EnableAnalysisOfImportedMedia { get; set; }

    /// <summary>
    /// Delete analysis files for media items that were deleted
    /// </summary>
    [Setting(SettingScope.Global)]
    public bool EnableCleanupOrphanedAnalysis { get; set; }

    /// <summary>
    /// Cleanup any orphaned analysis files
    /// </summary>
    [Setting(SettingScope.Global)]
    public int CleanupOrphanedAnalysisIntervalHours { get; set; }

    /// <summary>
    /// Enable caching of transcoded files so they can be reused at a later time
    /// </summary>
    [Setting(SettingScope.Global)]
    public bool CacheEnabled { get; set; }

    /// <summary>
    /// THe path where the trancoded cache files should be stored
    /// </summary>
    [Setting(SettingScope.Global)]
    public string CachePath { get; set; }

    /// <summary>
    /// The maximum size of the cache for transcoded files. If cache is bigger than this oldest trancoded files will be deleted.
    /// If set to zero no transcoded files are deleted.
    /// </summary>
    [Setting(SettingScope.Global)]
    public long CacheMaximumSizeInGB { get; set; }

    /// <summary>
    /// Maximum number of days to keep the transcoded files. Any transcoded file older than this will be deleted.
    /// If set to zero no transcoded files are deleted.
    /// </summary>
    [Setting(SettingScope.Global)]
    public long CacheMaximumAgeInDays { get; set; }

    /// <summary>
    /// Timeout in milliseconds for analyzing a media file.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int AnalyzerTimeout { get; set; }

    /// <summary>
    /// Timeout in milliseconds for analyzing a media stream.
    /// </summary>
    [Setting(SettingScope.Global)]
    public long AnalyzerStreamTimeout { get; set; }

    /// <summary>
    /// Maximum number of threads to user for analyzing media. If zero the maximum is determined automatically.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int AnalyzerMaximumThreads { get; set; }

    /// <summary>
    /// Maximum number of threads to user for transcoding media. If zero the maximum is determined automatically.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int TranscoderMaximumThreads { get; set; }

    /// <summary>
    /// Timeout in milliseconds for transcoding to start.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int TranscoderTimeout { get; set; }

    /// <summary>
    /// Timeout in milliseconds for HLS segment creation.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int HLSSegmentTimeInSeconds { get; set; }

    /// <summary>
    /// The subtitle encoding to assume if encoding cannot be determined automatically.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string SubtitleDefaultEncoding { get; set; }

    /// <summary>
    /// Hardware acceleration to use if possible.
    /// </summary>
    [Setting(SettingScope.Global)]
    public HWAcceleration HardwareAcceleration { get; set; }

    /// <summary>
    /// The transcoder to use for transcoding and analysis.
    /// </summary>
    [Setting(SettingScope.Global)]
    public Transcoder Transcoder { get; set; }

    /// <summary>
    /// The font to use for harcoded subtitles.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string SubtitleFont { get; set; }

    /// <summary>
    /// The font size to use for harcoded subtitles.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string SubtitleFontSize { get; set; }

    /// <summary>
    /// The primary color to use for subtitle to use for harcoded subtitles. 
    /// Hexadecimal in Blue Green Red order as per ASS standard.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string SubtitleColor { get; set; }

    /// <summary>
    /// Show an opaque box behind the hardcoded subtitles.
    /// </summary>
    [Setting(SettingScope.Global)]
    public bool SubtitleBox { get; set; }

    /// <summary>
    /// Always add subtitles if possible.
    /// </summary>
    [Setting(SettingScope.Global)]
    public bool ForceSubtitles { get; set; }

    /// <summary>
    /// Audio file extensions supported for transcoding.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] AudioFileExtensions
    {
      get { return _audioFileExtensions; }
      set { _audioFileExtensions = value; }
    }
    /// <summary>
    /// Video file extensions supported for transcoding.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] VideoFileExtensions
    {
      get { return _videoFileExtensions; }
      set { _videoFileExtensions = value; }
    }
    /// <summary>
    /// Image file extensions supported for transcoding.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] ImageFileExtensions
    {
      get { return _imageFileExtensions; }
      set { _imageFileExtensions = value; }
    }
  }
}
