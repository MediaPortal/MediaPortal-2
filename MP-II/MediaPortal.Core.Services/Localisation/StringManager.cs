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
using MediaPortal.Core;
using MediaPortal.Core.Localisation;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Logging;


namespace MediaPortal.Services.Localisation
{
  /// <summary>
  /// This class manages localisation strings.
  /// </summary>
  public class StringManager : ILocalisation
  {
    public event LanguageChangeHandler LanguageChange;

    private LocalisationStrings _strings;

    public StringManager()
    {
      ServiceScope.Get<ILogger>().Info("StringsManager.1");
      RegionSettings settings = new RegionSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);

      ServiceScope.Get<ILogger>().Info("StringsManager.2");
      if (settings.Culture == string.Empty)
      {
        ServiceScope.Get<ILogger>().Info("StringsManager.3");
        _strings = new LocalisationStrings("Language", null);
        settings.Culture = _strings.CurrentCulture.Name;

        ServiceScope.Get<ILogger>().Info("StringsManager.4");
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }
      else
      {
        ServiceScope.Get<ILogger>().Info("StringsManager.5");
        _strings = new LocalisationStrings("Language", settings.Culture);
      }
      ServiceScope.Get<ILogger>().Info("StringsManager.6");
    }

    public StringManager(string directory, string cultureName)
    {
      _strings = new LocalisationStrings(directory, cultureName);
    }

    public CultureInfo CurrentCulture
    {
      get { return _strings.CurrentCulture; }
    }

    public void ChangeLanguage(string cultureName)
    {
      _strings.ChangeLanguage(cultureName);
      RegionSettings settings = new RegionSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      settings.Culture = cultureName;
      ServiceScope.Get<ISettingsManager>().Save(settings);

      //send language change event
      LanguageChange(this);
    }

    public string ToString(string section, string name, object[] parameters)
    {
      return _strings.ToString(section, name, parameters);
    }

    public string ToString(string section, string name)
    {
      return _strings.ToString(section, name);
    }

    public string ToString(StringId id)
    {
      return _strings.ToString(id.Section, id.Name);
    }

    public bool IsLocaleSupported(string cultureName)
    {
      return _strings.IsLocaleSupported(cultureName);
    }

    public CultureInfo[] AvailableLanguages()
    {
      return _strings.AvailableLanguages();
    }

    public CultureInfo GetBestLanguage()
    {
      return _strings.GetBestLanguage();
    }

    public void AddDirectory(string stringsDirectory)
    {
      _strings.AddDirectory(stringsDirectory);
    }
  }
}
