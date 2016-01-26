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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Enums;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Extension;
using MediaPortal.UiComponents.Trakt.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using MediaPortal.UI.Services.Players;
using TraktSettings = MediaPortal.UiComponents.Trakt.Settings.TraktSettings;

namespace MediaPortal.UiComponents.Trakt.Service
{
  public class TraktHandler : IDisposable
  {
    // Defines the minimum playback progress in percent to consider a video as fully watched.
    private const int WATCHED_PERCENT = 85;
    private static readonly TimeSpan UPDATE_INTERVAL = TimeSpan.FromMinutes(10);
    private ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
    private TraktSettings TRAKT_SETTINGS = ServiceRegistration.Get<ISettingsManager>().Load<TraktSettings>();

    private class PositionWatcher
    {
      public IIntervalWork Work { get; set; }
      public TimeSpan Duration { get; set; }
      public TimeSpan ResumePosition { get; set; }
    }

    private AsynchronousMessageQueue _messageQueue;
    private readonly object _syncObj = new object();
    private readonly SettingsChangeWatcher<TraktSettings> _settings = new SettingsChangeWatcher<TraktSettings>();
    private readonly Dictionary<IPlayerSlotController, PositionWatcher> _progressUpdateWorks = new Dictionary<IPlayerSlotController, PositionWatcher>();

    public TraktHandler()
    {
      _settings.SettingsChanged += ConfigureHandler;
      ConfigureHandler();
    }

    private void ConfigureHandler(object sender, EventArgs e)
    {
      ConfigureHandler();
    }

    private void ConfigureHandler()
    {
      if (_settings.Settings.EnableTrakt /*&& UserloggedIntoTrakt()*/)
      {
        SubscribeToMessages();
      }
      else
      {
        UnsubscribeFromMessages();
      }
    }

    private bool UserloggedIntoTrakt()
    {

      if (string.IsNullOrEmpty(TRAKT_SETTINGS.TraktOAuthToken))
      {
        TraktLogger.Error("Authorise first");
        return false;
      }

      var response = TraktAPI.GetOAuthToken(TRAKT_SETTINGS.TraktOAuthToken);
      if (response == null || string.IsNullOrEmpty(response.AccessToken))
      {
        //TestStatus = Error
        TraktLogger.Error("Unable to login to trakt, check log for details");
        return false;
      }

      //TestStatus = Success
      TRAKT_SETTINGS.TraktOAuthToken = response.RefreshToken;
      settingsManager.Save(TRAKT_SETTINGS);
      TraktLogger.Info("Succes login to scrobble");
      _settings.Settings.AccountStatus = ConnectionState.Connected;

      return true;

    }

    void SubscribeToMessages()
    {
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PlayerManagerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        // React to player changes
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType)message.MessageType;
        IPlayerSlotController psc;
        // ServiceRegistration.Get<ILogger>().Debug("Trakt.tv: PlayerManagerMessage: {0}", message.MessageType);
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerResumeState:
            psc = (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            IResumeState resumeState = (IResumeState)message.MessageData[PlayerManagerMessaging.KEY_RESUME_STATE];
            Guid mediaItemId = (Guid)message.MessageData[PlayerManagerMessaging.KEY_MEDIAITEM_ID];
            HandleResumeInfo(psc, mediaItemId, resumeState);
            break;
          case PlayerManagerMessaging.MessageType.PlayerError:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
          case PlayerManagerMessaging.MessageType.PlayerStopped:
            psc = (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            HandleScrobble(psc, false);
            break;
          case PlayerManagerMessaging.MessageType.PlayerStarted:
            psc = (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            HandleScrobble(psc, true);
            break;
        }
      }
    }

    private void HandleResumeInfo(IPlayerSlotController psc, Guid mediaItemId, IResumeState resumeState)
    {
      PositionResumeState pos = resumeState as PositionResumeState;
      lock (_syncObj)
        if (_progressUpdateWorks.ContainsKey(psc))
          _progressUpdateWorks[psc].ResumePosition = pos != null ? pos.ResumePosition : _progressUpdateWorks[psc].Duration;
    }

    private bool CheckAccountDetails()
    {
      if (string.IsNullOrEmpty(TRAKT_SETTINGS.TraktOAuthToken))
      {
          //TestStatus = "Error";
          TraktLogger.Error("Trakt.tv error in credentials");
          return false;
      }
      return true;
    }

