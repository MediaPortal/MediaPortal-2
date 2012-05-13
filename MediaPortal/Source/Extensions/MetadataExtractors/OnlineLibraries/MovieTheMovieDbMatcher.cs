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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.TheMovieDB;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class MovieTheMovieDbMatcher
  {
    #region Static instance

    private static MovieTheMovieDbMatcher _instance;
    public static MovieTheMovieDbMatcher Instance
    {
      get { return _instance ?? (_instance = new MovieTheMovieDbMatcher()); }
    }

    #endregion

    #region Constants

    public const int MAX_FANART_IMAGES = 5;

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheMovieDB\");
    protected static string SETTINGS_MATCHES = Path.Combine(CACHE_PATH, "Matches.xml");

    #endregion

    #region Fields

    protected Dictionary<string, MovieDbMovie> _memoryCache = new Dictionary<string, MovieDbMovie>();

    /// <summary>
    /// Locking object to access settings.
    /// </summary>
    protected object _syncObj = new object();

    /// <summary>
    /// Contains the Movie ID for Downloading FanArt asynchronously.
    /// </summary>
    protected int _currentMovieDbId;

    /// <summary>
    /// Contains the initialized TheMovieDbWrapper.
    /// </summary>
    private TheMovieDbWrapper _movieDb;

    #endregion

    /// <summary>
    /// Tries to lookup the series from TheMovieDB and updates the given <paramref name="movieInfo"/> with the online information.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateMovie(MovieInfo movieInfo)
    {
      MovieDbMovie movieDetails;
      if (TryMatch(movieInfo.MovieName, movieInfo.Year, false, out movieDetails))
      {
        int movieDbId = 0;
        if (movieDetails != null)
        {
          movieDbId = movieDetails.Id;
          movieInfo.MovieName = movieDetails.MovieName;
          movieInfo.Summary = movieDetails.Overview;
          movieInfo.Budget = movieDetails.Budget;
          movieInfo.Revenue = movieDetails.Revenue;
          movieInfo.Runtime = movieDetails.Runtime;
          movieInfo.Popularity = movieDetails.Popularity;
          movieInfo.ImdbId = movieDetails.ImdbId;
          movieInfo.MovieDbId = movieDetails.Id;
          // todo: genres 
          // movieInfo.Genres;
          if (movieDetails.Cast != null)
          {
            movieInfo.Actors.Clear();
            movieInfo.Actors.AddRange(movieDetails.Cast.Where(p => p.Job == "Actor").Select(p => p.Name));
            movieInfo.Directors.Clear();
            movieInfo.Directors.AddRange(movieDetails.Cast.Where(p => p.Job == "Director").Select(p => p.Name));
          }
          int year = movieDetails.Released.Year;
          if (year > 0)
            movieInfo.Year = year;
        }

        //if (movieDbId > 0)
        //  DownloadFanArt(movieDbId);
        return true;
      }
      return false;
    }


    protected bool TryMatch(string movieName, int year, bool cacheOnly, out MovieDbMovie movieDetail)
    {
      movieDetail = null;
      try
      {
        // Prefer memory cache
        if (_memoryCache.TryGetValue(movieName, out movieDetail))
          return true;

        // Load cache or create new list
        List<MovieMatch> matches = Settings.Load<List<MovieMatch>>(SETTINGS_MATCHES) ?? new List<MovieMatch>();

        // Init empty
        movieDetail = null;

        // Use cached values before doing online query
        MovieMatch match = matches.Find(m => m.MovieName == movieName || m.MovieDBName == movieName);
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Try to lookup series \"{0}\" from cache: {1}", movieName, match != null && match.ID != 0);

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known series, only return the series details (including episodes).
        if (match != null)
          return match.ID != 0 && _movieDb.GetMovie(match.ID, out movieDetail);

        if (cacheOnly)
          return false;

        List<MovieDbMovie> movies;
        if (_movieDb.SearchMovieUnique(movieName, year, out movies))
        {
          movieDetail = movies[0];
          ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Found unique online match for \"{0}\": \"{1}\" [Lang: {2}]", movieName, movieDetail.MovieName, movieDetail.Language);
          if (_movieDb.GetMovie(movies[0].Id, out movieDetail))
          {
            // Add this match to cache
            MovieMatch onlineMatch = new MovieMatch
              {
                MovieName = movieName,
                ID = movieDetail.Id,
                MovieDBName = movieDetail.MovieName
              };

            // Save cache
            SaveNewMatch(movieName, onlineMatch);
          }
          return true;
        }
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: No unique match found for \"{0}\"", movieName);
        // Also save "non matches" to avoid retrying
        SaveNewMatch(movieName, new MovieMatch { MovieName = movieName });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception while processing series {0}", ex, movieName);
        return false;
      }
      finally
      {
        if (movieDetail != null && !_memoryCache.ContainsKey(movieName))
          _memoryCache.Add(movieName, movieDetail);
      }
    }

    private bool Init()
    {
      if (_movieDb != null)
        return true;

      _movieDb = new TheMovieDbWrapper();
      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _movieDb.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      return _movieDb.Init();
    }

    private void SaveNewMatch(string movieName, MovieMatch onlineMatch)
    {
      lock (_syncObj)
      {
        List<MovieMatch> matches = Settings.Load<List<MovieMatch>>(SETTINGS_MATCHES) ?? new List<MovieMatch>();
        if (matches.All(m => m.MovieName != movieName))
          matches.Add(onlineMatch);
        Settings.Save(SETTINGS_MATCHES, matches);
      }
    }
  }
}
