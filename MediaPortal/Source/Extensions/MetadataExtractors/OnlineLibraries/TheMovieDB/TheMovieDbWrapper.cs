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
    public const int MAX_LEVENSHTEIN_DIST = 2;

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
    public bool Init()
    {
      _movieDbHandler = new MovieDbApiV3("1e3f311b50e6ca53bbc3fcade2214b5e", MovieTheMovieDbMatcher.CACHE_PATH);
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
      if (TestMatch(movieName, year, ref movies))
        return true;

      if (movies.Count == 0 && language != MovieDbApiV3.DefaultLanguage)
      {
        movies = _movieDbHandler.SearchMovie(movieName, MovieDbApiV3.DefaultLanguage);
        // If also no match in default language is found, we will look for combined movies names:
        // i.e. "Sanctuary - Wächter der Kreaturen" is not found, but "Sanctuary" is.
        if (!TestMatch(movieName, year, ref movies) && movieName.Contains("-"))
        {
          string namePart = movieName.Split(new [] { '-' })[0].Trim();
          return SearchMovieUnique(namePart, year, language, out movies);
        }
        return movies.Count == 1;
      }
      return false;
    }

    /// <summary>
    /// Tests for matches. 
    /// </summary>
    /// <param name="moviesName">Movie name</param>
    /// <param name="year">Optional year</param>
    /// <param name="movies">Potential online matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    private bool TestMatch(string moviesName, int year, ref List<MovieSearchResult> movies)
    {
      // Exact match in preferred language
      ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper      : Test Match for \"{0}\"", moviesName);

      if (movies.Count == 1)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper      : Unique match found \"{0}\"!", moviesName);
        return true;
      }

      // Multiple matches
      if (movies.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper      : Multiple matches for \"{0}\" ({1}). Try to find exact name match.", moviesName, movies.Count);
        movies = movies.FindAll(s => s.Title == moviesName || s.OriginalTitle == moviesName || IsSimilarOrEqual(s.Title, moviesName) || IsSimilarOrEqual(s.OriginalTitle, moviesName));
        if (movies.Count > 1)
        {
          // Try to match the year, if available
          if (year > 0)
          {
            var yearFiltered = movies.FindAll(s => s.ReleaseDate.HasValue && s.ReleaseDate.Value.Year == year);
            if (yearFiltered.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper      : Unique match found \"{0}\" [{1}]!", moviesName, year);
              movies = yearFiltered;
              return true;
            }
          }
        }
        return movies.Count == 1;
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
    /// Replaces characters that are not necessary for comparing (like whitespaces) and diacritics. The result is returned as <see cref="string.ToLowerInvariant"/>.
    /// </summary>
    /// <param name="name">Name to clean up</param>
    /// <returns>Cleaned string</returns>
    protected string RemoveCharacters(string name)
    {
      string result = new[] { "-", ",", "/", ":", " ", " ", ".", "'" }.Aggregate(name, (current, s) => current.Replace(s, ""));
      result = result.Replace("&", "and");
      return StringUtils.RemoveDiacritics(result.ToLowerInvariant());
    }

    /// <summary>
    /// Gets images for the requested movie.
    /// </summary>
    /// <param name="id">TMDB ID of movie</param>
    /// <param name="imageCollection">Returns the ImageCollection</param>
    /// <returns><c>true</c> if successful</returns>
    public bool GetMovieFanArt(int id, out ImageCollection imageCollection)
    {
      imageCollection = _movieDbHandler.GetImages(id, null); // Download all image information, filter later!
      return imageCollection != null;
    }

    public bool DownloadImage(MovieImage image, string category)
    {
      return _movieDbHandler.DownloadImage(image, category);
    }
  }
}
