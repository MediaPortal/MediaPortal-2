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
  /// Abstract base class for the settings class of all StubMetadataExtractors
  /// </summary>
  /// <remarks>
  /// Each NfoMetadataExtractor (such as the <see cref="StubMovieMetadataExtractor"/>) has a settings class
  /// (such as <see cref="StubMovieMetadataExtractorSettings"/> that derives from this abstract base class.
  /// This class therefore contains settings (and their initialization with default values), which should be
  /// present in all derived non-abstract settings classes.
  /// The <see cref="SettingsManager"/> does not store the settings of the base class in a common file for all
  /// derived classes. As a result, settings contained in this class can be set to different values for each
  /// derived settings class.
  /// </remarks>
  public abstract class StubMetadataExtractorSettingsBase
  {
    #region Ctor

    protected StubMetadataExtractorSettingsBase()
    {
      // Set the default values
      SeparatorCharacters = new HashSet<char> { '|', ',', '/' };
      IgnoreStrings = new HashSet<string> { "unknown" };
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Specifies which separator characters are used for strings that may contain multiple values
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<char> SeparatorCharacters { get; set; }

    /// <summary>
    /// If a string value in the stub-file equals (OrdinalIgnoreCase) one of these values, the value is ignored
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<string> IgnoreStrings { get; set; }

    /// <summary>
    /// Indicates whether a very detailed Stub[...]MetadataExtractorDebug.log is created.
    /// </summary>
#if DEBUG
    [Setting(SettingScope.Global, true)]
#else
    [Setting(SettingScope.Global, false)]
#endif
    public bool EnableDebugLogging { get; set; }

    /// <summary>
    /// If <c>true</c>, all stub-files are written into the Stub[...]MetadataExtractorDebug.log (if enabled)
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool WriteRawStubFileIntoDebugLog { get; set; }

    /// <summary>
    /// If <c>true</c>, the filled stub objects are written as Json into the Stub[...]MetadataExtractorDebug.log (if enabled)
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool WriteStubObjectIntoDebugLog { get; set; }

    #endregion
  }
}
