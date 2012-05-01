/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TvdbLib.Data;
using System.Net;
using TvdbLib.Cache;
using TvdbLib.Data.Banner;
using TvdbLib.Xml;
using System.Diagnostics;
using TvdbLib.Exceptions;
using TvdbLib.Data.Comparer;

namespace TvdbLib
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
    private ICacheProvider m_cacheProvider;
    private String m_apiKey;
    private TvdbUser m_userInfo;
    private TvdbDownloader m_downloader;
    private TvdbData m_loadedData;
    private bool m_abortUpdate = false;
    private bool m_abortUpdateSaveChanges = false;
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
      /// <param name="_currentUpdateStage">The current state of the updating progress</param>
      /// <param name="_currentUpdateDescription">Description of the current update stage</param>
      /// <param name="_currentStageProgress">Progress of the current stage</param>
      /// <param name="_overallProgress">Overall progress of the update</param>
      public UpdateProgressEventArgs(UpdateStage _currentUpdateStage, String _currentUpdateDescription,
                                     int _currentStageProgress, int _overallProgress)
      {
        CurrentUpdateStage = _currentUpdateStage;
        CurrentUpdateDescription = _currentUpdateDescription;
        CurrentStageProgress = _currentStageProgress;
        OverallProgress = _overallProgress;
      }

      /// <summary>
      /// The current state of the updating progress
      /// </summary>
      public enum UpdateStage
      {
        /// <summary>
        /// we're currently downloading the update files from http://thetvdb.com
        /// </summary>
        downloading = 0,
        /// <summary>
        /// we're currently processing the updated series
        /// </summary>
        seriesupdate = 1,
        /// <summary>
        /// we're currently processing the updated episodes
        /// </summary>
        episodesupdate = 2,
        /// <summary>
        /// we're currently processing the updated banner
        /// </summary>
        bannerupdate = 3,
        /// <summary>
        /// the updating itself has finished, do cleanup work
        /// </summary>
        finishupdate = 4
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
      /// <param name="_started">When did the update start</param>
      /// <param name="_ended">When did the update finish</param>
      /// <param name="_updatedSeries">List of all series (ids) that were updated</param>
      /// <param name="_updatedEpisodes">List of all episode (ids)that were updated</param>
      /// <param name="_updatedBanners">List of all banners (ids) that were updated</param>
      public UpdateFinishedEventArgs(DateTime _started, DateTime _ended, List<int> _updatedSeries,
                                     List<int> _updatedEpisodes, List<int> _updatedBanners)
      {
        UpdateStarted = _started;
        UpdateFinished = _ended;
        UpdatedSeries = _updatedSeries;
        UpdatedEpisodes = _updatedEpisodes;
        UpdatedBanners = _updatedBanners;
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
      get { return m_userInfo; }
      set
      {
        m_userInfo = value;
        if (m_cacheProvider != null)
        {
          //try to load the userinfo from cache
          TvdbUser user = m_cacheProvider.LoadUserInfoFromCache(value.UserIdentifier);
          if (user != null)
          {
            m_userInfo.UserFavorites = user.UserFavorites;
            m_userInfo.UserPreferredLanguage = user.UserPreferredLanguage;
            m_userInfo.UserName = user.UserName;
          }
        }
      }
    }

    /// <summary>
    /// Unique id for every project that is using thetvdb
    /// 
    /// More information on: http://thetvdb.com/wiki/index.php/Programmers_API
    /// </summary>
    public String ApiKey
    {
      get { return m_apiKey; }
    }

    /// <summary>
    /// <para>Creates a new Tvdb handler</para>
    /// <para>The tvdb handler is used not only for downloading data from thetvdb but also to cache the downloaded data to a persistent storage,
    ///       handle user specific tasks and keep the downloaded data consistent with the online data (via the updates api)</para>
    /// </summary>
    /// <param name="_apiKey">The api key used for downloading data from thetvdb -> see http://thetvdb.com/wiki/index.php/Programmers_API</param>
    public TvdbHandler(String _apiKey)
    {
      m_apiKey = _apiKey; //store api key
      m_downloader = new TvdbDownloader(m_apiKey);
      m_cacheProvider = null;
    }

    /// <summary>
    /// Creates a new Tvdb handler
    /// </summary>
    /// <param name="_cacheProvider">The cache provider used to store the information</param>
    /// <param name="_apiKey">Api key to use for this project</param>
    public TvdbHandler(ICacheProvider _cacheProvider, String _apiKey)
      : this(_apiKey)
    {
      m_cacheProvider = _cacheProvider; //store given cache provider
    }

    /// <summary>
    /// Load previously stored information on (except series information) from cache
    /// </summary>
    /// <returns>true if cache could be loaded successfully, false otherwise</returns>
    public bool InitCache()
    {
      if (m_cacheProvider != null)
      {
        TvdbData data = null;
        if (!m_cacheProvider.Initialised)
        {
          data = m_cacheProvider.InitCache();
          if (data != null)
          {//cache provider was initialised successfully
            m_loadedData = data;
            return true;
          }
          else
          {//couldn't init the cache provider
            return false;
          }
        }
        else
        {
          if (m_loadedData != null)
          {
            return true;
          }
          else
          {
            return false;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Is the handler using caching and is the cache initialised
    /// </summary>
    public bool IsCacheInitialised
    {
      get { return m_cacheProvider != null && m_cacheProvider.Initialised; }
    }


    /// <summary>
    /// Completely refreshes the cache (all stored information is lost) -> cache
    /// must be initialised to call this method
    /// </summary>
    /// <returns>true if the cache was cleared successfully, 
    ///          false otherwise (e.g. no write rights,...)</returns>
    public bool ClearCache()
    {
      if (m_cacheProvider != null && m_cacheProvider.Initialised)
      {
        return m_cacheProvider.ClearCache();
      }
      else return false;
    }

    /// <summary>
    /// Search for a seris on tvdb using the name of the series using the default language (english)
    /// </summary>
    /// <param name="_name">Name of series</param>
    /// <returns>List of possible hits (containing only very basic information (id, name,....)</returns>
    public List<TvdbSearchResult> SearchSeries(String _name)
    {
      List<TvdbSearchResult> retSeries = m_downloader.DownloadSearchResults(_name);

      return retSeries;
    }

    /// <summary>
    /// Search for a seris on tvdb using the name of the series
    /// </summary>
    /// <param name="_name">Name of series</param>
    /// <param name="_language">Language to search in</param>
    /// <returns>List of possible hits (containing only very basic information (id, name,....)</returns>
    public List<TvdbSearchResult> SearchSeries(String _name, TvdbLanguage _language)
    {
      List<TvdbSearchResult> retSeries = m_downloader.DownloadSearchResults(_name, _language);

      return retSeries;
    }

    /// <summary>
    /// Searches for a series by the id of an external provider
    /// </summary>
    /// <param name="_externalSite">external provider</param>
    /// <param name="_id">id of the series</param>
    /// <returns>The tvdb series that corresponds to the external id</returns>
    public TvdbSearchResult GetSeriesByRemoteId(ExternalId _externalSite, String _id)
    {
      TvdbSearchResult retSeries = m_downloader.DownloadSeriesSearchByExternalId(_externalSite, _id);

      return retSeries;
    }

    /// <summary>
    /// Gets the series with the given id either from cache (if it has already been loaded) or from 
    /// the selected tvdb mirror.
    /// 
    /// To check if this series has already been cached, use the Method IsCached(TvdbSeries _series)
    /// </summary>
    /// <exception cref="TvdbNotAvailableException">Tvdb is not available</exception>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <param name="_seriesId">id of series</param>
    /// <param name="_language">language that should be retrieved</param>
    /// <param name="_loadEpisodes">if true, the full series record will be loaded (series + all episodes), otherwise only the base record will be loaded which contains only series information</param>
    /// <param name="_loadActors">if true also loads the extended actor information</param>
    /// <param name="_loadBanners">if true also loads the paths to the banners</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    public TvdbSeries GetSeries(int _seriesId, TvdbLanguage _language, bool _loadEpisodes,
                                bool _loadActors, bool _loadBanners)
    {
      return GetSeries(_seriesId, _language, _loadEpisodes, _loadActors, _loadBanners, false);
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
    /// <param name="_seriesId">id of series</param>
    /// <param name="_language">language that should be retrieved</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    internal TvdbSeries GetSeriesZipped(int _seriesId, TvdbLanguage _language)
    {
      return GetSeries(_seriesId, _language, true, true, true, true);
    }

    /// <summary>
    /// Gets the series with the given id either from cache (if it has already been loaded) or from 
    /// the selected tvdb mirror. If you use zip the request automatically downloads the episodes, the actors and the banners, so you should also select those features.
    /// 
    /// To check if this series has already been cached, use the Method IsCached(TvdbSeries _series)
    /// </summary>
    /// <exception cref="TvdbNotAvailableException">Tvdb is not available</exception>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <param name="_seriesId">id of series</param>
    /// <param name="_language">language abbriviation of the series that should be retrieved</param>
    /// <param name="_loadEpisodes">if true, the full series record will be loaded (series + all episodes), otherwise only the base record will be loaded which contains only series information</param>
    /// <param name="_loadBanners">if true also loads the paths to the banners</param>
    /// <param name="_loadActors">if true also loads the extended actor information</param>
    /// <param name="_useZip">If this series is not already cached and the series has to be downloaded, the zipped version will be downloaded</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public TvdbSeries GetSeries(int _seriesId, TvdbLanguage _language, bool _loadEpisodes,
                                bool _loadActors, bool _loadBanners, bool _useZip)
    {
      Stopwatch watch = new Stopwatch();
      watch.Start();
      TvdbSeries series = GetSeriesFromCache(_seriesId);
      //Did I get the series completely from cache or did I have to make an additional online request
      bool loadedAdditionalInfo = false;

      if (series == null || //series not yet cached
          (_useZip && (!series.EpisodesLoaded && !series.TvdbActorsLoaded && !series.BannersLoaded)))//only the basic series info has been loaded -> zip is still faster than fetching the missing informations without using zip
      {//load complete series from tvdb
        if (_useZip)
        {
          series = m_downloader.DownloadSeriesZipped(_seriesId, _language);
        }
        else
        {
          series = m_downloader.DownloadSeries(_seriesId, _language, _loadEpisodes, _loadActors, _loadBanners);
        }

        if (series == null)
        {
          return null;
        }
        watch.Stop();
        loadedAdditionalInfo = true;
        Log.Info("Loaded series " + _seriesId + " in " + watch.ElapsedMilliseconds + " milliseconds");
        series.IsFavorite = m_userInfo == null ? false : CheckIfSeriesFavorite(_seriesId, m_userInfo.UserFavorites);
      }
      else
      {//some (if not all) information has already been loaded from tvdb at some point -> fill the missing details and return the series

        if (_language != series.Language)
        {//user wants a different language than the one that has been loaded
          if (series.GetAvailableLanguages().Contains(_language))
          {
            series.SetLanguage(_language);
          }
          else
          {
            TvdbSeriesFields newFields = m_downloader.DownloadSeriesFields(_seriesId, _language);
            loadedAdditionalInfo = true;
            if (_loadEpisodes)
            {
              List<TvdbEpisode> epList = m_downloader.DownloadEpisodes(_seriesId, _language);
              if (epList != null)
              {
                newFields.Episodes = epList;
                newFields.EpisodesLoaded = true;
              }
            }
            if (newFields != null)
            {
              series.AddLanguage(newFields);
              series.SetLanguage(_language);
            }
            else
            {
              Log.Warn("Couldn't load new language " + _language.Abbriviation + " for series " + _seriesId);
              return null;
            }
          }
        }

        if (_loadActors && !series.TvdbActorsLoaded)
        {//user wants actors loaded
          Log.Debug("Additionally loading actors");
          List<TvdbActor> actorList = m_downloader.DownloadActors(_seriesId);
          loadedAdditionalInfo = true;
          if (actorList != null)
          {
            series.TvdbActorsLoaded = true;
            series.TvdbActors = actorList;
          }
        }

        if (_loadEpisodes && !series.EpisodesLoaded)
        {//user wants the full version but only the basic has been loaded (without episodes
          Log.Debug("Additionally loading episodes");
          List<TvdbEpisode> epList = m_downloader.DownloadEpisodes(_seriesId, _language);
          loadedAdditionalInfo = true;
          if (epList != null)
          {
            series.SetEpisodes(epList);
          }
        }

        if (_loadBanners && !series.BannersLoaded)
        {//user wants banners loaded but current series hasn't -> Do it baby
          Log.Debug("Additionally loading banners");
          List<TvdbBanner> bannerList = m_downloader.DownloadBanners(_seriesId);
          loadedAdditionalInfo = true;
          if (bannerList != null)
          {
            series.BannersLoaded = true;
            series.Banners = bannerList;
          }
        }

        watch.Stop();
        Log.Info("Loaded series " + _seriesId + " in " + watch.ElapsedMilliseconds + " milliseconds");
      }

      if (m_cacheProvider != null)
      {//we're using a cache provider
        //if we've loaded data from online source -> save to cache
        if (m_cacheProvider.Initialised && loadedAdditionalInfo)
        {
          Log.Info("Store series " + _seriesId + " with " + m_cacheProvider.ToString());
          m_cacheProvider.SaveToCache(series);
        }

        //Store a ref to the cacheprovider and series id in each banner, so the banners
        //can be stored/loaded to/from cache
        #region add cache provider/series id
        if (series.Banners != null)
        {
          series.Banners.ForEach(delegate(TvdbBanner b)
          {
            b.CacheProvider = m_cacheProvider;
            b.SeriesId = series.Id;
          });
        }

        if (series.Episodes != null)
        {
          series.Episodes.ForEach(delegate(TvdbEpisode e)
          {
            e.Banner.CacheProvider = m_cacheProvider;
            e.Banner.SeriesId = series.Id;
          });
        }

        if (series.TvdbActors != null)
        {
          series.TvdbActors.ForEach(delegate(TvdbActor a)
          {
            a.ActorImage.CacheProvider = m_cacheProvider;
            a.ActorImage.SeriesId = series.Id;
          });
        }
        #endregion


      }
      return series;
    }

    /// <summary>
    /// Gets the full series (including episode information and actors) with the given id either from cache 
    /// (if it has already been loaded) or from the selected tvdb mirror.
    /// 
    /// To check if this series has already been cached, pleas use the Method IsCached(TvdbSeries _series)
    /// </summary>
    /// <exception cref="TvdbNotAvailableException">Tvdb is not available</exception>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <param name="_seriesId">id of series</param>
    /// <param name="_language">language that should be retrieved</param>
    /// <param name="_loadBanners">if true also loads the paths to the banners</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public TvdbSeries GetFullSeries(int _seriesId, TvdbLanguage _language, bool _loadBanners)
    {
      return GetSeries(_seriesId, _language, true, true, _loadBanners);
    }

    /// <summary>
    /// Gets the basic series (without episode information and actors) with the given id either from cache 
    /// (if it has already been loaded) or from the selected tvdb mirror.
    /// 
    /// To check if this series has already been cached, please use the Method IsCached(TvdbSeries _series)
    /// </summary>
    /// <exception cref="TvdbNotAvailableException">Tvdb is not available</exception>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <param name="_seriesId">id of series</param>
    /// <param name="_language">language that should be retrieved</param>
    /// <param name="_loadBanners">if true also loads the paths to the banners</param>
    /// <returns>Instance of TvdbSeries containing all gained information</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public TvdbSeries GetBasicSeries(int _seriesId, TvdbLanguage _language, bool _loadBanners)
    {
      return GetSeries(_seriesId, _language, false, false, _loadBanners);
    }

    /// <summary>
    /// Returns if the series is locally cached
    /// </summary>
    /// <param name="_seriesId">Id of the series</param>
    /// <param name="_language">Language</param>
    /// <param name="_loadEpisodes">Load Episodes</param>
    /// <param name="_loadActors">Load Actors</param>
    /// <param name="_loadBanners">Load Banners</param>
    /// <returns>True if the series is cached in the given configuration</returns>
    public bool IsCached(int _seriesId, TvdbLanguage _language, bool _loadEpisodes,
                         bool _loadActors, bool _loadBanners)
    {
      if (m_cacheProvider != null && m_cacheProvider.Initialised)
      {
        return m_cacheProvider.IsCached(_seriesId, _language,
                                        _loadEpisodes, _loadBanners, _loadActors);
      }
      return false;
    }



    /// <summary>
    /// Retrieve the episode with the given id in the given language. 
    /// 
    /// Note that the episode is always downloaded from thetvdb since it would
    /// be practical to load each and every cached series to look for the 
    /// episode id
    /// </summary>
    /// <param name="_episodeId">id of the episode</param>
    /// <param name="_language">languageof the episode</param>
    /// <returns>The retrieved episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public TvdbEpisode GetEpisode(int _episodeId, TvdbLanguage _language)
    {
      return m_downloader.DownloadEpisode(_episodeId, _language);
    }

    /// <summary>
    /// Retrieve the episode with the given parameters. This function will find
    /// episodes that are already cached.
    /// </summary>
    /// <param name="_seriesId">id of the series</param>
    /// <param name="_seasonNr">season number of the episode</param>
    /// <param name="_episodeNr">number of the episode</param>
    /// <param name="_language">language of the episode</param>
    /// <param name="_order">The sorting order that should be user when downloading the episode</param>
    /// <returns>The retrieved episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public TvdbEpisode GetEpisode(int _seriesId, int _seasonNr, int _episodeNr,
                                  TvdbEpisode.EpisodeOrdering _order, TvdbLanguage _language)
    {
      TvdbEpisode episode = null;
      if (m_cacheProvider != null && m_cacheProvider.Initialised)
      {
        if (m_cacheProvider.IsCached(_seriesId, _language, true, false, false))
        {
          TvdbSeries series = m_cacheProvider.LoadSeriesFromCache(_seriesId);
          if (series.Language != _language)
          {
            series.SetLanguage(_language);
          }

          if (series.Episodes != null)
          {
            foreach (TvdbEpisode e in series.Episodes)
            {
              if (e.EpisodeNumber == _episodeNr && e.SeasonNumber == _seasonNr && _order == TvdbEpisode.EpisodeOrdering.DefaultOrder ||
                  e.DvdEpisodeNumber == _episodeNr && e.SeasonNumber == _seasonNr && _order == TvdbEpisode.EpisodeOrdering.DvdOrder ||
                  e.AbsoluteNumber == _episodeNr && _order == TvdbEpisode.EpisodeOrdering.AbsoluteOrder)
              {//We found the episode that matches the episode number according to the given ordering
                episode = e;
                break;
              }
            }
          }
        }
      }

      if (episode == null)
      {//we havn't found the episode -> download it
        episode = m_downloader.DownloadEpisode(_seriesId, _seasonNr, _episodeNr, _order, _language);
      }

      return episode;
    }

    /// <summary>
    /// Retrieve the episode with the given parameters.
    /// </summary>
    /// <param name="_seriesId">id of the series</param>
    /// <param name="_airDate">When did the episode air</param>
    /// <param name="_language">language of the episode</param>
    /// <exception cref="TvdbInvalidApiKeyException">The given api key is not valid</exception>
    /// <returns>The retrieved episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbContentNotFoundException">The episode/series/banner couldn't be located on the tvdb server.</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public TvdbEpisode GetEpisode(int _seriesId, DateTime _airDate, TvdbLanguage _language)
    {
      TvdbEpisode episode = null;
      if (m_cacheProvider != null && m_cacheProvider.Initialised)
      {
        if (m_cacheProvider.IsCached(_seriesId, _language, true, false, false))
        {
          TvdbSeries series = m_cacheProvider.LoadSeriesFromCache(_seriesId);
          if (series.Language != _language)
          {
            series.SetLanguage(_language);
          }

          foreach (TvdbEpisode e in series.Episodes)
          {
            if (e.FirstAired.Year == _airDate.Year && e.FirstAired.Month == _airDate.Month && e.FirstAired.Day == _airDate.Day)
            {//We found the episode that first aired at the given day
              episode = e;
              break;
            }
          }
        }
      }

      if (episode == null)
      {//we havn't found the episode -> download it
        episode = m_downloader.DownloadEpisode(_seriesId, _airDate, _language);
      }
      return episode;
    }

    /// <summary>
    /// Get the series from cache
    /// </summary>
    /// <param name="_seriesId">Id of series</param>
    /// <returns></returns>
    private TvdbSeries GetSeriesFromCache(int _seriesId)
    {
      //try to retrieve the series from the cache provider
      try
      {
        TvdbSeries series = m_cacheProvider.LoadSeriesFromCache(_seriesId);
        if (series != null)
        {
          series.IsFavorite = m_userInfo == null ? false : CheckIfSeriesFavorite(series.Id, m_userInfo.UserFavorites);
        }
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
    public bool UpdateAllSeries()
    {
      return UpdateAllSeries(false);
    }

    /// <summary>
    /// Update all the series with the updated information
    /// </summary>
    /// <param name="_zipped">download zipped file?</param>
    /// <exception cref="TvdbCacheNotInitialisedException">In order to update, the cache has to be initialised</exception>
    /// <returns>true if the update was successful, false otherwise</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public bool UpdateAllSeries(bool _zipped)
    {
      return UpdateAllSeries(Interval.automatic, _zipped);
    }

    /// <summary>
    /// Update all the series with the updated information
    /// </summary>
    /// <param name="_zipped">download zipped file?</param>
    /// <param name="_interval">Specifies the interval of the update (day, week, month)</param>
    /// <returns>true if the update was successful, false otherwise</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    /// <exception cref="TvdbCacheNotInitialisedException">In order to update, the cache has to be initialised</exception>
    public bool UpdateAllSeries(Interval _interval, bool _zipped)
    {
      if (m_loadedData == null)
      {//the cache hasn't been initialised yet
        throw new TvdbCacheNotInitialisedException("In order to update the series, "
                                                   + "the cache has to be initialisee");
      }



      if (_interval == Interval.automatic)
      {
        //MakeUpdate(Util.UpdateInterval.month);
        //return true;
        TimeSpan timespanLastUpdate = (DateTime.Now - m_loadedData.LastUpdated);
        //MakeUpdate(TvdbLinks.CreateUpdateLink(m_apiKey, TvdbLinks.UpdateInterval.day));
        if (timespanLastUpdate < new TimeSpan(1, 0, 0, 0))
        {//last update is less than a day ago -> make a daily update
          //MakeUpdate(TvdbLinks.CreateUpdateLink(m_apiKey, Util.UpdateInterval.day));
          return UpdateAllSeries(Interval.day, _zipped, false);
        }
        else if (timespanLastUpdate < new TimeSpan(7, 0, 0, 0))
        {//last update is less than a week ago -> make a weekly update
          //MakeUpdate(TvdbLinks.CreateUpdateLink(m_apiKey, Util.UpdateInterval.week));
          return UpdateAllSeries(Interval.week, _zipped, false);
        }
        else if (timespanLastUpdate < new TimeSpan(31, 0, 0, 0) ||
                  m_loadedData.LastUpdated == new DateTime())//lastUpdated not available -> make longest possible upgrade
        {//last update is less than a month ago -> make a monthly update
          //MakeUpdate(TvdbLinks.CreateUpdateLink(m_apiKey, Util.UpdateInterval.month));
          return UpdateAllSeries(Interval.month, _zipped, true);
        }
        else
        {//todo: Make a full update -> full update deosn't make sense... (do a complete re-scan?)
          Log.Warn("The last update occured longer than a month ago, to avoid data inconsistency, all cached series "
                   + "and episode informations is downloaded again");
          return UpdateAllSeries(Interval.month, _zipped, true);
        }
      }
      else if (_interval == Interval.day)
      {
        return UpdateAllSeries(_interval, _zipped, false);
      }
      else if (_interval == Interval.week)
      {
        return UpdateAllSeries(_interval, _zipped, false);
      }
      else if (_interval == Interval.month)
      {
        return UpdateAllSeries(_interval, _zipped, true);
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Update all the series with the updated information
    /// </summary>
    /// <param name="_zipped">download zipped file?</param>
    /// <param name="_interval">Specifies the interval of the update (day, week, month)</param>
    /// <param name="_reloadOldContent">If yes, will reload all series that haven't been updated longer than the update period (which means
    ///                                 that only a reload can guarantee that the data is up to date. Should only be used when the data hasn't
    ///                                 been updated for over a month (otherwise use monthly updates)</param>
    /// <returns>true if the update was successful, false otherwise</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbInvalidApiKeyException">The stored api key is invalid</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    /// <exception cref="TvdbCacheNotInitialisedException">In order to update, the cache has to be initialised</exception>
    public bool UpdateAllSeries(Interval _interval, bool _zipped, bool _reloadOldContent)
    {
      MakeUpdate(_interval, _zipped, _reloadOldContent);
      return true;
    }

    /// <summary>
    /// Gets the date of the last (successfull) update from thetvdb
    /// </summary>
    /// <returns>Date of last update or null if no previous update or cache not initialised</returns>
    public DateTime GetLastUpdate()
    {
      if (m_loadedData != null)
      {
        return m_loadedData.LastUpdated;
      }
      else
      {
        throw new TvdbCacheNotInitialisedException();
      }
    }

    /// <summary>
    /// Aborts the currently running Update
    /// </summary>
    /// <param name="_saveChangesMade">if true, all changes that have already been 
    ///             made will be saved to cache, if not they will be discarded</param>
    public void AbortUpdate(bool _saveChangesMade)
    {
      m_abortUpdate = true;
      m_abortUpdateSaveChanges = _saveChangesMade;
    }

    /// <summary>
    /// Make the update
    /// </summary>
    /// <param name="_interval">interval of update</param>
    /// <param name="_zipped">zipped downloading yes/no</param>
    /// <returns>true if successful, false otherwise</returns>
    private bool MakeUpdate(Interval _interval, bool _zipped, bool _reloadOldContent)
    {
      Log.Info("Started update (" + _interval.ToString() + ")");
      Stopwatch watch = new Stopwatch();
      watch.Start();
      DateTime startUpdate = DateTime.Now;
      if (UpdateProgressed != null)
      {//update has started, we're downloading the updated content from tvdb
        UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.downloading,
                                                     "Downloading " + (_zipped ? " zipped " : " unzipped") +
                                                     " updated content (" + _interval.ToString() + ")",
                                                     0, 0));
      }

      //update all flagged series
      List<TvdbSeries> updateSeries;
      List<TvdbEpisode> updateEpisodes;
      List<TvdbBanner> updateBanners;

      List<int> updatedSeriesIds = new List<int>();
      List<int> updatedEpisodeIds = new List<int>();
      List<int> updatedBannerIds = new List<int>();

      DateTime updateTime = m_downloader.DownloadUpdate(out updateSeries, out updateEpisodes, out updateBanners, _interval, _zipped);
      List<int> cachedSeries = m_cacheProvider.GetCachedSeries();

      //list of all series that have been loaded from cache 
      //and need to be saved to cache at the end of the update
      Dictionary<int, TvdbSeries> seriesToSave = new Dictionary<int, TvdbSeries>();

      if (UpdateProgressed != null)
      {//update has started, we're downloading the updated content from tvdb
        UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.seriesupdate,
                                                     "Begin updating series",
                                                     0, 25));
      }

      int countUpdatedSeries = updateSeries.Count;
      int countSeriesDone = 0;
      int lastProgress = 0;//send progress event at least every 1 percent
      String updateText = "Updating series";

      if (_reloadOldContent)
      {
        //if the last time an item (series or episode) was updated is longer ago than the timespan
        //we downloaded updates for, it's neccessary to completely reload the object to ensure that
        //the data is up to date.

        DateTime lastupdated = GetLastUpdate();

        TimeSpan span = new TimeSpan();
        switch (_interval)
        {
          case Interval.day:
            span = span.Add(new TimeSpan(1, 0, 0, 0));
            break;
          case Interval.week:
            span = span.Add(new TimeSpan(7, 0, 0, 0));
            break;
          case Interval.month:
            span = span.Add(new TimeSpan(30, 0, 0, 0));
            break;
        }



        if (lastupdated < DateTime.Now - span)
        {//the last update of the cache is longer ago than the timespan we make the update for
          List<int> allSeriesIds = m_cacheProvider.GetCachedSeries();
          foreach (int s in allSeriesIds)
          {
            //TvdbSeries series = m_cacheProvider.LoadSeriesFromCache(s);

            TvdbSeries series = null;
            if (seriesToSave.ContainsKey(s))
            {
              series = seriesToSave[s];
            }
            else
            {
              series = m_cacheProvider.LoadSeriesFromCache(s);
            }


            //ForceReload(series, false);
            if (series.LastUpdated > DateTime.Now - span)
            {
              //the series object is up to date, but some episodes might not be
              foreach (TvdbEpisode e in series.Episodes)
              {
                if (e.LastUpdated < DateTime.Now - span)
                {
                  if (Util.FindEpisodeInList(e.Id, updateEpisodes) == null)
                  {//The episode is not in the updates.xml file
                    TvdbEpisode newEp = new TvdbEpisode();
                    newEp.Id = e.Id;
                    newEp.LastUpdated = DateTime.Now;
                    updateEpisodes.Add(newEp);
                  }
                  if (!seriesToSave.ContainsKey(series.Id)) seriesToSave.Add(series.Id, series);

                }
              }
            }
            else
            {//the series hasn't been updated recently -> we need to do a complete re-download
              ForceReload(series, false);//redownload series and save it to cache
              countUpdatedSeries++;
              countSeriesDone++;
              int currProg = (int)(100.0 / countUpdatedSeries * countSeriesDone);
              updatedSeriesIds.Add(series.Id);
              updateText = "Reloaded series " + series.SeriesName + "(" + series.Id + ")";

              if (UpdateProgressed != null)
              {//update has started, we're downloading the updated content from tvdb
                UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.seriesupdate,
                                                             updateText, currProg, 25 + (int)(currProg / 4)));
              }
              //if (!seriesToSave.ContainsKey(series.Id)) seriesToSave.Add(series.Id, series);
            }
          }
        }
      }

      foreach (TvdbSeries us in updateSeries)
      {
        if (m_abortUpdate) break;//the update has been aborted
        //Update series that have been already cached
        foreach (int s in cachedSeries)
        {
          if (us.Id == s)
          {//changes occured in series
            TvdbSeries series = null;
            if (seriesToSave.ContainsKey(s))
            {
              series = seriesToSave[s];
            }
            else
            {
              series = m_cacheProvider.LoadSeriesFromCache(s);
            }

            int currProg = (int)(100.0 / countUpdatedSeries * countSeriesDone);

            bool updated = UpdateSeries(series, us.LastUpdated, currProg);
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
                UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.seriesupdate,
                                                             updateText, currProg, 25 + (int)(currProg / 4)));
              }
              lastProgress = currProg;
            }

            break;
          }
        }
        countSeriesDone++;
      }

      int countEpisodeUpdates = updateEpisodes.Count; ;
      int countEpisodesDone = 0;
      lastProgress = 0;
      updateText = "Updating episodes";
      //update all flagged episodes
      foreach (TvdbEpisode ue in updateEpisodes)
      {
        if (m_abortUpdate) break;//the update has been aborted

        foreach (int s in cachedSeries)
        {
          if (ue.SeriesId == s)
          {//changes occured in series
            TvdbSeries series = null;
            if (seriesToSave.ContainsKey(s))
            {
              series = seriesToSave[s];
            }
            else
            {
              series = m_cacheProvider.LoadSeriesFromCache(ue.SeriesId);
            }

            int progress = (int)(100.0 / countEpisodeUpdates * countEpisodesDone);
            String text = "";
            bool updated = UpdateEpisode(series, ue, progress, out text);
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
                UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.episodesupdate,
                                                             updateText, progress, 50 + (int)(progress / 4)));
              }
            }
            break;
          }
        }
        countEpisodesDone++;
      }

      int countUpdatedBanner = updateBanners.Count;
      int countBannerDone = 0;
      lastProgress = 0;
      updateText = "Updating banners";
      // todo: update banner information here -> wait for forum response regarding 
      // missing banner id within updates (atm. I'm matching banners via path)
      foreach (TvdbBanner b in updateBanners)
      {
        if (m_abortUpdate) break;//the update has been aborted

        foreach (int s in cachedSeries)
        {
          if (b.SeriesId == s)
          {//banner for this series has changed
            int currProg = (int)(100.0 / countUpdatedBanner * countBannerDone);
            TvdbSeries series = null;
            if (seriesToSave.ContainsKey(s))
            {
              series = seriesToSave[s];
            }
            else
            {
              series = m_cacheProvider.LoadSeriesFromCache(b.SeriesId);
            }
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

                UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.bannerupdate,
                                                             "Updating banner " + b.BannerPath + "(id=" + b.Id + ")",
                                                             currProg, 75 + (int)(currProg / 4)));
              }
            }
            break;
          }
        }
        countBannerDone++;
      }

      if (!m_abortUpdate)
      {//update has finished successfully
        if (UpdateProgressed != null)
        {
          UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.finishupdate,
                                                       "Update finished, saving loaded information to cache",
                                                       100, 100));
        }
      }
      else
      {//the update has been aborted
        if (UpdateProgressed != null)
        {
          UpdateProgressed(new UpdateProgressEventArgs(UpdateProgressEventArgs.UpdateStage.finishupdate,
                                                       "Update aborted by user, " +
                                                       (m_abortUpdateSaveChanges ? " saving " : " not saving ") +
                                                       "already loaded information to cache",
                                                       100, 100));
        }
      }

      if (!m_abortUpdate || m_abortUpdateSaveChanges)
      {//store the information we downloaded to cache
        Log.Info("Saving all series to cache that have been modified during the update (" + seriesToSave.Count + ")");
        foreach (KeyValuePair<int, TvdbSeries> kvp in seriesToSave)
        {//Save all series to cache that have been modified during the update
          try
          {
            m_cacheProvider.SaveToCache(kvp.Value);
          }
          catch (Exception ex)
          {
            Log.Warn("Couldn't save " + kvp.Key + " to cache: " + ex.ToString());
          }
        }
      }

      if (!m_abortUpdate)
      {//update finished and wasn't aborted
        //set the last updated time to time of this update
        m_loadedData.LastUpdated = updateTime;
        m_cacheProvider.SaveToCache(m_loadedData);
      }

      watch.Stop();

      Log.Info("Finished update (" + _interval.ToString() + ") in " + watch.ElapsedMilliseconds + " milliseconds");
      if (UpdateFinished != null)
      {
        if (!m_abortUpdate || m_abortUpdateSaveChanges)
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


    private void ReloadEpisode(TvdbSeries series, TvdbEpisode e)
    {
      throw new NotImplementedException();
    }

    #region updating of banners, episodes and series
    /// <summary>
    /// Update the series with the banner
    /// </summary>
    /// <param name="_series"></param>
    /// <param name="_banner"></param>
    /// <returns>true, if the banner was updated successfully, false otherwise</returns>
    private bool UpdateBanner(TvdbSeries _series, TvdbBanner _banner)
    {
      if (!_series.BannersLoaded)
      {//banners for this series havn't been loaded -> don't update banners
        Log.Debug("Not handling banner " + _banner.BannerPath + " because series " + _series.Id
                  + " doesn't have banners loaded");
        return false;
      }

      foreach (TvdbBanner b in _series.Banners)
      {
        if (_banner.GetType() == b.GetType() && _banner.BannerPath.Equals(b.BannerPath))
        {//banner was found
          if (b.LastUpdated < _banner.LastUpdated)
          {//update time of local banner is longer ago than update time of current update
            b.LastUpdated = _banner.LastUpdated;
            b.CacheProvider = m_cacheProvider;
            b.SeriesId = _series.Id;

            if (b.IsLoaded)
            {//the banner was previously loaded and is updated -> discard the previous image
              b.LoadBanner(null);
            }
            b.UnloadBanner(false);
            if (_banner.GetType() == typeof(TvdbBannerWithThumb))
            {
              TvdbBannerWithThumb thumb = (TvdbBannerWithThumb)b;
              if (thumb.IsThumbLoaded)
              {
                thumb.LoadThumb(null);
              }
              thumb.UnloadThumb(false);
            }


            if (_banner.GetType() == typeof(TvdbFanartBanner))
            {//update fanart specific content
              TvdbFanartBanner fanart = (TvdbFanartBanner)b;

              fanart.Resolution = ((TvdbFanartBanner)_banner).Resolution;
              if (fanart.IsThumbLoaded)
              {
                fanart.LoadThumb(null);
              }
              fanart.UnloadThumb(false);

              if (fanart.IsVignetteLoaded)
              {
                fanart.LoadVignette(null);
              }
              fanart.UnloadVignette();
            }

            Log.Info("Replacing banner " + _banner.Id);
            return true;
          }
          else
          {
            Log.Debug("Not replacing banner " + _banner.Id + " because it's not newer than current image");
            return false;
          }
        }
      }

      //banner not found -> add it to bannerlist
      Log.Info("Adding banner " + _banner.Id);
      _series.Banners.Add(_banner);
      return true;
    }

    /// <summary>
    /// Update the series with the episode (Add it to the series if it doesn't already exist or update the episode if the current episode is older than the updated one)
    /// </summary>
    /// <param name="_series">Series of the updating episode</param>
    /// <param name="_episode">Episode that is updated</param>
    /// <param name="_progress">Progress of the update run</param>
    /// <param name="_text">Description of the current update</param>
    /// <returns>true if episode has been updated, false if not (e.g. timestamp of updated episode older than
    ///          timestamp of existing episode</returns> 
    private bool UpdateEpisode(TvdbSeries _series, TvdbEpisode _episode, int _progress, out String _text)
    {
      bool updateDone = false;
      _text = "";
      TvdbLanguage currentLanguage = _series.Language;
      foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in _series.SeriesTranslations)
      {
        if (_series.EpisodesLoaded)
        {
          bool found = false;
          List<TvdbEpisode> eps = kvp.Value.Episodes;

          if (eps != null && eps.Count > 0)
          {
            //check all episodes if the updated episode is in it
            foreach (TvdbEpisode e in eps)
            {
              if (e.Id == _episode.Id)
              {
                found = true;
                if (e.LastUpdated < _episode.LastUpdated)
                {
                  //download episode which has been updated
                  TvdbEpisode newEpisode = null;
                  try
                  {
                    newEpisode = m_downloader.DownloadEpisode(e.Id, kvp.Key);
                  }
                  catch (TvdbContentNotFoundException)
                  {
                    Log.Warn("Couldn't download episode " + e.Id + "(" + e.EpisodeName + ")");
                  }
                  //update information of episode with new episodes informations
                  if (newEpisode != null)
                  {
                    newEpisode.LastUpdated = _episode.LastUpdated;


                    #region fix for http://forums.thetvdb.com/viewtopic.php?f=8&t=3993
                    //xml of single episodes doesn't contain  CombinedSeason and CombinedEpisodeNumber, so if
                    //we have values for those, don't override them
                    //-> http://forums.thetvdb.com/viewtopic.php?f=8&t=3993
                    //todo: remove this once tvdb fixed that issue
                    if (e.CombinedSeason != -99 && newEpisode.CombinedSeason == 0)
                    {
                      newEpisode.CombinedSeason = e.CombinedSeason;
                    }
                    if (e.CombinedEpisodeNumber != -99 && newEpisode.CombinedEpisodeNumber == 0)
                    {
                      newEpisode.CombinedEpisodeNumber = e.CombinedEpisodeNumber;
                    }
                    #endregion

                    e.UpdateEpisodeInfo(newEpisode);

                    e.Banner.CacheProvider = m_cacheProvider;
                    e.Banner.SeriesId = _series.Id;

                    e.Banner.UnloadBanner(false);
                    e.Banner.UnloadThumb(false);

                    _text = "Added/Updated episode " + _series.SeriesName + " " + e.SeasonNumber +
                            "x" + e.EpisodeNumber + "(id: " + e.Id + ")";
                    Log.Info("Updated Episode " + e.SeasonNumber + "x" + e.EpisodeNumber + " for series " + _series.SeriesName +
                             "(id: " + e.Id + ", lang: " + e.Language.Abbriviation + ")");
                    updateDone = true;
                  }
                }
                break;
              }
            }
          }

          if (!found)
          {
            //episode hasn't been found
            //hasn't been found -> add it to series
            TvdbEpisode ep = null;
            try
            {
              ep = m_downloader.DownloadEpisode(_episode.Id, kvp.Key);
            }
            catch (TvdbContentNotFoundException ex)
            {
              Log.Warn("Problem downloading " + _episode.Id + ": " + ex.ToString());
            }
            if (ep != null)
            {
              kvp.Value.Episodes.Add(ep);
              //sort the episodes according to default (aired) order
              kvp.Value.Episodes.Sort(new EpisodeComparerAired());
              _text = "Added/Updated episode " + _series.SeriesName + " " + ep.SeasonNumber +
                      "x" + ep.EpisodeNumber + "(id: " + ep.Id + ")";

              Log.Info("Added Episode " + ep.SeasonNumber + "x" + ep.EpisodeNumber + " for series " + _series.SeriesName +
                       "(id: " + ep.Id + ", lang: " + ep.Language.Abbriviation + ")");
              updateDone = true;
            }
          }
        }
        else
        {
          Log.Debug("Not handling episode " + _episode.Id + ", because series " + _series.SeriesName + " hasn't loaded episodes");
          updateDone = false;
        }
      }
      _series.SetLanguage(currentLanguage);
      return updateDone;
    }



    /// <summary>
    /// Download the new series and update the information
    /// </summary>
    /// <param name="_series">Series to update</param>
    /// <param name="_lastUpdated">When was the last update made</param>
    /// <param name="_progress">The progress done until now</param>
    /// <returns>true if the series has been upated false if not</returns>
    private bool UpdateSeries(TvdbSeries _series, DateTime _lastUpdated, int _progress)
    {
      //get series info
      bool updateDone = false;
      TvdbLanguage currentLanguage = _series.Language;
      foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in _series.SeriesTranslations)
      {
        if (kvp.Value.LastUpdated < _lastUpdated)
        {//the local content is older than the update time in the updates xml files
          TvdbSeries newSeries = null;
          try
          {//try to get the series
            newSeries = m_downloader.DownloadSeries(_series.Id, kvp.Key, false, false, false);
          }
          catch (TvdbContentNotFoundException ex)
          {//couldn't download the series
            Log.Warn("Problem downloading series (id: " + _series.Id + "): " + ex.ToString());
          }

          if (newSeries != null)
          {//download of the series successfull -> do updating
            newSeries.LastUpdated = _lastUpdated;

            //don't replace episodes, since we're only loading basic series
            kvp.Value.UpdateTvdbFields(newSeries, false);

            //kvp.Value.Update (newSeries);
            Log.Info("Updated Series " + _series.SeriesName + " (id: " + _series.Id + ", " +
                     kvp.Key.Abbriviation + ")");
            updateDone = true;
          }
        }
      }
      _series.SetLanguage(currentLanguage);//to copy the episode-fields to the base series
      return updateDone;
    }
    #endregion
    #endregion

    /// <summary>
    /// Returns list of all available Languages on tvdb
    /// </summary>
    /// <returns>list of available languages</returns>
    public List<TvdbLanguage> Languages
    {
      get
      {
        if (IsLanguagesCached)
        {
          return m_loadedData.LanguageList;
        }
        else
        {

          List<TvdbLanguage> list = m_downloader.DownloadLanguages();
          if (list != null && list.Count > 0)
          {
            if (m_loadedData != null)
            {
              m_loadedData.LanguageList = list;
            }
            return list;
          }
          else
          {
            return null;
          }
        }
      }
    }

    /// <summary>
    /// Reloads all language definitions from tvdb
    /// </summary>
    /// <returns>true if successful, false otherwise</returns>
    public bool ReloadLanguages()
    {
      List<TvdbLanguage> list = m_downloader.DownloadLanguages();
      if (list != null && list.Count > 0)
      {
        m_loadedData.LanguageList = list;
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Are the language definitions already cached
    /// </summary>
    public bool IsLanguagesCached
    {
      get
      {
        return (m_loadedData != null && m_loadedData.LanguageList != null && m_loadedData.LanguageList.Count > 0);
      }
    }

    /// <summary>
    /// Closes the cache provider (should be called before exiting the application)
    /// </summary>
    public void CloseCache()
    {
      if (m_cacheProvider != null)
      {
        m_cacheProvider.SaveToCache(m_loadedData);
        if (m_userInfo != null) m_cacheProvider.SaveToCache(m_userInfo);
        m_cacheProvider.CloseCache();
      }
    }


    /// <summary>
    /// Returns all series id's that are already cached in memory or locally via the cacheprovider
    /// </summary>
    /// <returns>List of loaded series</returns>
    public List<int> GetCachedSeries()
    {
      if (m_cacheProvider.Initialised)
      {
        List<int> retList = new List<int>();

        //add series that are stored with the cacheprovider
        if (m_cacheProvider != null)
        {
          retList.AddRange(m_cacheProvider.GetCachedSeries());
        }

        return retList;
      }
      else
      {
        throw new TvdbCacheNotInitialisedException("The cache has to be initialised first");
      }
    }


    /// <summary>
    /// Forces a complete reload of the series. All information that has already been loaded (including loaded images!) will be deleted and reloaded from tvdb -> if you only want to update the series, use the "MakeUpdate" method
    /// </summary>
    /// <param name="_series">Series to reload</param>
    /// <returns>The new TvdbSeries object</returns>
    public TvdbSeries ForceReload(TvdbSeries _series)
    {
      return ForceReload(_series, _series.EpisodesLoaded, _series.TvdbActorsLoaded, _series.BannersLoaded);
    }

    /// <summary>
    /// Forces a complete reload of the series. All information that has already been loaded will be deleted and reloaded from tvdb -> if you only want to update the series, use the "MakeUpdate" method
    /// </summary>
    /// <param name="_series">Series to reload</param> 
    /// <param name="_deleteArtwork">If yes, also deletes previously loaded images</param>
    /// <returns>The new TvdbSeries object</returns>
    public TvdbSeries ForceReload(TvdbSeries _series, bool _deleteArtwork)
    {
      return ForceReload(_series, _series.EpisodesLoaded, _series.TvdbActorsLoaded, _series.BannersLoaded, _deleteArtwork);
    }

    /// <summary>
    /// Forces a complete reload of the series. All information that has already been loaded (including loaded images!) will be deleted and reloaded from tvdb -> if you only want to update the series, use the "MakeUpdate" method
    /// </summary>
    /// <param name="_series">Series to update</param>
    /// <param name="_loadEpisodes">Should episodes be loaded as well</param>
    /// <param name="_loadActors">Should actors be loaded as well</param>
    /// <param name="_loadBanners">Should banners be loaded as well</param>
    /// <returns>The new TvdbSeries object</returns>
    public TvdbSeries ForceReload(TvdbSeries _series, bool _loadEpisodes,
                                bool _loadActors, bool _loadBanners)
    {
      return ForceReload(_series, _loadEpisodes, _loadActors, _loadBanners, true);
    }


    /// <summary>
    /// Forces a complete reload of the series. All information that has already been loaded will be deleted and reloaded from tvdb -> if you only want to update the series, use the "MakeUpdate" method
    /// </summary>
    /// <param name="_series">Series to update</param>
    /// <param name="_loadEpisodes">Should episodes be loaded as well</param>
    /// <param name="_loadActors">Should actors be loaded as well</param>
    /// <param name="_loadBanners">Should banners be loaded as well</param>
    /// <param name="_deleteArtwork">If yes, also deletes previously loaded images</param>
    /// <returns>The new TvdbSeries object</returns>
    public TvdbSeries ForceReload(TvdbSeries _series, bool _loadEpisodes,
                                bool _loadActors, bool _loadBanners, bool _replaceArtwork)
    {
      if (_series != null)
      {
        TvdbSeries newSeries = m_downloader.DownloadSeries(_series.Id, _series.Language, _loadEpisodes,
                                                           _loadActors, _loadBanners);

        if (newSeries != null)
        {
          if (m_cacheProvider != null && m_cacheProvider.Initialised)
          {//remove old series from cache and store the reloaded one
            if (_replaceArtwork) m_cacheProvider.RemoveFromCache(_series.Id);//removes all info (including images)
            m_cacheProvider.SaveToCache(newSeries);
          }

          return newSeries;
        }
        else
        {
          Log.Warn("Couldn't load series " + _series.Id + ", reloading not successful");
          return null;
        }
      }
      else
      {
        Log.Warn("no series given (null)");
        return null;
      }
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
    public TvdbLanguage GetPreferredLanguage()
    {
      if (m_userInfo != null)
      {
        TvdbLanguage userLang = m_downloader.DownloadUserPreferredLanguage(m_userInfo.UserIdentifier);

        if (userLang != null)
        {
          //only one language is contained in the userlang file
          foreach (TvdbLanguage l in m_loadedData.LanguageList)
          {
            if (l.Abbriviation.Equals(userLang.Abbriviation)) return l;
          }
          return userLang;//couldn't find language -> return new instance
        }
        else
        {
          return null; //problem with parsing xml file
        }
      }
      else
      {
        throw new TvdbUserNotFoundException("You can't get the preferred language when no user is specified");
      }
    }

    #region user favorites and rating

    /// <summary>
    /// Check if series is in the list of favorites
    /// </summary>
    /// <param name="_series"></param>
    /// <param name="_favs"></param>
    /// <returns></returns>
    private bool CheckIfSeriesFavorite(int _series, List<int> _favs)
    {
      if (_favs == null) return false;
      foreach (int f in _favs)
      {
        if (_series == f)
        {//series is a favorite
          return true;
        }
      }
      return false;
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
    public List<int> GetUserFavouritesList()
    {
      if (m_userInfo != null)
      {
        List<int> userFavs = m_downloader.DownloadUserFavoriteList(m_userInfo.UserIdentifier);
        m_userInfo.UserFavorites = userFavs;
        return userFavs;
      }
      else
      {
        throw new Exception("You can't get the list of user favorites when no user is specified");
      }
    }

    /// <summary>
    /// Get the favorite series of the user (only basic series information will be loaded)
    /// </summary>
    /// <param name="_lang">Which language should be used</param>
    /// <returns>List of favorite series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<TvdbSeries> GetUserFavorites(TvdbLanguage _lang)
    {
      if (m_userInfo != null)
      {
        if (_lang != null)
        {
          List<int> idList = GetUserFavouritesList();
          List<TvdbSeries> retList = new List<TvdbSeries>();

          foreach (int sId in idList)
          {
            if (IsCached(sId, _lang, false, false, false))
            {
              retList.Add(GetSeriesFromCache(sId));
            }
            else
            {
              TvdbSeries series = m_downloader.DownloadSeries(sId, _lang, false, false, false);
              if (series != null)
              {
                retList.Add(series);
              }

              //since we have downloaded the basic series -> if we have a cache provider, also
              //save the series via our CacheProvider
              if (m_cacheProvider != null && m_cacheProvider.Initialised)
              {
                m_cacheProvider.SaveToCache(series);
              }
            }
          }
          return retList;
        }
        else
        {
          throw new Exception("you have to define a language");
        }
      }
      else
      {
        throw new TvdbUserNotFoundException("You can't get the favourites when no user is defined");
      }
    }

    /// <summary>
    /// Adds the series id to the users list of favorites and returns the new list of
    /// favorites
    /// </summary>
    /// <param name="_seriesId">series to add to the favorites</param>
    /// <returns>new list with all favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<int> AddSeriesToFavorites(int _seriesId)
    {
      if (m_userInfo != null)
      {
        List<int> list = m_downloader.DownloadUserFavoriteList(m_userInfo.UserIdentifier,
                                                      Util.UserFavouriteAction.add,
                                                      _seriesId);

        m_userInfo.UserFavorites = list;
        return list;
      }
      else
      {
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      }
    }

    /// <summary>
    /// Adds the series to the users list of favorites and returns the new list of
    /// favorites
    /// </summary>
    /// <param name="_series">series to add to the favorites</param>
    /// <returns>new list with all favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<int> AddSeriesToFavorites(TvdbSeries _series)
    {
      if (_series == null) return null;
      return AddSeriesToFavorites(_series.Id);
    }

    /// <summary>
    /// Removes the series id from the users list of favorites and returns the new list of
    /// favorites
    /// </summary>
    /// <param name="_seriesId">series to remove from the favorites</param>
    /// <returns>new list with all favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<int> RemoveSeriesFromFavorites(int _seriesId)
    {
      if (m_userInfo != null)
      {

        List<int> list = m_downloader.DownloadUserFavoriteList(m_userInfo.UserIdentifier,
                                                      Util.UserFavouriteAction.remove,
                                                      _seriesId);
        m_userInfo.UserFavorites = list;
        return list;
      }
      else
      {
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      }
    }

    /// <summary>
    /// Removes the series from the users list of favorites and returns the new list of
    /// favorites
    /// </summary>
    /// <param name="_series">series to remove from the favorites</param>
    /// <returns>new list with all favorites</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">The tvdb database is unavailable</exception>
    public List<int> RemoveSeriesFromFavorites(TvdbSeries _series)
    {
      return RemoveSeriesFromFavorites(_series.Id);
    }


    /// <summary>
    /// Rate the given series
    /// </summary>
    /// <param name="_seriesId">series id</param>
    /// <param name="_rating">The rating we want to give for this series</param>
    /// <returns>Current rating of the series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double RateSeries(int _seriesId, int _rating)
    {
      if (m_userInfo != null)
      {
        if (_rating < 0 || _rating > 10)
        {
          throw new ArgumentOutOfRangeException("rating must be an integer between 0 and 10");
        }
        return m_downloader.RateSeries(m_userInfo.UserIdentifier, _seriesId, _rating);
      }
      else
      {
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      }
    }

    /// <summary>
    /// Rate the given episode
    /// </summary>
    /// <param name="_episodeId">Episode Id</param>
    /// <param name="_rating">Rating we want to give for episode</param>
    /// <returns>Current rating of episode</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public double RateEpisode(int _episodeId, int _rating)
    {
      if (m_userInfo != null)
      {
        if (_rating < 0 || _rating > 10)
        {
          throw new ArgumentOutOfRangeException("rating must be an integer between 0 and 10");
        }
        return m_downloader.RateEpisode(m_userInfo.UserIdentifier, _episodeId, _rating);
      }
      else
      {
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      }
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
    public Dictionary<int, TvdbRating> GetRatedSeries()
    {
      if (m_userInfo != null)
      {
        return m_downloader.DownloadAllSeriesRatings(m_userInfo.UserIdentifier);
      }
      else
      {
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      }
    }

    /// <summary>
    /// Gets all series this user has already ratet
    /// </summary>
    /// <param name="_seriesId">Id of series</param>
    /// <exception cref="TvdbUserNotFoundException">Thrown when no user is set</exception>
    /// <returns>A list of all ratings for the series</returns>
    /// <exception cref="TvdbInvalidXmlException"><para>Exception is thrown when there was an error parsing the xml files. </para>
    ///                                           <para>Feel free to post a detailed description of this issue on http://code.google.com/p/tvdblib 
    ///                                           or http://forums.thetvdb.com/</para></exception>  
    /// <exception cref="TvdbUserNotFoundException">The user doesn't exist</exception>
    /// <exception cref="TvdbNotAvailableException">Exception is thrown when thetvdb isn't available.</exception>
    public Dictionary<int, TvdbRating> GetRatingsForSeries(int _seriesId)
    {
      if (m_userInfo != null)
      {
        return m_downloader.DownloadRatingsForSeries(m_userInfo.UserIdentifier, _seriesId);
      }
      else
      {
        throw new TvdbUserNotFoundException("You can only add favorites if a user is set");
      }
    }

    #endregion


  }
}