    private bool Login()
    {
      TraktLogger.Info("Exchanging refresh-token for access-token... scrobble");
      var response = TraktAPI.GetOAuthToken(TRAKT_SETTINGS.TraktOAuthToken);
      if (response == null || string.IsNullOrEmpty(response.AccessToken))
      {
        //TestStatus = Error
        TraktLogger.Error("Unable to login to trakt, check log for details");
        return false;
      }

      //TestStatus = Success
      TRAKT_SETTINGS.TraktOAuthToken = response.RefreshToken;
      settingsManager.Save(TRAKT_SETTINGS);
      TraktLogger.Info("Succes");

      return true;
    }

    private void HandleScrobble(IPlayerSlotController psc, bool starting)
    {

      if(!CheckAccountDetails())
        return;

      if(!Login())
        return;


      try
      {
        IPlayerContext pc = PlayerContext.GetPlayerContext(psc);
        if (pc == null)
          return;

        bool removePsc = HandleTasks(psc, starting);

        bool isMovie = pc.CurrentMediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID);
        bool isSeries = pc.CurrentMediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID);

        if (isMovie)
        {
          var response = starting ? TraktAPI.StartMovieScrobble(CreateMovieScrobbleData(psc, pc, true)) : TraktAPI.StopMovieScrobble(CreateMovieScrobbleData(psc, pc, false));
          TraktLogger.LogTraktResponse(response);
        }

        if (isSeries)
        {
          var response = starting ? TraktAPI.StartEpisodeScrobble(CreateEpisodeScrobbleData(psc, pc, true)) : TraktAPI.StopEpisodeScrobble(CreateEpisodeScrobbleData(psc, pc, false));
          TraktLogger.LogTraktResponse(response);
        }

