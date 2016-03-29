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

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="FanArtTvMovieMatcher"/> is used to download movie images from FanArt.tv.
  /// </summary>
  public class FanArtTvMovieMatcher : BaseMatcher<MovieMatch, int>
  {
    #region Static instance

    public static FanArtTvMovieMatcher Instance
    {
      get { return ServiceRegistration.Get<FanArtTvMovieMatcher>(); }
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
    protected ConcurrentDictionary<int, int> _memoryCache = new ConcurrentDictionary<int, int>();

    /// <summary>
    /// Contains the initialized FanArtTvWrapper.
    /// </summary>
    private FanArtTVWrapper _fanArt;

    #endregion

    /// <summary>
    /// Tries to lookup the Movie from FanArt.tv and downloads imges.
    /// </summary>
    /// <param name="movieInfo">Movie to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateMovie(MovieInfo movieInfo)
    {
      // Try online lookup
      if (!Init())
        return false;

      int id = 0;
      if (movieInfo.MovieDbId > 0)
      {
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(movieInfo.MovieDbId, out id))
        {
          return true;
        }

        ScheduleDownload(movieInfo.MovieDbId);
        return true;
      }
      return false;
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

      // TODO: when updating track information is implemented, start here a job to do it
    }

    public override bool Init()
    {
      if (!base.Init())
        return false;

      if (_fanArt != null)
        return true;

      try
      {
        _fanArt = new FanArtTVWrapper();
        bool res = _fanArt.Init();
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

    protected override void DownloadFanArt(int tmDbid)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMovieMatcher Download: Started for ID {0}", tmDbid);

        if (!Init())
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMovieMatcher Download: Begin saving banners for ID {0}", tmDbid);
        _fanArt.DownloadMovieBanners(tmDbid.ToString());

        // Save Posters
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMovieMatcher Download: Begin saving posters for ID {0}", tmDbid);
        _fanArt.DownloadMoviePosters(tmDbid.ToString());

        // Save FanArt
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMovieMatcher Download: Begin saving fanarts for ID {0}", tmDbid);
        _fanArt.DownloadMovieFanArt(tmDbid.ToString());

        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMovieMatcher Download: Finished ID {0}", tmDbid);

        MovieMatch onlineMatch = new MovieMatch
        {
          ItemName = tmDbid.ToString(),
          Id = tmDbid
        };

        // Save cache
        _storage.TryAddMatch(onlineMatch);

        // Remember we are finished
        FinishDownloadFanArt(tmDbid);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMovieMatcher: Exception downloading FanArt for ID {0}", ex, tmDbid);
      }
    }
  }
}
