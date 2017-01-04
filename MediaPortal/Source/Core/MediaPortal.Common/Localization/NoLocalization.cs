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
  /// <summary>
  /// Dummy class which implements the <see cref="ILocalization"/> interface, but
  /// doesn't provide any localized strings.
  /// </summary>
  public class NoLocalization : ILocalization
  {
    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return new List<CultureInfo>(new CultureInfo[] {CultureInfo.CurrentUICulture}); }
    }

    public CultureInfo CurrentCulture
    {
      get { return CultureInfo.CurrentUICulture; }
    }

    public void Startup() { }

    public void AddLanguageDirectory(string directory) {}

    public void ChangeLanguage(CultureInfo culture) {}

    public bool TryTranslate(string section, string name, out string translation, params object[] parameters)
    {
      translation = name;
      return true;
    }

    public string ToString(string label, params object[] parameters)
    {
      return label;
    }

    public CultureInfo GetBestAvailableLanguage()
    {
      return CultureInfo.CurrentUICulture;
    }
  }
}
