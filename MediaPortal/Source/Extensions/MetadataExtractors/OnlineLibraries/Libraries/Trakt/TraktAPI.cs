#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Net;
using System.Threading;
using System.Web;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Enums;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Extension;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Web;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TraktAPI;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt
{
  public static class TraktAPI
  {
    #region Trakt Methods

    #region Sync

    public static TraktLastSyncActivities GetLastSyncActivities()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncLastActivities);
      return response.FromJSON<TraktLastSyncActivities>();
    }

    #endregion

    #region Playback

    public static IEnumerable<TraktSyncPausedMovie> GetPausedMovies()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncPausedMovies);
      return response.FromJSONArray<TraktSyncPausedMovie>();
    }

    public static IEnumerable<TraktSyncPausedEpisode> GetPausedEpisodes()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncPausedEpisodes);
      return response.FromJSONArray<TraktSyncPausedEpisode>();
    }

    #endregion

    #region Collection

    public static IEnumerable<TraktMovieCollected> GetCollectedMovies()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncCollectionMovies);
      return response.FromJSONArray<TraktMovieCollected>();
    }

    public static IEnumerable<TraktEpisodeCollected> GetCollectedEpisodes()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncCollectionEpisodes);
      return response.FromJSONArray<TraktEpisodeCollected>();
    }

    #endregion

    #region Watched History

    public static IEnumerable<TraktMovieWatched> GetWatchedMovies()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncWatchedMovies);
      return response.FromJSONArray<TraktMovieWatched>();
    }

    public static IEnumerable<TraktEpisodeWatched> GetWatchedEpisodes()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncWatchedEpisodes);
      return response.FromJSONArray<TraktEpisodeWatched>();
    }

    #endregion

    #region Ratings

    public static IEnumerable<TraktMovieRated> GetRatedMovies()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncRatedMovies);
      return response.FromJSONArray<TraktMovieRated>();
    }

    public static IEnumerable<TraktEpisodeRated> GetRatedEpisodes()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncRatedEpisodes);
      return response.FromJSONArray<TraktEpisodeRated>();
    }

    public static IEnumerable<TraktShowRated> GetRatedShows()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncRatedShows);
      return response.FromJSONArray<TraktShowRated>();
    }

    public static IEnumerable<TraktSeasonRated> GetRatedSeasons()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.SyncRatedSeasons);
      return response.FromJSONArray<TraktSeasonRated>();
    }

    #endregion

    #region User

    public static TraktUserStatistics GetUserStatistics(string user)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserStats, user));
      return response.FromJSON<TraktUserStatistics>();
    }

    public static TraktUserSummary GetUserProfile(string user)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserProfile, user));
      return response.FromJSON<TraktUserSummary>();
    }

    /// <summary>
    /// Gets a list of follower requests for the current user
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<TraktFollowerRequest> GetFollowerRequests()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.UserFollowerRequests);
      return response.FromJSONArray<TraktFollowerRequest>();
    }

    /// <summary>
    /// Returns a list of Friends for current user
    /// Friends are a two-way relationship ie. both following each other
    /// </summary>
    //public static IEnumerable<TraktNetworkFriend> GetNetworkFriends()
    //{
    //  return GetNetworkFriends(TraktSettings.Username);
    //}

    public static IEnumerable<TraktNetworkFriend> GetNetworkFriends(string user)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.NetworkFriends, user));
      return response.FromJSONArray<TraktNetworkFriend>();
    }

    public static IEnumerable<TraktNetworkUser> GetNetworkFollowing(string user)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.NetworkFollowing, user));
      return response.FromJSONArray<TraktNetworkUser>();
    }

    public static IEnumerable<TraktNetworkUser> GetNetworkFollowers(string user)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.NetworkFollowers, user));
      return response.FromJSONArray<TraktNetworkUser>();
    }

    public static TraktNetworkUser NetworkApproveFollower(int id)
    {
      string response = TraktWeb.PostToTrakt(string.Format(TraktURIs.NetworkFollowRequest, id), string.Empty);
      return response.FromJSON<TraktNetworkUser>();
    }

    public static IEnumerable<TraktMovieHistory> GetUsersMovieWatchedHistory(string username, int page = 1, int maxItems = 100)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserWatchedHistoryMovies, username, page, maxItems));
      return response.FromJSONArray<TraktMovieHistory>();
    }

    public static IEnumerable<TraktEpisodeHistory> GetUsersEpisodeWatchedHistory(string username, int page = 1, int maxItems = 100)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserWatchedHistoryEpisodes, username, page, maxItems));
      return response.FromJSONArray<TraktEpisodeHistory>();
    }

    /// <summary>
    /// Get comments for user sorted by most recent
    /// </summary>
    /// <param name="username">Username of person that made comment</param>
    /// <param name="commentType">all, reviews, shouts</param>
    /// <param name="type"> all, movies, shows, seasons, episodes, lists</param>
    public static TraktComments GetUsersComments(string username, string commentType = "all", string type = "all", string extendedInfoParams = "min", int page = 1, int maxItems = 10)
    {
      var headers = new WebHeaderCollection();

      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserComments, username, commentType, type, extendedInfoParams, page, maxItems));
      if (response == null)
        return null;

      try
      {
        return new TraktComments
        {
          CurrentPage = page,
          TotalItemsPerPage = maxItems,
          TotalPages = int.Parse(headers["X-Pagination-Page-Count"]),
          TotalItems = int.Parse(headers["X-Pagination-Item-Count"]),
          Comments = response.FromJSONArray<TraktCommentItem>()
        };
      }
      catch
      {
        // most likely bad header response
        return null;
      }
    }

    #endregion

    #region Lists

    public static IEnumerable<TraktListDetail> GetUserLists(string username)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserLists, username));
      return response.FromJSONArray<TraktListDetail>();
    }

    public static IEnumerable<TraktListItem> GetUserListItems(string username, string listId, string extendedInfoParams = "min")
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserListItems, username, listId, extendedInfoParams));
      return response.FromJSONArray<TraktListItem>();
    }

    public static TraktListDetail CreateCustomList(TraktList list, string username)
    {
      var response = TraktWeb.PostToTrakt(string.Format(TraktURIs.UserListAdd, username), list.ToJSON());
      return response.FromJSON<TraktListDetail>();
    }

    public static TraktSyncResponse AddItemsToList(string username, string id, TraktSyncAll items)
    {
      var response = TraktWeb.PostToTrakt(string.Format(TraktURIs.UserListItemsAdd, username, id), items.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveItemsFromList(string username, string id, TraktSyncAll items)
    {
      var response = TraktWeb.PostToTrakt(string.Format(TraktURIs.UserListItemsRemove, username, id), items.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static bool DeleteUserList(string username, string listId)
    {
      return DeleteFromTrakt(string.Format(TraktURIs.DeleteList, username, listId));
    }

    public static bool LikeList(string username, int id)
    {
      var response = TraktWeb.PostToTrakt(string.Format(TraktURIs.UserListLike, username, id), null);
      return response != null;
    }

    public static bool UnLikeList(string username, int id)
    {
      return DeleteFromTrakt(string.Format(TraktURIs.UserListLike, username, id));
    }

    #endregion

    #region Watchlists

    public static IEnumerable<TraktMovieWatchList> GetWatchListMovies(string username, string extendedInfoParams = "min")
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserWatchlistMovies, username, extendedInfoParams));
      return response.FromJSONArray<TraktMovieWatchList>();
    }

    public static IEnumerable<TraktShowWatchList> GetWatchListShows(string username, string extendedInfoParams = "min")
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserWatchlistShows, username, extendedInfoParams));
      return response.FromJSONArray<TraktShowWatchList>();
    }

    public static IEnumerable<TraktSeasonWatchList> GetWatchListSeasons(string username, string extendedInfoParams = "min")
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserWatchlistSeasons, username, extendedInfoParams), true);
      return response.FromJSONArray<TraktSeasonWatchList>();
    }

    public static IEnumerable<TraktEpisodeWatchList> GetWatchListEpisodes(string username, string extendedInfoParams = "min")
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserWatchlistEpisodes, username, extendedInfoParams), true);
      return response.FromJSONArray<TraktEpisodeWatchList>();
    }

    #endregion

    #region Likes

    /// <summary>
    /// Gets the current users liked items (comments and/or lists)
    /// </summary>
    /// <param name="type">The type of liked item: all (default), lists or comments</param>
    /// <param name="extendedInfoParams">Extended Info: min, full, images (comma separated)</param>
    /// <param name="page">Page Number</param>
    /// <param name="maxItems">Maximum number of items to request per page (this should be consistent per page request)</param>
    public static TraktLikes GetLikedItems(string type = "all", string extendedInfoParams = "min", int page = 1, int maxItems = 10)
    {
      var headers = new WebHeaderCollection();

      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.UserLikedItems, type, extendedInfoParams, page, maxItems));
      if (response == null)
        return null;

      try
      {
        return new TraktLikes
        {
          CurrentPage = page,
          TotalItemsPerPage = maxItems,
          TotalPages = int.Parse(headers["X-Pagination-Page-Count"]),
          TotalItems = int.Parse(headers["X-Pagination-Item-Count"]),
          Likes = response.FromJSONArray<TraktLike>()
        };
      }
      catch
      {
        // most likely bad header response
        return null;
      }
    }

    #endregion

    #region Movies

    #region Related

    public static IEnumerable<TraktMovieSummary> GetRelatedMovies(string id, int limit = 10)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.RelatedMovies, id, limit));
      return response.FromJSONArray<TraktMovieSummary>();
    }

    #endregion

    #region Comments

    public static IEnumerable<TraktComment> GetMovieComments(string id, int page = 1, int maxItems = 1000)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.MovieComments, id, page, maxItems));
      return response.FromJSONArray<TraktComment>();
    }

    #endregion

    #region Popular

    public static TraktMoviesPopular GetPopularMovies(int page = 1, int maxItems = 100)
    {
      var headers = new WebHeaderCollection();

      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.PopularMovies, page, maxItems));
      if (response == null)
        return null;

      try
      {
        return new TraktMoviesPopular
        {
          CurrentPage = page,
          TotalItemsPerPage = maxItems,
          Movies = response.FromJSONArray<TraktMovieSummary>()
        };
      }
      catch
      {
        // most likely bad header response
        return null;
      }
    }

    #endregion

    #region Trending

    public static TraktMoviesTrending GetTrendingMovies(int page = 1, int maxItems = 100)
    {
      var headers = new WebHeaderCollection();

      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.TrendingMovies, page, maxItems));
      if (response == null)
        return null;

      try
      {
        return new TraktMoviesTrending
        {
          CurrentPage = page,
          TotalItemsPerPage = maxItems,
          TotalPages = int.Parse(headers["X-Pagination-Page-Count"]),
          TotalItems = int.Parse(headers["X-Pagination-Item-Count"]),
          TotalWatchers = int.Parse(headers["X-Trending-User-Count"]),
          Movies = response.FromJSONArray<TraktMovieTrending>()
        };
      }
      catch
      {
        // most likely bad header response
        return null;
      }
    }

    #endregion

    #region Recommendations

    public static IEnumerable<TraktMovieSummary> GetRecommendedMovies(string extendedInfoParams = "min")
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.RecommendedMovies, extendedInfoParams));
      return response.FromJSONArray<TraktMovieSummary>();
    }

    public static bool DismissRecommendedMovie(string movieId)
    {
      return DeleteFromTrakt(string.Format(TraktURIs.DismissRecommendedMovie, movieId));
    }

    #endregion

    #region Updates

    public static TraktMoviesUpdated GetRecentlyUpdatedMovies(string sincedate, int page = 1, int maxItems = 100)
    {
      var headers = new WebHeaderCollection();

      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.MovieUpdates, sincedate, page, maxItems));
      if (response == null)
        return null;

      try
      {
        return new TraktMoviesUpdated
        {
          CurrentPage = page,
          TotalItemsPerPage = maxItems,
          TotalPages = int.Parse(headers["X-Pagination-Page-Count"]),
          TotalItems = int.Parse(headers["X-Pagination-Item-Count"]),
          Movies = response.FromJSONArray<TraktMovieUpdate>()
        };
      }
      catch
      {
        // most likely bad header response
        return null;
      }
    }

    #endregion

    #endregion

    #region Shows

    #region Related

    public static IEnumerable<TraktShowSummary> GetRelatedShows(string id, int limit = 10)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.RelatedShows, id, limit));
      return response.FromJSONArray<TraktShowSummary>();
    }

    #endregion

    #region Summary

    public static TraktShowSummary GetShowSummary(string id)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.ShowSummary, id));
      return response.FromJSON<TraktShowSummary>();
    }

    #endregion

    #region Updates

    public static TraktShowsUpdated GetRecentlyUpdatedShows(string sincedate, int page = 1, int maxItems = 100)
    {
      var headers = new WebHeaderCollection();

      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.ShowUpdates, sincedate, page, maxItems));
      if (response == null)
        return null;

      try
      {
        return new TraktShowsUpdated
        {
          CurrentPage = page,
          TotalItemsPerPage = maxItems,
          TotalPages = int.Parse(headers["X-Pagination-Page-Count"]),
          TotalItems = int.Parse(headers["X-Pagination-Item-Count"]),
          Shows = response.FromJSONArray<TraktShowUpdate>()
        };
      }
      catch
      {
        // most likely bad header response
        return null;
      }
    }

    #endregion

    #region Seasons

    /// <summary>
    /// Gets the seasons for a show
    /// </summary>
    /// <param name="id">the id of the tv show</param>
    /// <param name="extendedParameter">request parameters, "episodes,full,images"</param>
    public static IEnumerable<TraktSeasonSummary> GetShowSeasons(string id, string extendedParameter = "full,images")
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.ShowSeasons, id, extendedParameter));
      return response.FromJSONArray<TraktSeasonSummary>();
    }

    public static IEnumerable<TraktComment> GetSeasonComments(string id, int season, int page = 1, int maxItems = 1000)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.SeasonComments, id, season, page, maxItems), true);
      return response.FromJSONArray<TraktComment>();
    }

    #endregion

    #region Comments

    public static IEnumerable<TraktComment> GetShowComments(string id, int page = 1, int maxItems = 1000)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.ShowComments, id, page, maxItems), true);
      return response.FromJSONArray<TraktComment>();
    }

    #endregion

    #region Popular

    public static TraktShowsPopular GetPopularShows(int page = 1, int maxItems = 100)
    {
      var headers = new WebHeaderCollection();

      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.PopularShows, page, maxItems));
      if (response == null)
        return null;

      try
      {
        return new TraktShowsPopular
        {
          CurrentPage = page,
          TotalItemsPerPage = maxItems,
          Shows = response.FromJSONArray<TraktShowSummary>()
        };
      }
      catch
      {
        // most likely bad header response
        return null;
      }
    }

    #endregion

    #region Trending

    public static TraktShowsTrending GetTrendingShows(int page = 1, int maxItems = 100)
    {
      var headers = new WebHeaderCollection();

      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.TrendingShows, page, maxItems));
      if (response == null)
        return null;

      try
      {
        return new TraktShowsTrending
        {
          CurrentPage = page,
          TotalItemsPerPage = maxItems,
          TotalPages = int.Parse(headers["X-Pagination-Page-Count"]),
          TotalItems = int.Parse(headers["X-Pagination-Item-Count"]),
          TotalWatchers = int.Parse(headers["X-Trending-User-Count"]),
          Shows = response.FromJSONArray<TraktShowTrending>()
        };
      }
      catch
      {
        // most likely bad header response
        return null;
      }
    }

    #endregion

    #region Recommendations

    public static IEnumerable<TraktShowSummary> GetRecommendedShows()
    {
      var response = TraktWeb.GetFromTrakt(TraktURIs.RecommendedShows);
      return response.FromJSONArray<TraktShowSummary>();
    }

    public static bool DismissRecommendedShow(string showId)
    {
      return DeleteFromTrakt(string.Format(TraktURIs.DismissRecommendedShow, showId));
    }

    #endregion

    #region Calendar

    /// <summary>
    /// Returns list of episodes in the Calendar
    /// </summary>
    public static Dictionary<string, IEnumerable<TraktCalendar>> GetCalendarShows()
    {
      // 7-Days from Today
      DateTime dateNow = DateTime.UtcNow;
      return GetCalendarShows(dateNow.ToString("yyyyMMdd"), "7", false);
    }

    /// <summary>
    /// Returns list of episodes in the Calendar
    /// </summary>
    /// <param name="startDate">Start Date of calendar in form yyyyMMdd</param>
    /// <param name="days">Number of days to return in calendar</param>
    /// <param name="userCalendar">Set to true to get the calendar filtered by users shows in library</param>
    public static Dictionary<string, IEnumerable<TraktCalendar>> GetCalendarShows(string startDate, string days, bool userCalendar)
    {
      string calendar = TraktWeb.GetFromTrakt(string.Format(TraktURIs.CalendarShows, startDate, days), userCalendar);
      return calendar.FromJSONDictionary<Dictionary<string, IEnumerable<TraktCalendar>>>();
    }

    /// <summary>
    /// Returns list of episodes in the Premieres Calendar
    /// </summary>
    public static Dictionary<string, IEnumerable<TraktCalendar>> GetCalendarPremieres()
    {
      // 7-Days from Today
      DateTime dateNow = DateTime.UtcNow;
      return GetCalendarPremieres(dateNow.ToString("yyyyMMdd"), "7");
    }

    /// <summary>
    /// Returns list of episodes in the Premieres Calendar
    /// </summary>        
    /// <param name="startDate">Start Date of calendar in form yyyyMMdd</param>
    /// <param name="days">Number of days to return in calendar</param>
    public static Dictionary<string, IEnumerable<TraktCalendar>> GetCalendarPremieres(string startDate, string days)
    {
      string premieres = TraktWeb.GetFromTrakt(string.Format(TraktURIs.CalendarPremieres, startDate, days));
      return premieres.FromJSONDictionary<Dictionary<string, IEnumerable<TraktCalendar>>>();
    }

    #endregion

    #endregion

    #region Episodes

    #region Comments

    public static IEnumerable<TraktComment> GetEpisodeComments(string id, int season, int episode, int page = 1, int maxItems = 1000)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.EpisodeComments, id, season, episode, page, maxItems));
      return response.FromJSONArray<TraktComment>();
    }

    #endregion

    #region Season Episodes

    public static IEnumerable<TraktEpisodeSummary> GetSeasonEpisodes(string showId, string seasonId)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.SeasonEpisodes, showId, seasonId));
      return response.FromJSONArray<TraktEpisodeSummary>();
    }

    #endregion

    #endregion

    #region Activity TODO

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
      //// get comma seperated list of types and actions (if more than one)
      //string activityTypes = types == null ? "all" : string.Join(",", types.Select(t => t.ToString()).ToArray());
      //string activityActions = actions == null ? "all" : string.Join(",", actions.Select(a => a.ToString()).ToArray());

      //string startEnd = (start == 0 || end == 0) ? string.Empty : string.Format("/{0}/{1}", start, end);
      //string apiUrl = includeMe ? TraktURIs.ActivityFriendsMe : TraktURIs.ActivityFriends;

      //string activity = Transmit(string.Format(apiUrl, activityTypes, activityActions, startEnd), GetUserAuthentication());
      //return activity.FromJSON<TraktActivity>();
      return null;
    }

    public static TraktActivity GetFollowingActivity()
    {
      return GetFollowingActivity(null, null);
    }

    public static TraktActivity GetFollowingActivity(List<ActivityType> types, List<ActivityAction> actions)
    {
      return GetFollowingActivity(types, actions, 0, 0);
    }

    public static TraktActivity GetFollowingActivity(List<ActivityType> types, List<ActivityAction> actions, long start, long end)
    {
      //// get comma seperated list of types and actions (if more than one)
      //string activityTypes = types == null ? "all" : string.Join(",", types.Select(t => t.ToString()).ToArray());
      //string activityActions = actions == null ? "all" : string.Join(",", actions.Select(a => a.ToString()).ToArray());

      //string startEnd = (start == 0 || end == 0) ? string.Empty : string.Format("/{0}/{1}", start, end);

      //string activity = Transmit(string.Format(TraktURIs.ActivityFollowing, activityTypes, activityActions, startEnd), GetUserAuthentication());
      //return activity.FromJSON<TraktActivity>();
      return null;
    }

    public static TraktActivity GetFollowersActivity()
    {
      return GetFollowersActivity(null, null);
    }

    public static TraktActivity GetFollowersActivity(List<ActivityType> types, List<ActivityAction> actions)
    {
      return GetFollowersActivity(types, actions, 0, 0);
    }

    public static TraktActivity GetFollowersActivity(List<ActivityType> types, List<ActivityAction> actions, long start, long end)
    {
      //// get comma seperated list of types and actions (if more than one)
      //string activityTypes = types == null ? "all" : string.Join(",", types.Select(t => t.ToString()).ToArray());
      //string activityActions = actions == null ? "all" : string.Join(",", actions.Select(a => a.ToString()).ToArray());

      //string startEnd = (start == 0 || end == 0) ? string.Empty : string.Format("/{0}/{1}", start, end);

      //string activity = Transmit(string.Format(TraktURIs.ActivityFollowers, activityTypes, activityActions, startEnd), GetUserAuthentication());
      //return activity.FromJSON<TraktActivity>();
      return null;
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
      //// get comma seperated list of types and actions (if more than one)
      //string activityTypes = types == null ? "all" : string.Join(",", types.Select(t => t.ToString()).ToArray());
      //string activityActions = actions == null ? "all" : string.Join(",", actions.Select(a => a.ToString()).ToArray());

      //string startEnd = (start == 0 || end == 0) ? string.Empty : string.Format("/{0}/{1}", start, end);

      //string activity = Transmit(string.Format(TraktURIs.ActivityCommunity, activityTypes, activityActions, startEnd), GetUserAuthentication());
      //return activity.FromJSON<TraktActivity>();
      return null;
    }

    public static TraktActivity GetUserActivity(string username, List<ActivityType> types, List<ActivityAction> actions)
    {
      //// get comma seperated list of types and actions (if more than one)
      //string activityTypes = string.Join(",", types.Select(t => t.ToString()).ToArray());
      //string activityActions = string.Join(",", actions.Select(a => a.ToString()).ToArray());

      //string activity = Transmit(string.Format(TraktURIs.ActivityUser, username, activityTypes, activityActions), GetUserAuthentication());
      //return activity.FromJSON<TraktActivity>();
      return null;
    }

    #endregion

    #region Search

    private static readonly Object searchLock = new Object();

    /// <summary>
    /// //TODO switch over to comma-seperate types in a single search
    /// Search from one or more types, movies, episodes, shows etc...
    /// </summary>
    /// <param name="searchTerm">string to search for</param>
    /// <param name="types">a list of search types</param>
    /// <returns>returns results from multiple search types</returns>
    public static IEnumerable<TraktSearchResult> Search(string searchTerm, HashSet<SearchType> types, int maxResults)
    {
      // collect all the results from each type in this list
      List<TraktSearchResult> results = new List<TraktSearchResult>();

      // run all search types in parallel
      List<Thread> threads = new List<Thread>();

      foreach (var type in types)
      {
        switch (type)
        {
          case SearchType.movies:
            var tMovieSearch = new Thread(obj =>
            {
              var response = SearchMovies(obj as string, maxResults);
              if (response != null)
              {
                lock (searchLock)
                {
                  results.AddRange(response);
                }
              }
            });
            tMovieSearch.Start(searchTerm);
            tMovieSearch.Name = "Search";
            threads.Add(tMovieSearch);
            break;

          case SearchType.shows:
            var tShowSearch = new Thread(obj =>
            {
              var response = SearchShows(obj as string, maxResults);
              if (response != null)
              {
                lock (searchLock)
                {
                  results.AddRange(response);
                }
              }
            });
            tShowSearch.Start(searchTerm);
            tShowSearch.Name = "Search";
            threads.Add(tShowSearch);
            break;

          case SearchType.episodes:
            var tEpisodeSearch = new Thread(obj =>
            {
              var response = SearchEpisodes(obj as string, maxResults);
              if (response != null)
              {
                lock (searchLock)
                {
                  results.AddRange(response);
                }
              }
            });
            tEpisodeSearch.Start(searchTerm);
            tEpisodeSearch.Name = "Search";
            threads.Add(tEpisodeSearch);
            break;

          case SearchType.people:
            var tPeopleSearch = new Thread(obj =>
            {
              var response = SearchPeople(obj as string, maxResults);
              if (response != null)
              {
                lock (searchLock)
                {
                  results.AddRange(response);
                }
              }
            });
            tPeopleSearch.Start(searchTerm);
            tPeopleSearch.Name = "Search";
            threads.Add(tPeopleSearch);
            break;

          case SearchType.users:
            var tUserSearch = new Thread(obj =>
            {
              var response = SearchUsers(obj as string, maxResults);
              if (response != null)
              {
                lock (searchLock)
                {
                  results.AddRange(response);
                }
              }
            });
            tUserSearch.Start(searchTerm);
            tUserSearch.Name = "Search";
            threads.Add(tUserSearch);
            break;

          case SearchType.lists:
            var tListSearch = new Thread(obj =>
            {
              var response = SearchLists(obj as string, maxResults);
              if (response != null)
              {
                lock (searchLock)
                {
                  results.AddRange(response);
                }
              }
            });
            tListSearch.Start(searchTerm);
            tListSearch.Name = "Search";
            threads.Add(tListSearch);
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
    public static IEnumerable<TraktSearchResult> SearchForUsers(string searchTerm)
    {
      return SearchUsers(searchTerm, 30);
    }

    public static IEnumerable<TraktSearchResult> SearchUsers(string searchTerm, int maxResults)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.SearchUsers, HttpUtility.UrlEncode(searchTerm), 1, maxResults));
      return response.FromJSONArray<TraktSearchResult>();
    }

    /// <summary>
    /// Returns a list of movies found using search term
    /// </summary>
    public static IEnumerable<TraktSearchResult> SearchMovies(string searchTerm)
    {
      return SearchMovies(searchTerm, 30);
    }

    public static IEnumerable<TraktSearchResult> SearchMovies(string searchTerm, int maxResults)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.SearchMovies, HttpUtility.UrlEncode(searchTerm), 1, maxResults));
      return response.FromJSONArray<TraktSearchResult>();
    }

    /// <summary>
    /// Returns a list of shows found using search term
    /// </summary>
    public static IEnumerable<TraktSearchResult> SearchShows(string searchTerm)
    {
      return SearchShows(searchTerm, 30);
    }

    public static IEnumerable<TraktSearchResult> SearchShows(string searchTerm, int maxResults)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.SearchShows, HttpUtility.UrlEncode(searchTerm), 1, maxResults));
      return response.FromJSONArray<TraktSearchResult>();
    }

    /// <summary>
    /// Returns a list of episodes found using search term
    /// </summary>
    public static IEnumerable<TraktSearchResult> SearchEpisodes(string searchTerm)
    {
      return SearchEpisodes(searchTerm, 30);
    }

    public static IEnumerable<TraktSearchResult> SearchEpisodes(string searchTerm, int maxResults)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.SearchEpisodes, HttpUtility.UrlEncode(searchTerm), 1, maxResults));
      return response.FromJSONArray<TraktSearchResult>();
    }

    /// <summary>
    /// Returns a list of people found using search term
    /// </summary>
    public static IEnumerable<TraktSearchResult> SearchPeople(string searchTerm)
    {
      return SearchPeople(searchTerm, 30);
    }

    public static IEnumerable<TraktSearchResult> SearchPeople(string searchTerm, int maxResults)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.SearchPeople, HttpUtility.UrlEncode(searchTerm), 1, maxResults));
      return response.FromJSONArray<TraktSearchResult>();
    }

    /// <summary>
    /// Returns a list of lists found using search term
    /// </summary>
    public static IEnumerable<TraktSearchResult> SearchLists(string searchTerm)
    {
      return SearchLists(searchTerm, 30);
    }

    public static IEnumerable<TraktSearchResult> SearchLists(string searchTerm, int maxResults)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.SearchLists, HttpUtility.UrlEncode(searchTerm), 1, maxResults));
      return response.FromJSONArray<TraktSearchResult>();
    }

    /// <summary>
    /// Returns a list of items found when searching by id
    /// </summary>
    /// <param name="idType">trakt-movie, trakt-show, trakt-episode, imdb, tmdb, tvdb, tvrage</param>
    /// <param name="id">the id to search by e.g. tt0848228</param>
    public static IEnumerable<TraktSearchResult> SearchById(string idType, string id)
    {
      string response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.SearchById, idType, id));
      return response.FromJSONArray<TraktSearchResult>();
    }

    #endregion

    #region Collection

    public static TraktSyncResponse AddMoviesToCollecton(TraktSyncMoviesCollected movies)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncCollectionAdd, movies.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveMoviesFromCollecton(TraktSyncMovies movies)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncCollectionRemove, movies.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToCollectonEx(TraktSyncShowsEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncCollectionAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToCollecton(TraktSyncShows shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncCollectionAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveShowsFromCollecton(TraktSyncShows shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncCollectionAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddEpisodesToCollecton(TraktSyncEpisodesCollected episodes)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncCollectionAdd, episodes.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveEpisodesFromCollecton(TraktSyncEpisodes episodes)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncCollectionRemove, episodes.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToCollectonEx(TraktSyncShowsCollectedEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncCollectionAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveShowsFromCollectonEx(TraktSyncShowsEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncCollectionRemove, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    #endregion

    #region Collection (Single)

    public static TraktSyncResponse AddMovieToCollection(TraktSyncMovieCollected movie)
    {
      var movies = new TraktSyncMoviesCollected
      {
        Movies = new List<TraktSyncMovieCollected>() { movie }
      };

      return AddMoviesToCollecton(movies);
    }

    public static TraktSyncResponse RemoveMovieFromCollection(TraktMovie movie)
    {
      var movies = new TraktSyncMovies
      {
        Movies = new List<TraktMovie>() { movie }
      };

      return RemoveMoviesFromCollecton(movies);
    }

    public static TraktSyncResponse AddShowToCollection(TraktShow show)
    {
      var shows = new TraktSyncShows
      {
        Shows = new List<TraktShow>() { show }
      };

      return AddShowsToCollecton(shows);
    }

    public static TraktSyncResponse RemoveShowFromCollection(TraktShow show)
    {
      var shows = new TraktSyncShows
      {
        Shows = new List<TraktShow>() { show }
      };

      return RemoveShowsFromCollecton(shows);
    }

    public static TraktSyncResponse AddShowToCollectionEx(TraktSyncShowEx show)
    {
      var shows = new TraktSyncShowsEx
      {
        Shows = new List<TraktSyncShowEx>() { show }
      };

      return AddShowsToCollectonEx(shows);
    }

    public static TraktSyncResponse RemoveShowFromCollectionEx(TraktSyncShowEx show)
    {
      var shows = new TraktSyncShowsEx
      {
        Shows = new List<TraktSyncShowEx>() { show }
      };

      return RemoveShowsFromCollectonEx(shows);
    }

    public static TraktSyncResponse AddEpisodeToCollection(TraktSyncEpisodeCollected episode)
    {
      var episodes = new TraktSyncEpisodesCollected
      {
        Episodes = new List<TraktSyncEpisodeCollected>() { episode }
      };

      return AddEpisodesToCollecton(episodes);
    }

    public static TraktSyncResponse RemoveEpisodeFromCollection(TraktEpisode episode)
    {
      var episodes = new TraktSyncEpisodes
      {
        Episodes = new List<TraktEpisode>() { episode }
      };

      return RemoveEpisodesFromCollecton(episodes);
    }

    #endregion

    #region Watched History

    public static TraktSyncResponse AddMoviesToWatchedHistory(TraktSyncMoviesWatched movies)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchedHistoryAdd, movies.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveMoviesFromWatchedHistory(TraktSyncMovies movies)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchedHistoryRemove, movies.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToWatchedHistory(TraktSyncShows shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchedHistoryAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveShowsFromWatchedHistory(TraktSyncShows shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchedHistoryRemove, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddEpisodesToWatchedHistory(TraktSyncEpisodesWatched episodes)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchedHistoryAdd, episodes.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveEpisodesFromWatchedHistory(TraktSyncEpisodes episodes)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchedHistoryRemove, episodes.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToWatchedHistoryEx(TraktSyncShowsEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchedHistoryAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToWatchedHistoryEx(TraktSyncShowsWatchedEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchedHistoryAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveShowsFromWatchedHistoryEx(TraktSyncShowsEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchedHistoryRemove, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    #endregion

    #region Watched History (Single)

    public static TraktSyncResponse AddMovieToWatchedHistory(TraktSyncMovieWatched movie)
    {
      var movies = new TraktSyncMoviesWatched
      {
        Movies = new List<TraktSyncMovieWatched>() { movie }
      };

      return AddMoviesToWatchedHistory(movies);
    }

    public static TraktSyncResponse RemoveMovieFromWatchedHistory(TraktMovie movie)
    {
      var movies = new TraktSyncMovies
      {
        Movies = new List<TraktMovie>() { movie }
      };

      return RemoveMoviesFromWatchedHistory(movies);
    }

    public static TraktSyncResponse AddShowToWatchedHistory(TraktShow show)
    {
      var shows = new TraktSyncShows
      {
        Shows = new List<TraktShow>() { show }
      };

      return AddShowsToWatchedHistory(shows);
    }

    public static TraktSyncResponse RemoveShowFromWatchedHistory(TraktShow show)
    {
      var shows = new TraktSyncShows
      {
        Shows = new List<TraktShow>() { show }
      };

      return RemoveShowsFromWatchedHistory(shows);
    }

    public static TraktSyncResponse AddShowToWatchedHistoryEx(TraktSyncShowEx show)
    {
      var shows = new TraktSyncShowsEx
      {
        Shows = new List<TraktSyncShowEx>() { show }
      };

      return AddShowsToWatchedHistoryEx(shows);
    }

    public static TraktSyncResponse RemoveShowFromWatchedHistoryEx(TraktSyncShowEx show)
    {
      var shows = new TraktSyncShowsEx
      {
        Shows = new List<TraktSyncShowEx>() { show }
      };

      return RemoveShowsFromWatchedHistoryEx(shows);
    }

    public static TraktSyncResponse AddEpisodeToWatchedHistory(TraktSyncEpisodeWatched episode)
    {
      var episodes = new TraktSyncEpisodesWatched
      {
        Episodes = new List<TraktSyncEpisodeWatched>() { episode }
      };

      return AddEpisodesToWatchedHistory(episodes);
    }

    public static TraktSyncResponse RemoveEpisodeFromWatchedHistory(TraktEpisode episode)
    {
      var episodes = new TraktSyncEpisodes
      {
        Episodes = new List<TraktEpisode>() { episode }
      };

      return RemoveEpisodesFromWatchedHistory(episodes);
    }

    #endregion

    #region Ratings

    public static TraktSyncResponse AddMoviesToRatings(TraktSyncMoviesRated movies)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsAdd, movies.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveMoviesFromRatings(TraktSyncMovies movies)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsRemove, movies.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToRatings(TraktSyncShowsRated shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveShowsFromRatings(TraktSyncShows shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsRemove, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddEpisodesToRatings(TraktSyncEpisodesRated episodes)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsAdd, episodes.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToRatingsEx(TraktSyncShowsRatedEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddSeasonsToRatingsEx(TraktSyncSeasonsRatedEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveEpisodesFromRatings(TraktSyncEpisodes episodes)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsRemove, episodes.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveShowsFromRatingsEx(TraktSyncShowsRatedEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsRemove, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveSeasonsFromRatingsEx(TraktSyncSeasonsRatedEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncRatingsRemove, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    #endregion

    #region Ratings (Single)

    /// <summary>
    /// Rate a single episode on trakt.tv
    /// </summary>
    public static TraktSyncResponse AddEpisodeToRatings(TraktSyncEpisodeRated episode)
    {
      var episodes = new TraktSyncEpisodesRated
      {
        Episodes = new List<TraktSyncEpisodeRated>() { episode }
      };

      return AddEpisodesToRatings(episodes);
    }

    /// <summary>
    /// UnRate a single episode on trakt.tv
    /// </summary>
    public static TraktSyncResponse RemoveEpisodeFromRatings(TraktEpisode episode)
    {
      var episodes = new TraktSyncEpisodes
      {
        Episodes = new List<TraktEpisode>() { new TraktEpisode { Ids = episode.Ids } }
      };

      return RemoveEpisodesFromRatings(episodes);
    }

    /// <summary>
    /// Rate a single episode on trakt.tv (with show info)
    /// </summary>
    public static TraktSyncResponse AddEpisodeToRatingsEx(TraktSyncShowRatedEx item)
    {
      var episodes = new TraktSyncShowsRatedEx
      {
        Shows = new List<TraktSyncShowRatedEx>() { item }
      };

      return AddShowsToRatingsEx(episodes);
    }

    /// <summary>
    /// UnRate a single episode on trakt.tv (with show info)
    /// </summary>
    public static TraktSyncResponse RemoveEpisodeFromRatingsEx(TraktSyncShowRatedEx item)
    {
      var episodes = new TraktSyncShowsRatedEx
      {
        Shows = new List<TraktSyncShowRatedEx>() { item }
      };

      return RemoveShowsFromRatingsEx(episodes);
    }

    /// <summary>
    /// Rate a single season on trakt.tv (with show info)
    /// </summary>
    public static TraktSyncResponse AddSeasonToRatingsEx(TraktSyncSeasonRatedEx item)
    {
      var seasons = new TraktSyncSeasonsRatedEx
      {
        Shows = new List<TraktSyncSeasonRatedEx>() { item }
      };

      return AddSeasonsToRatingsEx(seasons);
    }

    /// <summary>
    /// UnRate a single season on trakt.tv (with show info)
    /// </summary>
    public static TraktSyncResponse RemoveSeasonFromRatingsEx(TraktSyncSeasonRatedEx item)
    {
      var seasons = new TraktSyncSeasonsRatedEx
      {
        Shows = new List<TraktSyncSeasonRatedEx>() { item }
      };

      return RemoveSeasonsFromRatingsEx(seasons);
    }

    /// <summary>
    /// Rate a single show on trakt.tv
    /// </summary>
    public static TraktSyncResponse AddShowToRatings(TraktSyncShowRated show)
    {
      var shows = new TraktSyncShowsRated
      {
        Shows = new List<TraktSyncShowRated>() { show }
      };

      return AddShowsToRatings(shows);
    }

    /// <summary>
    /// UnRate a single show on trakt.tv
    /// </summary>
    public static TraktSyncResponse RemoveShowFromRatings(TraktShow show)
    {
      var shows = new TraktSyncShows
      {
        Shows = new List<TraktShow>() { new TraktShow { Ids = show.Ids } }
      };

      return RemoveShowsFromRatings(shows);
    }

    /// <summary>
    /// Rate a single movie on trakt.tv
    /// </summary>
    public static TraktSyncResponse AddMovieToRatings(TraktSyncMovieRated movie)
    {
      var movies = new TraktSyncMoviesRated
      {
        Movies = new List<TraktSyncMovieRated>() { movie }
      };

      return AddMoviesToRatings(movies);
    }

    /// <summary>
    /// UnRate a single movie on trakt.tv
    /// </summary>
    public static TraktSyncResponse RemoveMovieFromRatings(TraktMovie movie)
    {
      var movies = new TraktSyncMovies
      {
        Movies = new List<TraktMovie>() { new TraktMovie { Ids = movie.Ids } }
      };

      return RemoveMoviesFromRatings(movies);
    }

    #endregion

    #region Community Ratings

    public static TraktRating GetShowRatings(string id)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.ShowRatings, id));
      return response.FromJSON<TraktRating>();
    }

    public static TraktRating GetSeasonRatings(string id, int season)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.ShowRatings, id, season));
      return response.FromJSON<TraktRating>();
    }

    public static TraktRating GetEpisodeRatings(string id, int season, int episode)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.EpisodeRatings, id, season, episode));
      return response.FromJSON<TraktRating>();
    }

    #endregion

    #region Scrobble

    public static TraktScrobbleResponse StartMovieScrobble(TraktScrobbleMovie movie)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.ScrobbleStart, movie.ToJSON());
      return response.FromJSON<TraktScrobbleResponse>();
    }

    public static TraktScrobbleResponse StartEpisodeScrobble(TraktScrobbleEpisode episode)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.ScrobbleStart, episode.ToJSON());
      return response.FromJSON<TraktScrobbleResponse>();
    }

    public static TraktScrobbleResponse PauseMovieScrobble(TraktScrobbleMovie movie)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.ScrobblePause, movie.ToJSON());
      return response.FromJSON<TraktScrobbleResponse>();
    }

    public static TraktScrobbleResponse PauseEpisodeScrobble(TraktScrobbleEpisode episode)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.ScrobblePause, episode.ToJSON());
      return response.FromJSON<TraktScrobbleResponse>();
    }

    public static TraktScrobbleResponse StopMovieScrobble(TraktScrobbleMovie movie)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.ScrobbleStop, movie.ToJSON());
      return response.FromJSON<TraktScrobbleResponse>();
    }

    public static TraktScrobbleResponse StopEpisodeScrobble(TraktScrobbleEpisode episode)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.ScrobbleStop, episode.ToJSON());
      return response.FromJSON<TraktScrobbleResponse>();
    }

    #endregion

    #region Watchlist

    public static TraktSyncResponse AddMoviesToWatchlist(TraktSyncMovies movies)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistAdd, movies.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveMoviesFromWatchlist(TraktSyncMovies movies)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistRemove, movies.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToWatchlist(TraktSyncShows shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddShowsToWatchlistEx(TraktSyncShowsEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveShowsFromWatchlist(TraktSyncShows shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistRemove, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveShowsFromWatchlistEx(TraktSyncShowsEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistRemove, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddSeasonsToWatchlist(TraktSyncSeasonsEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistAdd, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveSeasonsFromWatchlist(TraktSyncSeasonsEx shows)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistRemove, shows.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse AddEpisodesToWatchlist(TraktSyncEpisodes episodes)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistAdd, episodes.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    public static TraktSyncResponse RemoveEpisodesFromWatchlist(TraktSyncEpisodes episodes)
    {
      var response = TraktWeb.PostToTrakt(TraktURIs.SyncWatchlistRemove, episodes.ToJSON());
      return response.FromJSON<TraktSyncResponse>();
    }

    #endregion

    #region Watchlist (Single)

    public static TraktSyncResponse AddMovieToWatchlist(TraktMovie movie)
    {
      var movies = new TraktSyncMovies
      {
        Movies = new List<TraktMovie>() { movie }
      };

      return AddMoviesToWatchlist(movies);
    }

    public static TraktSyncResponse RemoveMovieFromWatchlist(TraktMovie movie)
    {
      var movies = new TraktSyncMovies
      {
        Movies = new List<TraktMovie>() { movie }
      };

      return RemoveMoviesFromWatchlist(movies);
    }

    public static TraktSyncResponse AddShowToWatchlist(TraktShow show)
    {
      var shows = new TraktSyncShows
      {
        Shows = new List<TraktShow>() { show }
      };

      return AddShowsToWatchlist(shows);
    }

    public static TraktSyncResponse AddShowToWatchlistEx(TraktSyncShowEx show)
    {
      var shows = new TraktSyncShowsEx
      {
        Shows = new List<TraktSyncShowEx>() { show }
      };

      return AddShowsToWatchlistEx(shows);
    }

    public static TraktSyncResponse AddSeasonToWatchlist(TraktSyncSeasonEx show)
    {
      var shows = new TraktSyncSeasonsEx
      {
        Shows = new List<TraktSyncSeasonEx>() { show }
      };

      return AddSeasonsToWatchlist(shows);
    }

    public static TraktSyncResponse RemoveSeasonFromWatchlist(TraktSyncSeasonEx show)
    {
      var shows = new TraktSyncSeasonsEx
      {
        Shows = new List<TraktSyncSeasonEx>() { show }
      };

      return RemoveSeasonsFromWatchlist(shows);
    }

    public static TraktSyncResponse RemoveShowFromWatchlist(TraktShow show)
    {
      var shows = new TraktSyncShows
      {
        Shows = new List<TraktShow>() { show }
      };

      return RemoveShowsFromWatchlist(shows);
    }

    public static TraktSyncResponse RemoveShowFromWatchlistEx(TraktSyncShowEx show)
    {
      var shows = new TraktSyncShowsEx
      {
        Shows = new List<TraktSyncShowEx>() { show }
      };

      return RemoveShowsFromWatchlistEx(shows);
    }

    public static TraktSyncResponse AddEpisodeToWatchlist(TraktEpisode episode)
    {
      var episodes = new TraktSyncEpisodes
      {
        Episodes = new List<TraktEpisode>() { episode }
      };

      return AddEpisodesToWatchlist(episodes);
    }

    public static TraktSyncResponse RemoveEpisodeFromWatchlist(TraktEpisode episode)
    {
      var episodes = new TraktSyncEpisodes
      {
        Episodes = new List<TraktEpisode>() { episode }
      };

      return RemoveEpisodesFromWatchlist(episodes);
    }

    #endregion

    #region Comments

    public static bool LikeComment(int id)
    {
      var response = TraktWeb.PostToTrakt(string.Format(TraktURIs.CommentLike, id), null);
      return response != null;
    }

    public static bool UnLikeComment(int id)
    {
      return DeleteFromTrakt(string.Format(TraktURIs.CommentLike, id));
    }

    public static IEnumerable<TraktComment> GetCommentReplies(string id)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.CommentReplies, id));
      return response.FromJSONArray<TraktComment>();
    }

    #endregion

    #region People

    public static TraktPersonSummary GetPersonSummary(string person)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.PersonSummary, person));
      return response.FromJSON<TraktPersonSummary>();
    }

    public static TraktPersonMovieCredits GetMovieCreditsForPerson(string person)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.PersonMovieCredits, person));
      return response.FromJSON<TraktPersonMovieCredits>();
    }

    public static TraktPersonShowCredits GetShowCreditsForPerson(string person)
    {
      var response = TraktWeb.GetFromTrakt(string.Format(TraktURIs.PersonShowCredits, person));
      return response.FromJSON<TraktPersonShowCredits>();
    }

    #endregion

    #region Web Helpersp

    private static bool DeleteFromTrakt(string address)
    {
      var response = TraktWeb.GetFromTrakt(address, false, "DELETE");
      return response != null;
    }

    #endregion

    #endregion
  }
}
