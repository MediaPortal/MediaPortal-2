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
using System.Globalization;

namespace MediaPortal.Common.Localization
{
  public delegate void LanguageChangeHandler(ILocalization localization, CultureInfo newCulture);

  /// <summary>
  /// Interface for accessing the client side localization module. The localization module is responsible
  /// for managing all available localized strings supported by the application and by plugins
  /// and for providing localized strings for a configured current culture.
  /// </summary>
  /// <remarks>
  /// Localized strings are referenced at the MP2 client side from the application via the interface
  /// <see cref="IResourceString"/>.
  /// When a localized string is to be requested and stored in memory, instances of <c>StringId</c>, which
  /// implements <see cref="IResourceString"/>, should be used. Do not store localized <see cref="string"/> instances
  /// for the reuse in an application module directly. The reason is, the localization system supports the change of
  /// the current language at runtime and all instances of <see cref="StringId"/> change their cached localized string
  /// instance automatically when the language changes.
  /// </remarks>
  public interface ILocalization
  {
    /// <summary>
    /// Returns the <see cref="CultureInfo"/>s for all installed languages.
    /// </summary>
    /// <returns>Collection containing all languages for which localized strings are availabe in this
    /// application.</returns>
    ICollection<CultureInfo> AvailableLanguages { get; }

    /// <summary>
    /// Gets the currently activated culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Starts loading all language resources. Must be called after plugins were enabled by the plugin manager.
    /// </summary>
    void Startup();

    /// <summary>
    /// Adds a static language directory to the collection of available language directories.
    /// </summary>
    /// <remarks>
    /// This method should be called to add static language directories, i.e. language directories which are not
    /// registered in the plugin tree and thus not loaded automatically in the plugin tree notification process.
    /// </remarks>
    /// <param name="directory">Directory with language resource files.</param>
    void AddLanguageDirectory(string directory);

    /// <summary>
    /// Changes the current language, to that all strings should be translated.
    /// </summary>
    /// <param name="culture">The culture whose language should be used.</param>
    void ChangeLanguage(CultureInfo culture);

    /// <summary>
    /// Tries to find a translation for the string with the given <paramref name="section"/> and <paramref name="name"/> and formats the
    /// string with the given parameters in the current language.
    /// </summary>
    /// <param name="section">Section of the localization resource.</param>
    /// <param name="name">Name of the localization resource.</param>
    /// <param name="parameters">Parameters used in the formating of the localized resource string.</param>
    /// <param name="translation">Translation, if available. Else, <c>null</c> is returned.</param>
    /// <returns><c>true</c>, if the resource specified by <paramref name="section"/> and <paramref name="name"/> was found,
    /// else <c>false</c>.</returns>
    bool TryTranslate(string section, string name, out string translation, params object[] parameters);

    /// <summary>
    /// Returns the translation for a given string resource and formats the string with the given parameters
    /// in the current language.
    /// </summary>
    /// <remarks>
    /// The given <paramref name="label"/> should be in the form <c>"[Section.Name]"</c>. In that case, the string
    /// with the given name in the given section is looked up and the localization is returned. If no localized resource
    /// is found for the given section/name combination or the label is not in the correct form, this method tries to
    /// use the label itself as format string for the given <paramref name="parameters"/>.
    /// </remarks>
    /// <param name="label">Label specifying the localization resource. The label should be in the form
    /// <c>"[Section.Name]"</c>.</param>
    /// <param name="parameters">Parameters used in the formating of the localized resource string.</param>
    /// <returns>
    /// String containing the translated text or the given <paramref name="label"/>, if the translation could not be evaluated.
    /// </returns>
    string ToString(string label, params object[] parameters);

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
  }
}
