#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Globalization;
using System.Collections.Generic;
using MediaPortal.Common.Services.Localization;
using MediaPortal.Utilities.FileSystem;
using System.IO;

namespace MediaPortal.Extensions.MetadataExtractors.GenreProvider
{
  /// <summary>
  /// This class manages genre matching strings.
  /// </summary>
  public class GenreStringManager : StringManagerBase
  {
    #region Protected fields

    protected Dictionary<string, LocalizationStrings> _strings = new Dictionary<string, LocalizationStrings>();
    
    #endregion

    #region Constructors/Destructors

    public GenreStringManager()
    {
      _languageDirectories = new List<string>();
      var path = FileUtils.BuildAssemblyRelativePath(@"Language\");
      if (Directory.Exists(path))
        AddLanguageDirectory(path);
      ReLoad();
    }

    #endregion

    #region Protected methods

    public bool TryGetGenreString(string section, string name, string language, out string genreString)
    {
      genreString = null;
      var bestLang = GetBestLanguage(language);
      if (bestLang == null)
        return false;

      lock (_syncObj)
      {
        if(!_strings.ContainsKey(language))
          _strings[language] = new LocalizationStrings(_languageDirectories, bestLang);
      }

      genreString = _strings[language].ToString(section, name);
      if (genreString == null)
        return false;

      return true;
    }

    protected CultureInfo GetBestLanguage(string language)
    {
      // Try the preferred language
      CultureInfo preferred = new CultureInfo(language);
      if (_availableLanguages.Contains(preferred))
        return preferred;

      // Try preferred Parent if it has one
      if (preferred.Parent != CultureInfo.InvariantCulture &&
        _availableLanguages.Contains(preferred.Parent))
        return preferred.Parent;

      // Default to English
      CultureInfo englishCulture = CultureInfo.GetCultureInfo("en");
      if (_availableLanguages.Contains(englishCulture))
        return englishCulture;

      return null;
    }

    #endregion
  }
}
