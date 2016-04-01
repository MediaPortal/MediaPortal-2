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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data;
using MediaPortal.Extensions.OnlineLibraries.OmDB;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class MovieOmDbMatcher : BaseMatcher<MovieMatch, string>
  {
    #region Static instance

    public static MovieOmDbMatcher Instance
    {
      get { return ServiceRegistration.Get<MovieOmDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\OmDB\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "MovieMatches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, Movie> _memoryCache = new ConcurrentDictionary<string, Movie>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized OmDbWrapper.
    /// </summary>
    private OmDbWrapper _omDb;

    #endregion

    /// <summary>
    /// Tries to lookup the Movie from OmDB and updates the given <paramref name="movieInfo"/> with the online information.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateMovie(MovieInfo movieInfo)
    {
      /* Clear the names from unwanted strings */
      NamePreprocessor.CleanupTitle(movieInfo);
      Movie movieDetails;
      if (
        /* Best way is to get details by an unique IMDB id */
        MatchByImdbId(movieInfo, out movieDetails) ||
        TryMatch(movieInfo.MovieName, movieInfo.ReleaseDate.HasValue ? movieInfo.ReleaseDate.Value.Year : 0, false, out movieDetails) ||
        /* Prefer passed year, if no year given, try to process movie title and split between title and year */
        (movieInfo.ReleaseDate.HasValue || NamePreprocessor.MatchTitleYear(movieInfo)) && TryMatch(movieInfo.MovieName, movieInfo.ReleaseDate.Value.Year, 
        false, out movieDetails)
        )
      {
        if (movieDetails != null)
        {
          movieInfo.ImDbId = movieDetails.ImdbID;
          movieInfo.MovieName = movieDetails.Title;
          movieInfo.Summary = movieDetails.Plot;
          if(!string.IsNullOrEmpty(movieDetails.Rated)) movieInfo.Certification = movieDetails.Rated;
          if (movieDetails.Revenue.HasValue) movieInfo.Revenue = movieDetails.Revenue.Value;
          if (movieDetails.Runtime.HasValue) movieInfo.Runtime = movieDetails.Runtime.Value;
          if (movieDetails.ImdbRating.HasValue)
          {
            movieInfo.TotalRating = movieDetails.ImdbRating.Value;
            if (movieDetails.ImdbVotes.HasValue)
              movieInfo.RatingCount = movieDetails.ImdbVotes.Value;
          }
          else if (movieDetails.TomatoRating.HasValue)
          {
            movieInfo.TotalRating = movieDetails.TomatoRating.Value;
            if (movieDetails.TomatoTotalReviews.HasValue)
              movieInfo.RatingCount = movieDetails.TomatoTotalReviews.Value;
          }
          else if (movieDetails.TomatoUserRating.HasValue)
          {
            movieInfo.TotalRating = movieDetails.TomatoUserRating.Value;
            if (movieDetails.TomatoUserTotalReviews.HasValue)
              movieInfo.RatingCount = movieDetails.TomatoUserTotalReviews.Value;
          }
          if (movieDetails.Genres.Count > 0)
          {
            movieInfo.Genres.Clear();
            movieInfo.Genres.AddRange(movieDetails.Genres);
          }

          //Only use these if absolutely necessary because there is no way to ID them
          if(movieInfo.Actors.Count == 0) movieInfo.Actors.AddRange(movieDetails.Actors);
          if (movieInfo.Writers.Count == 0) movieInfo.Writers.AddRange(movieDetails.Writers);
          if (movieInfo.Directors.Count == 0) movieInfo.Directors.AddRange(movieDetails.Directors);

          if (movieDetails.Released.HasValue)
          {
            movieInfo.ReleaseDate = movieDetails.Released.Value;
          }
        }
        return true;
      }
      return false;
    }

    private bool MatchByImdbId(MovieInfo movieInfo, out Movie movieDetails)
    {
      if (!string.IsNullOrEmpty(movieInfo.ImDbId) && _omDb.GetMovie(movieInfo.ImDbId, out movieDetails))
      {
        SaveMatchToPersistentCache(movieDetails, movieDetails.Title);
        return true;
      }
      movieDetails = null;
      return false;
    }

    protected bool TryMatch(string movieName, int year, bool cacheOnly, out Movie movieDetail)
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
        ServiceRegistration.Get<ILogger>().Debug("MovieOmDbMatcher: Try to lookup movie \"{0}\" from cache: {1}", movieName, match != null && !string.IsNullOrEmpty(match.Id));

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known movie, only return the movie details.
        if (match != null)
          return !string.IsNullOrEmpty(match.Id) && _omDb.GetMovie(match.Id, out movieDetail);

        if (cacheOnly)
          return false;

        List<SearchItem> movies;
        if (_omDb.SearchMovieUnique(movieName, year, out movies))
        {
          SearchItem movieResult = movies[0];
          ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: Found unique online match for \"{0}\": \"{1}\"", movieName, movieResult.Title);
          if (_omDb.GetMovie(movies[0].ImdbID, out movieDetail))
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
        Id = movieDetails.ImdbID,
        ItemName = movieName,
        MovieDBName = movieDetails.Title
      };
      _storage.TryAddMatch(onlineMatch);
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

      if (_omDb != null)
        return true;

      _omDb = new OmDbWrapper();
      return _omDb.Init(CACHE_PATH);
    }

    protected override void DownloadFanArt(string id)
    {
    }
  }
}
