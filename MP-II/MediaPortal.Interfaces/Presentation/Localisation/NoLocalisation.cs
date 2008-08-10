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

using System.Globalization;

namespace MediaPortal.Presentation.Localisation
{
  /// <summary>
  /// Dummy class which implements the <see cref="ILocalisation"/> interface, but
  /// doesn't provide any localized strings.
  /// </summary>
  internal class NoLocalisation : ILocalisation
  {
    public event LanguageChangeHandler LanguageChange;

    public CultureInfo CurrentCulture
    {
      get { return CultureInfo.CurrentUICulture; }
    }

    public int Characters
    {
      get { return 1; }
    }

    public void ChangeLanguage(string cultureName) {}

    public string ToString(string section, string name, object[] parameters)
    {
      return string.Format("{0}.{1}", section, name);
    }

    public string ToString(string section, string name)
    {
      return string.Format("{0}.{1}", section, name);
    }

    public string ToString(StringId id)
    {
      return id.Label;
    }

    public bool IsLocaleSupported(string cultureName)
    {
      return false;
    }

    public CultureInfo[] AvailableLanguages()
    {
      return new CultureInfo[] {CultureInfo.CurrentUICulture};
    }

    public CultureInfo GetBestLanguage()
    {
      return CultureInfo.CurrentUICulture;
    }

    public void AddDirectory(string stringsDirectory)
    {
    }
  }
}
