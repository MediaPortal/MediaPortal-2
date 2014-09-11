#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities;
using TraktSettings = MediaPortal.UiComponents.Trakt.Settings.TraktSettings;

namespace MediaPortal.UiComponents.Trakt.Models
{
  public class TraktSetupModel : IWorkflowModel
  {
    #region Consts

    public const string TRAKT_SETUP_MODEL_ID_STR = "65E4F7CA-3C9C-4538-966D-2A896BFEF4D3";
    public const string SERIES_BANNER_URL = "http://trakt.tv/user/{0}/widgets/watched/episode-thin-banner@2x.jpg";
    public const string MOVIES_BANNER_URL = "http://trakt.tv/user/{0}/widgets/watched/movie-thin-banner@2x.jpg";

    public readonly static Guid TRAKT_SETUP_MODEL_ID = new Guid(TRAKT_SETUP_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected readonly AbstractProperty _isEnabledProperty = new WProperty(typeof(bool), false);
    protected readonly AbstractProperty _isSynchronizingProperty = new WProperty(typeof(bool), false);
    protected readonly AbstractProperty _usermameProperty = new WProperty(typeof(string), null);
    protected readonly AbstractProperty _passwordProperty = new WProperty(typeof(string), null);
    protected readonly AbstractProperty _testStatusProperty = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _seriesBannerProperty = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _moviesBannerProperty = new WProperty(typeof(string), string.Empty);

    #endregion

    #region Public properties - Bindable Data

    public AbstractProperty IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool)_isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public AbstractProperty IsSynchronizingProperty
    {
      get { return _isSynchronizingProperty; }
    }

    public bool IsSynchronizing
    {
      get { return (bool)_isSynchronizingProperty.GetValue(); }
      set { _isSynchronizingProperty.SetValue(value); }
    }

    public AbstractProperty UsernameProperty
    {
      get { return _usermameProperty; }
    }

    public string Username
    {
      get { return (string)_usermameProperty.GetValue(); }
      set { _usermameProperty.SetValue(value); }
    }

    public AbstractProperty PasswordProperty
    {
      get { return _passwordProperty; }
    }

    public string Password
    {
      get { return (string)_passwordProperty.GetValue(); }
      set { _passwordProperty.SetValue(value); }
    }

    public AbstractProperty TestStatusProperty
    {
      get { return _testStatusProperty; }
    }

    public string TestStatus
    {
      get { return (string)_testStatusProperty.GetValue(); }
      set { _testStatusProperty.SetValue(value); }
    }

    public AbstractProperty SeriesBannerProperty
    {
      get { return _seriesBannerProperty; }
    }

    public string SeriesBanner
    {
      get { return (string)_seriesBannerProperty.GetValue(); }
      set { _seriesBannerProperty.SetValue(value); }
    }

    public AbstractProperty MoviesBannerProperty
    {
      get { return _moviesBannerProperty; }
    }

    public string MoviesBanner
    {
      get { return (string)_moviesBannerProperty.GetValue(); }
      set { _moviesBannerProperty.SetValue(value); }
    }

    #endregion

    #region Public methods - Commands

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      TraktSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TraktSettings>();
      settings.EnableTrakt = IsEnabled;
      settings.Authentication = new TraktAuthentication { Username = Username, Password = Password };
      // Save
      settingsManager.Save(settings);
    }

    /// <summary>
    /// Uses the current accound information and tries to validate them at trakt.
    /// </summary>
    public void TestAccount()
    {
      try
      {
        TraktResponse result = TraktAPI.TestAccount(new TraktAccount { Username = Username, Password = Password });
        if (!string.IsNullOrWhiteSpace(result.Error))
          TestStatus = result.Error;
        else if (!string.IsNullOrWhiteSpace(result.Message))
          TestStatus = result.Message;
        else
          TestStatus = string.Empty;
        BuildBannerUrls();
      }
      catch (Exception ex)
      {
        TestStatus = "Error";
        ServiceRegistration.Get<ILogger>().Error("Trakt.tv: Exception while testing account.", ex);
      }
    }

    public void SyncMediaToTrakt()
    {
      if (!IsSynchronizing)
      {
        IsSynchronizing = true;
        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
        threadPool.Add(SyncMediaToTrakt_Async, ThreadPriority.BelowNormal);
      }
    }

