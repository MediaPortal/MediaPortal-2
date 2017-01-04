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

using System.Collections.Generic;
using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings
{
  /// <summary>
  /// Settings class for the <see cref="NfoSeriesMetadataExtractor"/>
  /// </summary>
  /// <remarks>
  /// Also contains all properties defined in <see cref="NfoMetadataExtractorSettingsBase"/>
  /// </remarks>
  public class NfoSeriesMetadataExtractorSettings : NfoMetadataExtractorSettingsBase
  {
    #region Ctor

    /// <summary>
    /// Sets the default values specific to the <see cref="NfoSeriesMetadataExtractor"/>
    /// </summary>
    public NfoSeriesMetadataExtractorSettings()
    {
      SeriesNfoFileNames = new HashSet<string> { "tvshow" };
    }

    #endregion

    #region Public properties

    /// <summary>
    /// These file names are used to find a nfo-file for the series as a whole
    /// The nfo-file for episodes always has the same name as the episodes' media file
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<string> SeriesNfoFileNames { get; set; }

    /// <summary>
    /// If <c>true</c>, Actor details will be created.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeActorDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Character details will be created.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeCharacterDetails { get; set; }

    #endregion
  }
}
