using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Extension;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt
{
  #region Enumerables

  // ReSharper disable InconsistentNaming
  
  /// <summary>
  /// List of Scrobble States
  /// </summary>
  public enum TraktScrobbleStates
  {
    watching,
    scrobble,
    cancelwatching
  }

  /// <summary>
  /// List of Sync Modes
  /// </summary>
  public enum TraktSyncModes
  {
    library,
    seen,
    unlibrary,
    unseen,
    watchlist,
    unwatchlist
  }

  /// <summary>
  /// List of Clearing Modes
  /// </summary>
  public enum TraktClearingModes
  {
    all,
    movies,
    episodes
  }

  /// <summary>
  /// List of Item Types
  /// </summary>
  public enum TraktItemType
  {
    episode,
    season,
    show,
    movie
  }

  /// <summary>
  /// List of Rate Values
  /// </summary>
  public enum TraktRateValue
  {
    unrate,
    one,
    two,
    three,
    four,
    five,
    six,
    seven,
    eight,
    nine,
    ten,
    love, //deprecated - ten
    hate  //deprecated - one
  }

  /// <summary>
  /// Trakt Connection States
  /// </summary>
  public enum ConnectionState
  {
    Connected,
    Connecting,
    Disconnected,
    Invalid,
    Pending
  }

  /// <summary>
  /// Privacy Level for Lists
  /// </summary>
  public enum ListPrivacyLevel
  {
    Public,
    Private,
    Friends
  }

  /// <summary>
  /// Defaults to all, but you can instead send a comma delimited list of actions. 
  /// For example, /all or /watching,scrobble,seen or /rating.
  /// </summary>
  public enum ActivityAction
  {
    all,
    watching,
    scrobble,
    checkin,
    seen,
    collection,
    rating,
    watchlist,
    review,
    shout,
    created,
    item_added
  }

  /// <summary>
  /// Defaults to all, but you can instead send a comma delimited list of types.
  /// For example, /all or /movie,show or /list.
  /// </summary>
  public enum ActivityType
  {
    all,
    episode,
    show,
    movie,
    list
  }

  [Flags]
  public enum SearchType
  {
    none = 0,
    movies = 1,
    shows = 2,
    episodes = 4,
    people = 8,
    users = 16
  }
  // ReSharper restore InconsistentNaming

  #endregion

  /// <summary>
  /// Object that communicates with the Trakt API
  /// </summary>
  public class TraktAPI
  {
    #region Transmit Events
    // these events can be used to log data sent / received from trakt
    internal delegate void OnDataSendDelegate(string url, string postData);
    internal delegate void OnDataReceivedDelegate(string response);
    internal delegate void OnDataErrorDelegate(string error);

    internal static event OnDataSendDelegate OnDataSend;
    internal static event OnDataReceivedDelegate OnDataReceived;
    internal static event OnDataErrorDelegate OnDataError;
    #endregion

    #region Settings

    public static string Username { get; set; }
    public static string Password { get; set; }
    public static string UserAgent { get; set; }

    #endregion

    #region Scrobbling

    /// <summary>
    /// Sends Scrobble data to Trakt
    /// </summary>
    /// <param name="scrobbleData">The Data to send</param>
    /// <param name="status">The mode to send it as</param>
    /// <returns>The response from Trakt</returns>
    public static TraktResponse ScrobbleMovieState(TraktMovieScrobble scrobbleData, TraktScrobbleStates status)
    {
      //If we are cancelling a scrobble we don't need data
      if (status != TraktScrobbleStates.cancelwatching)
      {
        // check that we have everything we need
        // server can accept title if movie id is not supplied
        if (scrobbleData == null)
        {
          TraktResponse error = new TraktResponse
          {
            Error = "Not enough information to send to server",
            Status = "failure"
          };
          return error;
        }
      }

      // serialize Scrobble object to JSON and send to server
      string response = Transmit(string.Format(TraktURIs.ScrobbleMovie, status), scrobbleData.ToJSON());

      // return success or failure
      return response.FromJSON<TraktResponse>();
    }

    /// <summary>
    /// Sends Scrobble data to Trakt
    /// </summary>
    /// <param name="scrobbleData">The Data to send</param>
    /// <param name="status">The mode to send it as</param>
    /// <returns>The response from Trakt</returns>
    public static TraktResponse ScrobbleEpisodeState(TraktEpisodeScrobble scrobbleData, TraktScrobbleStates status)
    {
      // check that we have everything we need
      // server can accept title if movie id is not supplied
      if (status != TraktScrobbleStates.cancelwatching)
      {
        if (scrobbleData == null)
        {
          TraktResponse error = new TraktResponse
          {
            Error = "Not enough information to send to server",
            Status = "failure"
          };
          return error;
        }
      }

      // serialize Scrobble object to JSON and send to server
      string response = Transmit(string.Format(TraktURIs.ScrobbleShow, status), scrobbleData.ToJSON());

      // return success or failure
      return response.FromJSON<TraktResponse>();
    }

    #endregion

    #region Syncing

    /// <summary>
    /// Sends movie sync data to Trakt
    /// </summary>
    /// <param name="syncData">The sync data to send</param>
    /// <param name="mode">The sync mode to use</param>
    /// <returns>The response from trakt</returns>
    public static TraktSyncResponse SyncMovieLibrary(TraktMovieSync syncData, TraktSyncModes mode)
    {
      // check that we have everything we need
      // server can accept title/year if imdb id is not supplied
      if (syncData == null || syncData.MovieList.Count == 0)
      {
        TraktSyncResponse error = new TraktSyncResponse
        {
          Error = "Not enough information to send to server",
          Status = "failure"
        };
        return error;
      }

      // serialize Scrobble object to JSON and send to server
      string response = Transmit(string.Format(TraktURIs.SyncMovieLibrary, mode), syncData.ToJSON());

      // return success or failure
      return response.FromJSON<TraktSyncResponse>();
    }

    /// <summary>
    /// Add/Remove show to/from watchlist
    /// </summary>
    /// <param name="syncData">The sync data to send</param>
    /// <param name="mode">The sync mode to use</param>
    /// <returns>The response from trakt</returns>
    public static TraktResponse SyncShowWatchList(TraktShowSync syncData, TraktSyncModes mode)
    {
      // check that we have everything we need            
      if (syncData == null || syncData.Shows.Count == 0)
      {
        TraktResponse error = new TraktResponse
        {
          Error = "Not enough information to send to server",
          Status = "failure"
        };
        return error;
      }

      // serialize Scrobble object to JSON and send to server
      string response = Transmit(string.Format(TraktURIs.SyncShowWatchList, mode), syncData.ToJSON());

      // return success or failure
      return response.FromJSON<TraktResponse>();
    }

    /// <summary>
    /// Add/Remove episode to/from watchlist
    /// </summary>
    /// <param name="syncData">The sync data to send</param>
    /// <param name="mode">The sync mode to use</param>
    /// <returns>The response from trakt</returns>
    public static TraktResponse SyncEpisodeWatchList(TraktEpisodeSync syncData, TraktSyncModes mode)
    {
      // check that we have everything we need
      if (syncData == null || syncData.EpisodeList.Count == 0)
      {
        TraktResponse error = new TraktResponse
        {
          Error = "Not enough information to send to server",
          Status = "failure"
        };
        return error;
      }

      // serialize Scrobble object to JSON and send to server
      string response = Transmit(string.Format(TraktURIs.SyncEpisodeWatchList, mode), syncData.ToJSON());

      // return success or failure
      return response.FromJSON<TraktResponse>();
    }

    /// <summary>
    /// Sends episode sync data to Trakt
    /// </summary>
    /// <param name="syncData">The sync data to send</param>
    /// <param name="mode">The sync mode to use</param>
    public static TraktResponse SyncEpisodeLibrary(TraktEpisodeSync syncData, TraktSyncModes mode)
    {
      // check that we have everything we need
      // server can accept title/year if imdb id is not supplied
      if (syncData == null || string.IsNullOrEmpty(syncData.SeriesID) && string.IsNullOrEmpty(syncData.Title) && string.IsNullOrEmpty(syncData.Year))
      {
        TraktResponse error = new TraktResponse
        {
          Error = "Not enough information to send to server",
          Status = "failure"
        };
        return error;
      }

      // serialize Scrobble object to JSON and send to server
      string response = Transmit(string.Format(TraktURIs.SyncEpisodeLibrary, mode), syncData.ToJSON());

      // return success or failure
      return response.FromJSON<TraktResponse>();
    }

    public static TraktResponse SyncShowAsSeen(TraktShowSeen show)
    {
      string response = Transmit(TraktURIs.ShowSeen, show.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktResponse SyncSeasonAsSeen(TraktSeasonSeen showSeason)
    {
      string response = Transmit(TraktURIs.SeasonSeen, showSeason.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktResponse SyncShowAsLibrary(TraktShowLibrary show)
    {
      string response = Transmit(TraktURIs.ShowLibrary, show.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktResponse SyncSeasonAsLibrary(TraktSeasonLibrary showSeason)
    {
      string response = Transmit(TraktURIs.SeasonLibrary, showSeason.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    #endregion

    #region Trakt Library Calls

    /// <summary>
    /// Gets the trakt movie library for a user
    /// </summary>
    /// <param name="user">The user to get</param>
    /// <returns>The trakt movie library</returns>
    public static IEnumerable<TraktLibraryMovies> GetMovieCollectionForUser(string user)
    {
      // get the library
      string moviesForUser = Transmit(string.Format(TraktURIs.UserMoviesCollection, user), GetUserAuthentication());

      // if we timeout we will return an error response
      TraktResponse response = moviesForUser.FromJSON<TraktResponse>();
      if (response == null || response.Error != null) return null;

      return moviesForUser.FromJSONArray<TraktLibraryMovies>();
    }

    public static IEnumerable<TraktLibraryMovies> GetAllMoviesForUser(string user)
    {
      return GetAllMoviesForUser(user, true);
    }

    /// <summary>
    /// Gets all movies for a user from trakt, including movies not in collection
    /// </summary>
    /// <param name="user">The user to get</param>
    /// <param name="syncDataOnly">set this to true (default) if you want the absolute minimum data returned nessacary for syncing</param>
    /// <returns>The trakt movie library</returns>
    public static IEnumerable<TraktLibraryMovies> GetAllMoviesForUser(string user, bool syncDataOnly)
    {
      // Get the library
      string moviesForUser = Transmit(string.Format(TraktURIs.UserMoviesAll, user, syncDataOnly ? @"/min" : string.Empty), GetUserAuthentication());

      // if we timeout we will return an error response
      TraktResponse response = moviesForUser.FromJSON<TraktResponse>();
      if (response == null || response.Error != null) return null;

      return moviesForUser.FromJSONArray<TraktLibraryMovies>();
    }

    public static IEnumerable<TraktLibraryShow> GetLibraryEpisodesForUser(string user)
    {
      return GetLibraryEpisodesForUser(user, true);
    }

    /// <summary>
    /// Gets the trakt episode library for a user
    /// </summary>
    /// <param name="user">The user to get</param>
    /// <param name="syncDataOnly">set this to true (default) if you want the absolute minimum data returned nessacary for syncing</param>
    /// <returns>The trakt episode library</returns>
    public static IEnumerable<TraktLibraryShow> GetLibraryEpisodesForUser(string user, bool syncDataOnly)
    {
      string showsForUser = Transmit(string.Format(TraktURIs.UserEpisodesCollection, user, syncDataOnly ? @"/min" : string.Empty), GetUserAuthentication());

      // if we timeout we will return an error response
      TraktResponse response = showsForUser.FromJSON<TraktResponse>();
      if (response == null || response.Error != null) return null;

      return showsForUser.FromJSONArray<TraktLibraryShow>();
    }

    public static IEnumerable<TraktLibraryShow> GetWatchedEpisodesForUser(string user)
    {
      return GetWatchedEpisodesForUser(user, true);
    }

    /// <summary>
    /// Gets the trakt watched/seen episodes for a user
    /// </summary>
    /// <param name="user">The user to get</param>
    /// <param name="syncDataOnly">set this to true (default) if you want the absolute minimum data returned nessacary for syncing</param>
    /// <returns>The trakt episode library</returns>
    public static IEnumerable<TraktLibraryShow> GetWatchedEpisodesForUser(string user, bool syncDataOnly)
    {
      string showsForUser = Transmit(string.Format(TraktURIs.UserWatchedEpisodes, user, syncDataOnly ? @"/min" : string.Empty), GetUserAuthentication());

      // if we timeout we will return an error response
      TraktResponse response = showsForUser.FromJSON<TraktResponse>();
      if (response == null || response.Error != null) return null;

      return showsForUser.FromJSONArray<TraktLibraryShow>();
    }

    public static IEnumerable<TraktLibraryShow> GetUnSeenEpisodesForUser(string user)
    {
      return GetUnSeenEpisodesForUser(user, true);
    }

    public static IEnumerable<TraktLibraryShow> GetUnSeenEpisodesForUser(string user, bool syncDataOnly)
    {
      string showsForUser = Transmit(string.Format(TraktURIs.UserEpisodesUnSeen, user, syncDataOnly ? @"/min" : string.Empty), GetUserAuthentication());

      // if we timeout we will return an error response
      TraktResponse response = showsForUser.FromJSON<TraktResponse>();
      if (response == null || response.Error != null) return null;

      return showsForUser.FromJSONArray<TraktLibraryShow>();
    }

    #endregion

    #region Rating

    /// <summary>
    /// Sends episode rate data to Trakt
    /// </summary>
    /// <param name="episode">The Trakt rate data to send</param>
    /// <returns>The response from Trakt</returns>
    public static TraktRateResponse RateEpisode(TraktRateEpisode episode)
    {
      if (episode == null) return null;
      string response = Transmit(string.Format(TraktURIs.RateItem, TraktItemType.episode), episode.ToJSON());
      return response.FromJSON<TraktRateResponse>();
    }

    /// <summary>
    /// Sends episodes rate data to Trakt
    /// </summary>
    /// <param name="episodes">The Trakt rate data to send</param>
    /// <returns>The response from Trakt</returns>
    public static TraktRateResponse RateEpisodes(TraktRateEpisodes episodes)
    {
      if (episodes == null) return null;
      string response = Transmit(TraktURIs.RateEpisodes, episodes.ToJSON());
      return response.FromJSON<TraktRateResponse>();
    }

    /// <summary>
    /// Sends series rate data to Trakt
    /// </summary>
    /// <param name="series">The Trakt rate data to send</param>
    /// <returns>The response from Trakt</returns>
    public static TraktRateResponse RateSeries(TraktRateSeries series)
    {
      if (series == null) return null;
      string response = Transmit(string.Format(TraktURIs.RateItem, TraktItemType.show.ToString()), series.ToJSON());
      return response.FromJSON<TraktRateResponse>();
    }

    /// <summary>
    /// Sends multiple series rate data to Trakt
    /// </summary>
    /// <param name="shows">The Trakt rate data to send</param>
    /// <returns>The response from Trakt</returns>
    public static TraktRateResponse RateSeries(TraktRateShows shows)
    {
      if (shows == null) return null;
      string response = Transmit(TraktURIs.RateShows, shows.ToJSON());
      return response.FromJSON<TraktRateResponse>();
    }

    /// <summary>
    /// Sends movie rate data to Trakt
    /// </summary>
    /// <param name="movie">The Trakt rate data to send</param>
    /// <returns>The response from Trakt</returns>
    public static TraktRateResponse RateMovie(TraktRateMovie movie)
    {
      if (movie == null) return null;
      string response = Transmit(string.Format(TraktURIs.RateItem, TraktItemType.movie), movie.ToJSON());
      return response.FromJSON<TraktRateResponse>();
    }

    /// <summary>
    /// Sends movies rate data to Trakt
    /// </summary>
    /// <param name="movies">The Trakt rate data to send</param>
    /// <returns>The response from Trakt</returns>
    public static TraktRateResponse RateMovies(TraktRateMovies movies)
    {
      if (movies == null) return null;
      string response = Transmit(TraktURIs.RateMovies, movies.ToJSON());
      return response.FromJSON<TraktRateResponse>();
    }

    #endregion

    #region User

    public static TraktUserProfile GetUserProfile(string user)
    {
      string response = Transmit(string.Format(TraktURIs.UserProfile, user), GetUserAuthentication());
      return response.FromJSON<TraktUserProfile>();
    }

    /// <summary>
    /// Returns a list of Friends and their user profiles
    /// </summary>
    /// <param name="user">username of person to retrieve friend s list</param>
    public static IEnumerable<TraktUserProfile> GetUserFriends(string user)
    {
      string response = Transmit(string.Format(TraktURIs.UserFriends, user), GetUserAuthentication());
      return response.FromJSONArray<TraktUserProfile>();
    }

    /// <summary>
    /// Returns list of episodes in Users Calendar
    /// </summary>
    /// <param name="user">username of person to get Calendar</param>
    public static IEnumerable<TraktCalendar> GetCalendarForUser(string user)
    {
      // 7-Days from Today
      // All Dates should be in PST (GMT-8)
      DateTime dateNow = DateTime.UtcNow.Subtract(new TimeSpan(8, 0, 0));
      return GetCalendarForUser(user, dateNow.ToString("yyyyMMdd"), "7");
    }

    /// <summary>
    /// Returns list of episodes in Users Calendar
    /// </summary>
    /// <param name="user">username of person to get Calendar</param>
    /// <param name="startDate">Start Date of calendar in form yyyyMMdd (GMT-8hrs)</param>
    /// <param name="days">Number of days to return in calendar</param>
    public static IEnumerable<TraktCalendar> GetCalendarForUser(string user, string startDate, string days)
    {
      string userCalendar = Transmit(string.Format(TraktURIs.UserCalendarShows, user, startDate, days), GetUserAuthentication());
      return userCalendar.FromJSONArray<TraktCalendar>();
    }

    public static IEnumerable<TraktCalendar> GetCalendarPremieres()
    {
      // 7-Days from Today
      // All Dates should be in PST (GMT-8)
      DateTime dateNow = DateTime.UtcNow.Subtract(new TimeSpan(8, 0, 0));
      return GetCalendarPremieres(dateNow.ToString("yyyyMMdd"), "7");
    }

    /// <summary>
    /// Returns list of episodes in the Premieres Calendar
    /// </summary>        
    /// <param name="startDate">Start Date of calendar in form yyyyMMdd (GMT-8hrs)</param>
    /// <param name="days">Number of days to return in calendar</param>
    public static IEnumerable<TraktCalendar> GetCalendarPremieres(string startDate, string days)
    {
      string premieres = Transmit(string.Format(TraktURIs.CalendarPremieres, startDate, days), GetUserAuthentication());
      return premieres.FromJSONArray<TraktCalendar>();
    }

    public static IEnumerable<TraktCalendar> GetCalendarShows()
    {
      DateTime dateNow = DateTime.UtcNow.Subtract(new TimeSpan(8, 0, 0));
      return GetCalendarShows(dateNow.ToString("yyyyMMdd"), "7");
    }

    public static IEnumerable<TraktCalendar> GetCalendarShows(string startDate, string days)
    {
      string premieres = Transmit(string.Format(TraktURIs.CalendarAllShows, startDate, days), GetUserAuthentication());
      return premieres.FromJSONArray<TraktCalendar>();
    }

    /// <summary>
    /// Returns list of the 100 last watched episodes by a user
    /// </summary>
    /// <param name="user">username of person to get watched history</param>
    [Obsolete("This method is deprecated and has been replaced by GetUserActivity", false)]
    public static IEnumerable<TraktWatchedEpisode> GetUserEpisodeWatchedHistory(string user)
    {
      string watchedEpisodes = Transmit(string.Format(TraktURIs.UserEpisodeWatchedHistory, user), GetUserAuthentication());
      return watchedEpisodes.FromJSONArray<TraktWatchedEpisode>();
    }

    /// <summary>
    /// Returns list of the 100 last watched movies by a user
    /// </summary>
    /// <param name="user">username of person to get watched history</param>
    [Obsolete("This method is deprecated and has been replaced by GetUserActivity", false)]
    public static IEnumerable<TraktWatchedMovie> GetUserMovieWatchedHistory(string user)
    {
      string watchedMovies = Transmit(string.Format(TraktURIs.UserMovieWatchedHistory, user), GetUserAuthentication());
      return watchedMovies.FromJSONArray<TraktWatchedMovie>();
    }

    /// <summary>
    /// Returns a list of lists created by user
    /// </summary>
    /// <param name="user">username of person to get lists</param>
    public static IEnumerable<TraktUserList> GetUserLists(string user)
    {
      string userLists = Transmit(string.Format(TraktURIs.UserLists, user), GetUserAuthentication());
      return userLists.FromJSONArray<TraktUserList>();
    }

    /// <summary>
    /// Returns the contents of a lists for a user
    /// </summary>
    /// <param name="user">username of person</param>
    /// <param name="slug">slug (id) of list item e.g. "star-wars-collection"</param>
    public static TraktUserList GetUserList(string user, string slug)
    {
      string userList = Transmit(string.Format(TraktURIs.UserList, user, slug), GetUserAuthentication());
      return userList.FromJSON<TraktUserList>();
    }

    /// <summary>
    /// Returns the users Rated Movies
    /// </summary>
    /// <param name="user">username of person</param>
    public static IEnumerable<TraktUserMovieRating> GetUserRatedMovies(string user)
    {
      string ratedMovies = Transmit(string.Format(TraktURIs.UserRatedMoviesList, user), GetUserAuthentication());

      // if we timeout we will return an error response
      TraktResponse response = ratedMovies.FromJSON<TraktResponse>();
      if (response == null || response.Error != null) return null;

      return ratedMovies.FromJSONArray<TraktUserMovieRating>();
    }

    /// <summary>
    /// Returns the users Rated Shows
    /// </summary>
    /// <param name="user">username of person</param>
    public static IEnumerable<TraktUserShowRating> GetUserRatedShows(string user)
    {
      string ratedShows = Transmit(string.Format(TraktURIs.UserRatedShowsList, user), GetUserAuthentication());

      // if we timeout we will return an error response
      TraktResponse response = ratedShows.FromJSON<TraktResponse>();
      if (response == null || response.Error != null) return null;

      return ratedShows.FromJSONArray<TraktUserShowRating>();
    }

    /// <summary>
    /// Returns the users Rated Episodes
    /// </summary>
    /// <param name="user">username of person</param>
    public static IEnumerable<TraktUserEpisodeRating> GetUserRatedEpisodes(string user)
    {
      string ratedEpisodes = Transmit(string.Format(TraktURIs.UserRatedEpisodesList, user), GetUserAuthentication());

      // if we timeout we will return an error response
      TraktResponse response = ratedEpisodes.FromJSON<TraktResponse>();
      if (response == null || response.Error != null) return null;

      return ratedEpisodes.FromJSONArray<TraktUserEpisodeRating>();
    }

    #endregion

    #region Activity

    public static TraktActivity GetFriendActivity()
    {
      return GetFriendActivity(null, null, false);
    }

    public static TraktActivity GetFriendActivity(bool includeMe)
    {
      return GetFriendActivity(null, null, includeMe);
    }

    public static TraktActivity GetFriendActivity(List<ActivityType> types, List<ActivityAction> actions, bool includeMe)
    {
      return GetFriendActivity(types, actions, 0, 0, includeMe);
    }

    public static TraktActivity GetFriendActivity(List<ActivityType> types, List<ActivityAction> actions, long start, long end, bool includeMe)
    {
      // get comma seperated list of types and actions (if more than one)
      string activityTypes = types == null ? "all" : string.Join(",", types.Select(t => t.ToString()).ToArray());
      string activityActions = actions == null ? "all" : string.Join(",", actions.Select(a => a.ToString()).ToArray());

      string startEnd = (start == 0 || end == 0) ? string.Empty : string.Format("/{0}/{1}", start, end);
      string apiUrl = includeMe ? TraktURIs.ActivityFriendsMe : TraktURIs.ActivityFriends;

      string activity = Transmit(string.Format(apiUrl, activityTypes, activityActions, startEnd), GetUserAuthentication());
      return activity.FromJSON<TraktActivity>();
    }

    public static TraktActivity GetCommunityActivity()
    {
      return GetCommunityActivity(null, null);
    }

    public static TraktActivity GetCommunityActivity(List<ActivityType> types, List<ActivityAction> actions)
    {
      return GetCommunityActivity(types, actions, 0, 0);
    }

    public static TraktActivity GetCommunityActivity(List<ActivityType> types, List<ActivityAction> actions, long start, long end)
    {
      // get comma seperated list of types and actions (if more than one)
      string activityTypes = types == null ? "all" : string.Join(",", types.Select(t => t.ToString()).ToArray());
      string activityActions = actions == null ? "all" : string.Join(",", actions.Select(a => a.ToString()).ToArray());

      string startEnd = (start == 0 || end == 0) ? string.Empty : string.Format("/{0}/{1}", start, end);

      string activity = Transmit(string.Format(TraktURIs.ActivityCommunity, activityTypes, activityActions, startEnd), GetUserAuthentication());
      return activity.FromJSON<TraktActivity>();
    }

    public static TraktActivity GetUserActivity(string username, List<ActivityType> types, List<ActivityAction> actions)
    {
      // get comma seperated list of types and actions (if more than one)
      string activityTypes = string.Join(",", types.Select(t => t.ToString()).ToArray());
      string activityActions = string.Join(",", actions.Select(a => a.ToString()).ToArray());

      string activity = Transmit(string.Format(TraktURIs.ActivityUser, username, activityTypes, activityActions), GetUserAuthentication());
      return activity.FromJSON<TraktActivity>();
    }

    #endregion

    #region Friends / Network

    /// <summary>
    /// Returns a list of Friends for current user
    /// Friends are a two-way relationship ie. both following each other
    /// </summary>
    public static IEnumerable<TraktNetworkUser> GetNetworkFriends()
    {
      return GetNetworkFriends(TraktSettings.Username);
    }
    public static IEnumerable<TraktNetworkUser> GetNetworkFriends(string person)
    {
      string response = Transmit(string.Format(TraktURIs.NetworkFriends, person), GetUserAuthentication());
      return response.FromJSONArray<TraktNetworkUser>();
    }

    /// <summary>
    /// Returns a list of people the current user follows
    /// </summary>
    public static IEnumerable<TraktNetworkUser> GetNetworkFollowing()
    {
      return GetNetworkFollowing(TraktSettings.Username);
    }
    public static IEnumerable<TraktNetworkUser> GetNetworkFollowing(string person)
    {
      string response = Transmit(string.Format(TraktURIs.NetworkFollowing, person), GetUserAuthentication());
      return response.FromJSONArray<TraktNetworkUser>();
    }

    /// <summary>
    /// Returns a list of people that follow the current user
    /// </summary>
    public static IEnumerable<TraktNetworkUser> GetNetworkFollowers()
    {
      return GetNetworkFollowers(TraktSettings.Username);
    }
    public static IEnumerable<TraktNetworkUser> GetNetworkFollowers(string person)
    {
      string response = Transmit(string.Format(TraktURIs.NetworkFollowers, person), GetUserAuthentication());
      return response.FromJSONArray<TraktNetworkUser>();
    }

    /// <summary>
    /// Returns a list of people awaiting 'following' approval
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<TraktNetworkReqUser> GetNetworkRequests()
    {
      string response = Transmit(TraktURIs.NetworkRequests, GetUserAuthentication());
      return response.FromJSONArray<TraktNetworkReqUser>();
    }

    public static TraktNetworkFollowResponse NetworkFollow(TraktNetwork person)
    {
      string response = Transmit(TraktURIs.NetworkFollow, person.ToJSON());
      return response.FromJSON<TraktNetworkFollowResponse>();
    }

    public static TraktResponse NetworkUnFollow(TraktNetwork person)
    {
      string response = Transmit(TraktURIs.NetworkUnFollow, person.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktResponse NetworkApprove(TraktNetworkApprove person)
    {
      string response = Transmit(TraktURIs.NetworkApprove, person.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktResponse NetworkDeny(TraktNetwork person)
    {
      string response = Transmit(TraktURIs.NetworkDeny, person.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    #region Obselete Methods

    [Obsolete("This method is now obsolete, use GetNetworkFriends()", false)]
    public static IEnumerable<TraktUserProfile> GetFriends()
    {
      string response = Transmit(string.Format(TraktURIs.Friends, TraktSettings.Username), GetUserAuthentication());
      return response.FromJSONArray<TraktUserProfile>();
    }

    [Obsolete("This method is now obsolete, use GetNetworkRequests()", false)]
    public static IEnumerable<TraktUserProfile> GetFriendRequests()
    {
      string response = Transmit(TraktURIs.FriendRequests, GetUserAuthentication());
      return response.FromJSONArray<TraktUserProfile>();
    }

    [Obsolete("This method is now obsolete, use NetworkApprove()", false)]
    public static TraktResponse FriendApprove(TraktFriend friend)
    {
      string response = Transmit(TraktURIs.FriendApprove, friend.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    [Obsolete("This method is now obsolete, use NetworkFollow()", false)]
    public static TraktResponse FriendAdd(TraktFriend friend)
    {
      string response = Transmit(TraktURIs.FriendAdd, friend.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    [Obsolete("This method is now obsolete, use NetworkDeny()", false)]
    public static TraktResponse FriendDeny(TraktFriend friend)
    {
      string response = Transmit(TraktURIs.FriendDeny, friend.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    [Obsolete("This method is now obsolete, use NetworkUnfollow()", false)]
    public static TraktResponse FriendDelete(TraktFriend friend)
    {
      string response = Transmit(TraktURIs.FriendDelete, friend.ToJSON());
      return response.FromJSON<TraktResponse>();
    }
    #endregion

    #endregion

    #region Trending

    public static IEnumerable<TraktTrendingMovie> GetTrendingMovies()
    {
      string response = Transmit(TraktURIs.TrendingMovies, GetUserAuthentication());
      return response.FromJSONArray<TraktTrendingMovie>();
    }

    public static IEnumerable<TraktTrendingShow> GetTrendingShows()
    {
      string response = Transmit(TraktURIs.TrendingShows, GetUserAuthentication());
      return response.FromJSONArray<TraktTrendingShow>();
    }

    #endregion

    #region Recommendations

    public static TraktResponse DismissMovieRecommendation(TraktMovieSlug movie)
    {
      string response = Transmit(TraktURIs.DismissMovieRecommendation, movie.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktResponse DismissShowRecommendation(TraktShowSlug show)
    {
      string response = Transmit(TraktURIs.DismissShowRecommendation, show.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    /// <summary>
    /// Get Recommendations with out any filtering
    /// </summary>
    public static IEnumerable<TraktMovie> GetRecommendedMovies()
    {
      string response = Transmit(TraktURIs.UserMovieRecommendations, GetUserAuthentication());
      return response.FromJSONArray<TraktMovie>();
    }

    public static IEnumerable<TraktMovie> GetRecommendedMovies(string genre, bool hidecollected, bool hidewatchlisted, int startyear, int endyear)
    {
      var traktRecommendationPost = new TraktRecommendations
      {
        Username = TraktSettings.Username,
        Password = TraktSettings.Password,
        Genre = genre,
        HideCollected = hidecollected,
        HideWatchlisted = hidewatchlisted,
        StartYear = startyear,
        EndYear = endyear
      };

      string response = Transmit(TraktURIs.UserMovieRecommendations, traktRecommendationPost.ToJSON());
      return response.FromJSONArray<TraktMovie>();
    }

    /// <summary>
    /// Get Recommendations with out any filtering
    /// </summary>        
    public static IEnumerable<TraktShow> GetRecommendedShows()
    {
      string response = Transmit(TraktURIs.UserShowsRecommendations, GetUserAuthentication());
      return response.FromJSONArray<TraktShow>();
    }

    public static IEnumerable<TraktShow> GetRecommendedShows(string genre, bool hidecollected, bool hidewatchlisted, int startyear, int endyear)
    {
      var traktRecommendationPost = new TraktRecommendations
      {
        Username = TraktSettings.Username,
        Password = TraktSettings.Password,
        Genre = genre,
        HideCollected = hidecollected,
        HideWatchlisted = hidewatchlisted,
        StartYear = startyear,
        EndYear = endyear
      };

      string response = Transmit(TraktURIs.UserShowsRecommendations, traktRecommendationPost.ToJSON());
      return response.FromJSONArray<TraktShow>();
    }

    #endregion

    #region Watch List

    public static IEnumerable<TraktWatchListMovie> GetWatchListMovies(string user)
    {
      string response = Transmit(string.Format(TraktURIs.UserMovieWatchList, user), GetUserAuthentication());
      return response.FromJSONArray<TraktWatchListMovie>();
    }

    public static IEnumerable<TraktWatchListShow> GetWatchListShows(string user)
    {
      string response = Transmit(string.Format(TraktURIs.UserShowsWatchList, user), GetUserAuthentication());
      return response.FromJSONArray<TraktWatchListShow>();
    }

    public static IEnumerable<TraktWatchListEpisode> GetWatchListEpisodes(string user)
    {
      string response = Transmit(string.Format(TraktURIs.UserEpisodesWatchList, user), GetUserAuthentication());
      return response.FromJSONArray<TraktWatchListEpisode>();
    }

    #endregion

    #region Lists

    public static TraktAddListResponse ListAdd(TraktList list)
    {
      string response = Transmit(TraktURIs.ListAdd, list.ToJSON());
      return response.FromJSON<TraktAddListResponse>();
    }

    public static TraktResponse ListDelete(TraktList list)
    {
      string response = Transmit(TraktURIs.ListDelete, list.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktResponse ListUpdate(TraktList list)
    {
      string response = Transmit(TraktURIs.ListUpdate, list.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktSyncResponse ListAddItems(TraktList list)
    {
      string response = Transmit(TraktURIs.ListItemsAdd, list.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse ListDeleteItems(TraktList list)
    {
      string response = Transmit(TraktURIs.ListItemsDelete, list.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    #endregion

    #region Account

    public static TraktResponse CreateAccount(TraktAccount account)
    {
      string response = Transmit(TraktURIs.CreateAccount, account.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktResponse TestAccount(TraktAccount account)
    {
      string response = Transmit(TraktURIs.TestAccount, account.ToJSON());
      return response.FromJSON<TraktResponse>();
    }

    public static TraktAccountSettings GetAccountSettings()
    {
      string response = Transmit(TraktURIs.AccountSettings, GetUserAuthentication());
      return response.FromJSON<TraktAccountSettings>();
    }

    #endregion

    #region Search

    /// <summary>
    /// Search from one or more types, movies, episodes, shows etc...
    /// </summary>
    /// <param name="searchTerm">string to search for</param>
    /// <param name="types">a list of search types</param>
    /// <param name="maxResults"></param>
    /// <returns>returns results from multiple search types</returns>
    public static TraktSearchResult Search(string searchTerm, HashSet<SearchType> types, int maxResults)
    {
      // collect all the results from each type in this list
      TraktSearchResult results = new TraktSearchResult();

      // run all search types in parallel
      List<Thread> threads = new List<Thread>();

      foreach (var type in types)
      {
        switch (type)
        {
          case SearchType.movies:
            Thread tMovieSearch = new Thread(delegate(object obj) { results.Movies = SearchMovies(obj as string, maxResults); });
            tMovieSearch.Start(searchTerm);
            tMovieSearch.Name = "Search";
            threads.Add(tMovieSearch);
            break;

          case SearchType.shows:
            Thread tShowSearch = new Thread(delegate(object obj) { results.Shows = SearchShows(obj as string, maxResults); });
            tShowSearch.Start(searchTerm);
            tShowSearch.Name = "Search";
            threads.Add(tShowSearch);
            break;

          case SearchType.episodes:
            Thread tEpisodeSearch = new Thread(delegate(object obj) { results.Episodes = SearchEpisodes(obj as string, maxResults); });
            tEpisodeSearch.Start(searchTerm);
            tEpisodeSearch.Name = "Search";
            threads.Add(tEpisodeSearch);
            break;

          case SearchType.people:
            Thread tPeopleSearch = new Thread(delegate(object obj) { results.People = SearchPeople(obj as string, maxResults); });
            tPeopleSearch.Start(searchTerm);
            tPeopleSearch.Name = "Search";
            threads.Add(tPeopleSearch);
            break;

          case SearchType.users:
            Thread tUserSearch = new Thread(delegate(object obj) { results.Users = SearchForUsers(obj as string, maxResults); });
            tUserSearch.Start(searchTerm);
            tUserSearch.Name = "Search";
            threads.Add(tUserSearch);
            break;
        }
      }

      // wait until all search results are back
      threads.ForEach(t => t.Join());

      // now we have everything we need
      return results;
    }

    /// <summary>
    /// Returns a list of users found using search term
    /// </summary>
    public static IEnumerable<TraktUser> SearchForUsers(string searchTerm)
    {
      return SearchForUsers(searchTerm, 30);
    }
    public static IEnumerable<TraktUser> SearchForUsers(string searchTerm, int maxResults)
    {
      string response = Transmit(string.Format(TraktURIs.SearchUsers, HttpUtility.UrlEncode(searchTerm), maxResults), GetUserAuthentication());
      return response.FromJSONArray<TraktUser>();
    }

    /// <summary>
    /// Returns a list of movies found using search term
    /// </summary>
    public static IEnumerable<TraktMovie> SearchMovies(string searchTerm)
    {
      return SearchMovies(searchTerm, 30);
    }
    public static IEnumerable<TraktMovie> SearchMovies(string searchTerm, int maxResults)
    {
      string response = Transmit(string.Format(TraktURIs.SearchMovies, HttpUtility.UrlEncode(searchTerm), maxResults), GetUserAuthentication());
      return response.FromJSONArray<TraktMovie>();
    }

    /// <summary>
    /// Returns a list of shows found using search term
    /// </summary>
    public static IEnumerable<TraktShow> SearchShows(string searchTerm)
    {
      return SearchShows(searchTerm, 30);
    }
    public static IEnumerable<TraktShow> SearchShows(string searchTerm, int maxResults)
    {
      string response = Transmit(string.Format(TraktURIs.SearchShows, HttpUtility.UrlEncode(searchTerm), maxResults), GetUserAuthentication());
      return response.FromJSONArray<TraktShow>();
    }

    /// <summary>
    /// Returns a list of episodes found using search term
    /// </summary>
    public static IEnumerable<TraktEpisodeSummary> SearchEpisodes(string searchTerm)
    {
      return SearchEpisodes(searchTerm, 30);
    }
    public static IEnumerable<TraktEpisodeSummary> SearchEpisodes(string searchTerm, int maxResults)
    {
      string response = Transmit(string.Format(TraktURIs.SearchEpisodes, HttpUtility.UrlEncode(searchTerm), maxResults), GetUserAuthentication());
      return response.FromJSONArray<TraktEpisodeSummary>();
    }

    /// <summary>
    /// Returns a list of people found using search term
    /// </summary>
    public static IEnumerable<TraktPersonSummary> SearchPeople(string searchTerm)
    {
      return SearchPeople(searchTerm, 30);
    }
    public static IEnumerable<TraktPersonSummary> SearchPeople(string searchTerm, int maxResults)
    {
      string response = Transmit(string.Format(TraktURIs.SearchPeople, HttpUtility.UrlEncode(searchTerm), maxResults), string.Empty);
      return response.FromJSONArray<TraktPersonSummary>();
    }

    #endregion

    #region Summary

    /// <summary>
    /// Returns full movie details
    /// </summary>
    /// <param name="id">TMDB ID, IMDB ID or slug</param>
    /// <returns></returns>
    public static TraktMovieSummary MovieOverview(string id)
    {
      string response = Transmit(string.Format(TraktURIs.MovieOverview, HttpUtility.UrlEncode(id)), GetUserAuthentication());
      return response.FromJSON<TraktMovieSummary>();
    }

    /// <summary>
    /// Returns tv series details
    /// </summary>
    /// <param name="id">TVDB ID or slug</param>
    /// <param name="extended"></param>
    /// <returns></returns>
    public static TraktShowSummary SeriesOverview(string id, bool extended = false)
    {
      string url = extended ? TraktURIs.SeriesOverviewExtended : TraktURIs.SeriesOverview;

      string response = Transmit(string.Format(url, HttpUtility.UrlEncode(id)), GetUserAuthentication());
      return response.FromJSON<TraktShowSummary>();
    }

    #endregion

    #region Comments

    /// <summary>
    /// Return a list of shouts for a movie
    /// </summary>
    /// <param name="title">The movie search term, either (title-year seperate spaces with '-'), imdbid, tmdbid</param>    
    public static IEnumerable<TraktShout> GetMovieShouts(string title)
    {
      string response = Transmit(string.Format(TraktURIs.MovieShouts, title), GetUserAuthentication());
      return response.FromJSONArray<TraktShout>();
    }

    /// <summary>
    /// Return a list of shouts for a show
    /// </summary>
    /// <param name="title">The show search term, either (title seperate spaces with '-'), imdbid, tvdbid</param>    
    public static IEnumerable<TraktShout> GetShowShouts(string title)
    {
      string response = Transmit(string.Format(TraktURIs.ShowShouts, title), GetUserAuthentication());
      return response.FromJSONArray<TraktShout>();
    }

    /// <summary>
    /// Return a list of shouts for a episode
    /// </summary>
    /// <param name="title">The episode search term, either (title seperate spaces with '-'), imdbid, tmdbid</param>
    /// <param name="episode">The episode index</param>
    /// <param name="season">The season index</param>
    public static IEnumerable<TraktShout> GetEpisodeShouts(string title, string season, string episode)
    {
      string response = Transmit(string.Format(TraktURIs.EpisodeShouts, title, season, episode), GetUserAuthentication());
      return response.FromJSONArray<TraktShout>();
    }

    #endregion

    #region Related

    public static IEnumerable<TraktMovie> GetRelatedMovies(string title)
    {
      return GetRelatedMovies(title, false);
    }

    /// <summary>
    /// Return a list of related movies for a movie
    /// </summary>
    /// <param name="title">The movie search term, either (title-year seperate spaces with '-'), imdbid, tmdbid</param>
    /// <param name="hidewatched">Hide watched movies</param>
    public static IEnumerable<TraktMovie> GetRelatedMovies(string title, bool hidewatched)
    {
      string response = Transmit(string.Format(TraktURIs.RelatedMovies, title, hidewatched ? "/hidewatched" : string.Empty), GetUserAuthentication());
      return response.FromJSONArray<TraktMovie>();
    }

    public static IEnumerable<TraktShow> GetRelatedShows(string title)
    {
      return GetRelatedShows(title, false);
    }

    /// <summary>
    /// Return a list of related shows for a show
    /// </summary>
    /// <param name="title">The show search term, either (title-year seperate spaces with '-'), imdbid, tvdbid</param>
    /// <param name="hidewatched">Hide watched movies</param>
    public static IEnumerable<TraktShow> GetRelatedShows(string title, bool hidewatched)
    {
      string response = Transmit(string.Format(TraktURIs.RelatedShows, title, hidewatched ? "/hidewatched" : string.Empty), GetUserAuthentication());
      return response.FromJSONArray<TraktShow>();
    }

    #endregion

    #region Show Seasons

    /// <summary>
    /// Return a list of seasons for a tv show
    /// </summary>
    /// <param name="title">The show search term, either (title-year seperate spaces with '-'), imdbid, tvdbid</param>
    public static IEnumerable<TraktShowSeason> GetShowSeasons(string title)
    {
      string response = Transmit(string.Format(TraktURIs.ShowSeasons, title), string.Empty);
      return response.FromJSONArray<TraktShowSeason>();
    }

    #endregion

    #region Season Episodes

    /// <summary>
    /// Return a list of episodes for a tv show season
    /// </summary>
    /// <param name="title">The show search term, either (title-year seperate spaces with '-'), imdbid, tvdbid</param>
    /// <param name="season">The season, 0 for specials</param>
    public static IEnumerable<TraktEpisode> GetSeasonEpisodes(string title, string season)
    {
      string response = Transmit(string.Format(TraktURIs.SeasonEpisodes, title, season), GetUserAuthentication());
      return response.FromJSONArray<TraktEpisode>();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Gets a User Authentication object
    /// </summary>       
    /// <returns>The User Authentication json string</returns>
    private static string GetUserAuthentication()
    {
      return new TraktAuthentication { Username = Username, Password = Password }.ToJSON();
    }

    /// <summary>
    /// Communicates to and from Trakt
    /// </summary>
    /// <param name="address">The URI to use</param>
    /// <param name="data">The Data to send</param>
    /// <returns>The response from Trakt</returns>
    private static string Transmit(string address, string data)
    {
      if (OnDataSend != null) OnDataSend(address, data);

      try
      {
        ServicePointManager.Expect100Continue = false;
        WebClient client = new CompressionWebClient(true) { Encoding = Encoding.UTF8 };
        client.Headers.Add("user-agent", UserAgent);

        // wait for a response from the server
        string response = client.UploadString(address, data);

        // received data, pass it back
        if (OnDataReceived != null) OnDataReceived(response);
        return response;
      }
      catch (WebException e)
      {
        if (OnDataError != null) OnDataError(e.Message);

        if (e.Status == WebExceptionStatus.ProtocolError)
        {
          var response = ((HttpWebResponse)e.Response);
          try
          {
            using (var stream = response.GetResponseStream())
            {
              using (var reader = new StreamReader(stream))
              {
                return reader.ReadToEnd();
              }
            }
          }
          catch { }
        }

        // create a proper response object
        TraktResponse error = new TraktResponse
        {
          Status = "failure",
          Error = e.Message
        };
        return error.ToJSON();
      }
    }

    #endregion
  }
}
