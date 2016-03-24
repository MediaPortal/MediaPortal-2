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

namespace MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor.Settings
{
  public class TranscodeVideoMetadataExtractorSettings
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

    protected List<string> _videoFileExtensions = new List<string>(DEFAULT_VIDEO_FILE_EXTENSIONS);

    /// <summary>
    /// Video extensions for which the <see cref="TranscodeVideoMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<string> VideoFileExtensions
    {
      get { return _videoFileExtensions; }
      set { _videoFileExtensions = value; }
    }
  }
}