    public void SyncMediaToTrakt_Async()
    {
      if (SyncMovies() && SyncSeries())
      {
        TestStatus = "[Trakt.SyncFinished]";
      }
      IsSynchronizing = false;
      BuildBannerUrls();
    }

    public bool SyncMovies()
    {
      try
      {
        TestStatus = "[Trakt.SyncMovies]";
        Guid[] types = { MediaAspect.ASPECT_ID, MovieAspect.ASPECT_ID };
        var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (contentDirectory == null)
        {
          TestStatus = "[Trakt.MediaLibraryNotConnected]";
          return false;
        }
        var movies = contentDirectory.Search(new MediaItemQuery(types, null, null), true);
        TraktMovieSync syncData = new TraktMovieSync { UserName = Username, Password = Password, MovieList = new List<TraktMovieSync.Movie>() };
        // First send all movies to Trakt that we have so they appear in library
        foreach (var movie in movies)
          syncData.MovieList.Add(ToMovie(movie));

        TraktSyncModes traktSyncMode = TraktSyncModes.library;
        var response = TraktAPI.SyncMovieLibrary(syncData, traktSyncMode);
        ServiceRegistration.Get<ILogger>().Info("Trakt.tv: Movies '{0}': {1} inserted, {2} existing, {3} skipped movies.", traktSyncMode, response.Inserted, SafeCount(response.AlreadyExistMovies), SafeCount(response.SkippedMovies));

        syncData.MovieList.Clear();
        // Then send only the watched movies as "seen"
        foreach (var movie in movies.Where(IsWatched))
          syncData.MovieList.Add(ToMovie(movie));

        traktSyncMode = TraktSyncModes.seen;
        response = TraktAPI.SyncMovieLibrary(syncData, traktSyncMode);
        ServiceRegistration.Get<ILogger>().Info("Trakt.tv: Movies '{0}': {1} inserted, {2} existing, {3} skipped movies.", traktSyncMode, response.Inserted, SafeCount(response.AlreadyExistMovies), SafeCount(response.SkippedMovies));
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Trakt.tv: Exception while synchronizing media library.", ex);
      }
      return false;
    }

    public bool SyncSeries()
    {
      try
      {
        TestStatus = "[Trakt.SyncSeries]";
        Guid[] types = { MediaAspect.ASPECT_ID, SeriesAspect.ASPECT_ID };

        MediaItemQuery mediaItemQuery = new MediaItemQuery(types, null, null);
        var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (contentDirectory == null)
        {
          TestStatus = "[Trakt.MediaLibraryNotConnected]";
          return false;
        }
        var episodes = contentDirectory.Search(mediaItemQuery, true);

        var series = episodes.ToLookup(GetSeriesKey);
        foreach (var serie in series)
        {
          var imdbId = serie.Select(episode =>
          {
            string value;
            return MediaItemAspect.TryGetAttribute(episode.Aspects, SeriesAspect.ATTR_IMDB_ID, out value) ? value : null;
          }).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

          var tvdbId = serie.Select(episode =>
          {
            int value;
            return MediaItemAspect.TryGetAttribute(episode.Aspects, SeriesAspect.ATTR_TVDB_ID, out value) ? value : 0;
          }).FirstOrDefault(value => value != 0);

          TraktEpisodeSync syncData = new TraktEpisodeSync
          {
            UserName = Username,
            Password = Password,
            EpisodeList = new List<TraktEpisodeSync.Episode>(),
            Title = serie.Key,
            Year = serie.Min(e =>
            {
              int year;
              string seriesTitle;
              GetSeriesTitleAndYear(e, out seriesTitle, out year);
              return year;
            }).ToString()
          };

          if (!string.IsNullOrWhiteSpace(imdbId))
            syncData.IMDBID = imdbId;

          if (tvdbId > 0)
            syncData.SeriesID = tvdbId.ToString();

          HashSet<TraktEpisodeSync.Episode> uniqueEpisodes = new HashSet<TraktEpisodeSync.Episode>();
          foreach (var episode in serie)
          {
            string seriesTitle;
            int year = 0;
            if (!GetSeriesTitle /*AndYear*/(episode, out seriesTitle /*, out year*/))
              continue;

            // First send all movies to Trakt that we have so they appear in library
            CollectionUtils.AddAll(uniqueEpisodes, ToSeries(episode));
          }
          syncData.EpisodeList = uniqueEpisodes.ToList();

          TraktSyncModes traktSyncMode = TraktSyncModes.library;
          var response = TraktAPI.SyncEpisodeLibrary(syncData, traktSyncMode);
          ServiceRegistration.Get<ILogger>().Info("Trakt.tv: Series '{0}' '{1}': {2}{3}", syncData.Title, traktSyncMode, response.Message, response.Error);

          // Then send only the watched movies as "seen"
          uniqueEpisodes.Clear();
          foreach (var seenEpisode in episodes.Where(IsWatched))
            CollectionUtils.AddAll(uniqueEpisodes, ToSeries(seenEpisode));
          syncData.EpisodeList = uniqueEpisodes.ToList();

          traktSyncMode = TraktSyncModes.seen;
          response = TraktAPI.SyncEpisodeLibrary(syncData, traktSyncMode);
          ServiceRegistration.Get<ILogger>().Info("Trakt.tv: Series '{0}' '{1}': {2}{3}", syncData.Title, traktSyncMode, response.Message, response.Error);
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Trakt.tv: Exception while synchronizing media library.", ex);
      }
      return false;
    }

    private void BuildBannerUrls()
    {
      if (string.IsNullOrEmpty(Username))
      {
        SeriesBanner = MoviesBanner = string.Empty;
        return;
      }
      string noCache = "?nocache=" + DateTime.Now.Ticks;
      SeriesBanner = string.Format(SERIES_BANNER_URL, Username) + noCache;
      MoviesBanner = string.Format(MOVIES_BANNER_URL, Username) + noCache;
    }

    private static bool IsWatched(MediaItem mediaItem)
    {
      int playCount;
      return (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_PLAYCOUNT, 0, out playCount) && playCount > 0);
    }

