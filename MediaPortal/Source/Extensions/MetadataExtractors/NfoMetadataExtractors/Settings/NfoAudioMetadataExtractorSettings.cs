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

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings
{
  /// <summary>
  /// Settings class for the <see cref="NfoAudioMetadataExtractorSettings"/>
  /// </summary>
  /// <remarks>
  /// Also contains all properties defined in <see cref="NfoMetadataExtractorSettingsBase"/>
  /// </remarks>
  public class NfoAudioMetadataExtractorSettings : NfoMetadataExtractorSettingsBase
  {
    #region Ctor

    /// <summary>
    /// Sets the default values specific to the <see cref="NfoSeriesMetadataExtractor"/>
    /// </summary>
    public NfoAudioMetadataExtractorSettings()
    {
      AlbumNfoFileNames = new HashSet<string> { "album" };
      ArtistNfoFileNames = new HashSet<string> { "artist" };
    }

    #endregion

    #region Public properties

    /// <summary>
    /// If <c>true</c>, no online searches will be done for metadata.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool SkipOnlineSearches { get; set; }

    /// <summary>
    /// These file names are used to find a nfo-file for the album as a whole
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<string> AlbumNfoFileNames { get; set; }

    /// <summary>
    /// These file names are used to find a nfo-file for the artist as a whole
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<string> ArtistNfoFileNames { get; set; }

    /// <summary>
    /// If <c>true</c>, Artist details will be created.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeArtistDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Album details will be created.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeAlbumDetails { get; set; }

    #endregion
  }
}
