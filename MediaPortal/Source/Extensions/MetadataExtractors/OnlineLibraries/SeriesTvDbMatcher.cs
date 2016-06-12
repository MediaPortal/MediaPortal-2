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
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.TheTvDB;
using MediaPortal.Utilities;

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
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(1);
    protected static TimeSpan MIN_REFRESH_INTERVAL = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected DateTime _lastRefresh = DateTime.MinValue;
    protected ConcurrentDictionary<string, TvdbSeries> _memoryCache = new ConcurrentDictionary<string, TvdbSeries>(StringComparer.OrdinalIgnoreCase);
    protected bool _useUniversalLanguage = false; // Universal language often leads to unwanted cover languages (i.e. russian)

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

      if (TryMatch(seriesInfo, false, out seriesDetail))
      {
        int tvDbId = 0;
        if (seriesDetail != null)
        {
          tvDbId = seriesDetail.Id;
          seriesInfo.Series = seriesDetail.SeriesName;
          seriesInfo.Actors.Clear();
          if (seriesDetail.Actors.Count > 0)
            CollectionUtils.AddAll(seriesInfo.Actors, seriesDetail.Actors);
          seriesInfo.Genres.Clear();
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
      TvdbEpisode episode;
      List<TvdbEpisode> episodes = seriesDetail.Episodes.FindAll(e => e.EpisodeName == seriesInfo.Episode);
      // In few cases there can be multiple episodes with same name. In this case we cannot know which one is right
      // and keep the current episode details.
      // Use this way only for single episodes.
      if (seriesInfo.EpisodeNumbers.Count == 1 && episodes.Count == 1)
      {
        episode = episodes[0];
        seriesInfo.ImdbId = seriesDetail.ImdbId;
        seriesInfo.TvdbId = seriesDetail.Id;
        seriesInfo.SeasonNumber = episode.SeasonNumber;
        seriesInfo.EpisodeNumbers.Clear();
        seriesInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
        seriesInfo.FirstAired = episode.FirstAired;
        seriesInfo.DvdEpisodeNumbers.Clear();
        seriesInfo.DvdEpisodeNumbers.Add(episode.DvdEpisodeNumber);
        SetEpisodeDetails(seriesInfo, episode);
        return true;
      }

      episodes = seriesDetail.Episodes.Where(e => seriesInfo.EpisodeNumbers.Contains(e.EpisodeNumber) && e.SeasonNumber == seriesInfo.SeasonNumber).ToList();
      if (episodes.Count == 0)
        return false;

      // Single episode entry
      if (episodes.Count == 1)
      {
        episode = episodes[0];
        seriesInfo.ImdbId = seriesDetail.ImdbId;
        seriesInfo.TvdbId = seriesDetail.Id;
        seriesInfo.FirstAired = episode.FirstAired;
        seriesInfo.Episode = episode.EpisodeName;
        SetEpisodeDetails(seriesInfo, episode);
        return true;
      }

      // Multiple episodes
      SetMultiEpisodeDetails(seriesInfo, episodes);
      return true;
    }

    private static void SetMultiEpisodeDetails(SeriesInfo seriesInfo, List<TvdbEpisode> episodes)
    {
      seriesInfo.TotalRating = episodes.Sum(e => e.Rating) / episodes.Count; // Average rating
      seriesInfo.Episode = string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.EpisodeName).ToArray());
      seriesInfo.Summary = string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Overview)).ToArray());

      seriesInfo.ImdbId = episodes.Min(e => e.ImdbId);
      seriesInfo.TvdbId = episodes.Min(e => e.Id);
      seriesInfo.FirstAired = episodes.Min(e => e.FirstAired);

      // Don't clear seriesInfo.Actors again. It's already been filled with actors from series details.
      var guestStars = episodes.SelectMany(e => e.GuestStars).Distinct().ToList();
      if (guestStars.Count > 0)
        CollectionUtils.AddAll(seriesInfo.Actors, guestStars);
      seriesInfo.Directors.Clear();
      var directors = episodes.SelectMany(e => e.Directors).Distinct().ToList();
      if (directors.Count > 0)
        CollectionUtils.AddAll(seriesInfo.Directors, directors);
      var writers = episodes.SelectMany(e => e.Writer).Distinct().ToList();
      seriesInfo.Writers.Clear();
      if (writers.Count > 0)
        CollectionUtils.AddAll(seriesInfo.Writers, writers);
    }

    private static void SetEpisodeDetails(SeriesInfo seriesInfo, TvdbEpisode episode)
    {
      seriesInfo.TotalRating = episode.Rating;
      seriesInfo.Summary = episode.Overview;
      // Don't clear seriesInfo.Actors again. It's already been filled with actors from series details.
      if (episode.GuestStars.Count > 0)
        CollectionUtils.AddAll(seriesInfo.Actors, episode.GuestStars);
      seriesInfo.Directors.Clear();
      if (episode.Directors.Count > 0)
        CollectionUtils.AddAll(seriesInfo.Directors, episode.Directors);
      seriesInfo.Writers.Clear();
      if (episode.Writer.Count > 0)
        CollectionUtils.AddAll(seriesInfo.Writers, episode.Writer);
    }

    protected bool TryGetId(string seriesName, out int tvDbId)
    {
      tvDbId = 0;
      // Prefer memory cache
      TvdbSeries seriesDetail;
      CheckCacheAndRefresh();
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
      SeriesMatch match = matches.Find(m => string.Equals(m.ItemName, seriesName, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(m.TvDBName, seriesName, StringComparison.OrdinalIgnoreCase));
      if (match != null && match.Id != 0)
      {
        tvDbId = match.Id;
        return true;
      }
      return false;
    }

    protected bool TryMatch(SeriesInfo seriesInfo, bool cacheOnly, out TvdbSeries seriesDetail)
    {
      // If series has an TVDBID, prefer it over imdb or name lookup.
      if (seriesInfo.TvdbId != 0 && TryMatch(seriesInfo.Series, false, cacheOnly, out seriesDetail, seriesInfo.TvdbId))
        return true;

      // If series has an IMDBID, prefer it over name lookup.
      string imdbId = seriesInfo.ImdbId;
      if (!string.IsNullOrWhiteSpace(imdbId) && TryMatch(imdbId, true, cacheOnly, out seriesDetail))
        return true;

      // Perform name lookup.
      return TryMatch(seriesInfo.Series, false, cacheOnly, out seriesDetail);
    }

    protected bool TryMatch(string seriesNameOrImdbId, bool isImdbId, bool cacheOnly, out TvdbSeries seriesDetail, int tvdbid = 0)
    {
      seriesDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(seriesNameOrImdbId, out seriesDetail))
        {
          if (tvdbid == 0 || seriesDetail.Id == tvdbid)
            return true;
        }

        // Load cache or create new list
        List<SeriesMatch> matches;
        lock (_syncObj)
          matches = Settings.Load<List<SeriesMatch>>(MatchesSettingsFile) ?? new List<SeriesMatch>();

        // Init empty
        seriesDetail = null;

        // Use cached values before doing online query
        SeriesMatch match = matches.Find(m =>
          (
          string.Equals(m.ItemName, seriesNameOrImdbId, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(m.TvDBName, seriesNameOrImdbId, StringComparison.OrdinalIgnoreCase)
          ) && (tvdbid == 0 || m.Id == tvdbid));

        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesNameOrImdbId, match != null && match.Id != 0);

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known series, only return the series details (including episodes).
        if (match != null)
          return match.Id != 0 && _tv.GetSeries(match.Id, true, out seriesDetail);

        if (cacheOnly)
          return false;

        TvdbSearchResult matchedSeries = null;
        bool foundResult = false;
        if (tvdbid != 0)
        {
          foundResult = _tv.GetSeries(tvdbid, true, out seriesDetail);
        }
        else
          if (isImdbId)
          {
            // If we got an IMDBID, use it to lookup by key directly
            _tv.GetSeries(seriesNameOrImdbId, out matchedSeries);
          }
          else
          {
            // Otherwise we try to find unique series by name
            List<TvdbSearchResult> series;
            if (_tv.SearchSeriesUnique(seriesNameOrImdbId, out series))
              matchedSeries = series[0];
          }

        if (matchedSeries != null)
        {
          ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Found unique online match for \"{0}\": \"{1}\" [Lang: {2}]", seriesNameOrImdbId, matchedSeries.SeriesName, matchedSeries.Language);
          foundResult = _tv.GetSeries(matchedSeries.Id, true, out seriesDetail);
        }
        if (foundResult)
        {
          ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Loaded details for \"{0}\"", seriesDetail.SeriesName);
          // Add this match to cache
          SeriesMatch onlineMatch = new SeriesMatch
              {
                ItemName = seriesNameOrImdbId,
                Id = seriesDetail.Id,
                TvDBName = seriesDetail.SeriesName
              };

          // Save cache
          _storage.TryAddMatch(onlineMatch);
          return true;
        }

        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: No unique match found for \"{0}\"", seriesNameOrImdbId);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new SeriesMatch { ItemName = seriesNameOrImdbId });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing series {0}", ex, seriesNameOrImdbId);
        return false;
      }
      finally
      {
        if (seriesDetail != null)
          _memoryCache.TryAdd(seriesNameOrImdbId, seriesDetail);
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

      if (DateTime.Now - _lastRefresh <= MIN_REFRESH_INTERVAL)
        return;

      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>(false);
      if (threadPool != null)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Refreshing local cache");
        threadPool.Add(() =>
        {
          if (Init())
            _tv.UpdateCache();
        });
      }
      _lastRefresh = DateTime.Now;
    }

    public override bool Init()
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
        TvdbLanguage language = _tv.PreferredLanguage;
        SaveBanners(seriesDetail.SeriesBanners, language);

        // Save Season Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving season banners for ID {0}", tvDbId);
        // Build a key from Season number and banner type (season or seasonwide), so each combination is handled separately.
        var seasonLookup = seriesDetail.SeasonBanners.ToLookup(s => string.Format("{0}_{1}", s.Season, s.BannerType), v => v);
        foreach (IGrouping<string, TvdbSeasonBanner> tvdbSeasonBanners in seasonLookup)
          SaveBanners(seasonLookup[tvdbSeasonBanners.Key], language);

        // Save Posters
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving posters for ID {0}", tvDbId);
        SaveBanners(seriesDetail.PosterBanners, language);

        // Save FanArt
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving fanarts for ID {0}", tvDbId);
        SaveBanners(seriesDetail.FanartBanners, language);
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Finished ID {0}", tvDbId);

        // Remember we are finished
        FinishDownloadFanArt(tvDbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception downloading FanArt for ID {0}", ex, tvDbId);
      }
    }

    private int SaveBanners<TE>(IEnumerable<TE> banners, TvdbLanguage language) where TE : TvdbBanner
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
        if (_useUniversalLanguage)
        {
          idx = SaveBanners(banners, TvdbLanguage.UniversalLanguage);
          if (idx > 0)
            return idx;
        }

        idx = SaveBanners(banners, TvdbLanguage.DefaultLanguage);
      }
      return idx;
    }
  }
}
