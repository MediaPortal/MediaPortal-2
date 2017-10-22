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

namespace MediaPortal.Plugins.Transcoding.Service.Settings
{
  public enum Transcoder
  {
    FFMpeg
  };

  public enum HWAccelleration
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
      CacheEnabled = true;
      CacheMaximumSizeInGB = 0; //GB
      CacheMaximumAgeInDays = 30; //Days
      CachePath = DEFAULT_CACHE_PATH;
      AnalyzerMaximumThreads = 0; //Auto
      AnalyzerTimeout = 30000;
      AnalyzerStreamTimeout = 10000000;
      TranscoderMaximumThreads = 0; //Auto
      TranscoderTimeout = 5000;
      HLSSegmentTimeInSeconds = 10;
      SubtitleDefaultEncoding = "";
      SubtitleDefaultLanguage = "";
      HardwareAcceleration = HWAccelleration.Auto;
      Transcoder = Transcoder.FFMpeg;
      AudioFileExtensions = DEFAULT_AUDIO_FILE_EXTENSIONS;
      VideoFileExtensions = DEFAULT_VIDEO_FILE_EXTENSIONS;
      ImageFileExtensions = DEFAULT_IMAGE_FILE_EXTENSIONS;
    }

    /// <summary>
    /// Enable caching of transcoded files so they can be reused at a later time
    /// </summary>
    [Setting(SettingScope.Global)]
    public bool CacheEnabled { get; private set; }

    /// <summary>
    /// THe path where the trancoded cache files should be stored
    /// </summary>
    [Setting(SettingScope.Global)]
    public string CachePath { get; private set; }

    /// <summary>
    /// The maximum size of the cache for transcoded files. If cache is bigger than this oldest trancoded files will be deleted.
    /// If set to zero no transcoded files are deleted.
    /// </summary>
    [Setting(SettingScope.Global)]
    public long CacheMaximumSizeInGB { get; private set; }

    /// <summary>
    /// Maximum number of days to keep the transcoded files. Any transcoded file older than this will be deleted.
    /// If set to zero no transcoded files are deleted.
    /// </summary>
    [Setting(SettingScope.Global)]
    public long CacheMaximumAgeInDays { get; private set; }

    /// <summary>
    /// Timeout in milliseconds for analyzing a media file.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int AnalyzerTimeout { get; private set; }

    /// <summary>
    /// Timeout in milliseconds for analyzing a media stream.
    /// </summary>
    [Setting(SettingScope.Global)]
    public long AnalyzerStreamTimeout { get; private set; }

    /// <summary>
    /// Maximum number of threads to user for analyzing media. If zero the maximum is determined automatically.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int AnalyzerMaximumThreads { get; private set; }

    /// <summary>
    /// Maximum number of threads to user for transcoding media. If zero the maximum is determined automatically.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int TranscoderMaximumThreads { get; private set; }

    /// <summary>
    /// Timeout in milliseconds for transcoding to start.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int TranscoderTimeout { get; private set; }

    /// <summary>
    /// Timeout in milliseconds for HLS segment creation.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int HLSSegmentTimeInSeconds { get; private set; }

    /// <summary>
    /// The subtitle encoding to assume if encoding cannot be determined automatically.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string SubtitleDefaultEncoding { get; private set; }

    /// <summary>
    /// The preferred subtitle language.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string SubtitleDefaultLanguage { get; private set; }

    /// <summary>
    /// Hardware acceleration to use if possible.
    /// </summary>
    [Setting(SettingScope.Global)]
    public HWAccelleration HardwareAcceleration { get; private set; }

    /// <summary>
    /// The transcoder to use for transcoding and analysis.
    /// </summary>
    [Setting(SettingScope.Global)]
    public Transcoder Transcoder { get; private set; }

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
