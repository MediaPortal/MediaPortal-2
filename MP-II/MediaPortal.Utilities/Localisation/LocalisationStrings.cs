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
using System.Collections;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using MediaPortal.Utilities.Localisation.Strings;

namespace MediaPortal.Utilities.Localisation
{
  public class LocalisationStrings
  {
    #region Enums
    private enum LanguageType
    {
      User,
      Local,
      Parent,
      Default,
    }
    #endregion

    #region Variables
    readonly Dictionary<string, List<List<StringSection>>> _lazyLanguageStrings;
    readonly Dictionary<string, Dictionary<string, StringLocalised>> _languageStrings;
    readonly Dictionary<string, CultureInfo> _availableLanguages;
    readonly List<string> _languageDirectories;
    readonly string _systemDirectory;
    readonly string _userDirectory;
    CultureInfo _currentLanguage;
    bool _userLanguage;
    #endregion

    #region Constructors/Destructors
    public LocalisationStrings(string systemDirectory, string userDirectory, string cultureName)
    {
      // Base strings directory
      _systemDirectory = systemDirectory;
      // User strings directory
      _userDirectory = userDirectory;

      _languageDirectories = new List<string>();
      _languageDirectories.Add(_systemDirectory);

      _availableLanguages = new Dictionary<string, CultureInfo>();
      GetAvailableLangauges();

      // If the language cannot be found default to Local language or English
      if (cultureName != null && _availableLanguages.ContainsKey(cultureName))
        _currentLanguage = _availableLanguages[cultureName];
      else
        _currentLanguage = GetBestLanguage();

      if (_currentLanguage == null)
        throw (new ArgumentException("No available language found"));

      _languageStrings = new Dictionary<string, Dictionary<string, StringLocalised>>();
      _lazyLanguageStrings = new Dictionary<string, List<List<StringSection>>>();

      CheckUserStrings();
      ReloadAll();
    }

    public LocalisationStrings(string directory, string cultureName)
      : this(directory, directory, cultureName)
    {
    }

    public void Dispose()
    {
      Clear();
    }
    #endregion

    #region Properties
    public CultureInfo CurrentCulture
    {
      get { return _currentLanguage; }
    }
    #endregion

    #region Public Methods
    public void AddDirectory(string directory)
    {
      // Add directory to list, to enable reloading/changing language
      _languageDirectories.Add(directory);

      LoadStrings(directory);
    }

    public void ChangeLanguage(string cultureName)
    {
      if (!_availableLanguages.ContainsKey(cultureName))
        throw new ArgumentException("Language not available");

      _currentLanguage = _availableLanguages[cultureName];

      ReloadAll();
    }

    public string ToString(string section, string name)
    {
      LoadLazyStrings(section);
      if (_languageStrings.ContainsKey(section.ToLower()) && _languageStrings[section].ContainsKey(name.ToLower()))
        return _languageStrings[section.ToLower()][name.ToLower()].text;

      return null;
    }

    public string ToString(string section, string name, object[] parameters)
    {
      string translation = ToString(section, name);
      // if parameters or the translation is null, return the translation.
      if ((translation == null) || (parameters == null))
      {
        return translation;
      }
      // return the formatted string. If formatting fails, log the error
      // and return the unformatted string.
      try
      {
        return String.Format(translation, parameters);
      }
      catch (FormatException)
      {
        //Log.Error("Error formatting translation with id {0}", dwCode);
        //Log.Error("Unformatted translation: {0}", translation);
        //Log.Error(e);  
        // Throw exception??
        return translation;
      }
    }

    public CultureInfo[] AvailableLanguages()
    {
      CultureInfo[] available = new CultureInfo[_availableLanguages.Count];

      _availableLanguages.Values.CopyTo(available, 0);

      return available;
    }

    public bool IsLocaleSupported(string cultureName)
    {
      if (_availableLanguages.ContainsKey(cultureName))
        return true;

      return false;
    }

    public CultureInfo GetBestLanguage()
    {
      // Try current local language
      if (_availableLanguages.ContainsKey(CultureInfo.CurrentCulture.Name))
        return CultureInfo.CurrentCulture;

      // Try Language Parent if it has one
      if (!CultureInfo.CurrentCulture.IsNeutralCulture &&
        _availableLanguages.ContainsKey(CultureInfo.CurrentCulture.Parent.Name))
        return CultureInfo.CurrentCulture.Parent;

      // default to English
      if (_availableLanguages.ContainsKey("en"))
        return _availableLanguages["en"];

      return null;
    }
    #endregion

    #region Private Methods
    private void LoadUserStrings()
    {
      // Load User Custom strings
      if (_userLanguage)
        LoadStrings(_userDirectory, LanguageType.User);
    }

