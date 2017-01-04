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
using MediaPortal.Extensions.OnlineLibraries;
using System.Text.RegularExpressions;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Settings
{
  /// <summary>
  /// Settings class for the MovieMetadataExtractor
  /// </summary>
  public class MovieMetadataExtractorSettings
  {
    public MovieMetadataExtractorSettings()
    {
      // Init default patterns.
      MovieYearPatterns = new SerializableRegex[]
      {
        new SerializableRegex(@"(?<title>[^\\|\/]+?)\s*[\[\(]?(?<year>(19|20)\d{2})[\]\)]?[\.|\\|\/]*", RegexOptions.IgnoreCase),
      };
    }

    #region Public properties

    /// <summary>
    /// Regular expression used to find title and year in the movie name
    /// </summary>
    [Setting(SettingScope.Global)]
    public SerializableRegex[] MovieYearPatterns { get; set; }

    /// <summary>
    /// If <c>true</c>, no online searches will be done for metadata.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool SkipOnlineSearches { get; set; }

    /// <summary>
    /// If <c>true</c>, no FanArt is downloaded.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool SkipFanArtDownload { get; set; }

    /// <summary>
    /// If <c>true</c>, the MovieMetadataExtractor does not fetch any information for missing movies in a collection.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool OnlyLocalMedia { get; set; }

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

    /// <summary>
    /// If <c>true</c>, Actor details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeActorDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Character details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeCharacterDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Director details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeDirectorDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Writer details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeWriterDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Production company details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeProductionCompanyDetails { get; set; }

    #endregion
  }
}
