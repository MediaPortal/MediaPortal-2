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

namespace MediaPortal.Presentation.Localization
{
  public delegate void LanguageChangeHandler(ILocalization localization, CultureInfo newCulture);

  /// <summary>
  /// Interface for accessing the localization module. The localization module is responsible
  /// for managing culture data supported by the application, holding a current culture and
  /// providing localised strings for that culture.
  /// </summary>
  /// <remarks>
  /// Localized strings are referenced from the application by instances of <see cref="StringId"/>.
  /// Generally, the implementing instance of this interface should not be used directly,
  /// instances of <c>StringId</c> should be used instead for resolving localized strings.
  /// </remarks>
  public interface ILocalization
  {
    #region events

    /// <summary>
    /// Will be called if the language changes, which makes all former returned localized
    /// strings invalid.
    /// </summary>
    event LanguageChangeHandler LanguageChange;

    #endregion

    #region Properties

    CultureInfo CurrentCulture { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Changes the current language, to that all strings should be translated.
    /// </summary>
    /// <param name="culture">The culture whose language should be used.</param>
    void ChangeLanguage(CultureInfo culture);

    /// <summary>
    /// Returns the translation for a given string resource (given by section name and name)
    /// and format the string with the given parameters in the current language.
    /// </summary>
    /// <param name="section">Section of the string resource in the resource file.</param>
    /// <param name="name">Name of the string resource in the resource file.</param>
    /// <param name="parameters">Parameters used in the formating.</param>
    /// <returns>
    /// String containing the translated text.
    /// </returns>
    string ToString(string section, string name, params object[] parameters);

    /// <summary>
    /// Returns the <see cref="CultureInfo"/>s for all installed languages.
    /// </summary>
    /// <returns>Collection containing all languages for which localized strings are availabe in this
    /// application.</returns>
    ICollection<CultureInfo> AvailableLanguages { get; }

    /// <summary>
    /// Tries to guess the best language for that localization resources are available for the current system.
    /// Will default in english if no other language resources could be found.
    /// </summary>
    /// <remarks>
    /// If no installed language resources fit to the system language, the default (english) might not
    /// be contained in the <see cref="AvailableLanguages"/> collection.
    /// </remarks>
    /// <returns>Best language for this system, for that language resources are available.</returns>
    CultureInfo GetBestAvailableLanguage();

    #endregion
  }
}