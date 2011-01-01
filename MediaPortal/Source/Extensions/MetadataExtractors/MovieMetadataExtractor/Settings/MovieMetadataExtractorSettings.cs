#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Settings
{
  public class MovieMetadataExtractorSettings
  {

    // Don't add .ifo here because they are processed while processing the video DVD directory
    protected readonly static List<string> DEFAULT_MOVIE_EXTENSIONS = new List<string>
      {
          ".mkv",
          ".ogm",
          ".avi",
          ".wmv",
          ".mpg",
          ".mp4",
          ".ts",
          ".flv",
      };

    protected List<string> _movieExtensions = new List<string>(DEFAULT_MOVIE_EXTENSIONS);

    /// <summary>
    /// Movie extensions for which the <see cref="MovieMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<string> MovieExtensions
    {
      get { return _movieExtensions; }
      set { _movieExtensions = value; }
    }
  }
}
