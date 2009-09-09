#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Globalization;

namespace MediaPortal.Backend.Localization
{
  /// <summary>
  /// Interface for accessing the server side localization module. The localization module is responsible
  /// for managing culture data supported by the application and providing localized strings for potentially multiple
  /// languages at the same time.
  /// </summary>
  public interface IMultipleLocalization
  {
    #region Methods

    /// <summary>
    /// Returns the translation for a given string resource (given by section name and name)
    /// and format the string with the given parameters in the given language (or in a fallback language).
    /// </summary>
    /// <param name="culture">Culture to translate the requested string to.</param>
    /// <param name="section">Section of the string resource in the resource file.</param>
    /// <param name="name">Name of the string resource in the resource file.</param>
    /// <param name="parameters">Parameters used in the formating.</param>
    /// <returns>
    /// String containing the translated text.
    /// </returns>
    string ToString(CultureInfo culture, string section, string name, params object[] parameters);

    /// <summary>
    /// Returns the <see cref="CultureInfo"/>s for all installed languages.
    /// </summary>
    /// <returns>Collection containing all languages for which localized strings are availabe in this
    /// application.</returns>
    ICollection<CultureInfo> AvailableLanguages { get; }

    #endregion
  }
}
