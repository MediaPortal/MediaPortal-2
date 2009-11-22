#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Core.Configuration;
using MediaPortal.Core.Localization;

namespace MediaPortal.Configuration.ConfigurationManagement
{
  /// <summary>
  /// Matches <see cref="ConfigBase"/> instances with a text value. This can be used to find a
  /// configuration object by a given search text.
  /// </summary>
  public class ConfigObjectSearchMatcher
  {
    #region Protected fields

    protected readonly string _searchText;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigObjectSearchMatcher"/>.
    /// </summary>
    /// <param name="searchText">Search text to match.</param>
    public ConfigObjectSearchMatcher(string searchText)
    {
      _searchText = searchText;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Matches the specified config object with this search value.
    /// </summary>
    /// <param name="configObject">Configuration object to match.</param>
    /// <returns>
    /// Value between 0 and 1, with 1 representing an exact match, 0 representing no match.
    /// </returns>
    public float CalculateMatchQuality(ConfigBase configObject)
    {
      CultureInfo culture = ServiceScope.Get<ILocalization>().CurrentCulture;
      IEnumerable<string> searchTexts = configObject.GetSearchTexts();
      double result = 0;
      int count = 0;
      foreach (string text in searchTexts)
      {
        string searchText = text.ToLower(culture);
        if (searchText == _searchText)
          result++;
        else if (text.Contains(_searchText))
          result += 0.5;
        count++;
      }
      return (float)(result / count);
    }

    #endregion

  }
}