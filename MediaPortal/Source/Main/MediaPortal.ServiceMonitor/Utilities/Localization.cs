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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Localization;
using MediaPortal.Common.Settings;

namespace MediaPortal.ServiceMonitor.Utilities
{
  /// <summary>
  /// Description of Localization.
  /// </summary>
  public class Localization : ILocalization
  {
    public const string LANGUAGE_RESOURCES_REGISTRATION_PATH = "Language";

    #region Protected fields

    protected LocalizationStrings _strings = null;
    protected CultureInfo _currentCulture = null;
    protected ICollection<CultureInfo> _availableLanguages = null;
    protected ICollection<string> _languageDirectories = null;
    protected object _syncObj = new object();

    #endregion

    #region ctor/dtor

    public Localization()
    {
      _languageDirectories = new List<string>();

      ServiceRegistration.Get<ILogger>().Debug("Localization: Loading settings");
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<RegionSettings>();
      if (string.IsNullOrEmpty(settings.Culture))
      {
        _currentCulture = CultureInfo.CurrentUICulture;
        ServiceRegistration.Get<ILogger>().Info("Localization: Culture not set. Using culture: '{0}'", _currentCulture.Name);
      }
      else
      {
        _currentCulture = CultureInfo.GetCultureInfo(settings.Culture);
        ServiceRegistration.Get<ILogger>().Info("Localization: Using culture: " + _currentCulture.Name);
      }
    }

    #endregion

    #region Protected methods


    protected void InitializeLanguageResources()
    {
      var applicationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      var path = Path.Combine(applicationPath, LANGUAGE_RESOURCES_REGISTRATION_PATH);
      _languageDirectories.Add(path);

      ReLoad();

    }


    protected virtual void ReLoad()
    {
      lock (_syncObj)
      {
        _availableLanguages = LocalizationStrings.FindAvailableLanguages(_languageDirectories);
        _strings = new LocalizationStrings(_languageDirectories, _currentCulture);
      }
    }

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

    #endregion

    #region ILocalization implementation

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
        var settings = ServiceRegistration.Get<ISettingsManager>().Load<RegionSettings>();
        settings.Culture = _currentCulture.Name;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }
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
        ServiceRegistration.Get<ILogger>().Error("Localization: Error formatting localized string '{0}' (Section='{1}', Name='{2}')", e, translation, section, name);
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

    public void AddLanguageDirectory(string directory)
    {
      lock (_syncObj)
        _languageDirectories.Add(directory);
      ReLoad();
    }

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return _availableLanguages; }
    }

    #endregion
  }
}