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
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.TheTvDB;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Network;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="SeriesTvDbMatcher"/> is used to look up online series information from TheTvDB.com.
  /// </summary>
  public class SeriesTvDbMatcher : BaseMatcher<SeriesMatch, int>
  {
    #region Static instance

    public static SeriesTvDbMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesTvDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TvDB\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected Dictionary<string, TvdbSeries> _memoryCache = new Dictionary<string, TvdbSeries>();

    /// <summary>
    /// Contains the initialized TvDbWrapper.
    /// </summary>
    private TvDbWrapper _tv;

    #endregion

    /// <summary>
    /// Tries to lookup the series from TheTvDB and return the found ID.
    /// </summary>
    /// <param name="seriesName">Series name to check</param>
    /// <param name="tvDbId">Return the TvDB ID of series</param>
    /// <returns><c>true</c> if successful</returns>
    public bool TryGetTvDbId(string seriesName, out int tvDbId)
    {
      return TryGetId(seriesName, out tvDbId);
    }

    /// <summary>
    /// Tries to lookup the series from TheTvDB and updates the given <paramref name="seriesInfo"/> with the online information (Series and Episode names).
    /// </summary>
    /// <param name="seriesInfo">Series to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateSeries(SeriesInfo seriesInfo)
    {
      TvdbSeries seriesDetail;
      if (TryMatch(seriesInfo.Series, false, out seriesDetail))
      {
        int tvDbId = 0;
        if (seriesDetail != null)
        {
          tvDbId = seriesDetail.Id;
          seriesInfo.Series = seriesDetail.SeriesName;
          if (seriesDetail.Actors.Count > 0)
            CollectionUtils.AddAll(seriesInfo.Actors, seriesDetail.Actors);
          if (seriesDetail.Genre.Count > 0)
            CollectionUtils.AddAll(seriesInfo.Genres, seriesDetail.Genre);

          // Also try to fill episode title from series details (most file names don't contain episode name).
          TryMatchEpisode(seriesInfo, seriesDetail);
        }

        if (tvDbId > 0)
          ScheduleDownload(tvDbId);
        return true;
      }
      return false;
    }

    protected bool TryMatchEpisode(SeriesInfo seriesInfo, TvdbSeries seriesDetail)
    {
      // We deal with two scenarios here:
      //  - Having a real episode title, but the Season/Episode numbers might be wrong (seldom case)
      //  - Having only Season/Episode numbers and we need to fill Episode title (more common)
      TvdbEpisode episode = seriesDetail.Episodes.Find(e => e.EpisodeName == seriesInfo.Episode);
      if (episode != null)
      {
        seriesInfo.SeasonNumber = episode.SeasonNumber;
        seriesInfo.EpisodeNumbers.Clear();
        seriesInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
        SetEpisodeDetails(seriesInfo, episode);
        return true;
      }

      episode = seriesDetail.Episodes.Find(e => e.EpisodeNumber == seriesInfo.EpisodeNumbers.FirstOrDefault() && e.SeasonNumber == seriesInfo.SeasonNumber);
      if (episode != null)
      {
        seriesInfo.Episode = episode.EpisodeName;
        SetEpisodeDetails(seriesInfo, episode);
        return true;
      }
      return false;
    }

    private static void SetEpisodeDetails(SeriesInfo seriesInfo, TvdbEpisode episode)
    {
      seriesInfo.Summary = episode.Overview;
      if (episode.Directors.Count > 0)
        CollectionUtils.AddAll(seriesInfo.Directors, episode.Directors);
      if (episode.GuestStars.Count > 0)
        CollectionUtils.AddAll(seriesInfo.Actors, episode.GuestStars);
    }

    protected bool TryGetId(string seriesName, out int tvDbId)
    {
      tvDbId = 0;
      // Prefer memory cache
      TvdbSeries seriesDetail;
      if (_memoryCache.TryGetValue(seriesName, out seriesDetail))
      {
        tvDbId = seriesDetail.Id;
        return true;
      }

      // Load cache or create new list
      List<SeriesMatch> matches;
      lock (_syncObj)
        matches = Settings.Load<List<SeriesMatch>>(MatchesSettingsFile) ?? new List<SeriesMatch>();

      // Use cached values before doing online query
      SeriesMatch match = matches.Find(m => m.ItemName == seriesName || m.TvDBName == seriesName);
      if (match != null && match.Id != 0)
      {
        tvDbId = match.Id;
        return true;
      }
      return false;
    }

    protected bool TryMatch(string seriesName, bool cacheOnly, out TvdbSeries seriesDetail)
    {
      seriesDetail = null;
      try
      {
        // Prefer memory cache
        if (_memoryCache.TryGetValue(seriesName, out seriesDetail))
          return true;

        // Load cache or create new list
        List<SeriesMatch> matches;
        lock (_syncObj)
          matches = Settings.Load<List<SeriesMatch>>(MatchesSettingsFile) ?? new List<SeriesMatch>();

        // Init empty
        seriesDetail = null;

        // Use cached values before doing online query
        SeriesMatch match = matches.Find(m => m.ItemName == seriesName || m.TvDBName == seriesName);
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesName, match != null && match.Id != 0);

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known series, only return the series details (including episodes).
        if (match != null)
          return match.Id != 0 && _tv.GetSeries(match.Id, true, out seriesDetail);

        if (cacheOnly)
          return false;

        List<TvdbSearchResult> series;
        if (_tv.SearchSeriesUnique(seriesName, out series))
        {
          TvdbSearchResult matchedSeries = series[0];
          ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Found unique online match for \"{0}\": \"{1}\" [Lang: {2}]", seriesName, matchedSeries.SeriesName, matchedSeries.Language);

          if (_tv.GetSeries(matchedSeries.Id, true, out seriesDetail))
          {
            ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Loaded details for \"{0}\"", matchedSeries.SeriesName);
            // Add this match to cache
            SeriesMatch onlineMatch = new SeriesMatch
                {
                  ItemName = seriesName,
                  Id = seriesDetail.Id,
                  TvDBName = seriesDetail.SeriesName
                };

            // Save cache
            _storage.SaveNewMatch(seriesName, onlineMatch);
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: No unique match found for \"{0}\"", seriesName);
        // Also save "non matches" to avoid retrying
        _storage.SaveNewMatch(seriesName, new SeriesMatch { ItemName = seriesName });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing series {0}", ex, seriesName);
        return false;
      }
      finally
      {
        if (seriesDetail != null && !_memoryCache.ContainsKey(seriesName))
          _memoryCache.Add(seriesName, seriesDetail);
      }
    }

    protected override bool Init()
    {
      if (!base.Init())
        return false;

      if (_tv != null)
        return true;
      try
      {
        TvDbWrapper tv = new TvDbWrapper();
        // Try to lookup online content in the configured language
        CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
        tv.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
        bool res = tv.Init();
        _tv = tv;
        return res;
      }
      catch (Libraries.TvdbLib.Exceptions.TvdbNotAvailableException)
      {
        return false;
      }
    }

    protected override void DownloadFanArt(int tvDbId)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Started for ID {0}", tvDbId);

        if (!Init())
          return;

        TvdbSeries seriesDetail;
        if (!_tv.GetSeriesFanArt(tvDbId, out seriesDetail))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving banners for ID {0}", tvDbId);
        SaveBanners(seriesDetail.SeriesBanners, _tv.PreferredLanguage);

        // Save Posters
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving posters for ID {0}", tvDbId);
        SaveBanners(seriesDetail.PosterBanners, _tv.PreferredLanguage);

        // Save FanArt
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving fanarts for ID {0}", tvDbId);
        SaveBanners(seriesDetail.FanartBanners, _tv.PreferredLanguage);
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Finished ID {0}", tvDbId);

        // Remember we are finished
        FinishDownloadFanArt(tvDbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception downloading FanArt for ID {0}", ex, tvDbId);
      }
    }

    private static int SaveBanners<TE>(IEnumerable<TE> banners, TvdbLanguage language) where TE : TvdbBanner
    {
      int idx = 0;
      foreach (TE tvdbBanner in banners)
      {
        if (tvdbBanner.Language != language)
          continue;

        if (idx++ >= MAX_FANART_IMAGES)
          break;

        if (!tvdbBanner.IsLoaded)
        {
          // We need the image only loaded once, later we will access the cache directly
          try
          {
            tvdbBanner.LoadBanner();
            tvdbBanner.UnloadBanner();
          }
          catch (Exception ex)
          {
            ServiceRegistration.Get<ILogger>().Warn("SeriesTvDbMatcher: Exception saving FanArt image", ex);
          }
        }
      }
      if (idx > 0)
        return idx;

      // Try fallback languages if no images found for preferred
      if (language != TvdbLanguage.UniversalLanguage && language != TvdbLanguage.DefaultLanguage)
      {
        idx = SaveBanners(banners, TvdbLanguage.UniversalLanguage);
        if (idx > 0)
          return idx;

        idx = SaveBanners(banners, TvdbLanguage.DefaultLanguage);
      }
      return idx;
    }
  }
}