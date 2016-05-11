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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data;
using MediaPortal.Utilities;

namespace MediaPortal.Extensions.OnlineLibraries.TvMaze
{
  class TvMazeWrapper
  {
    protected TvMazeApiV1 _tvMazeHandler;
    public const int MAX_LEVENSHTEIN_DIST = 4;

    /// <summary>
    /// Returns the language that matches the value set by <see cref="SetPreferredLanguage"/> or the default language (en).
    /// </summary>
    public string PreferredLanguage
    {
      get { return TvMazeApiV1.DefaultLanguage; }
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath)
    {
      _tvMazeHandler = new TvMazeApiV1(cachePath);
      return true;
    }

    /// <summary>
    /// Search for Series by name.
    /// </summary>
    /// <param name="seriesName">Name</param>
    /// <param name="series">Returns the list of matches.</param>
    /// <returns><c>true</c> if at least one Series was found.</returns>
    public bool SearchSeries(string seriesName, out List<TvMazeSeries> series)
    {
      series = _tvMazeHandler.SearchSeries(seriesName);
      return series.Count > 0;
    }

    /// <summary>
    /// Search for unique matches of Series names. This method tries to find the best matching Series.
    /// - If series name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="seriesName">Name</param>
    /// <param name="series">Returns the list of matches.</param>
    /// <returns><c>true</c> if at least one Series was found.</returns>
    public bool SearchSeriesUnique(string seriesName, int year, out List<TvMazeSeries> series)
    {
      series = _tvMazeHandler.SearchSeries(seriesName);
      if (TestSeriesMatch(seriesName, year, ref series))
        return true;

      // If no match is found, we will look for combined series names:
      // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
      if (!TestSeriesMatch(seriesName, year, ref series) && seriesName.Contains("-"))
      {
        string namePart = seriesName.Split(new[] { '-' })[0].Trim();
        return SearchSeriesUnique(namePart, year, out series);
      }
      return series.Count == 1;
    }

    /// <summary>
    /// Tests for series matches. 
    /// </summary>
    /// <param name="seriesName">Series name</param>
    /// <param name="series">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    private bool TestSeriesMatch(string seriesName, int year, ref List<TvMazeSeries> series)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Test Match for \"{0}\"", seriesName);

      if (series.Count == 1)
      {
        if (GetLevenshteinDistance(series[0].Name, seriesName) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Unique match found \"{0}\"!", seriesName);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        series.Clear();
        return false;
      }

      // Multiple matches
      if (series.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Multiple matches for \"{0}\" ({1}). Try to find exact name match.", seriesName, series.Count);
        var exactMatches = series.FindAll(s => s.Name == seriesName || GetLevenshteinDistance(s.Name, seriesName) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Unique match found \"{0}\"!", seriesName);
          series = exactMatches;
          return true;
        }

        if (exactMatches.Count > 1)
        {
          // Try to match the year, if available
          if (year > 0)
          {
            var yearFiltered = exactMatches.FindAll(s => s.Premiered.HasValue && s.Premiered.Value.Year == year);
            if (yearFiltered.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Unique match found \"{0}\" [{1}]!", seriesName, year);
              series = yearFiltered;
              return true;
            }
          }
        }

        series = series.Where(s => GetLevenshteinDistance(s.Name, seriesName) <= MAX_LEVENSHTEIN_DIST).ToList();
        if (series.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Multiple matches found for \"{0}\" (count: {1})", seriesName, series.Count);

      }
      return false;
    }

    public bool GetSeries(int id, out TvMazeSeries seriesDetail)
    {
      seriesDetail = _tvMazeHandler.GetSeries(id);
      return seriesDetail != null;
    }

    public bool GetSeriesByTvDbId(int id, out TvMazeSeries seriesDetail)
    {
      seriesDetail = _tvMazeHandler.GetSeriesByTvDb(id);
      //Get series with external Id does not include episodes, so call with native Id
      if (seriesDetail.Id > 0)
        seriesDetail = _tvMazeHandler.GetSeries(seriesDetail.Id);
      return seriesDetail != null;
    }

    public bool GetSeriesByImDbId(string id, out TvMazeSeries seriesDetail)
    {
      seriesDetail = _tvMazeHandler.GetSeriesByImDb(id);
      //Get series with external Id does not include episodes, so call with native Id
      if(seriesDetail.Id > 0)
        seriesDetail = _tvMazeHandler.GetSeries(seriesDetail.Id);
      return seriesDetail != null;
    }

    public bool GetSeriesSeason(int id, int season, out TvMazeSeason seasonDetail)
    {
      List<TvMazeSeason> seasons = _tvMazeHandler.GetSeriesSeasons(id);
      seasonDetail = seasons.Where(s => s.SeasonNumber == season).First();
      return seasonDetail != null;
    }

    public bool GetSeriesEpisode(int id, int season, int episode, out TvMazeEpisode episodeDetail)
    {
      episodeDetail = _tvMazeHandler.GetSeriesEpisode(id, season, episode);
      return episodeDetail != null;
    }

    public bool GetPerson(int id, out TvMazePerson personDetail)
    {
      personDetail = _tvMazeHandler.GetPerson(id);
      return personDetail != null;
    }

    public bool GetCharacter(int id, out TvMazePerson characterDetail)
    {
      characterDetail = _tvMazeHandler.GetCharacter(id);
      return characterDetail != null;
    }

    /// <summary>
    /// Returns the Levenshtein distance for a <paramref name="movieName"/> and a given <paramref name="searchName"/>.
    /// </summary>
    /// <param name="movieName">MovieSearchResult</param>
    /// <param name="searchName">Movie name</param>
    /// <returns>Levenshtein distance</returns>
    protected int GetLevenshteinDistance(string movieName, string searchName)
    {
      string cleanedName = RemoveCharacters(searchName);
      return StringUtils.GetLevenshteinDistance(RemoveCharacters(movieName), cleanedName);
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

    public bool DownloadImage(int id, TvMazeImageCollection image, string category)
    {
      if (image == null) return false;
      return _tvMazeHandler.DownloadImage(id, image, category);
    }

    public byte[] GetImage(int id, TvMazeImageCollection image, string category)
    {
      if (image == null) return null;
      return _tvMazeHandler.GetImage(id, image, category);
    }
  }
}
