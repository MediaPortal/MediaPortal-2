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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

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
    protected ConcurrentDictionary<string, OmDbMovie> _memoryCache = new ConcurrentDictionary<string, OmDbMovie>(StringComparer.OrdinalIgnoreCase);

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
      OmDbMovie movieDetails;
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
          MetadataUpdater.SetOrUpdateId(ref movieInfo.ImDbId, movieDetails.ImdbID);
          MetadataUpdater.SetOrUpdateString(ref movieInfo.MovieName, movieDetails.Title, true);
          MetadataUpdater.SetOrUpdateString(ref movieInfo.Summary, movieDetails.Plot, true);
          MetadataUpdater.SetOrUpdateString(ref movieInfo.Certification, movieDetails.Rated, true);

          MetadataUpdater.SetOrUpdateValue(ref movieInfo.Revenue, movieDetails.Revenue.HasValue ? movieDetails.Revenue.Value : 0);
          MetadataUpdater.SetOrUpdateValue(ref movieInfo.Runtime, movieDetails.Runtime.HasValue ? movieDetails.Runtime.Value : 0);
          MetadataUpdater.SetOrUpdateValue(ref movieInfo.ReleaseDate, movieDetails.Released);

          List<string> awards = new List<string>();
          if(!string.IsNullOrEmpty(movieDetails.Awards))
          {
            if(movieDetails.Awards.IndexOf("Won ", StringComparison.InvariantCultureIgnoreCase)  >= 0 || 
              movieDetails.Awards.IndexOf(" Oscar", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
              awards.Add("Oscar");
            }
            if (movieDetails.Awards.IndexOf("Won ", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
              movieDetails.Awards.IndexOf(" Golden Globe", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
              awards.Add("Golden Globe");
            }
            MetadataUpdater.SetOrUpdateList(movieInfo.Awards, awards, true);
          }

          if (movieDetails.ImdbRating.HasValue)
          {
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.TotalRating, movieDetails.ImdbRating.HasValue ? movieDetails.ImdbRating.Value : 0);
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.RatingCount, movieDetails.ImdbVotes.HasValue ? movieDetails.ImdbVotes.Value : 0);
          }
          else if (movieDetails.TomatoRating.HasValue)
          {
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.TotalRating, movieDetails.TomatoRating.HasValue ? movieDetails.TomatoRating.Value : 0);
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.RatingCount, movieDetails.TomatoTotalReviews.HasValue ? movieDetails.TomatoTotalReviews.Value : 0);
          }
          else if (movieDetails.TomatoUserRating.HasValue)
          {
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.TotalRating, movieDetails.TomatoUserRating.HasValue ? movieDetails.TomatoUserRating.Value : 0);
            MetadataUpdater.SetOrUpdateValue(ref movieInfo.RatingCount, movieDetails.TomatoUserTotalReviews.HasValue ? movieDetails.TomatoUserTotalReviews.Value : 0);
          }
          MetadataUpdater.SetOrUpdateValue(ref movieInfo.Score, movieDetails.Metascore.HasValue ? movieDetails.Metascore.Value : 0);

          MetadataUpdater.SetOrUpdateList(movieInfo.Genres, movieDetails.Genres, false);

          //Only use these if absolutely necessary because there is no way to ID them
          if (movieInfo.Actors.Count == 0)
            MetadataUpdater.SetOrUpdateList(movieInfo.Actors, ConvertToPersons(movieDetails.Actors, PersonOccupation.Actor), true);
          if (movieInfo.Writers.Count == 0)
            MetadataUpdater.SetOrUpdateList(movieInfo.Writers, ConvertToPersons(movieDetails.Writers, PersonOccupation.Writer), true);
          if (movieInfo.Directors.Count == 0)
            MetadataUpdater.SetOrUpdateList(movieInfo.Directors, ConvertToPersons(movieDetails.Directors, PersonOccupation.Director), true);
        }
        return true;
      }
      return false;
    }

    public bool UpdateMoviePersons(MovieInfo movieInfo, List<PersonInfo> persons, PersonOccupation occupation)
    {
      OmDbMovie movieDetails;

      // Try online lookup
      if (!Init())
        return false;

      if (!string.IsNullOrEmpty(movieInfo.ImDbId) && _omDb.GetMovie(movieInfo.ImDbId, out movieDetails))
      {
        if (occupation == PersonOccupation.Actor)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(movieDetails.Actors, PersonOccupation.Actor), false);
        if (occupation == PersonOccupation.Writer)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(movieDetails.Writers, PersonOccupation.Writer), false);
        if (occupation == PersonOccupation.Director)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(movieDetails.Directors, PersonOccupation.Director), false);

        return true;
      }
      return false;
    }

    private List<PersonInfo> ConvertToPersons(List<string> names, PersonOccupation occupation)
    {
      if (names == null || names.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (string name in names)
        retValue.Add(new PersonInfo() { Name = name, Occupation = occupation });
      return retValue;
    }

    private bool MatchByImdbId(MovieInfo movieInfo, out OmDbMovie movieDetails)
    {
      if (!string.IsNullOrEmpty(movieInfo.ImDbId) && _omDb.GetMovie(movieInfo.ImDbId, out movieDetails))
      {
        SaveMatchToPersistentCache(movieDetails, movieDetails.Title);
        return true;
      }
      movieDetails = null;
      return false;
    }

    protected bool TryMatch(string movieName, int year, bool cacheOnly, out OmDbMovie movieDetail)
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

        List<OmDbSearchItem> movies;
        if (_omDb.SearchMovieUnique(movieName, year, out movies))
        {
          OmDbSearchItem movieResult = movies[0];
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

    private void SaveMatchToPersistentCache(OmDbMovie movieDetails, string movieName)
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
