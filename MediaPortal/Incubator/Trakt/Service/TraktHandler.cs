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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using MediaPortal.UI.Services.Players;

namespace MediaPortal.UiComponents.Trakt.Service
{
  public class TraktHandler : IDisposable
  {
    // Defines the minimum playback progress in percent to consider a video as fully watched.
    private const int WATCHED_PERCENT = 85;
    private static readonly TimeSpan UPDATE_INTERVAL = TimeSpan.FromMinutes(10);

    private class PositionWatcher
    {
      public IIntervalWork Work { get; set; }
      public TimeSpan Duration { get; set; }
      public TimeSpan ResumePosition { get; set; }
    }

    private AsynchronousMessageQueue _messageQueue;
    private readonly object _syncObj = new object();
    private readonly SettingsChangeWatcher<Settings.TraktSettings> _settings = new SettingsChangeWatcher<Settings.TraktSettings>();
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
      if (_settings.Settings.EnableTrakt)
      {
        SubscribeToMessages();
      }
      else
      {
        UnsubscribeFromMessages();
      }
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

    private void HandleScrobble(IPlayerSlotController psc, bool starting)
    {
      try
      {
        IPlayerContext pc = PlayerContext.GetPlayerContext(psc);
        if (pc == null)
          return;

        bool removePsc = HandleTasks(psc, starting);

        AbstractScrobble scrobbleData;
        TraktScrobbleStates state;
        if (TryCreateScrobbleData(psc, pc, starting, out scrobbleData, out state))
        {
          ServiceRegistration.Get<ILogger>().Debug("Trakt.tv: [{5}] {0}, Duration {1}, Percent {2}, PSC.Duration {3}, PSC.ResumePosition {4}",
            scrobbleData.Title, scrobbleData.Duration, scrobbleData.Progress, _progressUpdateWorks[psc].Duration, _progressUpdateWorks[psc].ResumePosition, state);

          TraktMovieScrobble movie = scrobbleData as TraktMovieScrobble;
          if (movie != null)
            TraktAPI.ScrobbleMovieState(movie, state);

          TraktEpisodeScrobble episode = scrobbleData as TraktEpisodeScrobble;
          if (episode != null)
            TraktAPI.ScrobbleEpisodeState(episode, state);
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
    /// Creates Scrobble data based on a DBMovieInfo object
    /// </summary>
    /// <param name="psc"></param>
    /// <param name="pc">PlayerContext</param>
    /// <param name="starting"></param>
    /// <param name="scrobbleData"></param>
    /// <param name="state"></param>
    /// <returns>The Trakt scrobble data to send</returns>
    private bool TryCreateScrobbleData(IPlayerSlotController psc, IPlayerContext pc, bool starting, out AbstractScrobble scrobbleData, out TraktScrobbleStates state)
    {
      scrobbleData = null;
      state = starting ? TraktScrobbleStates.watching : TraktScrobbleStates.scrobble;
      if (_settings.Settings.Authentication == null)
        return false;

      string username = _settings.Settings.Authentication.Username;
      string password = _settings.Settings.Authentication.Password;

      if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        return false;

      // For canceling the watching, it is to have no TraktMovieScrobble.
      if (pc.CurrentMediaItem == null)
      {
        if (starting)
          return false;
        state = TraktScrobbleStates.cancelwatching;
        return true;
      }

      bool isMovie = pc.CurrentMediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID);
      bool isSeries = pc.CurrentMediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID);
      if (!isMovie && !isSeries)
        return false;

      string title = pc.CurrentPlayer != null ? pc.CurrentPlayer.MediaItemTitle : null;
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

      int progress = currentPosition == TimeSpan.Zero ? (starting ? 0 : 100) : Math.Min((int)(currentPosition.TotalSeconds * 100 / _progressUpdateWorks[psc].Duration.TotalSeconds), 100);

      string value;
      int iValue;
      DateTime dtValue;
      long lValue;

      if (isMovie)
      {
        TraktMovieScrobble movie = new TraktMovieScrobble();
        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, MovieAspect.ATTR_IMDB_ID, out value) && !string.IsNullOrWhiteSpace(value))
          movie.IMDBID = value;

        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, MovieAspect.ATTR_TMDB_ID, out iValue) && iValue > 0)
          movie.TMDBID = iValue.ToString();

        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, MediaAspect.ATTR_RECORDINGTIME, out dtValue))
          movie.Year = dtValue.Year.ToString();

        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, MovieAspect.ATTR_RUNTIME_M, out iValue) && iValue > 0)
          movie.Duration = iValue.ToString();

        scrobbleData = movie;
      }
      if (isSeries)
      {
        TraktEpisodeScrobble series = new TraktEpisodeScrobble();
        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, SeriesAspect.ATTR_IMDB_ID, out value) && !string.IsNullOrWhiteSpace(value))
          series.IMDBID = value;

        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, SeriesAspect.ATTR_TVDB_ID, out iValue))
          series.SeriesID = iValue.ToString();

        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, SeriesAspect.ATTR_SERIESNAME, out value) && !string.IsNullOrWhiteSpace(value))
          series.Title = value;

        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, SeriesAspect.ATTR_FIRSTAIRED, out dtValue))
          series.Year = dtValue.Year.ToString();

        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, SeriesAspect.ATTR_SEASON, out iValue))
          series.Season = iValue.ToString();
        List<int> intList;
        if (MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, SeriesAspect.ATTR_EPISODE, out intList) && intList.Any())
          series.Episode = intList.First().ToString(); // TODO: multi episode files?!

        scrobbleData = series;
      }

      // Fallback duration info
      if (string.IsNullOrWhiteSpace(scrobbleData.Duration) && MediaItemAspect.TryGetAttribute(pc.CurrentMediaItem.Aspects, VideoAspect.ATTR_DURATION, out lValue) && lValue > 0)
        scrobbleData.Duration = (lValue / 60).ToString();

      if (string.IsNullOrWhiteSpace(scrobbleData.Title))
        scrobbleData.Title = title;

      scrobbleData.Progress = progress.ToString();
      if (!starting && progress < WATCHED_PERCENT)
        state = TraktScrobbleStates.cancelwatching;

      scrobbleData.PluginVersion = TraktSettings.Version;
      scrobbleData.MediaCenter = "MediaPortal 2";
      scrobbleData.MediaCenterVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
      scrobbleData.MediaCenterBuildDate = String.Empty;
      scrobbleData.Username = username;
      scrobbleData.Password = password;
      return true;
    }

    public void Dispose()
    {
      _settings.Dispose();
      UnsubscribeFromMessages();
    }
  }
}
