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
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using MediaPortal.Utilities;

namespace MediaPortal.Extensions.OnlineLibraries.TheMovieDB
{
  class TheMovieDbWrapper
  {
    protected MovieDbApiV3 _movieDbHandler;
    protected string _preferredLanguage;
    public const int MAX_LEVENSHTEIN_DIST = 4;

    /// <summary>
    /// Sets the preferred language in short format like: en, de, ...
    /// </summary>
    /// <param name="langShort">Short language</param>
    public void SetPreferredLanguage(string langShort)
    {
      _preferredLanguage = langShort;
    }

    /// <summary>
    /// Returns the language that matches the value set by <see cref="SetPreferredLanguage"/> or the default language (en).
    /// </summary>
    public string PreferredLanguage
    {
      get { return _preferredLanguage ?? MovieDbApiV3.DefaultLanguage; }
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath)
    {
      _movieDbHandler = new MovieDbApiV3("1e3f311b50e6ca53bbc3fcade2214b5e", cachePath);
      return true;
    }

    /// <summary>
    /// Search for Movie by name.
    /// </summary>
    /// <param name="movieName">Name</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <param name="movies">Returns the list of matches.</param>
    /// <returns><c>true</c> if at least one Movie was found.</returns>
    public bool SearchMovie(string movieName, string language, out List<MovieSearchResult> movies)
    {
      movies = _movieDbHandler.SearchMovie(movieName, language);
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
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <param name="movies">Returns the list of matches.</param>
    /// <returns><c>true</c> if at exactly one Movie was found.</returns>
    public bool SearchMovieUnique(string movieName, int year, string language, out List<MovieSearchResult> movies)
    {
      language = language ?? PreferredLanguage;
      movies = _movieDbHandler.SearchMovie(movieName, language);
      if (TestMovieMatch(movieName, year, ref movies))
        return true;

      if (movies.Count == 0 && language != MovieDbApiV3.DefaultLanguage)
      {
        movies = _movieDbHandler.SearchMovie(movieName, MovieDbApiV3.DefaultLanguage);
        // If also no match in default language is found, we will look for combined movies names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        if (!TestMovieMatch(movieName, year, ref movies) && movieName.Contains("-"))
        {
          string namePart = movieName.Split(new [] { '-' })[0].Trim();
          return SearchMovieUnique(namePart, year, language, out movies);
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
    private bool TestMovieMatch(string moviesName, int year, ref List<MovieSearchResult> movies)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Test Match for \"{0}\"", moviesName);

      if (movies.Count == 1)
      {
        if (GetLevenshteinDistance(movies[0], moviesName) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Unique match found \"{0}\"!", moviesName);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        movies.Clear();
        return false;
      }

      // Multiple matches
      if (movies.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Multiple matches for \"{0}\" ({1}). Try to find exact name match.", moviesName, movies.Count);
        var exactMatches = movies.FindAll(s => s.Title == moviesName || s.OriginalTitle == moviesName || GetLevenshteinDistance(s, moviesName) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Unique match found \"{0}\"!", moviesName);
          movies = exactMatches;
          return true;
        }

        if (exactMatches.Count > 1)
        {
          // Try to match the year, if available
          if (year > 0)
          {
            var yearFiltered = exactMatches.FindAll(s => s.ReleaseDate.HasValue && s.ReleaseDate.Value.Year == year);
            if (yearFiltered.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Unique match found \"{0}\" [{1}]!", moviesName, year);
              movies = yearFiltered;
              return true;
            }
          }
        }

        movies = movies.Where(s => GetLevenshteinDistance(s, moviesName) <= MAX_LEVENSHTEIN_DIST).ToList();
        if (movies.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Multiple matches found for \"{0}\" (count: {1})", moviesName, movies.Count);

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
    public bool SearchSeries(string seriesName, string language, out List<SeriesSearchResult> series)
    {
      series = _movieDbHandler.SearchSeries(seriesName, language);
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
    public bool SearchSeriesUnique(string seriesName, int year, string language, out List<SeriesSearchResult> series)
    {
      series = _movieDbHandler.SearchSeries(seriesName, language);
      if (TestSeriesMatch(seriesName, year, ref series))
        return true;

      if (series.Count == 0 && PreferredLanguage != MovieDbApiV3.DefaultLanguage)
      {
        series = _movieDbHandler.SearchSeries(seriesName, MovieDbApiV3.DefaultLanguage);
        // If also no match in default language is found, we will look for combined series names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        if (!TestSeriesMatch(seriesName, year, ref series) && seriesName.Contains("-"))
        {
          string namePart = seriesName.Split(new[] { '-' })[0].Trim();
          return SearchSeriesUnique(namePart, year, language, out series);
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
    private bool TestSeriesMatch(string seriesName, int year, ref List<SeriesSearchResult> series)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Test Match for \"{0}\"", seriesName);

      if (series.Count == 1)
      {
        if (GetLevenshteinDistance(series[0], seriesName) <= MAX_LEVENSHTEIN_DIST)
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
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Multiple matches for \"{0}\" ({1}). Try to find exact name match.", seriesName, series.Count);
        var exactMatches = series.FindAll(s => s.Name == seriesName || s.OriginalName == seriesName || GetLevenshteinDistance(s, seriesName) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Unique match found \"{0}\"!", seriesName);
          series = exactMatches;
          return true;
        }

        if (exactMatches.Count > 1)
        {
          // Try to match the year, if available
          if (year > 0)
          {
            var yearFiltered = exactMatches.FindAll(s => s.FirstAirDate.HasValue && s.FirstAirDate.Value.Year == year);
            if (yearFiltered.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Unique match found \"{0}\" [{1}]!", seriesName, year);
              series = yearFiltered;
              return true;
            }
          }
        }

        series = series.Where(s => GetLevenshteinDistance(s, seriesName) <= MAX_LEVENSHTEIN_DIST).ToList();
        if (series.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Multiple matches found for \"{0}\" (count: {1})", seriesName, series.Count);

      }
      return false;
    }

