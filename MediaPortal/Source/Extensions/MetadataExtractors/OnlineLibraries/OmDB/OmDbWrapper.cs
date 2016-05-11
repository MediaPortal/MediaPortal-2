#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data;

namespace MediaPortal.Extensions.OnlineLibraries.OmDB
{
  class OmDbWrapper
  {
    protected OmDbApiV1 _omDbHandler;
    public const int MAX_LEVENSHTEIN_DIST = 4;

    /// <summary>
    /// Returns the language that matches the value set by <see cref="SetPreferredLanguage"/> or the default language (en).
    /// </summary>
    public string PreferredLanguage
    {
      get { return OmDbApiV1.DefaultLanguage; }
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath)
    {
      _omDbHandler = new OmDbApiV1(cachePath);
      return true;
    }

    /// <summary>
    /// Search for Movie by name.
    /// </summary>
    /// <param name="movieName">Name</param>
    /// <param name="year">Optional year of movie</param>
    /// <param name="movies">Returns the list of matches.</param>
    /// <returns><c>true</c> if at least one Movie was found.</returns>
    public bool SearchMovie(string movieName, int year, out List<OmDbSearchItem> movies)
    {
      movies = _omDbHandler.SearchMovie(movieName, year);
      if (movies == null) return false;
      return movies.Count > 0;
    }

    /// <summary>
    /// Search for unique matches of Movie names. This method tries to find the best matching Movie in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If movies name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="movieName">Name</param>
    /// <param name="year">Optional year of movie</param>
    /// <param name="movies">Returns the list of matches.</param>
    /// <returns><c>true</c> if at exactly one Movie was found.</returns>
    public bool SearchMovieUnique(string movieName, int year, out List<OmDbSearchItem> movies)
    {
      movies = _omDbHandler.SearchMovie(movieName, year);
      if (movies == null) return false;

      if (TestMovieMatch(movieName, year, ref movies))
        return true;

      if (movies.Count == 0)
      {
        // If also no match in default language is found, we will look for combined movies names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        if (!TestMovieMatch(movieName, year, ref movies) && movieName.Contains("-"))
        {
          string namePart = movieName.Split(new [] { '-' })[0].Trim();
          return SearchMovieUnique(namePart, year, out movies);
        }
        return movies.Count == 1;
      }
      return false;
    }

    /// <summary>
    /// Tests for movie matches. 
    /// </summary>
    /// <param name="moviesName">Movie name</param>
    /// <param name="year">Optional year</param>
    /// <param name="movies">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    private bool TestMovieMatch(string moviesName, int year, ref List<OmDbSearchItem> movies)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Test Match for \"{0}\"", moviesName);

      if (movies.Count == 1)
      {
        if (GetLevenshteinDistance(movies[0].Title, moviesName) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Unique match found \"{0}\"!", moviesName);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        movies.Clear();
        return false;
      }

      // Multiple matches
      if (movies.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Multiple matches for \"{0}\" ({1}). Try to find exact name match.", moviesName, movies.Count);
        var exactMatches = movies.FindAll(s => s.Title == moviesName || GetLevenshteinDistance(s.Title, moviesName) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Unique match found \"{0}\"!", moviesName);
          movies = exactMatches;
          return true;
        }

