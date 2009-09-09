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

using System;
using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Backend.Localization;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Services.Localization;

namespace MediaPortal.Backend.Services.Localization
{
  /// <summary>
  /// This class manages localization strings at the MP-II server side.
  /// </summary>
  public class StringManager : StringManagerBase, IMultipleLocalization
  {
    #region Protected fields

    protected IDictionary<CultureInfo, LocalizationStrings> _strings = new Dictionary<CultureInfo, LocalizationStrings>();

    #endregion

    #region Protected methods

    protected override void ReLoad()
    {
      base.ReLoad();
      lock (_syncObj)
        _strings.Clear();
    }

    #endregion

    #region IMultipleLocalization implementation

    public string ToString(CultureInfo culture, string section, string name, params object[] parameters)
    {
      LocalizationStrings localization;
      if (!_strings.TryGetValue(culture, out localization))
      {
        while (!_availableLanguages.Contains(culture))
          if (culture.Parent == CultureInfo.InvariantCulture)
            return null;
          else
            culture = culture.Parent;
        localization = _strings[culture] = new LocalizationStrings(_languageDirectories, culture);
      }
      string translation = localization.ToString(section, name);
      if (translation == null || parameters == null || parameters.Length == 0)
        return translation;
      try
      {
        return string.Format(translation, parameters);
      }
      catch (FormatException e)
      {
        ServiceScope.Get<ILogger>().Error("StringManager: Error formatting localized string '{0}' (Section='{1}', Name='{2}')", e, translation, section, name);
        return translation;
      }
    }

    #endregion
  }
}
