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

using System.Collections.Generic;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries;
using System.Text.RegularExpressions;

namespace MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor.Settings
{
  public class VideoMetadataExtractorSettings
  {
    // Don't add .ifo here because they are processed while processing the video DVD directory
    protected readonly static List<string> DEFAULT_VIDEO_FILE_EXTENSIONS = new List<string>
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

    // Don't add any others unless support has been added for them
    protected readonly static List<string> DEFAULT_SUBTITLE_FILE_EXTENSIONS = new List<string>
      {
        ".srt",
        ".smi",
        ".ass",
        ".ssa",
        ".sub",
        ".vtt",
      };

    public VideoMetadataExtractorSettings()
    {
      VideoFileExtensions = new List<string>(DEFAULT_VIDEO_FILE_EXTENSIONS);
      SubtitleFileExtensions = new List<string>(DEFAULT_SUBTITLE_FILE_EXTENSIONS);
      MultiPartVideoRegex = new SerializableRegex(@"\\(?<file>[^\\|^\/]*)(\s|-|_)*(?<media>Disc|CD|DVD)\s*(?<disc>\d{1,2})", RegexOptions.IgnoreCase);
      StereoscopicVideoRegex = new SerializableRegex(@"\\[-. _](3d|.)?([-. _]*|3d)(?<stereo>(h[-. _]*|half[-. _]*|full[-. _]*)?sbs|(h[-. _]*|half[-. _]*|full[-. _]*)?tab|(h|half[-. _]*|full[-. _]*)?ou)[-. _]", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Video extensions for which the <see cref="VideoMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<string> VideoFileExtensions { get; set; }

    /// <summary>
    /// Subtitle extensions for which the <see cref="VideoMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<string> SubtitleFileExtensions { get; set; }

    /// <summary>
    /// Regular expression used to find a the part number of a multiple video parts
    /// </summary>
    [Setting(SettingScope.Global)]
    public SerializableRegex MultiPartVideoRegex { get; set; }

    /// <summary>
    /// Regular expression used to find a year in the series name
    /// </summary>
    [Setting(SettingScope.Global)]
    public SerializableRegex StereoscopicVideoRegex { get; set; }
  }
}
