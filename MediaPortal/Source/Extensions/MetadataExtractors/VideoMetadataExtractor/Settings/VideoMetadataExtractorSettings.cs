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

using System.Collections.Generic;
using MediaPortal.Common.Settings;
using System.Text.RegularExpressions;

namespace MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor.Settings
{
  public class VideoMetadataExtractorSettings
  {
    // Don't add .ifo here because they are processed while processing the video DVD directory
    protected readonly static string[] DEFAULT_VIDEO_FILE_EXTENSIONS = 
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
          ".divx",
          ".mpeg",
          ".m2p",
          ".qt",
          ".rm"
      };

    // Don't add any others unless support has been added for them
    protected readonly static string[] DEFAULT_SUBTITLE_FILE_EXTENSIONS = 
      {
        ".srt",
        ".smi",
        ".ass",
        ".ssa",
        ".sub",
        ".vtt",
        ".idx",
      };

    protected readonly static string[] DEFAULT_SUBTITLE_FOLDERS = 
      {
        "subtitles",
        "subs",
      };

    protected string[] _videoFileExtensions;
    protected string[] _subtitleFileExtensions;
    protected string[] _subtitleFolders;

    public VideoMetadataExtractorSettings()
    {
      _videoFileExtensions = DEFAULT_VIDEO_FILE_EXTENSIONS;
      _subtitleFileExtensions = DEFAULT_SUBTITLE_FILE_EXTENSIONS;
      _subtitleFolders = DEFAULT_SUBTITLE_FOLDERS;

      MultiPartVideoRegex = new SerializableRegex(@"\\(?<file>[^\\|^\/]*)(\s|-|_)*(?<media>Disc|Disk|CD|DVD|File)\s*(?<disc>\d{1,2})", RegexOptions.IgnoreCase);
      StereoVideoRegex = new SerializableRegex(@"\\.*?[-. _](3d|.*?)?([-. _]?|3d)(?<mode>h[-]?|half[-]?|full[-]?)*(?<stereo>sbs|tab|ou|mvc|anaglyph)[-. _]", RegexOptions.IgnoreCase);
      MaxSampleSize = 150;
      SampleVideoRegex = new SerializableRegex(@"(sample)|(trailer)", RegexOptions.IgnoreCase);
      CacheOfflineFanArt = true;
      CacheLocalFanArt = false;
    }

    /// <summary>
    /// Video extensions for which the <see cref="VideoMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] VideoFileExtensions
    {
      get => _videoFileExtensions;
      set => _videoFileExtensions = value;
    }

    /// <summary>
    /// Subtitle extensions for which the <see cref="VideoMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] SubtitleFileExtensions
    {
      get => _subtitleFileExtensions;
      set => _subtitleFileExtensions = value;
    }

    /// <summary>
    /// Regular expression used to find a the part number of a multiple video parts
    /// </summary>
    [Setting(SettingScope.Global)]
    public SerializableRegex MultiPartVideoRegex { get; set; }

    /// <summary>
    /// Regular expression used to detect stereoscopic videos
    /// </summary>
    [Setting(SettingScope.Global)]
    public SerializableRegex StereoVideoRegex { get; set; }

    /// <summary>
    /// Subtitle folders where subtitles for media can be found
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] SubtitleFolders
    {
      get => _subtitleFolders;
      set => _subtitleFolders = value;
    }

    /// <summary>
    /// Maximum size (in MB) of a video before it is detected as a possible sample file
    /// </summary>
    [Setting(SettingScope.Global)]
    public long MaxSampleSize { get; set; }

    /// <summary>
    /// Regular expression used to detect sample files
    /// </summary>
    [Setting(SettingScope.Global)]
    public SerializableRegex SampleVideoRegex { get; set; }

    /// <summary>
    /// If <c>true</c>, a copy will be made of FanArt placed on network drives to allow browsing when they are offline.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool CacheOfflineFanArt { get; set; }

    /// <summary>
    /// If <c>true</c>, a copy will be made of FanArt placed on local drives to allow browsing when they are asleep.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool CacheLocalFanArt { get; set; }
  }
}
