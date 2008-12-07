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
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Utilities.Localisation.Strings;

namespace MediaPortal.Services.Localization
{
  public class LocalizationStrings
  {
    #region Variables

    readonly Dictionary<string, Dictionary<string, StringLocalised>> _languageStrings =
        new Dictionary<string, Dictionary<string, StringLocalised>>(
            StringComparer.Create(CultureInfo.InvariantCulture, true));
    readonly Dictionary<string, CultureInfo> _availableLanguages =
        new Dictionary<string, CultureInfo>(StringComparer.Create(CultureInfo.InvariantCulture, true));
    readonly List<string> _languageDirectories = new List<string>();
    readonly string _systemDirectory;
    CultureInfo _currentLanguage;
    
    #endregion

    #region Constructors/Destructors
    
    public LocalizationStrings(string systemDirectory, string cultureName)
    {
      // Base strings directory
      _systemDirectory = systemDirectory;

      _languageDirectories.Add(_systemDirectory);

      GetAvailableLangauges();

      // If the language cannot be found default to Local language or English
      if (cultureName != null && _availableLanguages.ContainsKey(cultureName))
        _currentLanguage = _availableLanguages[cultureName];
      else
        _currentLanguage = GetBestLanguage();

      if (_currentLanguage == null)
        throw (new ArgumentException("No available language found"));

      ReloadAll();
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

    public void RemoveDirectory(string directory)
    {
      // TODO: remove strings from the given directory. Probably we have to change the
      // backing data structures for this.
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
      if (_languageStrings.ContainsKey(section) && _languageStrings[section].ContainsKey(name))
        return _languageStrings[section][name].text;

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

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return _availableLanguages.Values; }
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

    private void ReloadAll()
    {
      Clear();

      foreach (string directory in _languageDirectories)
        LoadStrings(directory);
    }

    private void Clear()
    {
      if (_languageStrings != null)
        _languageStrings.Clear();
    }

    private void GetAvailableLangauges()
    {
      foreach (string filePath in Directory.GetFiles(_systemDirectory, "strings_*.xml"))
      {
        int pos = filePath.IndexOf('_') + 1;
        string cultName = filePath.Substring(pos, filePath.Length - Path.GetExtension(filePath).Length - pos);

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

    private void LoadStrings(string directory)
    {
      string filename = string.Format("strings_{0}.xml", _currentLanguage.Name);
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
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("Failed decode {0} : {1}",path, ex.ToString());
          return;
        }

        foreach (StringSection section in strings.sections)
        {
          Dictionary<string, StringLocalised> sectionContents = _languageStrings.ContainsKey(section.name) ?
              _languageStrings[section.name] :
              new Dictionary<string, StringLocalised>(
                  StringComparer.Create(CultureInfo.InvariantCulture, true));
          foreach (StringLocalised languageString in section.localisedStrings)
            sectionContents[languageString.name] = languageString;
          if (sectionContents.Count > 0)
            _languageStrings[section.name] = sectionContents;
        }
      }
    }

    #endregion
  }
}
