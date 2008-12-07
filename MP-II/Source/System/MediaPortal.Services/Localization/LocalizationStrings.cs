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
using MediaPortal.Utilities;
using MediaPortal.Utilities.Localization.StringsFile;

namespace MediaPortal.Services.Localization
{
  /// <summary>
  /// Management class for localization resources distributed among different directories. The localization
  /// resources must be available in XML files of the name "strings_[culture name].xml", for example
  /// "strings_en.xml".
  /// </summary>
  public class LocalizationStrings
  {
    #region Variables

    readonly Dictionary<string, Dictionary<string, StringLocalised>> _languageStrings =
        new Dictionary<string, Dictionary<string, StringLocalised>>(
            StringComparer.Create(CultureInfo.InvariantCulture, true));
    readonly ICollection<CultureInfo> _availableLanguages =
        new List<CultureInfo>();
    readonly ICollection<string> _languageDirectories = new List<string>();
    CultureInfo _currentLanguage;
    
    #endregion

    #region Constructors/Destructors
    
    public LocalizationStrings(string cultureName)
    {
      if (string.IsNullOrEmpty(cultureName))
        cultureName = "en";

      _currentLanguage = new CultureInfo(cultureName);
    }

    public void Dispose()
    {
      Clear();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns the culture whose language is currently used for translating strings.
    /// </summary>
    public CultureInfo CurrentCulture
    {
      get { return _currentLanguage; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Adds a directory containing localization resources. The strings will be automatically loaded.
    /// </summary>
    /// <param name="directory">Directory which potentially contains localization resources.</param>
    public void AddDirectory(string directory)
    {
      // Add directory to list, to enable reloading/changing language
      _languageDirectories.Add(directory);

      Load(directory);
    }

    /// <summary>
    /// Removes a directory of localization resources.
    /// </summary>
    /// <param name="directory">Directory of localization resources which maybe was added before by
    /// <see cref="AddDirectory"/>.</param>
    public void RemoveDirectory(string directory)
    {
      _languageDirectories.Remove(directory);
      ReloadAll();
    }

    /// <summary>
    /// Sets the language to that all strings should be translated to the language of specified
    /// <see cref="culture"/>.
    /// </summary>
    /// <param name="culture">The new culture.</param>
    public void ChangeLanguage(CultureInfo culture)
    {
      if (!_availableLanguages.Contains(culture))
        throw new ArgumentException(string.Format("Language '{0}' is not available", culture.Name));

      _currentLanguage = culture;

      ReloadAll();
    }

    /// <summary>
    /// Returns the localized string specified by its <paramref name="section"/> and <paramref name="name"/>.
    /// </summary>
    /// <param name="section">The section of the string to translate.</param>
    /// <param name="name">The name of the string to translate.</param>
    /// <returns>Translated string or <c>null</c>, if the string isn't available.</returns>
    public string ToString(string section, string name)
    {
      if (_languageStrings.ContainsKey(section) && _languageStrings[section].ContainsKey(name))
        return _languageStrings[section][name].text;

      return null;
    }

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return _availableLanguages; }
    }

    public static ICollection<CultureInfo> FindAvailableLanguages(string directory)
    {
      ICollection<CultureInfo> result = new List<CultureInfo>();
      foreach (string filePath in Directory.GetFiles(directory, "strings_*.xml"))
      {
        int pos = filePath.IndexOf('_') + 1;
        string cultName = filePath.Substring(pos, filePath.Length - Path.GetExtension(filePath).Length - pos);

        result.Add(new CultureInfo(cultName));
      }
      return result;
    }

    #endregion

    #region Protected Methods

    protected void ReloadAll()
    {
      Clear();

      foreach (string directory in _languageDirectories)
        Load(directory);
    }

    protected void Clear()
    {
      if (_languageStrings != null)
        _languageStrings.Clear();
    }

    protected void Load(string directory)
    {
      CollectionUtils.AddAll(_availableLanguages, FindAvailableLanguages(directory));

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
