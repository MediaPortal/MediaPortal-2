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

using System;
using System.Globalization;
using System.Collections.Generic;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;

namespace MediaPortal.Common.Services.Localization
{
  /// <summary>
  /// This class manages localization strings.
  /// </summary>
  public class StringManager : StringManagerBase, ILocalization
  {
    #region Protected fields

    protected LocalizationStrings _strings = null;
    protected CultureInfo _currentCulture = null;

    #endregion

    #region Constructors/Destructors

    public StringManager()
    {
      ServiceRegistration.Get<ILogger>().Debug("StringManager: Loading settings");
      RegionSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RegionSettings>();
      if (string.IsNullOrEmpty(settings.Culture))
      {
        _currentCulture = CultureInfo.CurrentUICulture;
        ServiceRegistration.Get<ILogger>().Info("StringManager: Culture not set. Using culture: '{0}'", _currentCulture.Name);
      }
      else
      {
        _currentCulture = CultureInfo.GetCultureInfo(settings.Culture);
        ServiceRegistration.Get<ILogger>().Info("StringManager: Using culture: " + _currentCulture.Name);
      }
    }

    #endregion

    #region Protected methods

    protected static CultureInfo GetBestLanguage(ICollection<CultureInfo> availableLanguages)
    {
      // Try current local language
      if (availableLanguages.Contains(CultureInfo.CurrentUICulture))
        return CultureInfo.CurrentUICulture;

      // Try Language Parent if it has one
      if (CultureInfo.CurrentUICulture.Parent != CultureInfo.InvariantCulture &&
        availableLanguages.Contains(CultureInfo.CurrentUICulture.Parent))
        return CultureInfo.CurrentUICulture.Parent;

      // Default to English
      CultureInfo englishCulture = CultureInfo.GetCultureInfo("en");
      if (availableLanguages.Contains(englishCulture))
        return englishCulture;

      return null;
    }

    protected override void ReLoad()
    {
      base.ReLoad();
      lock (_syncObj)
        _strings = new LocalizationStrings(_languageDirectories, _currentCulture);
    }

    #endregion

    #region ILocalization implementation

    // ICollection<CultureInfo> AvailableLanguages { get; } -> implemented by base class

    public CultureInfo CurrentCulture
    {
      get { return _currentCulture; }
    }

    public void Startup()
    {
      InitializeLanguageResources();
    }

    public void ChangeLanguage(CultureInfo culture)
    {
      lock (_syncObj)
      {
        _currentCulture = culture;
        ReLoad();
        RegionSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RegionSettings>();
        settings.Culture = _currentCulture.Name;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }

      LocalizationMessaging.SendLanguageChangedMessage(culture);
    }

    public bool TryTranslate(string section, string name, out string translation, params object[] parameters)
    {
      translation = _strings.ToString(section, name);
      if (translation == null)
        return false;
      if (parameters == null || parameters.Length == 0)
        return true;
      try
      {
        translation = string.Format(translation, parameters);
      }
      catch (FormatException e)
      {
        ServiceRegistration.Get<ILogger>().Error("StringManager: Error formatting localized string '{0}' (Section='{1}', Name='{2}')", e, translation, section, name);
        return false;
      }
      return true;
    }

    public string ToString(string label, params object[] parameters)
    {
      string section;
      string name;
      string translation;
      if (StringId.ExtractSectionAndName(label, out section, out name) && TryTranslate(section, name, out translation, parameters))
        return translation;
      return label;
    }

    public CultureInfo GetBestAvailableLanguage()
    {
      return GetBestLanguage(_availableLanguages);
    }

    #endregion
  }
}
