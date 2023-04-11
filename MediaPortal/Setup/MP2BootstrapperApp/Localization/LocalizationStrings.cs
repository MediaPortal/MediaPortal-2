#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common.Logging;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Localization.StringsFile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace MP2BootstrapperApp.Localization
{
  /// <summary>
  /// Loads localization strings for a given language from localization resources contained in Assemblies.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This implementation is largely identical to the corresponding classes in MP2, except that the xml files
  /// are contained as embedded resources within assemblies rather than in external directories.
  /// </para>
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
    protected ILogger _logger;

    #endregion

    #region Constructors/Destructors

    /// <summary>
    /// Initializes a new instance of <see cref="LocalizationStrings"/> which collects language resources from the
    /// given <paramref name="languageDirectories"/> for the specified culture.
    /// </summary>
    /// <param name="languageAssemblies">Collection of assemblies containing all language files to be used.</param>
    /// <param name="culture">Culture whose language resources will be loaded.</param>
    /// <param name="logger">Implementation of ILogger to use for logging.</param>
    public LocalizationStrings(IEnumerable<Assembly> languageAssemblies, CultureInfo culture, ILogger logger)
    {
      _culture = culture;
      _logger = logger;
      LoadStrings(languageAssemblies, culture);
    }

    protected void LoadStrings(IEnumerable<Assembly> languageAssemblies, CultureInfo culture2Load)
    {
      if (culture2Load.Parent != CultureInfo.InvariantCulture)
        LoadStrings(languageAssemblies, culture2Load.Parent);
      else
        if (culture2Load.Name != "en")
          LoadStrings(languageAssemblies, CultureInfo.GetCultureInfo("en"));
      foreach (Assembly assembly in languageAssemblies)
        TryAddLanguageFile(assembly, culture2Load);
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
    public ICollection<CultureInfo> FindAvailableLanguages(IEnumerable<Assembly> languageAssemblies)
    {
      ICollection<CultureInfo> result = new HashSet<CultureInfo>();
      foreach (Assembly assembly in languageAssemblies)
        AddAvailableLanguages(assembly, result);
      return result;
    }

    /// <summary>
    /// Searches the specified <paramref name="assembly"/> for all available language resource files and
    /// collects the available languages.
    /// </summary>
    /// <param name="assembly">Assembly to look through. This assembly should contain language files in the form
    /// <code>strings_en.xml</code> as embedded resources.</param>
    /// <param name="result">Result collection to add all languages which are found in the given <paramref name="assembly"/>.</param>
    /// <returns>Collection of cultures for that language resources are available in the given
    /// <paramref name="assembly"/>.</returns>
    public void AddAvailableLanguages(Assembly assembly, ICollection<CultureInfo> result)
    {
      // Get all embedded resource names
      foreach (string resourceName in assembly.GetManifestResourceNames())
      {
        // Find any that have a name in the form strings_[culture name].xml
        string[] resourceNameParts = resourceName.Split('.');
        if (resourceNameParts.Length < 2 || resourceNameParts[resourceNameParts.Length - 1] != "xml" || !resourceNameParts[resourceNameParts.Length - 2].StartsWith("strings_"))
          continue;

        int pos = resourceNameParts[resourceNameParts.Length - 2].LastIndexOf('_') + 1;
        string cultName = resourceNameParts[resourceNameParts.Length - 2].Substring(pos);

        try
        {
          result.Add(CultureInfo.GetCultureInfo(cultName));
        }
        catch (ArgumentException)
        {
          _logger.Warn("Failed to create CultureInfo for language resource file '{0}'", resourceName);
        }
      }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Tries to load all language files for the <paramref name="culture2Load"/> in the specified
    /// <paramref name="assembly"/>.
    /// </summary>
    /// <remarks>
    /// The language for a culture can be split up into more than one file: We search the language for
    /// the parent culture (if present), then the more specific region language.
    /// If a language string is already present in the internal dictionary, it will be overwritten by
    /// the new string.
    /// </remarks>
    /// <param name="assembly">Assembly to load from.</param>
    /// <param name="culture2Load">Culture for that the language resource file will be searched.</param>
    protected void TryAddLanguageFile(Assembly assembly, CultureInfo culture2Load)
    {
      string resourceFileName = string.Format("strings_{0}.xml", culture2Load.Name);
      string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(resourceFileName, StringComparison.InvariantCultureIgnoreCase));
      if (resourceName == null)
        return;

      try
      {
        using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
        {
          XmlSerializer s = new XmlSerializer(typeof(StringFile));
          Encoding encoding = Encoding.UTF8;
          using (TextReader r = new StreamReader(resourceStream, encoding))
          {
            StringFile resources = (StringFile)s.Deserialize(r);

            foreach (StringLocalized languageString in resources.Strings)
              _languageStrings[languageString.StringName] = PrepareAndroidFormat(languageString.Text);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.Warn("Failed to load language resource file '{0}'", ex, resourceName);
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