    public bool GetMovie(int id, out Movie movieDetail)
    {
      movieDetail = _movieDbHandler.GetMovie(id, PreferredLanguage);
      return movieDetail != null;
    }

    public bool GetMovie(string imdbId, out Movie movieDetail)
    {
      movieDetail = _movieDbHandler.GetMovie(imdbId, PreferredLanguage);
      return movieDetail != null;
    }

    public bool GetMovieCast(int id, out MovieCasts movieCast)
    {
      movieCast = _movieDbHandler.GetMovieCastCrew(id);
      return movieCast != null;
    }

    public bool GetSeries(int id, out Series seriesDetail)
    {
      seriesDetail = _movieDbHandler.GetSeries(id, PreferredLanguage);
      return seriesDetail != null;
    }

    public bool GetSeriesSeason(int id, int season, out Season seasonDetail)
    {
      seasonDetail = _movieDbHandler.GetSeriesSeason(id, season, PreferredLanguage);
      return seasonDetail != null;
    }

    public bool GetSeriesEpisode(int id, int season, int episode, out Episode episodeDetail)
    {
      episodeDetail = _movieDbHandler.GetSeriesEpisode(id, season, episode, PreferredLanguage);
      return episodeDetail != null;
    }

    public bool GetSeriesCast(int id, out MovieCasts seriesCast)
    {
      seriesCast = _movieDbHandler.GetSeriesCastCrew(id);
      return seriesCast != null;
    }

    public bool GetSeriesSeasonCast(int id, int season, out MovieCasts seasonCast)
    {
      seasonCast = _movieDbHandler.GetSeriesSeasonCastCrew(id, season);
      return seasonCast != null;
    }