        if (removePsc)
          lock (_syncObj)
            _progressUpdateWorks.Remove(psc);

      }
      catch (ThreadAbortException)
      { }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Trakt.tv: Exception while scrobbling", ex);
      }
    }

    /// <summary>
    /// Creates or removes <see cref="IIntervalWork"/> from <see cref="IThreadPool"/>.
    /// </summary>
    /// <param name="psc">IPlayerSlotController</param>
    /// <param name="starting"><c>true</c> if starting, <c>false</c> if stopping.</param>
    /// <returns><c>true</c> if work should be removed when done.</returns>
    private bool HandleTasks(IPlayerSlotController psc, bool starting)
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      lock (_syncObj)
      {
        // On stop, abort background interval work
        if (!starting && _progressUpdateWorks.ContainsKey(psc))
        {
          threadPool.RemoveIntervalWork(_progressUpdateWorks[psc].Work);
          return true;
        }

        // When starting, create an asynchronous work and exit here
        if (!_progressUpdateWorks.ContainsKey(psc))
        {
          IntervalWork work = new IntervalWork(() => HandleScrobble(psc, true), UPDATE_INTERVAL);
          threadPool.AddIntervalWork(work, false);
          _progressUpdateWorks[psc] = new PositionWatcher { Work = work };
        }
      }
      return false;
    }

    /// <summary>
    /// Creates Scrobble data based on playing MediaItem object
    /// </summary>
    private TraktScrobbleMovie CreateMovieScrobbleData(IPlayerSlotController psc, IPlayerContext pc, bool starting)
    {
      IMediaPlaybackControl pmc = pc.CurrentPlayer as IMediaPlaybackControl;
      TimeSpan currentPosition;
      if (pmc != null)
      {
        _progressUpdateWorks[psc].Duration = pmc.Duration;
        currentPosition = pmc.CurrentTime;
      }
      else
      {
        // Player is already removed on stopping, so take the resume position if available
        currentPosition = _progressUpdateWorks[psc].ResumePosition;
      }

      double progress = currentPosition == TimeSpan.Zero ? (starting ? 0 : 100) : Math.Min((int)(currentPosition.TotalSeconds * 100 / _progressUpdateWorks[psc].Duration.TotalSeconds), 100);

      var movieScrobbleData = new TraktScrobbleMovie
      {
        Movie = new TraktMovie
        {
          Ids = new TraktMovieId { Imdb = GetMovieImdb(pc.CurrentMediaItem), Tmdb = GetMovieTmdb(pc.CurrentMediaItem) },
          Title = GetMovieTitle(pc.CurrentMediaItem),
          Year = GetVideoYear(pc.CurrentMediaItem)
        },
      //  AppVersion = _settings.Settings.Version,
       // AppDate = _settings.Settings.BuildDate,
        Progress = progress
      };

      return movieScrobbleData;
    }

    /// <summary>
    /// Creates Scrobble data based on a MediaItem object
    /// </summary>
    private TraktScrobbleEpisode CreateEpisodeScrobbleData(IPlayerSlotController psc, IPlayerContext pc, bool starting)
    {
      IMediaPlaybackControl pmc = pc.CurrentPlayer as IMediaPlaybackControl;
      TimeSpan currentPosition;
      if (pmc != null)
      {
        _progressUpdateWorks[psc].Duration = pmc.Duration;
        currentPosition = pmc.CurrentTime;
      }
      else
      {
        // Player is already removed on stopping, so take the resume position if available
        currentPosition = _progressUpdateWorks[psc].ResumePosition;
      }

      double progress = currentPosition == TimeSpan.Zero ? (starting ? 0 : 100) : Math.Min((int)(currentPosition.TotalSeconds * 100 / _progressUpdateWorks[psc].Duration.TotalSeconds), 100);

      var episodeScrobbleData = new TraktScrobbleEpisode
      {
        Episode = new TraktEpisode
        {
          Ids = new TraktEpisodeId
          {
            Tvdb = GetSeriesTvdbId(pc.CurrentMediaItem),
            Imdb = GetSeriesImdbId(pc.CurrentMediaItem)
          },
          Title = GetSeriesTitle(pc.CurrentMediaItem),
          Season = GetSeasonIndex(pc.CurrentMediaItem),
          Number = GetEpisodeIndex(pc.CurrentMediaItem)
        },
        Show = new TraktShow
        {
          Ids = new TraktShowId
          {
            Tvdb = GetSeriesTvdbId(pc.CurrentMediaItem),
            Imdb = GetSeriesImdbId(pc.CurrentMediaItem)
          },
          Title = GetSeriesTitle(pc.CurrentMediaItem),
          Year = GetVideoYear(pc.CurrentMediaItem)
        },
        //AppVersion = _settings.Settings.Version,
        //AppDate = _settings.Settings.BuildDate,
        Progress = progress
      };

      return episodeScrobbleData;
    }

    private string GetMovieImdb(MediaItem currMediaItem)
    {
      string value;
      return MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, MovieAspect.ATTR_IMDB_ID, out value) ? value : null;
    }

    private int GetMovieTmdb(MediaItem currMediaItem)
    {
      int iValue;
      return MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, MovieAspect.ATTR_TMDB_ID, out iValue) ? iValue : 0;
    }

    private int GetVideoYear(MediaItem currMediaItem)
    {
      DateTime dtValue;
      if (MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, MediaAspect.ATTR_RECORDINGTIME, out dtValue))
        return dtValue.Year;

      return 0;
    }

    private string GetSeriesTitle(MediaItem currMediaItem)
    {
      string value;
      return MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, SeriesAspect.ATTR_SERIESNAME, out value) ? value : null;
    }

    private int GetSeriesTvdbId(MediaItem currMediaItem)
    {
      int value;
      return MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, SeriesAspect.ATTR_TVDB_ID, out value) ? value : 0;
    }

    private int GetSeasonIndex(MediaItem currMediaItem)
    {
      int value;
      return MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, SeriesAspect.ATTR_SEASON, out value) ? value : 0;
    }

    private int GetEpisodeIndex(MediaItem currMediaItem)
    {
      List<int> intList;
      if (MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, SeriesAspect.ATTR_EPISODE, out intList) && intList.Any())
        return intList.First(); // TODO: multi episode files?!

      return intList.FirstOrDefault();
    }

    private string GetSeriesImdbId(MediaItem currMediaItem)
    {
      string value;
      return MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, SeriesAspect.ATTR_IMDB_ID, out value) ? value : null;
    }

    private string GetMovieTitle(MediaItem currMediaItem)
    {
      string value;
      return MediaItemAspect.TryGetAttribute(currMediaItem.Aspects, MovieAspect.ATTR_MOVIE_NAME, out value) ? value : null;
    }

    public void Dispose()
    {
      _settings.Dispose();
      UnsubscribeFromMessages();
    }
  }
}
