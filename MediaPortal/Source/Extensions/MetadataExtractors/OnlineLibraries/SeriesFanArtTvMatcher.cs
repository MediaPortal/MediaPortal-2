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
    protected ConcurrentDictionary<string, EpisodeInfo> _memoryCache = new ConcurrentDictionary<string, EpisodeInfo>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized FanArtTvWrapper.
    /// </summary>
    private FanArtTVWrapper _fanArt;

    #endregion

    #region Metadata updaters

    /// <summary>
    /// Tries to lookup the series from FanArt.tv and downloads images.
    /// </summary>
    /// <param name="episodeInfo">Series to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateEpisode(EpisodeInfo episodeInfo)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        if (episodeInfo.SeriesTvdbId > 0)
        {
          EpisodeInfo oldEpisodeInfo;
          CheckCacheAndRefresh();
          if (_memoryCache.TryGetValue(episodeInfo.SeriesTvdbId.ToString(), out oldEpisodeInfo))
          {
            //Already downloaded
            return true;
          }

          if (_memoryCache.TryAdd(episodeInfo.SeriesTvdbId.ToString(), episodeInfo))
          {
            ScheduleDownload(episodeInfo.SeriesTvdbId.ToString());
            return true;
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher: Exception while processing episode {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeries(SeriesInfo seriesInfo)
    {
      try
      {
        if (seriesInfo.Thumbnail == null)
        {
          List<string> thumbs = GetFanArtFiles(seriesInfo, FanArtScope.Series, FanArtType.Posters);
          if (thumbs.Count > 0)
            seriesInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
        }
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher: Exception while processing series {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeason(SeasonInfo seasonInfo)
    {
      try
      {
        if (seasonInfo.Thumbnail == null && seasonInfo.SeasonNumber.HasValue)
        {
          List<string> thumbs = GetFanArtFiles(seasonInfo, FanArtScope.Season, FanArtType.Posters);
          if (thumbs.Count > 0)
            seasonInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
        }
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher: Exception while processing season {0}", ex, seasonInfo.ToString());
        return false;
      }
    }

    #endregion

    #region Metadata update helpers

    private void StoreSeriesMatch(EpisodeInfo episode)
    {
      SeriesInfo seriesMatch = new SeriesInfo()
      {
        Series = episode.Series,
        FirstAired = episode.SeriesFirstAired.HasValue ? episode.SeriesFirstAired.Value : default(DateTime?)
      };
      var onlineMatch = new SeriesMatch
      {
        Id = episode.TvdbId.ToString(),
        ItemName = seriesMatch.ToString(),
        OnlineName = seriesMatch.ToString()
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
      if (scope == FanArtScope.Series)
      {
        SeriesInfo series = infoObject as SeriesInfo;
        if (series != null && series.TvdbId > 0)
        {
          path = Path.Combine(CACHE_PATH, series.TvdbId.ToString(), string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtScope.Season)
      {
        SeasonInfo season = infoObject as SeasonInfo;
        if (season != null && season.SeriesTvdbId > 0 && season.SeasonNumber.HasValue)
        {
          path = Path.Combine(CACHE_PATH, season.SeriesTvdbId.ToString(), string.Format(@"{0} {1}\{2}\", scope, season.SeasonNumber, type));
        }
      }
      if (Directory.Exists(path))
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
      return fanartFiles;
    }

    protected override void DownloadFanArt(string tvDbId)
    {
      try
      {
        if (string.IsNullOrEmpty(tvDbId))
          return;

        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Started for ID {0}", tvDbId);

        EpisodeInfo episodeInfo;
        if (!_memoryCache.TryGetValue(tvDbId, out episodeInfo))
          return;

        if (episodeInfo.SeriesTvdbId <= 0)
          return;

        if (!Init())
          return;

        FanArtTVThumbs thumbs;
        if (!_fanArt.GetSeriesFanArt(episodeInfo.SeriesTvdbId.ToString(), out thumbs))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Begin saving banners for ID {0}", tvDbId);
        SaveBanners(tvDbId, thumbs.SeriesBanners.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Series, FanArtType.Banners));
        foreach (int season in thumbs.SeasonBanners.Select(b => b.Season).Distinct().ToList())
          SaveBanners(tvDbId, thumbs.SeasonBanners.FindAll(b => b.Season == season).OrderByDescending(b => b.Likes).ToList<FanArtMovieThumb>(),
            string.Format(@"{0} {1}\{2}", FanArtScope.Season, season, FanArtType.Banners));

        // Save Posters
        SaveBanners(tvDbId, thumbs.SeriesPosters.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Series, FanArtType.Posters));
        foreach (int season in thumbs.SeasonBanners.Select(b => b.Season).Distinct().ToList())
          SaveBanners(tvDbId, thumbs.SeasonPosters.FindAll(b => b.Season == season).OrderByDescending(b => b.Likes).ToList<FanArtMovieThumb>(),
            string.Format(@"{0} {1}\{2}", FanArtScope.Season, season, FanArtType.Posters));

        // Save Thumbnails
        SaveBanners(tvDbId, thumbs.SeriesThumbnails.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Series, FanArtType.Thumbnails));
        foreach (int season in thumbs.SeasonThumbnails.Where(b => b.Season != null).Select(b => b.Season).Distinct().ToList())
          SaveBanners(tvDbId, thumbs.SeasonThumbnails.FindAll(b => b.Season == season).OrderByDescending(b => b.Likes).ToList<FanArtMovieThumb>(),
            string.Format(@"{0} {1}\{2}", FanArtScope.Season, season, FanArtType.Thumbnails));

        // Save FanArt
        SaveBanners(tvDbId, thumbs.SeriesFanArt.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Series, FanArtType.Backdrops));
        SaveBanners(tvDbId, thumbs.HDSeriesClearArt.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Series, FanArtType.ClearArt));
        SaveBanners(tvDbId, thumbs.HDSeriesLogos.OrderByDescending(b => b.Likes).ToList(), string.Format(@"{0}\{1}", FanArtScope.Series, FanArtType.Logos));

        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Finished ID {0}", tvDbId);

        StoreSeriesMatch(episodeInfo);

        // Remember we are finished
        FinishDownloadFanArt(tvDbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher: Exception downloading FanArt for ID {0}", ex, tvDbId);
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
      ServiceRegistration.Get<ILogger>().Debug("SeriesFanArtTvMatcher Download: Saved {0} {1}", idx, category);
      return idx;
    }

    #endregion
  }
}
