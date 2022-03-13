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

using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace MP2BootstrapperApp.Localization
{
  /// <summary>
  /// Description of Localization.
  /// </summary>
  public class StringManager : ILocalization, ILanguageChanged
  {
    #region Protected fields

    protected ILogger _logger;
    protected LocalizationStrings _strings = null;
    protected CultureInfo _currentCulture = null;
    protected ICollection<CultureInfo> _availableLanguages = null;
    protected ICollection<Assembly> _languageAssemblies = null;
    protected object _syncObj = new object();

    #endregion

    #region ctor/dtor

    public StringManager(ILogger logger)
    {
      _logger = logger;
      _languageAssemblies = new List<Assembly>();
      _currentCulture = CultureInfo.CurrentUICulture;
      _logger.Info("Localization: Using culture: '{0}'", _currentCulture.Name);
    }

    #endregion

    #region Protected methods


    protected void InitializeLanguageResources()
    {
      var assembly = Assembly.GetExecutingAssembly();
      _languageAssemblies.Add(assembly);
      ReLoad();
    }


    protected virtual void ReLoad()
    {
      lock (_syncObj)
      {
        _strings = new LocalizationStrings(_languageAssemblies, _currentCulture, _logger);
        _availableLanguages = _strings.FindAvailableLanguages(_languageAssemblies);
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
      }
      OnLanguageChanged();
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
        _logger.Error("Localization: Error formatting localized string '{0}' (Section='{1}', Name='{2}')", e, translation, section, name);
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
      // Languages are loaded from assemblies
    }

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return _availableLanguages; }
    }

    #endregion

    /// <summary>
    /// Adds the specified <paramref name="assembly"/> to the collection of assemblies
    /// to search for language files and reloads the strings.
    /// </summary>
    /// <param name="assembly">The additional assembly to search.</param>
    public void AddLanguageAssembly(Assembly assembly)
    {
      lock (_syncObj)
      {
        _languageAssemblies.Add(assembly);
        ReLoad();
      }
    }

    #region ILanguageChanged implementation

    public event EventHandler LanguageChanged;

    protected virtual void OnLanguageChanged()
    {
      LanguageChanged?.Invoke(this, new EventArgs());
    }

    #endregion
  }
}
