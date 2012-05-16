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
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
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

    protected Dictionary<string, Movie> _memoryCache = new Dictionary<string, Movie>();

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
      Movie movieDetails;
      if (TryMatch(movieInfo.MovieName, movieInfo.Year, false, out movieDetails))
      {
        int movieDbId = 0;
        if (movieDetails != null)
        {
          movieDbId = movieDetails.Id;
          movieInfo.MovieName = movieDetails.Title;
          movieInfo.Summary = movieDetails.Overview;
          movieInfo.Budget = movieDetails.Budget;
          movieInfo.Revenue = movieDetails.Revenue;
          movieInfo.Runtime = movieDetails.Runtime;
          movieInfo.Popularity = movieDetails.Popularity;
          movieInfo.ImdbId = movieDetails.ImdbId;
          movieInfo.MovieDbId = movieDetails.Id;
          if (movieDetails.Genres.Count > 0)
          {
            movieInfo.Genres.Clear();
            movieInfo.Genres.AddRange(movieDetails.Genres.Select(p => p.Name));
          }
          //if (movieDetails.Cast != null)
          //{
          //  movieInfo.Actors.Clear();
          //  movieInfo.Actors.AddRange(movieDetails.Cast.Where(p => p.Job == "Actor").Select(p => p.Name));
          //  movieInfo.Directors.Clear();
          //  movieInfo.Directors.AddRange(movieDetails.Cast.Where(p => p.Job == "Director").Select(p => p.Name));
          //}
          int year = movieDetails.ReleaseDate.Year;
          if (year > 0)
            movieInfo.Year = year;
        }

        if (movieDbId > 0)
          DownloadFanArt(movieDbId);
        return true;
      }
      return false;
    }

    protected bool TryMatch(string movieName, int year, bool cacheOnly, out Movie movieDetail)
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

        List<MovieSearchResult> movies;
        if (_movieDb.SearchMovieUnique(movieName, year, out movies))
        {
          MovieSearchResult movieResult = movies[0];
          ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Found unique online match for \"{0}\": \"{1}\"", movieName, movieResult.Title);
          if (_movieDb.GetMovie(movies[0].Id, out movieDetail))
          {
            // Add this match to cache
            MovieMatch onlineMatch = new MovieMatch
              {
                MovieName = movieName,
                ID = movieDetail.Id,
                MovieDBName = movieDetail.Title
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

    public bool DownloadFanArt(int tvDbId)
    {
      bool fanArtDownloaded = false;
      lock (_syncObj)
      {
        // Load cache or create new list
        List<MovieMatch> matches = Settings.Load<List<MovieMatch>>(SETTINGS_MATCHES) ?? new List<MovieMatch>();
        foreach (MovieMatch movieMatch in matches.FindAll(m => m.ID == tvDbId))
        {
          // We can have multiple matches for one TvDbId in list, if one has FanArt downloaded already, update the flag for all matches.
          if (movieMatch.FanArtDownloaded)
            fanArtDownloaded = true;
          movieMatch.FanArtDownloaded = true;
        }
        Settings.Save(SETTINGS_MATCHES, matches);
      }
      if (fanArtDownloaded)
        return true;

      _currentMovieDbId = tvDbId;
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(DownloadFanArt_Async, "FanArt Downloader " + tvDbId, QueuePriority.Low, ThreadPriority.Lowest);
      return true;
    }

    protected void DownloadFanArt_Async()
    {
      int movieDbId = _currentMovieDbId;
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Started for ID {0}", movieDbId);

        if (!Init())
          return;

        ImageCollection imageCollection;
        if (!_movieDb.GetMovieFanArt(movieDbId, out imageCollection))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Begin saving banners for ID {0}", movieDbId);
        SaveBanners(imageCollection.Backdrops, "Backdrops");
        SaveBanners(imageCollection.Covers, "Covers");
        SaveBanners(imageCollection.Posters, "Posters");
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Finished saving banners for ID {0}", movieDbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception downloading FanArt for ID {0}", ex, movieDbId);
      }
    }

    private int SaveBanners(IEnumerable<MovieImage> banners, string category)
    {
      if (banners == null)
        return 0;

      int idx = 0;
      foreach (MovieImage banner in banners.Where(b=> b.Language == null || b.Language == _movieDb.PreferredLanguage))
      {
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_movieDb.DownloadImage(banner, category))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Saved {0} {1}", idx, category);
      return idx;
    }
  }
}