    private void LoadStrings(string directory)
    {
      // Local Language
      LoadStrings(directory, LanguageType.Local);

      // Parent Language
      LoadStrings(directory, LanguageType.Parent);

      // Default to English
      LoadStrings(directory, LanguageType.Default);
    }

    private void ReloadAll()
    {
      Clear();

      LoadUserStrings();

      foreach (string directory in _languageDirectories)
        LoadStrings(directory);
    }

    private void Clear()
    {
      if (_lazyLanguageStrings != null)
        _lazyLanguageStrings.Clear();

      if (_languageStrings != null)
        _languageStrings.Clear();
    }

    private void CheckUserStrings()
    {
      _userLanguage = false;

      string path = Path.Combine(_userDirectory, "strings_user.xml");

      if (File.Exists(path))
        _userLanguage = true;
    }

    private void GetAvailableLangauges()
    {
      DirectoryInfo dir = new DirectoryInfo(_systemDirectory);
      foreach (FileInfo file in dir.GetFiles("strings_*.xml"))
      {
        int pos = file.Name.IndexOf('_') + 1;
        string cultName = file.Name.Substring(pos, file.Name.Length - file.Extension.Length - pos);

        try
        {
          CultureInfo cultInfo = new CultureInfo(cultName);
          _availableLanguages.Add(cultName, cultInfo);
        }
        catch (ArgumentException)
        {
          // Log file error?
        }

      }
    }

    private void LoadStrings(string directory, LanguageType type)
    {
      string language = null;
      switch (type)
      {
        case LanguageType.User:
          language = "user";
          break;
        case LanguageType.Local:
          language = _currentLanguage.Name;
          break;
        case LanguageType.Parent:
          if (!_currentLanguage.IsNeutralCulture)
            language = _currentLanguage.Parent.Name;
          break;
        case LanguageType.Default:
          if (_currentLanguage.Name != "en")
            language = "en";
          break;
      }

      if (language != null)
      {
        string filename = "strings_" + language + ".xml";
        //ServiceScope.Get<ILogger>().Info("    Loading strings file: {0}", filename);

        string path = Path.Combine(directory, filename);
        if (File.Exists(path))
        {
          StringFile strings;
          try
          {
            XmlSerializer s = new XmlSerializer(typeof(StringFile));
            Encoding encoding = Encoding.UTF8;
            TextReader r = new StreamReader(path, encoding);
            strings = (StringFile)s.Deserialize(r);
          }
          catch (Exception)
          {
            return;
          }

          foreach (StringSection section in strings.sections)
          {
            // convert section name tolower -> no case matching.
            section.name = section.name.ToLower();

            if (!_lazyLanguageStrings.ContainsKey(section.name))
            {
              List<List<StringSection>> lazyLoad = new List<List<StringSection>>();
              foreach (LanguageType langType in Enum.GetValues(typeof(LanguageType)))
                lazyLoad.Add(new List<StringSection>());

              _lazyLanguageStrings.Add(section.name, lazyLoad);
            }

            _lazyLanguageStrings[section.name][(int)type].Add(section);

            //Dictionary<string, StringLocalised> newSection;
            //if (_languageStrings.ContainsKey(section.name))
            //{
            //  newSection = _languageStrings[section.name];
            //  _languageStrings.Remove(section.name);
            //}
            //else
            //{
            //  newSection = new Dictionary<string, StringLocalised>();
            //}

            //foreach (StringLocalised languageString in section.localisedStrings)
            //{
            //  if (!newSection.ContainsKey(languageString.name))
            //  {
            //    languageString.language = language;
            //    newSection.Add(languageString.name, languageString);
            //  }
            //}

            //if (newSection.Count > 0)
            //  _languageStrings.Add(section.name, newSection);
          }

        }
      }
    }

    private void LoadLazyStrings(string sectionName)
    {
      if (_lazyLanguageStrings.ContainsKey(sectionName))
      {
        foreach (LanguageType type in Enum.GetValues(typeof(LanguageType)))
        {
          foreach (StringSection section in _lazyLanguageStrings[sectionName][(int)type])
          {
            Dictionary<string, StringLocalised> newSection;
            if (_languageStrings.ContainsKey(section.name))
            {
              newSection = _languageStrings[section.name];
              _languageStrings.Remove(section.name);
            }
            else
            {
              newSection = new Dictionary<string, StringLocalised>();
            }

            foreach (StringLocalised languageString in section.localisedStrings)
            {
              if (!newSection.ContainsKey(languageString.name))
              {
                //languageString.language = language;
                newSection.Add(languageString.name, languageString);
              }
            }

            if (newSection.Count > 0)
              _languageStrings.Add(section.name, newSection);
          }
        }

        _lazyLanguageStrings.Remove(sectionName);
      }
    }
    #endregion
  }
}
