﻿/*
 *   TvdbLib: A library to retrieve information and media from http://thetvdb.com
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Cache;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Comparer;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Exceptions;
using MediaPortal.Utilities.Cache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib
{
  /// <summary>
  /// Tvdb Handler for handling all features that are available on http://thetvdb.com/
  /// 
  /// http://thetvdb.com/ is an open database that can be modified by anybody. All content and images on the site have been contributed by our users. The database schema and website are open source under the GPL, and are available at Sourceforge.
  /// The site also has a full XML API that allows other software and websites to use this information. The API is currently being used by the myTV add-in for Windows Media Center, XBMC (formerly XBox Media Center); the meeTVshows and TVNight plugins for Meedio; the MP-TVSeries plugin for MediaPortal; Boxee; and many more.
  /// </summary>
  public class TvdbHandler
  {
    #region private fields

    /// <summary>
    /// Default timeout for the cache is 2 minutes
    /// </summary>
    private static readonly TimeSpan CACHE_TIMEOUT = new TimeSpan(0, 2, 0);

    /// <summary>
    /// Cache used to temporarily store <see cref="TvdbSeries"/> objects so that the same cache file
    /// doesn't have to be parsed every time a series is loaded.
    /// </summary>
    private readonly AsyncStaticTimeoutCache<int, TvdbSeries> _memoryCache = new AsyncStaticTimeoutCache<int, TvdbSeries>(CACHE_TIMEOUT);

    private SemaphoreSlim _seriesLoadingSync = new SemaphoreSlim(1, 1);
    private readonly ICacheProvider _cacheProvider;
    private readonly String _apiKey;
    private readonly TvdbDownloader _downloader;
    private TvdbUser _userInfo;
    private TvdbData _loadedData;
    private bool _abortUpdate = false;
    private bool _abortUpdateSaveChanges = false;
    #endregion

    #region events

    /// <summary>
    /// EventArgs used when a running update progresses, contains information on the
    /// current stage and progress
    /// </summary>
    public class UpdateProgressEventArgs : EventArgs
    {
      /// <summary>
      /// Constructor for UpdateProgressEventArgs
      /// </summary>
      /// <param name="currentUpdateStage">The current state of the updating progress</param>
      /// <param name="currentUpdateDescription">Description of the current update stage</param>
      /// <param name="currentStageProgress">Progress of the current stage</param>
      /// <param name="overallProgress">Overall progress of the update</param>
      public UpdateProgressEventArgs(UpdateStage currentUpdateStage, String currentUpdateDescription,
                                     int currentStageProgress, int overallProgress)
      {
        CurrentUpdateStage = currentUpdateStage;
        CurrentUpdateDescription = currentUpdateDescription;
        CurrentStageProgress = currentStageProgress;
        OverallProgress = overallProgress;
      }

      /// <summary>
      /// The current state of the updating progress
      /// </summary>
      public enum UpdateStage
      {
        /// <summary>
        /// We're currently downloading the update files from http://thetvdb.com
        /// </summary>
        Downloading = 0,
        /// <summary>
        /// We're currently processing the updated series
        /// </summary>
        SeriesUpdate = 1,
        /// <summary>
        /// We're currently processing the updated episodes
        /// </summary>
        EpisodesUpdate = 2,
        /// <summary>
        /// We're currently processing the updated banner
        /// </summary>
        BannerUpdate = 3,
        /// <summary>
        /// The updating itself has finished, do cleanup work
        /// </summary>
        FinishUpdate = 4
      };

      /// <summary>
      /// Current state of update progress
      /// </summary>
      public UpdateStage CurrentUpdateStage { get; set; }

      /// <summary>
      /// Description of the current update stage
      /// </summary>
      public String CurrentUpdateDescription { get; set; }

      /// <summary>
      /// Progress of the current stage
      /// </summary>
      public int CurrentStageProgress { get; set; }

      /// <summary>
      /// Overall progress of the update
      /// </summary>
      public int OverallProgress { get; set; }
    }

    /// <summary>
    /// EventArgs used when an update has finished, contains start date, end date and 
    /// an overview of all updated content
    /// </summary>
    public class UpdateFinishedEventArgs : EventArgs
    {
      /// <summary>
      /// Constructor for UpdateFinishedEventArgs
      /// </summary>
      /// <param name="started">When did the update start</param>
      /// <param name="ended">When did the update finish</param>
      /// <param name="updatedSeries">List of all series (ids) that were updated</param>
      /// <param name="updatedEpisodes">List of all episode (ids)that were updated</param>
      /// <param name="updatedBanners">List of all banners (ids) that were updated</param>
      public UpdateFinishedEventArgs(DateTime started, DateTime ended, List<int> updatedSeries,
                                     List<int> updatedEpisodes, List<int> updatedBanners)
      {
        UpdateStarted = started;
        UpdateFinished = ended;
        UpdatedSeries = updatedSeries;
        UpdatedEpisodes = updatedEpisodes;
        UpdatedBanners = updatedBanners;
      }
      /// <summary>
      /// When did the update start
      /// </summary>
      public DateTime UpdateStarted { get; set; }

      /// <summary>
      /// When did the update finish
      /// </summary>
      public DateTime UpdateFinished { get; set; }

      /// <summary>
      /// List of all series (ids) that were updated
      /// </summary>
      public List<int> UpdatedSeries { get; set; }

      /// <summary>
      /// List of all episode (ids)that were updated
      /// </summary>
      public List<int> UpdatedEpisodes { get; set; }

      /// <summary>
      /// List of all banners (ids) that were updated
      /// </summary>
      public List<int> UpdatedBanners { get; set; }
    }

    /// <summary>
    /// Delegate for UpdateProgressed Event
    /// </summary>
    /// <param name="_event">EventArgs</param>
    public delegate void UpdateProgressDelegate(UpdateProgressEventArgs _event);

    /// <summary>
    /// Called whenever an running update makes any progress
    /// </summary>
    public event UpdateProgressDelegate UpdateProgressed;

    /// <summary>
    /// Delegate for UpdateFinished event
    /// </summary>
    /// <param name="_event">EventArgs</param>
    public delegate void UpdateFinishedDelegate(UpdateFinishedEventArgs _event);

    /// <summary>
    /// Called when a running update finishes, UpdateFinishedEventArgs gives an overview
    /// of the update
    /// </summary>
    public event UpdateFinishedDelegate UpdateFinished;
    #endregion

    /// <summary>
    /// UserInfo for this tvdb handler
    /// </summary>
    public TvdbUser UserInfo
    {
      get { return _userInfo; }
      set
      {
        _userInfo = value;
        if (_cacheProvider == null)
          return;
        //try to load the userinfo from cache
        TvdbUser user = _cacheProvider.LoadUserInfoFromCache(value.UserIdentifier);
        if (user == null)
          return;
        _userInfo.UserFavorites = user.UserFavorites;
        _userInfo.UserPreferredLanguage = user.UserPreferredLanguage;
        _userInfo.UserName = user.UserName;
      }
    }

    /// <summary>
    /// Unique id for every project that is using thetvdb
    /// 
    /// More information on: http://thetvdb.com/wiki/index.php/Programmers_API
    /// </summary>
    public String ApiKey
    {
      get { return _apiKey; }
    }

    /// <summary>
    /// <para>Creates a new Tvdb handler</para>
    /// <para>The tvdb handler is used not only for downloading data from thetvdb but also to cache the downloaded data to a persistent storage,
    ///       handle user specific tasks and keep the downloaded data consistent with the online data (via the updates api)</para>
    /// <para>An api key is used for downloading data from thetvdb -> see http://thetvdb.com/wiki/index.php/Programmers_API</para>
    /// </summary>
    public TvdbHandler(string apiKey, bool useHttps)
    {
      _apiKey = apiKey; //store api key
      _downloader = new TvdbDownloader(_apiKey, useHttps);
      _cacheProvider = null;
    }

    /// <summary>
    /// Creates a new Tvdb handler
    /// </summary>
    /// <param name="cacheProvider">The cache provider used to store the information</param>
    /// <param name="apiKey">Api key to use for this project</param>
    public TvdbHandler(string apiKey, bool useHttps, ICacheProvider cacheProvider)
    {
      _apiKey = apiKey; //store api key
      _downloader = new TvdbDownloader(_apiKey, useHttps);
      _cacheProvider = cacheProvider; //store given cache provider
    }

    /// <summary>
    /// Load previously stored information on (except series information) from cache
    /// </summary>
    /// <returns>true if cache could be loaded successfully, false otherwise</returns>
    public bool InitCache()
    {
      if (_cacheProvider != null)
      {
        if (!_cacheProvider.Initialised)
        {
          TvdbData data = _cacheProvider.InitCache();
          if (data != null)
          {//cache provider was initialised successfully
            _loadedData = data;
            return true;
          }
          //couldn't init the cache provider
          return false;
        }
        return _loadedData != null;
      }
      return false;
    }

    /// <summary>
    /// Is the handler using caching and is the cache initialised
    /// </summary>
    public bool IsCacheInitialised
    {
      get { return _cacheProvider != null && _cacheProvider.Initialised; }
    }


    /// <summary>
    /// Completely refreshes the cache (all stored information is lost) -> cache
    /// must be initialised to call this method
    /// </summary>
    /// <returns>true if the cache was cleared successfully, 
    ///          false otherwise (e.g. no write rights,...)</returns>
    public bool ClearCache()
    {
      return _cacheProvider != null && _cacheProvider.Initialised && _cacheProvider.ClearCache();
    }

    /// <summary>
    /// Search for a seris on tvdb using the name of the series using the default language (english)
    /// </summary>
    /// <param name="name">Name of series</param>
    /// <returns>List of possible hits (containing only very basic information (id, name,....)</returns>
    public async Task<List<TvdbSearchResult>> SearchSeriesAsync(String name)
    {
      List<TvdbSearchResult> retSeries = await _downloader.DownloadSearchResultsAsync(name).ConfigureAwait(false);
      if (retSeries != null && retSeries.Count == 0)
        return null;
      return retSeries;
    }

    /// <summary>
    /// Search for a seris on tvdb using the name of the series
    /// </summary>
    /// <param name="name">Name of series</param>
    /// <param name="language">Language to search in</param>
    /// <returns>List of possible hits (containing only very basic information (id, name,....)</returns>
    public async Task<List<TvdbSearchResult>> SearchSeriesAsync(String name, TvdbLanguage language)
    {
      List<TvdbSearchResult> retSeries = await _downloader.DownloadSearchResultsAsync(name, language).ConfigureAwait(false);
      if (retSeries != null && retSeries.Count == 0)
        return null;
      return retSeries;
    }

    /// <summary>
    /// Searches for a series by the id of an external provider
    /// </summary>
    /// <param name="externalSite">external provider</param>
    /// <param name="id">id of the series</param>
    /// <returns>The tvdb series that corresponds to the external id</returns>
    public Task<TvdbSearchResult> GetSeriesByRemoteIdAsync(ExternalId externalSite, String id)
    {
      return _downloader.DownloadSeriesSearchByExternalIdAsync(externalSite, id);
    }

    /// <summary>
    /// Gets the series with the given id either from cache (if it has already been loaded) or from 
    /// the selected tvdb mirror.
    /// 
    /// To check if this series has already been cached, use the Method IsCached(TvdbSeries _series)
    /// </summary>
    /// <exception cref="TvdbNotAvailableException">Tvdb is not available</exception>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <param name="seriesId">id of series</param>
    /// <param name="language">language that should be retrieved</param>
    /// <param name="loadEpisodes">if true, the full series record will be loaded (series + all episodes), otherwise only the base record will be loaded which contains only series information</param>
    /// <param name="loadActors">if true also loads the extended actor information</param>
    /// <param name="loadBanners">if true also loads the paths to the banners</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    public Task<TvdbSeries> GetSeriesAsync(int seriesId, TvdbLanguage language, bool loadEpisodes,
                                bool loadActors, bool loadBanners)
    {
      return GetSeriesAsync(seriesId, language, loadEpisodes, loadActors, loadBanners, false);
    }

    /// <summary>
    /// Gets the series with the given id either from cache (if it has already been loaded) or from 
    /// the selected tvdb mirror. If this series is not already cached and the series has to be 
    /// downloaded, the zipped version will be downloaded
    /// 
    /// To check if this series has already been cached, use the Method IsCached(TvdbSeries _series)
    /// </summary>
    /// <exception cref="TvdbNotAvailableException">Tvdb is not available</exception>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <param name="seriesId">id of series</param>
    /// <param name="language">language that should be retrieved</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    internal Task<TvdbSeries> GetSeriesZippedAsync(int seriesId, TvdbLanguage language)
    {
      return GetSeriesAsync(seriesId, language, true, true, true, true);
    }

    protected async Task<TvdbSeries> GetSeriesAsync(int seriesId, TvdbLanguage language, bool loadEpisodes, bool loadActors, bool loadBanners, bool useZip)
    {
      // First try and get the series from the memory cache, or add it if not yet cached
      TvdbSeries series = await _memoryCache.GetValue(seriesId, _ => LoadSeriesAsync(seriesId, language, loadEpisodes, loadActors, loadBanners, useZip));
      if (series == null)
        return null;
      // If the cached series has all requested info return it
      if (series.Language == language && (series.EpisodesLoaded || !loadEpisodes) && (series.TvdbActorsLoaded || !loadActors) && (series.BannersLoaded || !loadBanners))
        return series;
      // Some info was missing, load all requested info and update the cache
      return await _memoryCache.UpdateValue(seriesId, _ => LoadSeriesAsync(seriesId, language, loadEpisodes, loadActors, loadBanners, useZip));
    }

    /// <summary>
    /// Gets the series with the given id either from cache (if it has already been loaded) or from 
    /// the selected tvdb mirror. If you use zip the request automatically downloads the episodes, the actors and the banners, so you should also select those features.
    /// 
    /// To check if this series has already been cached, use the Method IsCached(TvdbSeries _series)
    /// </summary>
    /// <exception cref="TvdbNotAvailableException">Tvdb is not available</exception>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <param name="seriesId">id of series</param>
    /// <param name="language">language abbriviation of the series that should be retrieved</param>
    /// <param name="loadEpisodes">if true, the full series record will be loaded (series + all episodes), otherwise only the base record will be loaded which contains only series information</param>
    /// <param name="loadBanners">if true also loads the paths to the banners</param>
    /// <param name="loadActors">if true also loads the extended actor information</param>
    /// <param name="useZip">If this series is not already cached and the series has to be downloaded, the zipped version will be downloaded</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public async Task<TvdbSeries> LoadSeriesAsync(int seriesId, TvdbLanguage language, bool loadEpisodes,
                                bool loadActors, bool loadBanners, bool useZip)
    {
      Log.Info($"TvdbHandler: Loading series {seriesId}");
      Stopwatch watch = new Stopwatch();
      watch.Start();
      TvdbSeries series = null;
      //Synchronise series loading, this avoids multiple threads each missing the cache then trying to load the series whilst another thread is already loading it
      await _seriesLoadingSync.WaitAsync().ConfigureAwait(false);
      try
      {
        //Check the cache again now we're in the loading lock in case another thread has loaded it whist we were waiting
        series = GetSeriesFromCache(seriesId);

        //Did I get the series completely from cache or did I have to make an additional online request
        bool loadedAdditionalInfo = false;

        if (series == null || //series not yet cached
            (useZip && (!series.EpisodesLoaded && !series.TvdbActorsLoaded && !series.BannersLoaded)))//only the basic series info has been loaded -> zip is still faster than fetching the missing informations without using zip
        {//load complete series from tvdb
          series = useZip ?
            await _downloader.DownloadSeriesZippedAsync(seriesId, language).ConfigureAwait(false) :
            await _downloader.DownloadSeriesAsync(seriesId, language, loadEpisodes, loadActors, loadBanners).ConfigureAwait(false);

          if (series == null)
          {
            return null;
          }
          watch.Stop();
          loadedAdditionalInfo = true;
          Log.Debug("Loaded series " + seriesId + " in " + watch.ElapsedMilliseconds + " milliseconds");
          series.IsFavorite = _userInfo != null && CheckIfSeriesFavorite(seriesId, _userInfo.UserFavorites);
        }
        else
        {//some (if not all) information has already been loaded from tvdb at some point -> fill the missing details and return the series

          if (language != series.Language)
          {//user wants a different language than the one that has been loaded
            if (series.GetAvailableLanguages().Contains(language))
              series.SetLanguage(language);
            else
            {
              TvdbSeriesFields newFields = await _downloader.DownloadSeriesFieldsAsync(seriesId, language).ConfigureAwait(false);
              loadedAdditionalInfo = true;
              if (loadEpisodes)
              {
                List<TvdbEpisode> epList = await _downloader.DownloadEpisodesAsync(seriesId, language).ConfigureAwait(false);
                if (epList != null)
                {
                  newFields.Episodes.Clear();
                  newFields.Episodes.AddRange(epList);
                  newFields.EpisodesLoaded = true;
                }
              }
              if (newFields != null)
              {
                series.AddLanguage(newFields);
                series.SetLanguage(language);
              }
              else
              {
                Log.Warn("Couldn't load new language " + language.Abbriviation + " for series " + seriesId);
                return null;
              }
            }
          }

          if (loadActors && !series.TvdbActorsLoaded)
          {//user wants actors loaded
            Log.Debug("Additionally loading actors");
            List<TvdbActor> actorList = await _downloader.DownloadActorsAsync(seriesId).ConfigureAwait(false);
            loadedAdditionalInfo = true;
            if (actorList != null)
            {
              series.TvdbActorsLoaded = true;
              series.TvdbActors = actorList;
            }
          }

          if (loadEpisodes && !series.EpisodesLoaded)
          {//user wants the full version but only the basic has been loaded (without episodes
            Log.Debug("Additionally loading episodes");
            List<TvdbEpisode> epList = await _downloader.DownloadEpisodesAsync(seriesId, language).ConfigureAwait(false);
            loadedAdditionalInfo = true;
            if (epList != null)
              series.SetEpisodes(epList);
          }

          if (loadBanners && !series.BannersLoaded)
          {//user wants banners loaded but current series hasn't -> Do it baby
            Log.Debug("Additionally loading banners");
            List<TvdbBanner> bannerList = await _downloader.DownloadBannersAsync(seriesId).ConfigureAwait(false);
            loadedAdditionalInfo = true;
            if (bannerList != null)
            {
              series.BannersLoaded = true;
              series.Banners = bannerList;
            }
          }

          watch.Stop();
          Log.Debug("Loaded series " + seriesId + " in " + watch.ElapsedMilliseconds + " milliseconds");
        }

        if (_cacheProvider != null)
        {
          //we're using a cache provider

          //if we've loaded data from online source -> save to cache
          if (_cacheProvider.Initialised && loadedAdditionalInfo)
          {
            Log.Info("Store series " + seriesId + " with " + _cacheProvider);
            _cacheProvider.SaveToCache(series);
          }

          //Store a ref to the cacheprovider and series id in each banner, so the banners
          //can be stored/loaded to/from cache
          #region add cache provider/series id
          if (series.Banners != null)
          {
            series.Banners.ForEach(b =>
            {
              b.CacheProvider = _cacheProvider;
              b.SeriesId = series.Id;
            });
          }

          if (series.Episodes != null)
          {
            series.Episodes.ForEach(e =>
            {
              e.Banner.CacheProvider = _cacheProvider;
              e.Banner.SeriesId = series.Id;
            });
          }

          if (series.TvdbActors != null)
          {
            series.TvdbActors.ForEach(a =>
            {
              a.ActorImage.CacheProvider = _cacheProvider;
              a.ActorImage.SeriesId = series.Id;
            });
          }
          #endregion
        }
      }
      finally
      {
        _seriesLoadingSync.Release();
      }
      return series;
    }

    /// <summary>
    /// Gets the series cache files. These should not be manipulated or changed in any manner 
    /// because they are managed by the cache provider.
    /// </summary>
    /// <param name="seriesId">id of series</param>
    public string[] GetSeriesCacheFiles(int seriesId)
    {
      return _cacheProvider.GetSeriesCacheFiles(seriesId);
    }

    /// <summary>
    /// Gets the full series (including episode information and actors) with the given id either from cache 
    /// (if it has already been loaded) or from the selected tvdb mirror.
    /// 
    /// To check if this series has already been cached, pleas use the Method IsCached(TvdbSeries _series)
    /// </summary>
    /// <exception cref="TvdbNotAvailableException">Tvdb is not available</exception>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <param name="seriesId">id of series</param>
    /// <param name="language">language that should be retrieved</param>
    /// <param name="loadBanners">if true also loads the paths to the banners</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public Task<TvdbSeries> GetFullSeriesAsync(int seriesId, TvdbLanguage language, bool loadBanners)
    {
      return GetSeriesAsync(seriesId, language, true, true, loadBanners);
    }

    /// <summary>
    /// Gets the basic series (without episode information and actors) with the given id either from cache 
    /// (if it has already been loaded) or from the selected tvdb mirror.
    /// 
    /// To check if this series has already been cached, please use the Method IsCached(TvdbSeries _series)
    /// </summary>
    /// <exception cref="TvdbNotAvailableException">Tvdb is not available</exception>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <param name="seriesId">id of series</param>
    /// <param name="language">language that should be retrieved</param>
    /// <param name="loadBanners">if true also loads the paths to the banners</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public Task<TvdbSeries> GetBasicSeriesAsync(int seriesId, TvdbLanguage language, bool loadBanners)
    {
      return GetSeriesAsync(seriesId, language, false, false, loadBanners);
    }

    /// <summary>
    /// Returns if the series is locally cached
    /// </summary>
    /// <param name="seriesId">Id of the series</param>
    /// <param name="language">Language</param>
    /// <param name="loadEpisodes">Load Episodes</param>
    /// <param name="loadActors">Load Actors</param>
    /// <param name="loadBanners">Load Banners</param>
    /// <returns>True if the series is cached in the given configuration</returns>
    public bool IsCached(int seriesId, TvdbLanguage language, bool loadEpisodes,
                         bool loadActors, bool loadBanners)
    {
      if (_cacheProvider != null && _cacheProvider.Initialised)
        return _cacheProvider.IsCached(seriesId, language, loadEpisodes, loadBanners, loadActors);
      return false;
    }



    /// <summary>
    /// Retrieve the episode with the given id in the given language. 
    /// 
    /// Note that the episode is always downloaded from thetvdb since it would
    /// be practical to load each and every cached series to look for the 
    /// episode id
    /// </summary>
    /// <param name="episodeId">id of the episode</param>
    /// <param name="language">languageof the episode</param>
    /// <returns>The retrieved episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Task<TvdbEpisode> GetEpisodeAsync(int episodeId, TvdbLanguage language)
    {
      return _downloader.DownloadEpisodeAsync(episodeId, language);
    }

    /// <summary>
    /// Retrieve the episode with the given parameters. This function will find
    /// episodes that are already cached.
    /// </summary>
    /// <param name="seriesId">id of the series</param>
    /// <param name="seasonNr">Season number of the episode</param>
    /// <param name="episodeNr">number of the episode</param>
    /// <param name="language">language of the episode</param>
    /// <param name="order">The sorting order that should be user when downloading the episode</param>
    /// <returns>The retrieved episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public async Task<TvdbEpisode> GetEpisodeAsync(int seriesId, int seasonNr, int episodeNr, TvdbEpisode.EpisodeOrdering order, TvdbLanguage language)
    {
      TvdbEpisode episode = null;
      if (_cacheProvider != null && _cacheProvider.Initialised)
      {
        if (_cacheProvider.IsCached(seriesId, language, true, false, false))
        {
          TvdbSeries series = _cacheProvider.LoadSeriesFromCache(seriesId);
          if (series.Language != language)
          {
            series.SetLanguage(language);
          }

          if (series.Episodes != null)
          {
            foreach (TvdbEpisode e in series.Episodes)
            {
              if (e.EpisodeNumber == episodeNr && e.SeasonNumber == seasonNr && order == TvdbEpisode.EpisodeOrdering.DefaultOrder ||
                  e.DvdEpisodeNumber == episodeNr && e.SeasonNumber == seasonNr && order == TvdbEpisode.EpisodeOrdering.DvdOrder ||
                  e.AbsoluteNumber == episodeNr && order == TvdbEpisode.EpisodeOrdering.AbsoluteOrder)
              {//We found the episode that matches the episode number according to the given ordering
                episode = e;
                break;
              }
            }
          }
        }
      }

      return episode ?? await _downloader.DownloadEpisodeAsync(seriesId, seasonNr, episodeNr, order, language).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieve the episode with the given parameters.
    /// </summary>
    /// <param name="seriesId">id of the series</param>
    /// <param name="airDate">When did the episode air</param>
    /// <param name="language">language of the episode</param>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <returns>The retrieved episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public async Task<TvdbEpisode> GetEpisodeAsync(int seriesId, DateTime airDate, TvdbLanguage language)
    {
      TvdbEpisode episode = null;
      if (_cacheProvider != null && _cacheProvider.Initialised)
      {
        if (_cacheProvider.IsCached(seriesId, language, true, false, false))
        {
          TvdbSeries series = _cacheProvider.LoadSeriesFromCache(seriesId);
          if (series.Language != language)
            series.SetLanguage(language);

          foreach (TvdbEpisode e in series.Episodes)
          {
            if (e.FirstAired.Year == airDate.Year && e.FirstAired.Month == airDate.Month && e.FirstAired.Day == airDate.Day)
            {//We found the episode that first aired at the given day
              episode = e;
              break;
            }
          }
        }
      }
      return episode ?? await _downloader.DownloadEpisodeAsync(seriesId, airDate, language).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the series from cache
    /// </summary>
    /// <param name="seriesId">Id of series</param>
    /// <returns></returns>
    private TvdbSeries GetSeriesFromCache(int seriesId)
    {
      //try to retrieve the series from the cache provider
      try
      {
        TvdbSeries series = _cacheProvider.LoadSeriesFromCache(seriesId);
        if (series != null)
          series.IsFavorite = _userInfo != null && CheckIfSeriesFavorite(series.Id, _userInfo.UserFavorites);
        return series;
      }
      catch (Exception)
      {
        return null;
      }
    }

    #region updating

    /// <summary>
    /// Update all the series (not using zip) with the updated information
    /// </summary>
    /// <returns>true if the update was successful, false otherwise</returns>
    public Task<bool> UpdateAllSeriesAsync()
    {
      return UpdateAllSeriesAsync(false);
    }

    /// <summary>
    /// Update all the series with the updated information
    /// </summary>
    /// <param name="zipped">download zipped file?</param>
    /// <exception cref="TvdbCacheNotInitialisedException">In order to update, the cache has to be initialised</exception>
    /// <returns>true if the update was successful, false otherwise</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Task<bool> UpdateAllSeriesAsync(bool zipped)
    {
      return UpdateAllSeriesAsync(Interval.Automatic, zipped);
    }

    /// <summary>
    /// Update all the series with the updated information
    /// </summary>
    /// <param name="zipped">download zipped file?</param>
    /// <param name="interval">Specifies the interval of the update (day, week, month)</param>
    /// <returns>true if the update was successful, false otherwise</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    /// <exception cref="TvdbCacheNotInitialisedException">In order to update, the cache has to be initialised</exception>
    public Task<bool> UpdateAllSeriesAsync(Interval interval, bool zipped)
    {
      if (_loadedData == null)
      {//the cache hasn't been initialised yet
        throw new TvdbCacheNotInitialisedException("In order to update the series, "
                                                   + "the cache has to be initialisee");
      }

      if (interval == Interval.Automatic)
      {
        TimeSpan timespanLastUpdate = (DateTime.Now - _loadedData.LastUpdated);
        if (_loadedData.LastUpdated == DateTime.MinValue)
        {//lastUpdated not available -> make longest possible upgrade
         //todo: Make a full update -> full update deosn't make sense... (do a complete re-scan?)
          return UpdateAllSeriesAsync(Interval.Month, zipped, true);
        }
        if (timespanLastUpdate > new TimeSpan(31, 0, 0, 0))
        {//last update is over 1 month ago -> make a monthly update
          Log.Warn("The last update occured longer than a month ago, to avoid data inconsistency, all cached series "
         + "and episode informations is downloaded again");
          return UpdateAllSeriesAsync(Interval.Month, zipped, true);
        }
        if (timespanLastUpdate > new TimeSpan(29, 0, 0, 0))
        {//last update is more than a month ago -> make a monthly update
          return UpdateAllSeriesAsync(Interval.Month, zipped, true);
        }
        if (timespanLastUpdate > new TimeSpan(7, 0, 0, 0))
        {//last update is more than a week ago -> make a weekly update
          return UpdateAllSeriesAsync(Interval.Week, zipped, false);
        }
        if (timespanLastUpdate > new TimeSpan(1, 0, 0, 0))
        {//last update is more than a day ago -> make a daily update
          return UpdateAllSeriesAsync(Interval.Day, zipped, false);
        }
        return Task.FromResult(false);
      }
      if (interval == Interval.Day)
        return UpdateAllSeriesAsync(interval, zipped, false);
      if (interval == Interval.Week)
        return UpdateAllSeriesAsync(interval, zipped, false);
      if (interval == Interval.Month)
        return UpdateAllSeriesAsync(interval, zipped, true);
      return Task.FromResult(false);
    }

    /// <summary>
    /// Update all the series with the updated information
    /// </summary>
    /// <param name="zipped">download zipped file?</param>
    /// <param name="interval">Specifies the interval of the update (day, week, month)</param>
    /// <param name="reloadOldContent">If yes, will reload all series that haven't been updated longer than the update period (which means
    ///                                 that only a reload can guarantee that the data is up to date. Should only be used when the data hasn't
    ///                                 been updated for over a month (otherwise use monthly updates)</param>
    /// <returns>true if the update was successful, false otherwise</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    /// <exception cref="TvdbCacheNotInitialisedException">In order to update, the cache has to be initialised</exception>
    public async Task<bool> UpdateAllSeriesAsync(Interval interval, bool zipped, bool reloadOldContent)
    {
      try
      {
        await MakeUpdateAsync(interval, zipped, reloadOldContent).ConfigureAwait(false);
        return true;
      }
      catch (Exception ex)
      {
        Log.Error("TvDbHandler: Error updating series information:", ex);
        return false;
      }
    }

    /// <summary>
    /// Gets the date of the last (successfull) update from thetvdb
    /// </summary>
    /// <returns>Date of last update or null if no previous update or cache not initialised</returns>
    public DateTime GetLastUpdate()
    {
      if (_loadedData != null)
        return _loadedData.LastUpdated;
      throw new TvdbCacheNotInitialisedException();
    }

    /// <summary>
    /// Aborts the currently running Update
    /// </summary>
    /// <param name="saveChangesMade">if true, all changes that have already been 
    ///             made will be saved to cache, if not they will be discarded</param>
    public void AbortUpdate(bool saveChangesMade)
    {
      _abortUpdate = true;
      _abortUpdateSaveChanges = saveChangesMade;
    }

    /// <summary>
    /// Make the update
    /// </summary>
    /// <param name="interval">interval of update</param>
    /// <param name="zipped">zipped downloading yes/no</param>
    /// <param name="reloadOldContent"> </param>
    /// <returns>true if successful, false otherwise</returns>
    private async Task<bool> MakeUpdateAsync(Interval interval, bool zipped, bool reloadOldContent)
    {
      Log.Info("Started update (" + interval + ")");
      Stopwatch watch = new Stopwatch();
      watch.Start();
      DateTime startUpdate = DateTime.Now;
      if (UpdateProgressed != null)
      {//update has started, we're downloading the updated content from tvdb
        UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.Downloading,
                                                     "Downloading " + (zipped ? " zipped " : " unzipped") +
                                                     " updated content (" + interval.ToString() + ")",
                                                     0, 0));
      }

      List<int> updatedSeriesIds = new List<int>();
      List<int> updatedEpisodeIds = new List<int>();
      List<int> updatedBannerIds = new List<int>();

      //update all flagged series
      TvDbUpdate update = await _downloader.DownloadUpdateAsync(interval, zipped).ConfigureAwait(false);
      List<int> cachedSeries = _cacheProvider.GetCachedSeries();

      //list of all series that have been loaded from cache 
      //and need to be saved to cache at the end of the update
      Dictionary<int, TvdbSeries> seriesToSave = new Dictionary<int, TvdbSeries>();

      if (UpdateProgressed != null)
      {//update has started, we're downloading the updated content from tvdb
        UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.SeriesUpdate,
                                                     "Begin updating series",
                                                     0, 25));
      }

      int countUpdatedSeries = update.UpdateSeries.Count;
      int countSeriesDone = 0;
      int lastProgress = 0;//send progress event at least every 1 percent
      String updateText = "Updating series";

      if (reloadOldContent)
      {
        //if the last time an item (series or episode) was updated is longer ago than the timespan
        //we downloaded updates for, it's neccessary to completely reload the object to ensure that
        //the data is up to date.

        DateTime lastupdated = GetLastUpdate();

        TimeSpan span = new TimeSpan();
        switch (interval)
        {
          case Interval.Day:
            span = span.Add(new TimeSpan(1, 0, 0, 0));
            break;
          case Interval.Week:
            span = span.Add(new TimeSpan(7, 0, 0, 0));
            break;
          case Interval.Month:
            span = span.Add(new TimeSpan(30, 0, 0, 0));
            break;
        }

        if (lastupdated < DateTime.Now - span)
        {//the last update of the cache is longer ago than the timespan we make the update for
          List<int> allSeriesIds = _cacheProvider.GetCachedSeries();
          foreach (int s in allSeriesIds)
          {
            //TvdbSeries series = _cacheProvider.LoadSeriesFromCache(s);
            TvdbSeries series = seriesToSave.ContainsKey(s) ? seriesToSave[s] : _cacheProvider.LoadSeriesFromCache(s);

            //ForceReload(series, false);
            if (series.LastUpdated > DateTime.Now - span)
            {
              //the series object is up to date, but some episodes might not be
              foreach (TvdbEpisode e in series.Episodes)
              {
                if (e.LastUpdated < DateTime.Now - span)
                {
                  if (TvDbUtils.FindEpisodeInList(e.Id, update.UpdateEpisodes) == null)
                  {//The episode is not in the updates.xml file
                    TvdbEpisode newEp = new TvdbEpisode();
                    newEp.Id = e.Id;
                    newEp.LastUpdated = DateTime.Now;
                    update.UpdateEpisodes.Add(newEp);
                  }
                  if (!seriesToSave.ContainsKey(series.Id)) seriesToSave.Add(series.Id, series);

                }
              }
            }
            else
            {//the series hasn't been updated recently -> we need to do a complete re-download
              await ForceReloadAsync(series, false).ConfigureAwait(false);//redownload series and save it to cache
              countUpdatedSeries++;
              countSeriesDone++;
              int currProg = (int)(100.0 / countUpdatedSeries * countSeriesDone);
              updatedSeriesIds.Add(series.Id);
              updateText = "Reloaded series " + series.SeriesName + "(" + series.Id + ")";

              if (UpdateProgressed != null)
              {//update has started, we're downloading the updated content from tvdb
                UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.SeriesUpdate,
                                                             updateText, currProg, 25 + currProg / 4));
              }
              //if (!seriesToSave.ContainsKey(series.Id)) seriesToSave.Add(series.Id, series);
            }
          }
        }
      }

      foreach (TvdbSeries us in update.UpdateSeries)
      {
        if (_abortUpdate) break;//the update has been aborted
        //Update series that have been already cached
        foreach (int s in cachedSeries)
        {
          if (us.Id == s)
          {//changes occured in series
            TvdbSeries series;
            series = seriesToSave.ContainsKey(s) ? seriesToSave[s] : _cacheProvider.LoadSeriesFromCache(s);
            if (series == null)
              break;

            int currProg = (int)(100.0 / countUpdatedSeries * countSeriesDone);

            bool updated = await UpdateSeriesAsync(series, us.LastUpdated, currProg).ConfigureAwait(false);
            if (updated)
            {//the series has been updated
              updatedSeriesIds.Add(us.Id);
              updateText = "Updated series " + series.SeriesName + "(" + series.Id + ")";
              //store the updated series to cache
              if (!seriesToSave.ContainsKey(series.Id)) seriesToSave.Add(series.Id, series);
            }

            if (updated || currProg > lastProgress)
            {//the series has been updated OR the last event was fired at least one percent ago
              if (UpdateProgressed != null)
              {//update has started, we're downloading the updated content from tvdb
                UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.SeriesUpdate,
                                                             updateText, currProg, 25 + currProg / 4));
              }
              lastProgress = currProg;
            }

            break;
          }
        }
        countSeriesDone++;
      }

      int countEpisodeUpdates = update.UpdateEpisodes.Count;
      int countEpisodesDone = 0;
      lastProgress = 0;
      updateText = "Updating episodes";
      //update all flagged episodes
      foreach (TvdbEpisode ue in update.UpdateEpisodes)
      {
        if (_abortUpdate) break;//the update has been aborted

        foreach (int s in cachedSeries)
        {
          if (ue.SeriesId == s)
          {//changes occured in series
            TvdbSeries series;
            series = seriesToSave.ContainsKey(s) ? seriesToSave[s] : _cacheProvider.LoadSeriesFromCache(ue.SeriesId);
            if (series == null)
              break;

            int progress = (int)(100.0 / countEpisodeUpdates * countEpisodesDone);
            Tuple<bool, string> updateResult = await UpdateEpisodeAsync(series, ue, progress).ConfigureAwait(false);
            bool updated = updateResult.Item1;
            string text = updateResult.Item2;
            if (updated)
            {//The episode was updated or added
              updatedEpisodeIds.Add(ue.Id);
              if (!seriesToSave.ContainsKey(series.Id)) seriesToSave.Add(series.Id, series);
              updateText = text;
            }
            if (updated || progress > lastProgress)
            {
              if (UpdateProgressed != null)
              {//update has started, we're downloading the updated content from tvdb
                UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.EpisodesUpdate,
                                                             updateText, progress, 50 + progress / 4));
              }
            }
            break;
          }
        }
        countEpisodesDone++;
      }

      int countUpdatedBanner = update.UpdateBanners.Count;
      int countBannerDone = 0;
      lastProgress = 0;
      // todo: update banner information here -> wait for forum response regarding 
      // missing banner id within updates (atm. I'm matching banners via path)
      foreach (TvdbBanner b in update.UpdateBanners)
      {
        if (_abortUpdate) break;//the update has been aborted

        foreach (int s in cachedSeries)
        {
          if (b.SeriesId == s)
          {//banner for this series has changed
            int currProg = (int)(100.0 / countUpdatedBanner * countBannerDone);
            TvdbSeries series = seriesToSave.ContainsKey(s) ? seriesToSave[s] : _cacheProvider.LoadSeriesFromCache(b.SeriesId);
            if (series == null)
              break;
            bool updated = UpdateBanner(series, b);
            if (updated)
            {
              updatedBannerIds.Add(b.Id);
              if (!seriesToSave.ContainsKey(series.Id)) seriesToSave.Add(series.Id, series);
            }

            if (updated || currProg > lastProgress)
            {
              if (UpdateProgressed != null)
              {//update has started, we're downloading the updated content from tvdb

                UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.BannerUpdate,
                                                             "Updating banner " + b.BannerPath + "(id=" + b.Id + ")",
                                                             currProg, 75 + currProg / 4));
              }
            }
            break;
          }
        }
        countBannerDone++;
      }

      if (!_abortUpdate)
      {//update has finished successfully
        if (UpdateProgressed != null)
        {
          UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.FinishUpdate,
                                                       "Update finished, saving loaded information to cache",
                                                       100, 100));
        }
      }
      else
      {//the update has been aborted
        if (UpdateProgressed != null)
        {
          UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.FinishUpdate,
                                                       "Update aborted by user, " +
                                                       (_abortUpdateSaveChanges ? " saving " : " not saving ") +
                                                       "already loaded information to cache",
                                                       100, 100));
        }
      }

      if (!_abortUpdate || _abortUpdateSaveChanges)
      {//store the information we downloaded to cache
        Log.Info("Saving all series to cache that have been modified during the update (" + seriesToSave.Count + ")");
        foreach (KeyValuePair<int, TvdbSeries> kvp in seriesToSave)
        {//Save all series to cache that have been modified during the update
          try
          {
            _cacheProvider.SaveToCache(kvp.Value);
          }
          catch (Exception ex)
          {
            Log.Warn("Couldn't save " + kvp.Key + " to cache: " + ex);
          }
        }
      }

      if (!_abortUpdate)
      {//update finished and wasn't aborted
        //set the last updated time to time of this update
        _loadedData.LastUpdated = update.UpdateTime;
        _cacheProvider.SaveToCache(_loadedData);
      }

      watch.Stop();

      Log.Info("Finished update (" + interval + ") in " + watch.ElapsedMilliseconds + " milliseconds");
      if (UpdateFinished != null)
      {
        if (!_abortUpdate || _abortUpdateSaveChanges)
        {//we either updated everything successfully or we at least saved the changes made before the update completed
          UpdateFinished(new UpdateFinishedEventArgs(startUpdate, DateTime.Now, updatedSeriesIds, updatedEpisodeIds, updatedBannerIds));
        }
        else
        {//we didn't update anything because the update was aborted
          UpdateFinished(new UpdateFinishedEventArgs(startUpdate, DateTime.Now, new List<int>(), new List<int>(), new List<int>()));

        }
      }
      return true;
    }

    #region updating of banners, episodes and series
    /// <summary>
    /// Update the series with the banner
    /// </summary>
    /// <param name="series"></param>
    /// <param name="banner"></param>
    /// <returns>true, if the banner was updated successfully, false otherwise</returns>
    private bool UpdateBanner(TvdbSeries series, TvdbBanner banner)
    {
      if (!series.BannersLoaded)
      {//banners for this series havn't been loaded -> don't update banners
        Log.Debug("Not handling banner " + banner.BannerPath + " because series " + series.Id
                  + " doesn't have banners loaded");
        return false;
      }

      foreach (TvdbBanner b in series.Banners)
      {
        if (banner.GetType() == b.GetType() && banner.BannerPath.Equals(b.BannerPath))
        {//banner was found
          if (b.LastUpdated < banner.LastUpdated)
          {//update time of local banner is longer ago than update time of current update
            b.LastUpdated = banner.LastUpdated;
            b.CacheProvider = _cacheProvider;
            b.SeriesId = series.Id;

            if (b.IsLoaded)
            {//the banner was previously loaded and is updated -> discard the previous image
              b.LoadBanner(null);
            }
            b.UnloadBanner(false);
            if (banner.GetType() == typeof(TvdbBannerWithThumb))
            {
              TvdbBannerWithThumb thumb = (TvdbBannerWithThumb)b;
              if (thumb.IsThumbLoaded)
                thumb.LoadThumb(null);
              thumb.UnloadThumb(false);
            }


            if (banner.GetType() == typeof(TvdbFanartBanner))
            {//update fanart specific content
              TvdbFanartBanner fanart = (TvdbFanartBanner)b;

              fanart.Resolution = ((TvdbFanartBanner)banner).Resolution;
              if (fanart.IsThumbLoaded)
                fanart.LoadThumb(null);
              fanart.UnloadThumb(false);

              if (fanart.IsVignetteLoaded)
                fanart.LoadVignette(null);
              fanart.UnloadVignette();
            }

            Log.Info("Replacing banner " + banner.Id);
            return true;
          }
          Log.Debug("Not replacing banner " + banner.Id + " because it's not newer than current image");
          return false;
        }
      }

      //banner not found -> add it to bannerlist
      Log.Info("Adding banner " + banner.Id);
      series.Banners.Add(banner);
      return true;
    }

    /// <summary>
    /// Update the series with the episode (Add it to the series if it doesn't already exist or update the episode if the current episode is older than the updated one)
    /// </summary>
    /// <param name="series">Series of the updating episode</param>
    /// <param name="episode">Episode that is updated</param>
    /// <param name="progress">Progress of the update run</param>
    /// <param name="text">Description of the current update</param>
    /// <returns>true if episode has been updated, false if not (e.g. timestamp of updated episode older than
    ///          timestamp of existing episode</returns> 
    private async Task<Tuple<bool, string>> UpdateEpisodeAsync(TvdbSeries series, TvdbEpisode episode, int progress)
    {
      bool updateDone = false;
      string text = "";
      TvdbLanguage currentLanguage = series.Language;
      foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in series.SeriesTranslations)
      {
        if (series.EpisodesLoaded)
        {
          bool found = false;
          List<TvdbEpisode> eps = kvp.Value.Episodes;

          if (eps != null && eps.Count > 0)
          {
            //check all episodes if the updated episode is in it
            foreach (TvdbEpisode e in eps.Where(e => e.Id == episode.Id))
            {
              found = true;
              if (e.LastUpdated < episode.LastUpdated)
              {
                //download episode which has been updated
                TvdbEpisode newEpisode = null;
                try
                {
                  newEpisode = await _downloader.DownloadEpisodeAsync(e.Id, kvp.Key).ConfigureAwait(false);
                }
                catch (TvdbContentNotFoundException)
                {
                  Log.Warn("Couldn't download episode " + e.Id + "(" + e.EpisodeName + ")");
                }
                //update information of episode with new episodes informations
                if (newEpisode != null)
                {
                  newEpisode.LastUpdated = episode.LastUpdated;

                  #region fix for http://forums.thetvdb.com/viewtopic.php?f=8&t=3993
                  //xml of single episodes doesn't contain  CombinedSeason and CombinedEpisodeNumber, so if
                  //we have values for those, don't override them
                  //-> http://forums.thetvdb.com/viewtopic.php?f=8&t=3993
                  //todo: remove this once tvdb fixed that issue
                  if (e.CombinedSeason != Util.NO_VALUE && newEpisode.CombinedSeason == 0)
                  {
                    newEpisode.CombinedSeason = e.CombinedSeason;
                  }
                  if (e.CombinedEpisodeNumber != Util.NO_VALUE && newEpisode.CombinedEpisodeNumber == 0)
                  {
                    newEpisode.CombinedEpisodeNumber = e.CombinedEpisodeNumber;
                  }
                  #endregion

                  e.UpdateEpisodeInfo(newEpisode);

                  e.Banner.CacheProvider = _cacheProvider;
                  e.Banner.SeriesId = series.Id;

                  e.Banner.UnloadBanner(false);
                  e.Banner.UnloadThumb(false);

                  text = "Added/Updated episode " + series.SeriesName + " " + e.SeasonNumber +
                         "x" + e.EpisodeNumber + "(id: " + e.Id + ")";
                  Log.Info("Updated Episode " + e.SeasonNumber + "x" + e.EpisodeNumber + " for series " + series.SeriesName +
                           "(id: " + e.Id + ", lang: " + e.Language.Abbriviation + ")");
                  updateDone = true;
                }
              }
              break;
            }
          }

          if (!found)
          {
            //episode hasn't been found
            //hasn't been found -> add it to series
            TvdbEpisode ep = null;
            try
            {
              ep = await _downloader.DownloadEpisodeAsync(episode.Id, kvp.Key).ConfigureAwait(false);
            }
            catch (TvdbContentNotFoundException ex)
            {
              Log.Warn("Problem downloading " + episode.Id + ": " + ex);
            }
            if (ep != null)
            {
              kvp.Value.Episodes.Add(ep);
              //sort the episodes according to default (aired) order
              kvp.Value.Episodes.Sort(new EpisodeComparerAired());
              text = "Added/Updated episode " + series.SeriesName + " " + ep.SeasonNumber +
                      "x" + ep.EpisodeNumber + "(id: " + ep.Id + ")";

              Log.Info("Added Episode " + ep.SeasonNumber + "x" + ep.EpisodeNumber + " for series " + series.SeriesName +
                       "(id: " + ep.Id + ", lang: " + ep.Language.Abbriviation + ")");
              updateDone = true;
            }
          }
        }
        else
        {
          Log.Debug("Not handling episode " + episode.Id + ", because series " + series.SeriesName + " hasn't loaded episodes");
          updateDone = false;
        }
      }
      series.SetLanguage(currentLanguage);
      return new Tuple<bool, string>(updateDone, text);
    }



    /// <summary>
    /// Download the new series and update the information
    /// </summary>
    /// <param name="series">Series to update</param>
    /// <param name="lastUpdated">When was the last update made</param>
    /// <param name="progress">The progress done until now</param>
    /// <returns>true if the series has been upated false if not</returns>
    private async Task<bool> UpdateSeriesAsync(TvdbSeries series, DateTime lastUpdated, int progress)
    {
      //get series info
      bool updateDone = false;
      TvdbLanguage currentLanguage = series.Language;
      foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in series.SeriesTranslations)
      {
        if (kvp.Value.LastUpdated < lastUpdated)
        {//the local content is older than the update time in the updates xml files
          TvdbSeries newSeries = null;
          try
          {//try to get the series
            newSeries = await _downloader.DownloadSeriesAsync(series.Id, kvp.Key, false, false, false).ConfigureAwait(false);
          }
          catch (TvdbContentNotFoundException ex)
          {//couldn't download the series
            Log.Warn("Problem downloading series (id: " + series.Id + "): " + ex);
          }

          if (newSeries != null)
          {//download of the series successfull -> do updating
            newSeries.LastUpdated = lastUpdated;

            //don't replace episodes, since we're only loading basic series
            kvp.Value.UpdateTvdbFields(newSeries, false);

            //kvp.Value.Update (newSeries);
            Log.Info("Updated Series " + series.SeriesName + " (id: " + series.Id + ", " +
                     kvp.Key.Abbriviation + ")");
            updateDone = true;
          }
        }
      }
      series.SetLanguage(currentLanguage);//to copy the episode-fields to the base series
      return updateDone;
    }
    #endregion
    #endregion

    /// <summary>
    /// Returns list of all available Languages on tvdb
    /// </summary>
    /// <returns>list of available languages</returns>
    public async Task<List<TvdbLanguage>> GetLanguagesAsync()
    {
      if (IsLanguagesCached)
        return _loadedData.LanguageList;

      List<TvdbLanguage> list = await _downloader.DownloadLanguagesAsync().ConfigureAwait(false);
      if (list == null || list.Count == 0)
        return null;
      if (_loadedData != null)
        _loadedData.LanguageList = list;
      return list;
    }

    /// <summary>
    /// Reloads all language definitions from tvdb
    /// </summary>
    /// <returns>true if successful, false otherwise</returns>
    public async Task<bool> ReloadLanguagesAsync()
    {
      List<TvdbLanguage> list = await _downloader.DownloadLanguagesAsync().ConfigureAwait(false);
      if (list == null || list.Count == 0)
        return false;
      _loadedData.LanguageList = list;
      return true;
    }

    /// <summary>
    /// Are the language definitions already cached
    /// </summary>
    public bool IsLanguagesCached
    {
      get
      {
        return (_loadedData != null && _loadedData.LanguageList != null && _loadedData.LanguageList.Count > 0);
      }
    }

    /// <summary>
    /// Closes the cache provider (should be called before exiting the application)
    /// </summary>
    public void CloseCache()
    {
      if (_cacheProvider == null)
        return;
      _cacheProvider.SaveToCache(_loadedData);
      if (_userInfo != null)
        _cacheProvider.SaveToCache(_userInfo);
      _cacheProvider.CloseCache();
    }


    /// <summary>
    /// Returns all series id's that are already cached in memory or locally via the cacheprovider
    /// </summary>
    /// <returns>List of loaded series</returns>
    public List<int> GetCachedSeries()
    {
      if (!_cacheProvider.Initialised)
        throw new TvdbCacheNotInitialisedException("The cache has to be initialised first");

      List<int> retList = new List<int>();
      //add series that are stored with the cacheprovider
      if (_cacheProvider != null)
        retList.AddRange(_cacheProvider.GetCachedSeries());
      return retList;
    }


    /// <summary>
    /// Forces a complete reload of the series. All information that has already been loaded (including loaded images!) will be deleted and reloaded from tvdb -> if you only want to update the series, use the "MakeUpdate" method
    /// </summary>
    /// <param name="series">Series to reload</param>
    /// <returns>The new TvdbSeries object</returns>
    public Task<TvdbSeries> ForceReloadAsync(TvdbSeries series)
    {
      return ForceReloadAsync(series, series.EpisodesLoaded, series.TvdbActorsLoaded, series.BannersLoaded);
    }

    /// <summary>
    /// Forces a complete reload of the series. All information that has already been loaded will be deleted and reloaded from tvdb -> if you only want to update the series, use the "MakeUpdate" method
    /// </summary>
    /// <param name="series">Series to reload</param> 
    /// <param name="deleteArtwork">If yes, also deletes previously loaded images</param>
    /// <returns>The new TvdbSeries object</returns>
    public Task<TvdbSeries> ForceReloadAsync(TvdbSeries series, bool deleteArtwork)
    {
      return ForceReloadAsync(series, series.EpisodesLoaded, series.TvdbActorsLoaded, series.BannersLoaded, deleteArtwork);
    }

    /// <summary>
    /// Forces a complete reload of the series. All information that has already been loaded (including loaded images!) will be deleted and reloaded from tvdb -> if you only want to update the series, use the "MakeUpdate" method
    /// </summary>
    /// <param name="series">Series to update</param>
    /// <param name="loadEpisodes">Should episodes be loaded as well</param>
    /// <param name="loadActors">Should actors be loaded as well</param>
    /// <param name="loadBanners">Should banners be loaded as well</param>
    /// <returns>The new TvdbSeries object</returns>
    public Task<TvdbSeries> ForceReloadAsync(TvdbSeries series, bool loadEpisodes,
                                bool loadActors, bool loadBanners)
    {
      return ForceReloadAsync(series, loadEpisodes, loadActors, loadBanners, true);
    }


    /// <summary>
    /// Forces a complete reload of the series. All information that has already been loaded will be deleted and reloaded from tvdb -> if you only want to update the series, use the "MakeUpdate" method
    /// </summary>
    /// <param name="series">Series to update</param>
    /// <param name="loadEpisodes">Should episodes be loaded as well</param>
    /// <param name="loadActors">Should actors be loaded as well</param>
    /// <param name="loadBanners">Should banners be loaded as well</param>
    /// <param name="replaceArtwork">If yes, also deletes previously loaded images</param>
    /// <returns>The new TvdbSeries object</returns>
    public async Task<TvdbSeries> ForceReloadAsync(TvdbSeries series, bool loadEpisodes, bool loadActors, bool loadBanners, bool replaceArtwork)
    {
      if (series == null)
      {
        Log.Warn("no series given (null)");
        return null;
      }
      TvdbSeries newSeries = await _downloader.DownloadSeriesAsync(series.Id, series.Language, loadEpisodes, loadActors, loadBanners).ConfigureAwait(false);

      if (newSeries == null)
      {
        Log.Warn("Couldn't load series " + series.Id + ", reloading not successful");
        return null;
      }

      if (_cacheProvider != null && _cacheProvider.Initialised)
      {
        //remove old series from cache and store the reloaded one
        if (replaceArtwork)
          _cacheProvider.RemoveFromCache(series.Id); //removes all info (including images)
        _cacheProvider.SaveToCache(newSeries);
      }
      return newSeries;
    }

    /// <summary>
    /// Gets the preferred language of the user
    /// 
    /// user information has to be set, otherwise TvdbUserNotFoundException is thrown
    /// </summary>
    /// <returns>preferred language of user</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public async Task<TvdbLanguage> GetPreferredLanguageAsync()
    {
      if (_userInfo != null)
      {
        TvdbLanguage userLang = await _downloader.DownloadUserPreferredLanguageAsync(_userInfo.UserIdentifier).ConfigureAwait(false);

        if (userLang != null)
        {
          //only one language is contained in the userlang file
          foreach (TvdbLanguage l in _loadedData.LanguageList)
          {
            if (l.Abbriviation.Equals(userLang.Abbriviation)) return l;
          }
          return userLang;//couldn't find language -> return new instance
        }
        return null; //problem with parsing xml file
      }
      throw new TvdbUserNotFoundException("You can't get the preferred language when no user is specified");
    }

    #region user favorites and rating

    /// <summary>
    /// Check if series is in the list of favorites
    /// </summary>
    /// <param name="series"></param>
    /// <param name="favs"></param>
    /// <returns></returns>
    private bool CheckIfSeriesFavorite(int series, IEnumerable<int> favs)
    {
      return favs != null && favs.Any(f => series == f);
    }

    /// <summary>
    /// Gets a list of IDs of the favorite series of the user
    /// </summary>
    /// <returns>id list of favorite series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public async Task<List<int>> GetUserFavouritesListAsync()
    {
      if (_userInfo == null)
        throw new Exception("You can't get the list of user favorites when no user is specified");
      List<int> userFavs = await _downloader.DownloadUserFavoriteListAsync(_userInfo.UserIdentifier).ConfigureAwait(false);
      _userInfo.UserFavorites = userFavs;
      return userFavs;
    }

    /// <summary>
    /// Get the favorite series of the user (only basic series information will be loaded)
    /// </summary>
    /// <param name="lang">Which language should be used</param>
    /// <returns>List of favorite series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public async Task<List<TvdbSeries>> GetUserFavoritesAsync(TvdbLanguage lang)
    {
      if (_userInfo == null)
        throw new TvdbUserNotFoundException("You can't get the favourites when no user is defined");
      if (lang == null)
        throw new Exception("you have to define a language");
      List<int> idList = await GetUserFavouritesListAsync().ConfigureAwait(false);
      List<TvdbSeries> retList = new List<TvdbSeries>();

      foreach (int sId in idList)
      {
        if (IsCached(sId, lang, false, false, false))
          retList.Add(GetSeriesFromCache(sId));
        else
        {
          TvdbSeries series = await _downloader.DownloadSeriesAsync(sId, lang, false, false, false).ConfigureAwait(false);
          if (series != null)
            retList.Add(series);

          //since we have downloaded the basic series -> if we have a cache provider, also
          //save the series via our CacheProvider
          if (_cacheProvider != null && _cacheProvider.Initialised)
            _cacheProvider.SaveToCache(series);
        }
      }
      return retList;
    }

    /// <summary>
    /// Adds the series id to the users list of favorites and returns the new list of
    /// favorites
    /// </summary>
    /// <param name="seriesId">series to add to the favorites</param>
    /// <returns>new list with all favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public async Task<List<int>> AddSeriesToFavoritesAsync(int seriesId)
    {
      if (_userInfo == null)
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");

      List<int> list = await _downloader.DownloadUserFavoriteListAsync(_userInfo.UserIdentifier, Util.UserFavouriteAction.Add, seriesId).ConfigureAwait(false);
      _userInfo.UserFavorites = list;
      return list;
    }

    /// <summary>
    /// Adds the series to the users list of favorites and returns the new list of
    /// favorites
    /// </summary>
    /// <param name="series">series to add to the favorites</param>
    /// <returns>new list with all favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public async Task<List<int>> AddSeriesToFavoritesAsync(TvdbSeries series)
    {
      return series == null ? null : await AddSeriesToFavoritesAsync(series.Id).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the series id from the users list of favorites and returns the new list of
    /// favorites
    /// </summary>
    /// <param name="seriesId">series to remove from the favorites</param>
    /// <returns>new list with all favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public async Task<List<int>> RemoveSeriesFromFavoritesAsync(int seriesId)
    {
      if (_userInfo == null)
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      List<int> list = await _downloader.DownloadUserFavoriteListAsync(_userInfo.UserIdentifier, Util.UserFavouriteAction.Remove, seriesId).ConfigureAwait(false);
      _userInfo.UserFavorites = list;
      return list;
    }

    /// <summary>
    /// Removes the series from the users list of favorites and returns the new list of
    /// favorites
    /// </summary>
    /// <param name="series">series to remove from the favorites</param>
    /// <returns>new list with all favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public Task<List<int>> RemoveSeriesFromFavoritesAsync(TvdbSeries series)
    {
      return RemoveSeriesFromFavoritesAsync(series.Id);
    }


    /// <summary>
    /// Rate the given series
    /// </summary>
    /// <param name="seriesId">series id</param>
    /// <param name="rating">The rating we want to give for this series</param>
    /// <returns>Current rating of the series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Task<double> RateSeriesAsync(int seriesId, int rating)
    {
      if (_userInfo == null)
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      if (rating < 0 || rating > 10)
        throw new ArgumentOutOfRangeException("rating", rating, "rating must be an integer between 0 and 10");
      return _downloader.RateSeriesAsync(_userInfo.UserIdentifier, seriesId, rating);
    }

    /// <summary>
    /// Rate the given episode
    /// </summary>
    /// <param name="episodeId">Episode Id</param>
    /// <param name="rating">Rating we want to give for episode</param>
    /// <returns>Current rating of episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Task<double> RateEpisodeAsync(int episodeId, int rating)
    {
      if (_userInfo == null)
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      if (rating < 0 || rating > 10)
        throw new ArgumentOutOfRangeException("rating", rating, "rating must be an integer between 0 and 10");
      return _downloader.RateEpisodeAsync(_userInfo.UserIdentifier, episodeId, rating);
    }

    /// <summary>
    /// Gets all series this user has already ratet
    /// </summary>
    /// <exception cref="TvdbUserNotFoundException">Thrown when no user is set</exception>
    /// <returns>A list of all rated series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Task<Dictionary<int, TvdbRating>> GetRatedSeriesAsync()
    {
      if (_userInfo == null)
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      return _downloader.DownloadAllSeriesRatingsAsync(_userInfo.UserIdentifier);
    }

    /// <summary>
    /// Gets all series this user has already ratet
    /// </summary>
    /// <param name="seriesId">Id of series</param>
    /// <exception cref="TvdbUserNotFoundException">Thrown when no user is set</exception>
    /// <returns>A list of all ratings for the series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Task<Dictionary<int, TvdbRating>> GetRatingsForSeriesAsync(int seriesId)
    {
      if (_userInfo == null)
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      return _downloader.DownloadRatingsForSeriesAsync(_userInfo.UserIdentifier, seriesId);
    }

    #endregion
  }
}
