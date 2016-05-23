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
using System.Globalization;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.FanArtTV;
using System.Collections.Generic;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data;
using System.Linq;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="MovieFanArtTvMatcher"/> is used to download movie images from FanArt.tv.
  /// </summary>
  public class MovieFanArtTvMatcher : BaseMatcher<MovieMatch, string>
  {
    #region Static instance

    public static MovieFanArtTvMatcher Instance
    {
      get { return ServiceRegistration.Get<MovieFanArtTvMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArtTV\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "MovieMatches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, MovieInfo> _memoryCache = new ConcurrentDictionary<string, MovieInfo>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized FanArtTvWrapper.
    /// </summary>
    private FanArtTVWrapper _fanArt;

    #endregion

    #region Metadata updaters

    /// <summary>
    /// Tries to lookup the Movie from FanArt.tv and downloads imges.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateMovie(MovieInfo movieInfo)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        if (movieInfo.MovieDbId > 0)
        {
          CheckCacheAndRefresh();
          MovieInfo oldMovieInfo;
          if (_memoryCache.TryGetValue(movieInfo.MovieDbId.ToString(), out oldMovieInfo))
          {
            //Already downloaded
            return true;
          }

          if (movieInfo.Thumbnail == null)
          {
            List<string> thumbs = GetFanArtFiles(movieInfo, FanArtScope.Movie, FanArtType.Posters);
            if (thumbs.Count > 0)
              movieInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }

          if (_memoryCache.TryAdd(movieInfo.MovieDbId.ToString(), movieInfo))
          {
            ScheduleDownload(movieInfo.MovieDbId.ToString());
            return true;
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieFanArtTvMatcher: Exception while processing movie {0}", ex, movieInfo.ToString());
        return false;
      }
    }

    #endregion

    #region Metadata update helpers

    private void StoreMovieMatch(MovieInfo movie)
    {
      MovieInfo movieMatch = new MovieInfo()
      {
        MovieName = movie.MovieName,
        ReleaseDate = movie.ReleaseDate.HasValue ? movie.ReleaseDate.Value : default(DateTime?)
      };
      var onlineMatch = new MovieMatch
      {
        Id = movie.MovieDbId.ToString(),
        ItemName = movieMatch.ToString(),
        OnlineName = movieMatch.ToString()
      };
      _storage.TryAddMatch(onlineMatch);
    }

    #endregion

    #region Caching

    /// <summary>
    /// Check if the memory cache should be cleared and starts an online update of (file-) cached series information.
    /// </summary>
    private void CheckCacheAndRefresh()
    {
      if (DateTime.Now - _memoryCacheInvalidated <= MAX_MEMCACHE_DURATION)
        return;
      _memoryCache.Clear();
      _memoryCacheInvalidated = DateTime.Now;

      // TODO: when updating track information is implemented, start here a job to do it
    }

    #endregion

    public override bool Init()
    {
      if (!base.Init())
        return false;

      if (_fanArt != null)
        return true;

      try
      {
        _fanArt = new FanArtTVWrapper();
        bool res = _fanArt.Init(CACHE_PATH);
        // Try to lookup online content in the configured language
        CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
        _fanArt.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
        return res;
      }
      catch (Exception)
      {
        return false;
      }
    }

    #region FanArt

    public List<string> GetFanArtFiles<T>(T infoObject, string scope, string type)
    {
      List<string> fanartFiles = new List<string>();
      string path = null;
      if (scope == FanArtScope.Movie)
      {
        MovieInfo movie = infoObject as MovieInfo;
        if (movie != null && movie.MovieDbId > 0)
        {
          path = Path.Combine(CACHE_PATH, movie.MovieDbId.ToString(), string.Format(@"{0}\{1}\", scope, type));
        }
      }
      if (Directory.Exists(path))
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
      return fanartFiles;
    }

    protected override void DownloadFanArt(string tmDbid)
    {
      try
      {
        if (string.IsNullOrEmpty(tmDbid))
          return;

        ServiceRegistration.Get<ILogger>().Debug("MovieFanArtTvMatcher Download: Started for ID {0}", tmDbid);

        MovieInfo movieInfo;
        if (!_memoryCache.TryGetValue(tmDbid, out movieInfo))
          return;

        if (!Init())
          return;

        FanArtMovieThumbs thumbs;
        if (!_fanArt.GetMovieFanArt(tmDbid, out thumbs))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("MovieFanArtTvMatcher Download: Begin saving banners for ID {0}", tmDbid);
        SaveBanners(tmDbid, thumbs.MovieFanArt.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Movie, FanArtType.Backdrops));
        SaveBanners(tmDbid, thumbs.MovieBanners.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Movie, FanArtType.Banners));
        SaveBanners(tmDbid, thumbs.MoviePosters.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Movie, FanArtType.Posters));
        SaveBanners(tmDbid, thumbs.MovieCDArt.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Movie, FanArtType.DiscArt));
        SaveBanners(tmDbid, thumbs.HDMovieClearArt.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Movie, FanArtType.ClearArt));
        SaveBanners(tmDbid, thumbs.HDMovieLogos.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Movie, FanArtType.Logos));
        SaveBanners(tmDbid, thumbs.MovieThumbnails.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Movie, FanArtType.Thumbnails));

        ServiceRegistration.Get<ILogger>().Debug("MovieFanArtTvMatcher Download: Finished saving banners for ID {0}", tmDbid);

        StoreMovieMatch(movieInfo);

        // Remember we are finished
        FinishDownloadFanArt(tmDbid);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MovieFanArtTvMatcher: Exception downloading FanArt for ID {0}", ex, tmDbid);
      }
    }

    private int SaveBanners(string id, IEnumerable<FanArtMovieThumb> banners, string category)
    {
      if (banners == null)
        return 0;

      int idx = 0;
      foreach (FanArtMovieThumb banner in banners.Where(b => b.Language == null || b.Language == _fanArt.PreferredLanguage))
      {
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_fanArt.DownloadFanArt(id, banner, category))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug("MovieFanArtTvMatcher Download: Saved {0} {1}", idx, category);
      return idx;
    }

    #endregion
  }
}
