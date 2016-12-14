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
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Localization.StringsFile;

namespace MediaPortal.Common.Services.Localization
{
  /// <summary>
  /// Loads localization strings for a given language from localization resources distributed among different
  /// language directories.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The localization resources must be available in XML files of the name "strings_[culture name].xml", where
  /// the "culture name" can either only contain the language code (like "en") or the language code plus
  /// region code (like "en-US"), for example "strings_en.xml" or "strings_en-US.xml".<br/>
  /// For a list of valid culture names, see the Microsoft docs in MSDN for class <see cref="CultureInfo"/>.
  /// </para>
  /// </remarks>
  public class LocalizationStrings
  {
    #region Variables

    protected readonly IDictionary<string, string> _languageStrings =
        new Dictionary<string, string>(StringComparer.Create(CultureInfo.InvariantCulture, true)); // Map: Resource names to resource
    protected CultureInfo _culture;

    #endregion

    #region Constructors/Destructors

    /// <summary>
    /// Initializes a new instance of <see cref="LocalizationStrings"/> which collects language resources from the
    /// given <paramref name="languageDirectories"/> for the specified culture.
    /// </summary>
    /// <param name="languageDirectories">Collection of directory paths containing all language files to be used.</param>
    /// <param name="culture">Culture whose language resources will be loaded.</param>
    public LocalizationStrings(IEnumerable<string> languageDirectories, CultureInfo culture)
    {
      _culture = culture;
      LoadStrings(languageDirectories, culture);
    }

    protected void LoadStrings(IEnumerable<string> languageDirectories, CultureInfo culture2Load)
    {
      if (culture2Load.Parent != CultureInfo.InvariantCulture)
        LoadStrings(languageDirectories, culture2Load.Parent);
      else
        if (culture2Load.Name != "en")
          LoadStrings(languageDirectories, CultureInfo.GetCultureInfo("en"));
      foreach (string directory in languageDirectories)
        TryAddLanguageFile(directory, culture2Load);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns the culture whose language is loaded.
    /// </summary>
    public CultureInfo Culture
    {
      get { return _culture; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Returns the localized string specified by its <paramref name="section"/> and <paramref name="name"/>.
    /// </summary>
    /// <remarks>
    /// Technically, it would not be necessary to force a string name "section". But for the sake of better maintainance, we want each string to have
    /// at least one section (i.e. at least one '.' in the name).
    /// </remarks>
    /// <param name="section">The section of the string to translate.</param>
    /// <param name="name">The name of the string to translate.</param>
    /// <returns>Translated string or <c>null</c>, if the string isn't available.</returns>
    public string ToString(string section, string name)
    {
      string resName = section + '.' + name;
      string res;
      return _languageStrings.TryGetValue(resName, out res) ? res : null;
    }

    /// <summary>
    /// Returns a collection of cultures for that language resources are available in our language
    /// directories pool.
    /// </summary>
    public static ICollection<CultureInfo> FindAvailableLanguages(ICollection<string> languageDirectories)
    {
      ICollection<CultureInfo> result = new HashSet<CultureInfo>();
      foreach (string directory in languageDirectories)
        AddAvailableLanguages(directory, result);
      return result;
    }

    /// <summary>
    /// Searches the specified language <paramref name="directory"/> for all available language resource files and
    /// collects the available languages.
    /// </summary>
    /// <param name="directory">Directory to look through. This directory should contain language files in the form
    /// <code>strings_en.xml</code>. Only the given directory will be searched, the search will not descend recursively
    /// into sub directories.</param>
    /// <param name="result">Result collection to add all languages which are found in the given <paramref name="directory"/>.</param>
    /// <returns>Collection of cultures for that language resources are available in the given
    /// <paramref name="directory"/>.</returns>
    public static void AddAvailableLanguages(string directory, ICollection<CultureInfo> result)
    {
      foreach (string filePath in Directory.GetFiles(directory, "strings_*.xml"))
      {
        int pos = filePath.LastIndexOf('_') + 1;
        string cultName = filePath.Substring(pos, filePath.Length - Path.GetExtension(filePath).Length - pos);

        try
        {
          result.Add(CultureInfo.GetCultureInfo(cultName));
        }
        catch (ArgumentException)
        {
          ServiceRegistration.Get<ILogger>().Warn("Failed to create CultureInfo for language resource file '{0}'", filePath);
        }
      }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Tries to load all language files for the <paramref name="culture2Load"/> in the specified
    /// <paramref name="directory"/>.
    /// </summary>
    /// <remarks>
    /// The language for a culture can be split up into more than one file: We search the language for
    /// the parent culture (if present), then the more specific region language.
    /// If a language string is already present in the internal dictionary, it will be overwritten by
    /// the new string.
    /// </remarks>
    /// <param name="directory">Directory to load from.</param>
    /// <param name="culture2Load">Culture for that the language resource file will be searched.</param>
    protected void TryAddLanguageFile(string directory, CultureInfo culture2Load)
    {
      string fileName = string.Format("strings_{0}.xml", culture2Load.Name);
      string filePath = Path.Combine(directory, fileName);

      if (!File.Exists(filePath))
        return;

      try
      {
        XmlSerializer s = new XmlSerializer(typeof(StringFile));
        Encoding encoding = Encoding.UTF8;
        using (TextReader r = new StreamReader(filePath, encoding))
        {
          StringFile resources = (StringFile) s.Deserialize(r);

          foreach (StringLocalized languageString in resources.Strings)
            _languageStrings[languageString.StringName] = PrepareAndroidFormat(languageString.Text);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Failed to load language resource file '{0}'", ex, filePath);
      }
    }

    /// <summary>
    /// Android string resources require escaped apostrophes and double quotes.
    /// </summary>
    /// <param name="languageString">Escaped string.</param>
    /// <returns>Unescaped format.</returns>
    protected static string PrepareAndroidFormat(string languageString)
    {
      return StringUtils.TrimToEmpty(languageString)
        .Replace(@"\'", "'")
        .Replace("\\\"", "\"");
    }

    #endregion
  }
}