    private static int SafeCount(IList list)
    {
      return list != null ? list.Count : 0;
    }

    private TraktMovieSync.Movie ToMovie(MediaItem mediaItem)
    {
      string value;
      int iValue;
      DateTime dtValue;

      TraktMovieSync.Movie movie = new TraktMovieSync.Movie();
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MovieAspect.ATTR_MOVIE_NAME, out value) && !string.IsNullOrWhiteSpace(value))
        movie.Title = value;

      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MovieAspect.ATTR_IMDB_ID, out value) && !string.IsNullOrWhiteSpace(value))
        movie.IMDBID = value;

      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MovieAspect.ATTR_TMDB_ID, out iValue) && iValue > 0)
        movie.TMDBID = iValue.ToString();

      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_RECORDINGTIME, out dtValue))
        movie.Year = dtValue.Year.ToString();

      return movie;
    }

    private string GetSeriesKey(MediaItem mediaItem)
    {
      string series;
      //int year;
      if (!GetSeriesTitle(mediaItem, out series))
        return string.Empty;

      return string.Format("{0}", series);
      //return string.Format("{0} ({1})", series, year);
    }

    private static bool GetSeriesTitleAndYear(MediaItem mediaItem, out string series, out int year)
    {
      DateTime dtFirstAired;
      year = 0;

      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_SERIESNAME, out series) || string.IsNullOrWhiteSpace(series))
        return false;

      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_FIRSTAIRED, out dtFirstAired))
        return false;

      year = dtFirstAired.Year;
      return true;
    }

    private static bool GetSeriesTitle(MediaItem mediaItem, out string series)
    {
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_SERIESNAME, out series) && !string.IsNullOrWhiteSpace(series);
    }

    private List<TraktEpisodeSync.Episode> ToSeries(MediaItem mediaItem)
    {
      int seriesIndex;
      List<int> episodeList;

      List<TraktEpisodeSync.Episode> episodes = new List<TraktEpisodeSync.Episode>();

      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_SEASON, out seriesIndex))
        return episodes;

      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_EPISODE, out episodeList))
        return episodes;

      foreach (var episode in episodeList)
        episodes.Add(new TraktEpisodeSync.Episode { SeasonIndex = seriesIndex.ToString(), EpisodeIndex = episode.ToString() });

      return episodes;
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return TRAKT_SETUP_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Load settings
      TraktSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TraktSettings>();
      IsEnabled = settings.EnableTrakt;
      Username = settings.Authentication != null ? settings.Authentication.Username : null;
      Password = settings.Authentication != null ? settings.Authentication.Password : null;
      TestStatus = string.Empty;
      BuildBannerUrls();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do here
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
