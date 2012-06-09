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
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Runtime;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.TheTvDB;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="SeriesTvDbMatcher"/> is used to look up online series information from TheTvDB.com.
  /// </summary>
  public class SeriesTvDbMatcher
  {
    #region Static instance

    private static SeriesTvDbMatcher _instance;
    public static SeriesTvDbMatcher Instance
    {
      get { return _instance ?? (_instance = new SeriesTvDbMatcher()); }
    }

    #endregion

    #region Constants

    public const int MAX_FANART_IMAGES = 5;
    public const int MAX_FANART_DOWNLOADERS = 3;

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TvDB\");
    protected static string SETTINGS_MATCHES = Path.Combine(CACHE_PATH, "Matches.xml");

    #endregion

    #region Fields

    protected Dictionary<string, TvdbSeries> _memoryCache = new Dictionary<string, TvdbSeries>();

    /// <summary>
    /// Locking object to access settings.
    /// </summary>
    protected object _syncObj = new object();

    /// <summary>
    /// Contains the Series ID for Downloading FanArt asynchronously.
    /// </summary>
    protected EventedQueue<int> _downloadQueue = new EventedQueue<int>();
    protected List<Thread> _downloadThreads = new List<Thread>(MAX_FANART_DOWNLOADERS);
    protected bool _downloadAllowed = true;

    /// <summary>
    /// Contains the initialized TvDbWrapper.
    /// </summary>
    private TvDbWrapper _tv;

    private AsynchronousMessageQueue _messageQueue;

    #endregion

    public SeriesTvDbMatcher ()
    {
      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new[] { SystemMessaging.CHANNEL });
      _messageQueue.Start();
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL &&
        ((SystemMessaging.MessageType) message.MessageType) == SystemMessaging.MessageType.SystemStateChanged)
      {
        SystemState newState = (SystemState) message.MessageData[SystemMessaging.NEW_STATE];
        if (newState == SystemState.ShuttingDown)
          EndDownloads();
      }
    }
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
        return true;
      }

      episode = seriesDetail.Episodes.Find(e => e.EpisodeNumber == seriesInfo.EpisodeNumbers.FirstOrDefault() && e.SeasonNumber == seriesInfo.SeasonNumber);
      if (episode != null)
      {
        seriesInfo.Episode = episode.EpisodeName;
        return true;
      }
      return false;
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
        matches = Settings.Load<List<SeriesMatch>>(SETTINGS_MATCHES) ?? new List<SeriesMatch>();

      // Use cached values before doing online query
      SeriesMatch match = matches.Find(m => m.SeriesName == seriesName || m.TvDBName == seriesName);
      if (match != null && match.TvDBID != 0)
      {
        tvDbId = match.TvDBID;
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
          matches = Settings.Load<List<SeriesMatch>>(SETTINGS_MATCHES) ?? new List<SeriesMatch>();

        // Init empty
        seriesDetail = null;

        // Use cached values before doing online query
        SeriesMatch match = matches.Find(m => m.SeriesName == seriesName || m.TvDBName == seriesName);
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesName, match != null && match.TvDBID != 0);

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known series, only return the series details (including episodes).
        if (match != null)
          return match.TvDBID != 0 && _tv.GetSeries(match.TvDBID, true, out seriesDetail);

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
                  SeriesName = seriesName,
                  TvDBID = seriesDetail.Id,
                  TvDBName = seriesDetail.SeriesName
                };

            // Save cache
            SaveNewMatch(seriesName, onlineMatch);
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: No unique match found for \"{0}\"", seriesName);
        // Also save "non matches" to avoid retrying
        SaveNewMatch(seriesName, new SeriesMatch { SeriesName = seriesName });
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

    private void SaveNewMatch(string seriesName, SeriesMatch onlineMatch)
    {
      lock (_syncObj)
      {
        List<SeriesMatch> matches = Settings.Load<List<SeriesMatch>>(SETTINGS_MATCHES) ?? new List<SeriesMatch>();
        if (matches.All(m => m.SeriesName != seriesName))
          matches.Add(onlineMatch);
        Settings.Save(SETTINGS_MATCHES, matches);
      }
    }

    private bool Init()
    {
      if (_tv != null)
        return true;

      _tv = new TvDbWrapper();
      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _tv.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      return _tv.Init();
    }

    public bool ScheduleDownload(int tvDbId)
    {
      bool fanArtDownloaded = CheckBeginDownloadFanArt(tvDbId);
      if (fanArtDownloaded)
        return true;

      lock (_downloadQueue.SyncObj)
      {
        bool newEnqueued = _downloadQueue.TryEnqueue(tvDbId);
        if (newEnqueued && _downloadThreads.Count < _downloadThreads.Capacity)
        {
          Thread downloader = new Thread(DownloadFanArtQueue) { Name = "FanArt Downloader " + _downloadThreads.Count, Priority = ThreadPriority.Lowest };
          downloader.Start();
          _downloadThreads.Add(downloader);
        }
      }
      return true;
    }

    public void EndDownloads()
    {
      lock (_downloadQueue.SyncObj)
      {
        _downloadQueue.Clear();
        _downloadAllowed = false;
      }
      foreach (Thread downloadThread in _downloadThreads)
        if (!downloadThread.Join(2000))
          downloadThread.Abort();

      _downloadThreads.Clear();
    }

    // TODO: implement lookup table and download stats in database
    private bool CheckBeginDownloadFanArt(int tvDbId)
    {
      bool fanArtDownloaded = false;
      lock (_syncObj)
      {
        // Load cache or create new list
        List<SeriesMatch> matches = Settings.Load<List<SeriesMatch>>(SETTINGS_MATCHES) ?? new List<SeriesMatch>();
        foreach (SeriesMatch seriesMatch in matches.FindAll(m => m.TvDBID == tvDbId))
        {
          // We can have multiple matches for one TvDbId in list, if one has FanArt downloaded already, update the flag for all matches.
          if (seriesMatch.FanArtDownloadFinished.HasValue)
            fanArtDownloaded = true;

          if (!seriesMatch.FanArtDownloadStarted.HasValue)
            seriesMatch.FanArtDownloadStarted = DateTime.Now;
        }
        Settings.Save(SETTINGS_MATCHES, matches);
      }
      return fanArtDownloaded;
    }

    private bool FinishDownloadFanArt(int tvDbId)
    {
      lock (_syncObj)
      {
        // Load cache or create new list
        List<SeriesMatch> matches = Settings.Load<List<SeriesMatch>>(SETTINGS_MATCHES) ?? new List<SeriesMatch>();
        foreach (SeriesMatch seriesMatch in matches.FindAll(m => m.TvDBID == tvDbId))
          if (!seriesMatch.FanArtDownloadFinished.HasValue)
            seriesMatch.FanArtDownloadFinished = DateTime.Now;

        Settings.Save(SETTINGS_MATCHES, matches);
      }
      return true;
    }

    protected void DownloadFanArtQueue()
    {
      while (_downloadAllowed)
      {
        _downloadQueue.OnEnqueued.WaitOne(1000);
        int tvDbId;
        lock (_downloadQueue.SyncObj)
        {
          if (_downloadQueue.Count == 0)
            continue;
          tvDbId = _downloadQueue.Dequeue();
        }
        DownloadFanArt(tvDbId);
      }
    }

    protected void DownloadFanArt(int tvDbId)
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

  public class EventedQueue<T> : Queue<T>
  {
    protected readonly HashSet<T> _index = new HashSet<T>();
    public readonly AutoResetEvent OnEnqueued = new AutoResetEvent(false);
    public readonly object SyncObj = new object();

    public new virtual void Enqueue(T item)
    {
      if (_index.Contains(item))
        return;

      _index.Add(item);
      base.Enqueue(item);
      OnEnqueued.Set();
    }
    public virtual bool TryEnqueue(T item)
    {
      if (_index.Contains(item))
        return false;

      _index.Add(item);
      base.Enqueue(item);
      OnEnqueued.Set();
      return true;
    }
  }
}