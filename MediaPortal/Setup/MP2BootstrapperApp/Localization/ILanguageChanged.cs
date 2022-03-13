#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System;

namespace MP2BootstrapperApp.Localization
{
  /// <summary>
  /// Interface for classes that can raise an event when the language has been changed and get the updated translation.
  /// </summary>
  public interface ILanguageChanged
  {
    /// <summary>
    /// Raised when the language has been changed.
    /// </summary>
    event EventHandler LanguageChanged;

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
  }
}
