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
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data;
using System.Linq;
using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="SeriesFanArtTvMatcher"/> is used to download series images from FanArt.tv.
  /// </summary>
  public class SeriesFanArtTvMatcher : BaseMatcher<SeriesMatch, string>
  {
    #region Static instance

    public static SeriesFanArtTvMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesFanArtTvMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArtTV\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "SeriesMatches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, int> _memoryCache = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized FanArtTvWrapper.
    /// </summary>
    private FanArtTVWrapper _fanArt;

    #endregion

    /// <summary>
    /// Tries to lookup the series from FanArt.tv and downloads images.
    /// </summary>
    /// <param name="episodeInfo">Series to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateSeries(EpisodeInfo episodeInfo)
    {
      // Try online lookup
      if (!Init())
        return false;

      if (episodeInfo.TvdbId > 0)
      {
        int tvDbId = 0;
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(episodeInfo.TvdbId.ToString(), out tvDbId))
        {
          return true;
        }

        ScheduleDownload(episodeInfo.TvdbId.ToString());
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

    protected override void DownloadFanArt(string tvDbId)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Started for ID {0}", tvDbId);

        if (!Init())
          return;

        TVThumbs thumbs = _fanArt.GetSeriesFanArt(tvDbId);
        if (thumbs == null)
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Begin saving banners for ID {0}", tvDbId);
        SaveBanners(tvDbId, thumbs.SeriesBanners.OrderByDescending(b => b.Likes).ToList(), "Banners");

        // Save Season Banners
        foreach (int season in thumbs.SeasonBanners.Select(b => b.Season).Distinct().ToList())
          SaveBanners(tvDbId, thumbs.SeasonBanners.FindAll(b => b.Season == season).OrderByDescending(b => b.Likes).ToList<MovieThumb>(), 
            string.Format(@"Banners\Season {0}", season));

        // Save Posters
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Begin saving posters for ID {0}", tvDbId);
        SaveBanners(tvDbId, thumbs.SeriesPosters.OrderByDescending(b => b.Likes).ToList(), "Posters");

        // Save Season Posters
        foreach (int season in thumbs.SeasonBanners.Select(b => b.Season).Distinct().ToList())
          SaveBanners(tvDbId, thumbs.SeasonPosters.FindAll(b => b.Season == season).OrderByDescending(b => b.Likes).ToList<MovieThumb>(),
            string.Format(@"Posters\Season {0}", season));

        // Save FanArt
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Begin saving fanarts for ID {0}", tvDbId);
        SaveBanners(tvDbId, thumbs.SeriesFanArt.OrderByDescending(b => b.Likes).ToList(), "Backdrops");

        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Finished ID {0}", tvDbId);

        SeriesMatch onlineMatch = new SeriesMatch
        {
          ItemName = tvDbId.ToString(),
          Id = tvDbId
        };

        // Save cache
        _storage.TryAddMatch(onlineMatch);

        // Remember we are finished
        FinishDownloadFanArt(tvDbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher: Exception downloading FanArt for ID {0}", ex, tvDbId);
      }
    }

    private int SaveBanners(string id, IEnumerable<MovieThumb> banners, string category)
    {
      if (banners == null)
        return 0;

      int idx = 0;
      foreach (MovieThumb banner in banners.Where(b => b.Language == null || b.Language == _fanArt.PreferredLanguage))
      {
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_fanArt.DownloadFanArt(id, banner, category))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Saved {0} {1}", idx, category);
      return idx;
    }
  }
}
