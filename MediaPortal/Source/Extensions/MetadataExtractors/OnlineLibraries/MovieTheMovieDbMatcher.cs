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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.TheMovieDB;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class MovieTheMovieDbMatcher : BaseMatcher<MovieMatch, int>
  {
    #region Static instance

    public static MovieTheMovieDbMatcher Instance
    {
      get { return ServiceRegistration.Get<MovieTheMovieDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheMovieDB\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected static string _collectionMatchesFile = Path.Combine(CACHE_PATH, "CollectionMatches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(1);

    readonly MatchStorage<MovieCollectionMatch, int> _collectionStorage = new MatchStorage<MovieCollectionMatch, int>(_collectionMatchesFile);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, Movie> _memoryCache = new ConcurrentDictionary<string, Movie>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized TheMovieDbWrapper.
    /// </summary>
    private TheMovieDbWrapper _movieDb;

    private bool _disposed;

    #endregion

    /// <summary>
    /// Tries to lookup the Movie from TheMovieDB and updates the given <paramref name="movieInfo"/> with the online information.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateMovie(MovieInfo movieInfo)
    {
      /* Clear the names from unwanted strings */
      NamePreprocessor.CleanupTitle(movieInfo);
      string preferredLookupLanguage = FindBestMatchingLanguage(movieInfo);
      Movie movieDetails;
      if (
        /* Best way is to get details by an unique IMDB id */
        MatchByImdbId(movieInfo, out movieDetails) ||
        TryMatch(movieInfo.MovieName, movieInfo.Year, preferredLookupLanguage, false, out movieDetails) ||
        /* Prefer passed year, if no year given, try to process movie title and split between title and year */
        (movieInfo.Year != 0 || NamePreprocessor.MatchTitleYear(movieInfo)) && TryMatch(movieInfo.MovieName, movieInfo.Year, preferredLookupLanguage, false, out movieDetails)
        )
      {
        int movieDbId = 0;
        if (movieDetails != null)
        {
          movieDbId = movieDetails.Id;
          movieInfo.MovieName = movieDetails.Title;
          movieInfo.OriginalName = movieDetails.OriginalTitle;
          movieInfo.Summary = movieDetails.Overview;
          movieInfo.Tagline = movieDetails.Tagline;
          if (movieDetails.Budget.HasValue) movieInfo.Budget = movieDetails.Budget.Value;
          if (movieDetails.Revenue.HasValue) movieInfo.Revenue = movieDetails.Revenue.Value;
          if (movieDetails.Runtime.HasValue) movieInfo.Runtime = movieDetails.Runtime.Value;
          if (movieDetails.Popularity.HasValue) movieInfo.Popularity = movieDetails.Popularity.Value;
          if (movieDetails.Rating.HasValue) movieInfo.TotalRating = movieDetails.Rating.Value;
          if (movieDetails.RatingCount.HasValue) movieInfo.RatingCount = movieDetails.RatingCount.Value;
          movieInfo.ImdbId = movieDetails.ImdbId;
          movieInfo.MovieDbId = movieDetails.Id;
          if (movieDetails.Genres.Count > 0)
          {
            movieInfo.Genres.Clear();
            movieInfo.Genres.AddRange(movieDetails.Genres.Select(p => p.Name));
          }

          MovieCasts movieCasts;
          if (_movieDb.GetMovieCast(movieDbId, out movieCasts))
          {
            movieInfo.Actors.Clear();
            movieInfo.Actors.AddRange(movieCasts.Cast.Select(p => p.Name));
            movieInfo.Directors.Clear();
            movieInfo.Directors.AddRange(movieCasts.Crew.Where(p => p.Job == "Director").Select(p => p.Name));
            movieInfo.Writers.Clear();
            movieInfo.Writers.AddRange(movieCasts.Crew.Where(p => p.Job == "Author").Select(p => p.Name));
          }
          if (movieDetails.ReleaseDate.HasValue)
          {
            int year = movieDetails.ReleaseDate.Value.Year;
            if (year > 0)
              movieInfo.Year = year;
          }

          if (movieDetails.Collection != null &&
            movieDetails.Collection.Id > 0)
          {
            movieInfo.CollectionMovieDbId = movieDetails.Collection.Id;
            movieInfo.CollectionName = movieDetails.Collection.Name;
          }
        }

        if (movieDbId > 0)
          ScheduleDownload(movieDbId);
        return true;
      }
      return false;
    }

    private static string FindBestMatchingLanguage(MovieInfo movieInfo)
    {
      CultureInfo mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
      if (movieInfo.Languages.Count == 0 || movieInfo.Languages.Contains(mpLocal.TwoLetterISOLanguageName))
        return mpLocal.TwoLetterISOLanguageName;

      // If there is only one language available, use this one.
      if (movieInfo.Languages.Count == 1)
        return movieInfo.Languages[0];

      // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
      // By returning null we allow fallback to the default language of the online source (en).
      return null;
    }

    private bool MatchByImdbId(MovieInfo movieInfo, out Movie movieDetails)
    {
      if (!string.IsNullOrEmpty(movieInfo.ImdbId) && _movieDb.GetMovie(movieInfo.ImdbId, out movieDetails))
      {
        SaveMatchToPersistentCache(movieDetails, movieDetails.Title);
        return true;
      }
      movieDetails = null;
      return false;
    }

    public bool TryGetCollectionId(string collectionName, out int collectionId)
    {
      MovieCollectionMatch match = _collectionStorage.GetMatches().Find(m => string.Equals(m.ItemName, collectionName, StringComparison.OrdinalIgnoreCase));
      collectionId = match == null ? 0 : match.Id;
      return collectionId != 0;
    }

    public bool TryGetMovieDbId(string movieName, out int movieDbId)
    {
      Movie movieDetails;
      if (TryMatch(movieName, 0, null, true, out movieDetails))
      {
        movieDbId = movieDetails.Id;
        return true;
      }
      movieDbId = 0;
      return false;
    }

    protected bool TryMatch(string movieName, int year, string language, bool cacheOnly, out Movie movieDetail)
    {
      movieDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(movieName, out movieDetail))
          return true;

        // Load cache or create new list
        List<MovieMatch> matches = _storage.GetMatches();

        // Init empty
        movieDetail = null;

        // Use cached values before doing online query
        MovieMatch match = matches.Find(m => 
          string.Equals(m.ItemName, movieName, StringComparison.OrdinalIgnoreCase) || 
          string.Equals(m.MovieDBName, movieName, StringComparison.OrdinalIgnoreCase));
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Try to lookup movie \"{0}\" from cache: {1}", movieName, match != null && match.Id != 0);

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known movie, only return the movie details.
        if (match != null)
          return match.Id != 0 && _movieDb.GetMovie(match.Id, out movieDetail);

        if (cacheOnly)
          return false;

        List<MovieSearchResult> movies;
        if (_movieDb.SearchMovieUnique(movieName, year, language, out movies))
        {
          MovieSearchResult movieResult = movies[0];
          ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Found unique online match for \"{0}\": \"{1}\"", movieName, movieResult.Title);
          if (_movieDb.GetMovie(movies[0].Id, out movieDetail))
          {
            SaveMatchToPersistentCache(movieDetail, movieName);
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: No unique match found for \"{0}\"", movieName);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new MovieMatch { ItemName = movieName });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception while processing movie {0}", ex, movieName);
        return false;
      }
      finally
      {
        if (movieDetail != null)
          _memoryCache.TryAdd(movieName, movieDetail);
      }
    }

    private void SaveMatchToPersistentCache(Movie movieDetails, string movieName)
    {
      var onlineMatch = new MovieMatch
      {
        Id = movieDetails.Id,
        ItemName = movieName,
        MovieDBName = movieDetails.Title
      };
      _storage.TryAddMatch(onlineMatch);

      // Save collection mapping, if available
      if (movieDetails.Collection != null)
      {
        var collectionMatch = new MovieCollectionMatch
        {
          Id = movieDetails.Collection.Id,
          ItemName = movieDetails.Collection.Name
        };
        _collectionStorage.TryAddMatch(collectionMatch);
      }
    }

    /// <summary>
    /// Check if the memory cache should be cleared and starts an online update of (file-) cached series information.
    /// </summary>
    private void CheckCacheAndRefresh()
    {
      if (DateTime.Now - _memoryCacheInvalidated <= MAX_MEMCACHE_DURATION)
        return;
      _memoryCache.Clear();
      _memoryCacheInvalidated = DateTime.Now;

      // TODO: when updating movie information is implemented, start here a job to do it
    }

    public override bool Init()
    {
      if (!base.Init())
        return false;

      if (_movieDb != null)
        return true;

      _movieDb = new TheMovieDbWrapper();
      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _movieDb.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      return _movieDb.Init();
    }

    protected override void DownloadFanArt(int movieDbId)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Started for ID {0}", movieDbId);

        if (!Init())
          return;

        // If movie belongs to a collection, also download collection poster and fanart
        Movie movie;
        if (_movieDb.GetMovie(movieDbId, out movie) && movie.Collection != null)
          SaveBanners(movie.Collection);

        ImageCollection imageCollection;
        if (!_movieDb.GetMovieFanArt(movieDbId, out imageCollection))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Begin saving banners for ID {0}", movieDbId);
        SaveBanners(imageCollection.Backdrops, "Backdrops");
        SaveBanners(imageCollection.Covers, "Covers");
        SaveBanners(imageCollection.Posters, "Posters");
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Finished saving banners for ID {0}", movieDbId);

        // Remember we are finished
        FinishDownloadFanArt(movieDbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception downloading FanArt for ID {0}", ex, movieDbId);
      }
    }

    private void SaveBanners(MovieCollection movieCollection)
    {
      bool result = _movieDb.DownloadImages(movieCollection);
      ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download Collection: Saved {0} {1}", movieCollection.Name, result);
    }

    private int SaveBanners(IEnumerable<MovieImage> banners, string category)
    {
      if (banners == null)
        return 0;

      int idx = 0;
      foreach (MovieImage banner in banners.Where(b => b.Language == null || b.Language == _movieDb.PreferredLanguage))
      {
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_movieDb.DownloadImage(banner, category))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher Download: Saved {0} {1}", idx, category);
      return idx;
    }

    protected override void Dispose(bool disposing)
    {
      if (_disposed)
        return;
      if (disposing)
      {
        // We need to call EndDownloads here (as well as in base.Dispose)
        // to make sure the downloads have stopped before we dispose _collectionStorage.
        EndDownloads();
        _collectionStorage.Dispose();
      }
      base.Dispose(disposing);
      _disposed = true;
    }
  }
}
