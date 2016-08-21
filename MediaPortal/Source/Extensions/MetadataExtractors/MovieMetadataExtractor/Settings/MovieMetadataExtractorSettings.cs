#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Extensions.OnlineLibraries;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  /// <summary>
  /// Settings class for the MovieMetadataExtractor
  /// </summary>
  public class MovieMetadataExtractorSettings
  {
    public MovieMetadataExtractorSettings()
    {
      // Init default patterns.
      MovieYearPatterns = new List<SerializableRegex>
      {
        new SerializableRegex(@"(?<title>[^\\|\/]+?)\s*[\[\(]?(?<year>(19|20)\d{2})[\]\)]?[\.|\\|\/]*", RegexOptions.IgnoreCase),
      };
    }

    #region Public properties

    /// <summary>
    /// Regular expression used to find title and year in the movie name
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<SerializableRegex> MovieYearPatterns { get; set; }

    /// <summary>
    /// If <c>true</c>, the MovieMetadataExtractor does not store any information in the MediaLibrary but just downloads fanart.
    /// Useful if all metadata is available e.g. via nfo-files and must not be overwritten.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool OnlyFanArt { get; set; }

    /// <summary>
    /// If <c>true</c>, the MovieMetadataExtractor does not fetch any information for missing movies in a collection.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool OnlyLocalMedia { get; set; }

    #endregion
  }
}
