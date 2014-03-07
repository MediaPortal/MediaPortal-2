using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Services.Players;

namespace MediaPortal.UiComponents.Trakt.Service
{
  public class TraktHandler : IDisposable
  {
    private AsynchronousMessageQueue _messageQueue;
    private readonly SettingsChangeWatcher<Settings.TraktSettings> _settings = new SettingsChangeWatcher<Settings.TraktSettings>();
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
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerError:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
          case PlayerManagerMessaging.MessageType.PlayerStopped:
            psc = (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            HandlePlayerStopped(psc);
            break;
          case PlayerManagerMessaging.MessageType.PlayerStarted:
            psc = (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            HandlePlayerStarted(psc);
            break;
        }
      }
    }

    private void HandlePlayerStopped(IPlayerSlotController psc)
    {
      HandleScrobble(psc, false);
    }

    private void HandlePlayerStarted(IPlayerSlotController psc)
    {
      HandleScrobble(psc, true);
    }

    private void HandleScrobble(IPlayerSlotController psc, bool starting)
    {
      IPlayerContext pc = PlayerContext.GetPlayerContext(psc);
      if (pc == null || pc.CurrentMediaItem == null)
        return;

      AbstractScrobble scrobbleData;
      TraktScrobbleStates state;
      if (TryCreateScrobbleData(pc, starting, out scrobbleData, out state))
      {
        TraktMovieScrobble movie = scrobbleData as TraktMovieScrobble;
        if (movie != null)
          TraktAPI.ScrobbleMovieState(movie, state);

        TraktEpisodeScrobble episode = scrobbleData as TraktEpisodeScrobble;
        if (episode != null)
          TraktAPI.ScrobbleEpisodeState(episode, state);
      }
    }

    /// <summary>
    /// Creates Scrobble data based on a DBMovieInfo object
    /// </summary>
    /// <param name="pc">PlayerContext</param>
    /// <param name="starting"></param>
    /// <param name="scrobbleData"></param>
    /// <param name="state"></param>
    /// <returns>The Trakt scrobble data to send</returns>
    public bool TryCreateScrobbleData(IPlayerContext pc, bool starting, out AbstractScrobble scrobbleData, out TraktScrobbleStates state)
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