        if (exactMatches.Count > 1)
        {
          // Try to match the year, if available
          if (year > 0)
          {
            var yearFiltered = exactMatches.FindAll(s => s.Year > 0 && s.Year == year);
            if (yearFiltered.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Unique match found \"{0}\" [{1}]!", moviesName, year);
              movies = yearFiltered;
              return true;
            }
          }
        }

        movies = movies.Where(s => GetLevenshteinDistance(s.Title, moviesName) <= MAX_LEVENSHTEIN_DIST).ToList();
        if (movies.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Multiple matches found for \"{0}\" (count: {1})", moviesName, movies.Count);

        return movies.Count == 1;
      }
      return false;
    }

    /// <summary>
    /// Search for Series by name.
    /// </summary>
    /// <param name="seriesName">Name</param>
    /// <param name="series">Returns the list of matches.</param>
    /// <returns><c>true</c> if at least one Series was found.</returns>
    public bool SearchSeries(string seriesName, int year, out List<OmDbSearchItem> series)
    {
      series = _omDbHandler.SearchSeries(seriesName, year);
      if (series == null) return false;
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
    public bool SearchSeriesUnique(string seriesName, int year, out List<OmDbSearchItem> series)
    {
      series = _omDbHandler.SearchSeries(seriesName, year);
      if (series == null) return false;

      if (TestSeriesMatch(seriesName, year, ref series))
        return true;

      if (series.Count == 0)
      {
        // If also no match in default language is found, we will look for combined series names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        if (!TestSeriesMatch(seriesName, year, ref series) && seriesName.Contains("-"))
        {
          string namePart = seriesName.Split(new[] { '-' })[0].Trim();
          return SearchSeriesUnique(namePart, year, out series);
        }
        return series.Count == 1;
      }
      return false;
    }

    /// <summary>
    /// Tests for series matches. 
    /// </summary>
    /// <param name="seriesName">Series name</param>
    /// <param name="series">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    private bool TestSeriesMatch(string seriesName, int year, ref List<OmDbSearchItem> series)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Test Match for \"{0}\"", seriesName);

      if (series.Count == 1)
      {
        if (GetLevenshteinDistance(series[0].Title, seriesName) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Unique match found \"{0}\"!", seriesName);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        series.Clear();
        return false;
      }

      // Multiple matches
      if (series.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Multiple matches for \"{0}\" ({1}). Try to find exact name match.", seriesName, series.Count);
        var exactMatches = series.FindAll(s => s.Title == seriesName || GetLevenshteinDistance(s.Title, seriesName) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Unique match found \"{0}\"!", seriesName);
          series = exactMatches;
          return true;
        }

        if (exactMatches.Count > 1)
        {
          // Try to match the year, if available
          if (year > 0)
          {
            var yearFiltered = exactMatches.FindAll(s => s.Year > 0 && s.Year == year);
            if (yearFiltered.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Unique match found \"{0}\" [{1}]!", seriesName, year);
              series = yearFiltered;
              return true;
            }
          }
        }

        series = series.Where(s => GetLevenshteinDistance(s.Title, seriesName) <= MAX_LEVENSHTEIN_DIST).ToList();
        if (series.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Multiple matches found for \"{0}\" (count: {1})", seriesName, series.Count);

      }
      return false;
    }

    public bool GetMovie(string imdbId, out OmDbMovie movieDetail)
    {
      movieDetail = _omDbHandler.GetMovie(imdbId);
      return movieDetail != null;
    }

    public bool GetSeries(string imdbId, out OmDBSeries seriesDetail)
    {
      seriesDetail = _omDbHandler.GetSeries(imdbId);
      return seriesDetail != null;
    }

    public bool GetSeriesSeason(string imdbId, int season, out OmDBSeason seasonDetail)
    {
      seasonDetail = _omDbHandler.GetSeriesSeason(imdbId, season);
      return seasonDetail != null;
    }

    public bool GetSeriesEpisode(string imdbId, int season, int episode, out OmDbEpisode episodeDetail)
    {
      episodeDetail = _omDbHandler.GetSeriesEpisode(imdbId, season, episode);
      return episodeDetail != null;
    }

    /// <summary>
    /// Removes special characters and compares the remaining strings. Strings are processed by <see cref="RemoveCharacters"/> before comparing.
    /// The result is <c>true</c>, if the cleaned strings are equal or have a Levenshtein distance less or equal to <see cref="MAX_LEVENSHTEIN_DIST"/>.
    /// </summary>
    /// <param name="name1">Name 1</param>
    /// <param name="name2">Name 2</param>
    /// <returns><c>true</c> if similar or equal</returns>
    protected bool IsSimilarOrEqual(string name1, string name2)
    {
      return string.Equals(RemoveCharacters(name1), RemoveCharacters(name2)) || StringUtils.GetLevenshteinDistance(name1, name2) <= MAX_LEVENSHTEIN_DIST;
    }

    /// <summary>
    /// Returns the Levenshtein distance for a movie name and a given <paramref name="movieName"/>.
    /// </summary>
    /// <param name="movieSearch">Movie search result</param>
    /// <param name="movieName">Movie name</param>
    /// <returns>Levenshtein distance</returns>
    protected int GetLevenshteinDistance(string movieSearch, string movieName)
    {
      string cleanedName = RemoveCharacters(movieName);
      return StringUtils.GetLevenshteinDistance(RemoveCharacters(movieSearch), cleanedName);
    }

    /// <summary>
    /// Replaces characters that are not necessary for comparing (like whitespaces) and diacritics. The result is returned as <see cref="string.ToLowerInvariant"/>.
    /// </summary>
    /// <param name="name">Name to clean up</param>
    /// <returns>Cleaned string</returns>
    protected string RemoveCharacters(string name)
    {
      name = name.ToLowerInvariant();
      string result = new[] { "-", ",", "/", ":", " ", " ", ".", "'", "(", ")", "[", "]", "teil", "part" }.Aggregate(name, (current, s) => current.Replace(s, ""));
      result = result.Replace("&", "and");
      return StringUtils.RemoveDiacritics(result);
    }
  }
}
