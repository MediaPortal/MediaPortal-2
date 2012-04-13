#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Generic;
using System.Linq;
using TvdbLib;
using TvdbLib.Cache;
using TvdbLib.Data;

namespace MediaPortal.Extensions.OnlineLibraries.TheTvDB
{
  class TvDbWrapper
  {
    protected TvdbHandler _tvdbHandler;
    protected string _langShort;

    /// <summary>
    /// Sets the preferred language in short format like: en, de, ...
    /// </summary>
    /// <param name="langShort">Short language</param>
    public void SetPreferredLanguage(string langShort)
    {
      _langShort = langShort;
    }
    /// <summary>
    /// Returns the language that matches the value set by <see cref="SetPreferredLanguage"/> or the default language (en).
    /// </summary>
    public TvdbLanguage PreferredLanguage
    {
      get { return _tvdbHandler.Languages.Find(l => l.Abbriviation == _langShort) ?? TvdbLanguage.DefaultLanguage; }
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init()
    {
      ICacheProvider cacheProvider = new XmlCacheProvider(@"C:\ProgramData\Team MediaPortal\MP2-Client\TvDB");
      _tvdbHandler = new TvdbHandler(cacheProvider, "9628A4332A8F3487");
      _tvdbHandler.InitCache();
      if (!_tvdbHandler.IsLanguagesCached)
        _tvdbHandler.ReloadLanguages();
      return true;
    }

    /// <summary>
    /// Search for Series by name.
    /// </summary>
    /// <param name="seriesName">Name</param>
    /// <param name="series">Returns the list of matches.</param>
    /// <returns><c>true</c> if at least one Series was found.</returns>
    public bool SearchSeries(string seriesName, out List<TvdbSearchResult> series)
    {
      series = _tvdbHandler.SearchSeries(seriesName, PreferredLanguage);
      return series.Count > 0;
    }

    /// <summary>
    /// Search for unique matches of Series names. This method tries to find the best matching Series in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If series name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="seriesName">Name</param>
    /// <param name="series">Returns the list of matches.</param>
    /// <returns><c>true</c> if at least one Series was found.</returns>
    public bool SearchSeriesUnique(string seriesName, out List<TvdbSearchResult> series)
    {
      series = _tvdbHandler.SearchSeries(seriesName, PreferredLanguage);
      if (TestMatch(seriesName, ref series))
        return true;

      if (series.Count == 0 && PreferredLanguage != TvdbLanguage.DefaultLanguage)
      {
        series = _tvdbHandler.SearchSeries(seriesName, TvdbLanguage.DefaultLanguage);
        // If also no match in default language is found, we will look for combined series names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        if (!TestMatch(seriesName, ref series) && seriesName.Contains("-"))
        {
          string namePart = seriesName.Split(new [] { '-' })[0].Trim();
          return SearchSeriesUnique(namePart, out series);
        }
      }
      return false;
    }

    /// <summary>
    /// Tests for matches. 
    /// </summary>
    /// <param name="seriesName">Series name</param>
    /// <param name="series">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    private bool TestMatch(string seriesName, ref List<TvdbSearchResult> series)
    {
      // Exact match in preferred language
      if (series.Count == 1)
        return true;

      // Multiple matches
      if (series.Count > 1)
      {
        series = series.FindAll(s => s.SeriesName == seriesName || IsSimilarOrEqual(s.SeriesName, seriesName));
        if (series.Count > 1)
          series = series.FindAll(s => s.Language == PreferredLanguage);
        return series.Count == 1;
      }
      return false;
    }

    /// <summary>
    /// Removes special characters and compares the remaining strings.
    /// </summary>
    /// <param name="name1"></param>
    /// <param name="name2"></param>
    /// <returns></returns>
    protected bool IsSimilarOrEqual(string name1, string name2)
    {
      return string.Equals(RemoveCharacters(name1), RemoveCharacters(name2));
    }

    protected string RemoveCharacters(string name)
    {
      string result = new[] { "-", ",", "/", ":", " ", " " }.Aggregate(name, (current, s) => current.Replace(s, ""));
      return result.ToLowerInvariant();
    }

    /// <summary>
    /// Gets Series information from TvDB. Results will be added automatically to cache.
    /// </summary>
    /// <param name="seriesId">TvDB ID of series</param>
    /// <param name="loadBanners"><c>true</c> to load banners</param>
    /// <param name="series">Returns the Series information</param>
    /// <returns><c>true</c> if successful</returns>
    public bool GetSeries(int seriesId, bool loadBanners, out TvdbSeries series)
    {
      series = _tvdbHandler.GetSeries(seriesId, PreferredLanguage, false, false, loadBanners);
      return series != null;
    }
  }
}
