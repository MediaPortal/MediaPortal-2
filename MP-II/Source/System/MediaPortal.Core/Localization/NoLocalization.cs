#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Core.Localization
{
  /// <summary>
  /// Dummy class which implements the <see cref="ILocalization"/> interface, but
  /// doesn't provide any localized strings.
  /// </summary>
  internal class NoLocalization : ILocalization
  {
    public event LanguageChangeHandler LanguageChange;

    public CultureInfo CurrentCulture
    {
      get { return CultureInfo.CurrentUICulture; }
    }

    public void Startup() { }

    public void ChangeLanguage(CultureInfo culture) {}

    public string ToString(string section, string name, params object[] parameters)
    {
      return string.Format("{0}.{1}", section, name);
    }

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return new List<CultureInfo>(new CultureInfo[] {CultureInfo.CurrentUICulture}); }
    }

    public CultureInfo GetBestAvailableLanguage()
    {
      return CultureInfo.CurrentUICulture;
    }

    private void InvokeLanguageChange()
    {
      LanguageChangeHandler dlgt = LanguageChange;
      if (dlgt != null)
        dlgt(this, CurrentCulture);
    }
  }
}
