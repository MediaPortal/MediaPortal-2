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
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.TheMovieDB;
using MediaPortal.Utilities.Network;

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
    
    MatchStorage<MovieCollectionMatch, int> _collectionStorage = new MatchStorage<MovieCollectionMatch, int>(_collectionMatchesFile);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected Dictionary<string, Movie> _memoryCache = new Dictionary<string, Movie>();

    /// <summary>
    /// Contains the initialized TheMovieDbWrapper.
    /// </summary>
    private TheMovieDbWrapper _movieDb;

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
        /* Best way is to get a unique IMDB id */
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

    private string FindBestMatchingLanguage(MovieInfo movieInfo)
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
        // Add this match to cache
        MovieMatch onlineMatch = new MovieMatch
        {
          Id = movieDetails.Id,
          ItemName = movieDetails.Title,
          MovieDBName = movieDetails.Title
        };
        // Save cache
        _storage.SaveNewMatch(movieDetails.Title, onlineMatch);
        return true;
      }
      movieDetails = null;
      return false;
    }

    public bool TryGetCollectionId(string collectionName, out int collectionId)
    {
      MovieCollectionMatch match = _collectionStorage.LoadMatches().Find(m => m.ItemName == collectionName);
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
        if (_memoryCache.TryGetValue(movieName, out movieDetail))
          return true;

        // Load cache or create new list
        List<MovieMatch> matches = _storage.LoadMatches();

        // Init empty
        movieDetail = null;

        // Use cached values before doing online query
        MovieMatch match = matches.Find(m => m.ItemName == movieName || m.MovieDBName == movieName);
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
            // Add this match to cache
            MovieMatch onlineMatch = new MovieMatch
              {
                Id = movieDetail.Id,
                ItemName = movieName,
                MovieDBName = movieDetail.Title
              };

            // Save collection mapping, if available
            if (movieDetail.Collection != null)
            {
              MovieCollectionMatch collectionMatch = new MovieCollectionMatch
                {
                  Id = movieDetail.Collection.Id,
                  ItemName = movieDetail.Collection.Name
                };
              _collectionStorage.SaveNewMatch(movieDetail.Collection.Name, collectionMatch);
            }

            // Save cache
            _storage.SaveNewMatch(movieName, onlineMatch);
          }
          return true;
        }
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: No unique match found for \"{0}\"", movieName);
        // Also save "non matches" to avoid retrying
        _storage.SaveNewMatch(movieName, new MovieMatch { ItemName = movieName });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Exception while processing movie {0}", ex, movieName);
        return false;
      }
      finally
      {
        if (movieDetail != null && !_memoryCache.ContainsKey(movieName))
          _memoryCache.Add(movieName, movieDetail);
      }
    }

    protected override bool Init()
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
  }
}