    public bool GetSeriesEpisodeCast(int id, int season, int episode, out MovieCasts episodeCast)
    {
      episodeCast = _movieDbHandler.GetMovieCastCrew(id);
      return episodeCast != null;
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
    /// Returns the Levenshtein distance for a <see cref="MovieSearchResult"/> and a given <paramref name="movieName"/>.
    /// It considers both <see cref="MovieSearchResult.Title"/> and <see cref="MovieSearchResult.OriginalTitle"/>
    /// </summary>
    /// <param name="movie">MovieSearchResult</param>
    /// <param name="movieName">Movie name</param>
    /// <returns>Levenshtein distance</returns>
    protected int GetLevenshteinDistance(MovieSearchResult movie, string movieName)
    {
      string cleanedName = RemoveCharacters(movieName);
      return Math.Min(
        StringUtils.GetLevenshteinDistance(RemoveCharacters(movie.Title), cleanedName),
        StringUtils.GetLevenshteinDistance(RemoveCharacters(movie.OriginalTitle), cleanedName)
        );
    }

    protected int GetLevenshteinDistance(SeriesSearchResult series, string movieName)
    {
      string cleanedName = RemoveCharacters(movieName);
      return Math.Min(
        StringUtils.GetLevenshteinDistance(RemoveCharacters(series.Name), cleanedName),
        StringUtils.GetLevenshteinDistance(RemoveCharacters(series.OriginalName), cleanedName)
        );
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

    /// <summary>
    /// Gets images for the requested movie.
    /// </summary>
    /// <param name="id">TMDB ID of movie</param>
    /// <param name="imageCollection">Returns the ImageCollection</param>
    /// <returns><c>true</c> if successful</returns>
    public bool GetMovieFanArt(int id, out ImageCollection imageCollection)
    {
      imageCollection = _movieDbHandler.GetMovieImages(id, null); // Download all image information, filter later!
      return imageCollection != null;
    }

    /// <summary>
    /// Gets images for the requested series.
    /// </summary>
    /// <param name="id">TMDB ID of series</param>
    /// <param name="imageCollection">Returns the ImageCollection</param>
    /// <returns><c>true</c> if successful</returns>
    public bool GetSeriesFanArt(int id, out ImageCollection imageCollection)
    {
      imageCollection = _movieDbHandler.GetSeriesImages(id, null); // Download all image information, filter later!
      return imageCollection != null;
    }

    /// <summary>
    /// Gets images for the requested series season.
    /// </summary>
    /// <param name="id">TMDB ID of series</param>
    /// <param name="season">Season number</param>
    /// <param name="imageCollection">Returns the ImageCollection</param>
    /// <returns><c>true</c> if successful</returns>
    public bool GetSeriesSeasonFanArt(int id, int season, out ImageCollection imageCollection)
    {
      imageCollection = _movieDbHandler.GetSeriesSeasonImages(id, season, null); // Download all image information, filter later!
      return imageCollection != null;
    }

    /// <summary>
    /// Gets images for the requested series episode.
    /// </summary>
    /// <param name="id">TMDB ID of series</param>
    /// <param name="season">Season number</param>
    /// <param name="episode">Episode number</param>
    /// <param name="imageCollection">Returns the ImageCollection</param>
    /// <returns><c>true</c> if successful</returns>
    public bool GetSeriesEpsiodeFanArt(int id, int season, int episode, out ImageCollection imageCollection)
    {
      imageCollection = _movieDbHandler.GetSeriesEpisodeImages(id, season, episode, null); // Download all image information, filter later!
      return imageCollection != null;
    }

    /// <summary>
    /// Gets images for the requested person.
    /// </summary>
    /// <param name="id">TMDB ID of person</param>
    /// <param name="imageCollection">Returns the ImageCollection</param>
    /// <returns><c>true</c> if successful</returns>
    public bool GetPersonFanArt(int id, out ImageCollection imageCollection)
    {
      imageCollection = _movieDbHandler.GetPersonImages(id, null); // Download all image information, filter later!
      return imageCollection != null;
    }

    public bool DownloadImage(ImageItem image, string category)
    {
      return _movieDbHandler.DownloadImage(image, category);
    }

    public bool DownloadImages(MovieCollection movieCollection)
    {
      return _movieDbHandler.DownloadImages(movieCollection);
    }
  }
}
