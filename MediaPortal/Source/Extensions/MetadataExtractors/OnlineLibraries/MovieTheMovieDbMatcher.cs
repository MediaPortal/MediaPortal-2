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
      Movie movieDetails;
      if (TryMatch(movieInfo.MovieName, movieInfo.Year, false, out movieDetails))
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
          //if (movieDetails.Cast != null)
          //{
          //  movieInfo.Actors.Clear();
          //  movieInfo.Actors.AddRange(movieDetails.Cast.Where(p => p.Job == "Actor").Select(p => p.Name));
          //  movieInfo.Directors.Clear();
          //  movieInfo.Directors.AddRange(movieDetails.Cast.Where(p => p.Job == "Director").Select(p => p.Name));
          //}
          if (movieDetails.ReleaseDate.HasValue)
          {
            int year = movieDetails.ReleaseDate.Value.Year;
            if (year > 0)
              movieInfo.Year = year;
          }
        }

        if (movieDbId > 0)
          ScheduleDownload(movieDbId);
        return true;
      }
      return false;
    }

    public bool TryGetMovieDbId(string movieName, out int movieDbId)
    {
      Movie movieDetails;
      if (TryMatch(movieName, 0, true, out movieDetails))
      {
        movieDbId = movieDetails.Id;
        return true;
      }
      movieDbId = 0;
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
        List<MovieMatch> matches = LoadMatches();

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
        if (_movieDb.SearchMovieUnique(movieName, year, out movies))
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

            // Save cache
            SaveNewMatch(movieName, onlineMatch);
          }
          return true;
        }
        ServiceRegistration.Get<ILogger>().Debug("MovieTheMovieDbMatcher: No unique match found for \"{0}\"", movieName);
        // Also save "non matches" to avoid retrying
        SaveNewMatch(movieName, new MovieMatch { ItemName = movieName });
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
      if (_movieDb != null)
        return true;

      _movieDb = new TheMovieDbWrapper();
      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _movieDb.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      return _movieDb.Init();
    }

    protected override List<MovieMatch> FindNameMatch (List<MovieMatch> matches, string name)
    {
      return matches.FindAll(m => m.ItemName == name);
    }

    protected override void DownloadFanArt(int movieDbId)
    {
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

        // Remember we are finished
        FinishDownloadFanArt(movieDbId);
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
