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

namespace MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.Settings
{
  /// <summary>
  /// Settings class for the <see cref="StubMovieMetadataExtractor"/>
  /// </summary>
  /// <remarks>
  /// Also contains all properties defined in <see cref="StubMetadataExtractorSettingsBase"/>
  /// </remarks>
  public class StubMovieMetadataExtractorSettings : StubMetadataExtractorSettingsBase
  {
    #region Ctor

    /// <summary>
    /// Sets the default values specific to the <see cref="StubMovieMetadataExtractor"/>
    /// </summary>
    public StubMovieMetadataExtractorSettings()
    {
      MovieDvdStubFileExtensions = new HashSet<string> { "dvd.disc" };
      MovieBlurayStubFileExtensions = new HashSet<string> { "bluray.disc", "brrip.disc", "bd25.disc", "bd50.disc" };
      MovieHddvdStubFileExtensions = new HashSet<string> { "hddvd.disc" };
      MovieTvStubFileExtensions = new HashSet<string> { "hdtv.disc", "pdtv.disc", "dsr.disc" };
      MovieVhsStubFileExtensions = new HashSet<string> { "vhs.disc" };
    }

    #endregion

    #region Public properties

    /// <summary>
    /// These file extensions are used to find a stub-files for DVD's
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<string> MovieDvdStubFileExtensions { get; set; }

    /// <summary>
    /// These file extensions are used to find a stub-files for Blu-ray's
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<string> MovieBlurayStubFileExtensions { get; set; }

    /// <summary>
    /// These file extensions are used to find a stub-files for HDDVD's
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<string> MovieHddvdStubFileExtensions { get; set; }

    /// <summary>
    /// These file extensions are used to find a stub-files for TV disc's
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<string> MovieTvStubFileExtensions { get; set; }

    /// <summary>
    /// These file extensions are used to find a stub-files for VHS's
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<string> MovieVhsStubFileExtensions { get; set; }

    #endregion
  }
}
