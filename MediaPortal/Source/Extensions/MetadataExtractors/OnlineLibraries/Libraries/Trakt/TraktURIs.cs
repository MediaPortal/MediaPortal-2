namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt
{
  /// <summary>
  /// List of URIs for the Trakt API
  /// </summary>
  public static class TraktURIs
  {
    public const string ApiKey = "32414037ddd94db084ca56be1a60095f0d7520db"; // MP2 specific
    public const string ScrobbleShow = @"http://api.trakt.tv/show/{0}/" + ApiKey;
    public const string ScrobbleMovie = @"http://api.trakt.tv/movie/{0}/" + ApiKey;
    public const string UserWatchedEpisodes = @"http://api.trakt.tv/user/library/shows/watched.json/" + ApiKey + @"/{0}{1}";
    public const string UserWatchedMovies = @"http://api.trakt.tv/user/watched/movies.json/" + ApiKey + @"/{0}";
    public const string UserProfile = @"http://api.trakt.tv/user/profile.json/" + ApiKey + @"/{0}";
    public const string SeriesOverview = @"http://api.trakt.tv/show/summary.json/" + ApiKey + @"/{0}";
    public const string SeriesOverviewExtended = @"http://api.trakt.tv/show/summary.json/" + ApiKey + @"/{0}/extended";
    public const string UserEpisodesUnSeen = @"http://api.trakt.tv/user/library/shows/unseen.json/" + ApiKey + @"/{0}{1}";
    public const string UserEpisodesCollection = @"http://api.trakt.tv/user/library/shows/collection.json/" + ApiKey + @"/{0}{1}";
    public const string UserMoviesCollection = @"http://api.trakt.tv/user/library/movies/collection.json/" + ApiKey + @"/{0}";
    public const string UserMoviesAll = @"http://api.trakt.tv/user/library/movies/all.json/" + ApiKey + @"/{0}{1}";
    public const string SyncEpisodeLibrary = @"http://api.trakt.tv/show/episode/{0}/" + ApiKey;
    public const string SyncShowWatchList = @"http://api.trakt.tv/show/{0}/" + ApiKey;
    public const string SyncEpisodeWatchList = @"http://api.trakt.tv/show/episode/{0}/" + ApiKey;
    public const string SyncMovieLibrary = @"http://api.trakt.tv/movie/{0}/" + ApiKey;
    public const string SyncMovieWatchList = @"http://api.trakt.tv/movie/watchlist/" + ApiKey;
    public const string MovieOverview = @"http://api.trakt.tv/movie/summary.json/" + ApiKey + @"/{0}";
    public const string UserCalendarShows = @"http://api.trakt.tv/user/calendar/shows.json/" + ApiKey + @"/{0}/{1}/{2}";
    public const string CalendarPremieres = @"http://api.trakt.tv/calendar/premieres.json/" + ApiKey + @"/{0}/{1}";
    public const string CalendarAllShows = @"http://api.trakt.tv/calendar/shows.json/" + ApiKey + @"/{0}/{1}";
    public const string UserFriends = @"http://api.trakt.tv/user/friends.json/" + ApiKey + @"/{0}";
    public const string UserFriendsExtended = @"http://api.trakt.tv/user/friends.json/" + ApiKey + @"/{0}/extended";
    public const string RateItem = @"http://api.trakt.tv/rate/{0}/" + ApiKey;
    public const string TrendingMovies = @"http://api.trakt.tv/movies/trending.json/" + ApiKey;
    public const string TrendingShows = @"http://api.trakt.tv/shows/trending.json/" + ApiKey;
    public const string UserMovieRecommendations = @"http://api.trakt.tv/recommendations/movies/" + ApiKey;
    public const string UserShowsRecommendations = @"http://api.trakt.tv/recommendations/shows/" + ApiKey;
    public const string UserMovieWatchList = @"http://api.trakt.tv/user/watchlist/movies.json/" + ApiKey + @"/{0}";
    public const string UserShowsWatchList = @"http://api.trakt.tv/user/watchlist/shows.json/" + ApiKey + @"/{0}";
    public const string UserEpisodesWatchList = @"http://api.trakt.tv/user/watchlist/episodes.json/" + ApiKey + @"/{0}";
    public const string UserRatedMoviesList = @"http://api.trakt.tv/user/ratings/movies.json/" + ApiKey + @"/{0}";
    public const string UserRatedShowsList = @"http://api.trakt.tv/user/ratings/shows.json/" + ApiKey + @"/{0}";
    public const string UserRatedEpisodesList = @"http://api.trakt.tv/user/ratings/episodes.json/" + ApiKey + @"/{0}";
    public const string CreateAccount = @"http://api.trakt.tv/account/create/" + ApiKey;
    public const string TestAccount = @"http://api.trakt.tv/account/test/" + ApiKey;
    public const string UserEpisodeWatchedHistory = @"http://api.trakt.tv/user/watched/episodes.json/" + ApiKey + @"/{0}";
    public const string UserMovieWatchedHistory = @"http://api.trakt.tv/user/watched/movies.json/" + ApiKey + @"/{0}";
    public const string Friends = @"http://api.trakt.tv/friends/all/" + ApiKey;
    public const string FriendRequests = @"http://api.trakt.tv/friends/requests/" + ApiKey;
    public const string FriendAdd = @"http://api.trakt.tv/friends/add/" + ApiKey;
    public const string FriendApprove = @"http://api.trakt.tv/friends/approve/" + ApiKey;
    public const string FriendDeny = @"http://api.trakt.tv/friends/deny/" + ApiKey;
    public const string FriendDelete = @"http://api.trakt.tv/friends/delete/" + ApiKey;
    public const string SearchUsers = @"http://api.trakt.tv/search/users.json/" + ApiKey + @"?query={0}&limit={1}";
    public const string SearchMovies = @"http://api.trakt.tv/search/movies.json/" + ApiKey + @"?query={0}&limit={1}";
    public const string SearchShows = @"http://api.trakt.tv/search/shows.json/" + ApiKey + @"?query={0}&limit={1}";
    public const string SearchEpisodes = @"http://api.trakt.tv/search/episodes.json/" + ApiKey + @"?query={0}&limit={1}";
    public const string SearchPeople = @"http://api.trakt.tv/search/people.json/" + ApiKey + @"?query={0}&limit={1}";
    public const string MovieShouts = @"http://api.trakt.tv/movie/comments.json/" + ApiKey + @"/{0}";
    public const string ShowShouts = @"http://api.trakt.tv/show/comments.json/" + ApiKey + @"/{0}";
    public const string EpisodeShouts = @"http://api.trakt.tv/show/episode/comments.json/" + ApiKey + @"/{0}/{1}/{2}";
    public const string DismissMovieRecommendation = @"http://api.trakt.tv/recommendations/movies/dismiss/" + ApiKey;
    public const string DismissShowRecommendation = @"http://api.trakt.tv/recommendations/shows/dismiss/" + ApiKey;
    public const string ListAdd = @"http://api.trakt.tv/lists/add/" + ApiKey;
    public const string ListDelete = @"http://api.trakt.tv/lists/delete/" + ApiKey;
    public const string ListItemsAdd = @"http://api.trakt.tv/lists/items/add/" + ApiKey;
    public const string ListItemsDelete = @"http://api.trakt.tv/lists/items/delete/" + ApiKey;
    public const string ListUpdate = @"http://api.trakt.tv/lists/update/" + ApiKey;
    public const string UserList = @"http://api.trakt.tv/user/list.json/" + ApiKey + @"/{0}/{1}";
    public const string UserLists = @"http://api.trakt.tv/user/lists.json/" + ApiKey + @"/{0}";
    public const string RelatedMovies = @"http://api.trakt.tv/movie/related.json/" + ApiKey + @"/{0}{1}";
    public const string RelatedShows = @"http://api.trakt.tv/show/related.json/" + ApiKey + @"/{0}{1}";
    public const string ActivityUser = @"http://api.trakt.tv/activity/user.json/" + ApiKey + @"/{0}/{1}/{2}";
    public const string ActivityFriends = @"http://api.trakt.tv/activity/friends.json/" + ApiKey + @"/{0}/{1}{2}";
    public const string ActivityFriendsMe = @"http://api.trakt.tv/activity/friendsme.json/" + ApiKey + @"/{0}/{1}{2}";
    public const string ActivityCommunity = @"http://api.trakt.tv/activity/community.json/" + ApiKey + @"/{0}/{1}{2}";
    public const string AccountSettings = @"http://api.trakt.tv/account/settings/" + ApiKey;
    public const string RateMovies = @"http://api.trakt.tv/rate/movies/" + ApiKey;
    public const string RateShows = @"http://api.trakt.tv/rate/shows/" + ApiKey;
    public const string RateEpisodes = @"http://api.trakt.tv/rate/episodes/" + ApiKey;
    public const string ShowSeasons = @"http://api.trakt.tv/show/seasons.json/" + ApiKey + @"/{0}";
    public const string SeasonEpisodes = @"http://api.trakt.tv/show/season.json/" + ApiKey + @"/{0}/{1}";
    public const string ShowSeen = @"http://api.trakt.tv/show/seen/" + ApiKey;
    public const string SeasonSeen = @"http://api.trakt.tv/show/season/seen/" + ApiKey;
    public const string ShowLibrary = @"http://api.trakt.tv/show/library/" + ApiKey;
    public const string SeasonLibrary = @"http://api.trakt.tv/show/season/library/" + ApiKey;
    public const string NetworkFriends = @"http://api.trakt.tv/user/network/friends.json/" + ApiKey + "/{0}";
    public const string NetworkFollowers = @"http://api.trakt.tv/user/network/followers.json/" + ApiKey + "/{0}";
    public const string NetworkFollowing = @"http://api.trakt.tv/user/network/following.json/" + ApiKey + "/{0}";
    public const string NetworkRequests = @"http://api.trakt.tv/network/requests/" + ApiKey;
    public const string NetworkFollow = @"http://api.trakt.tv/network/follow/" + ApiKey;
    public const string NetworkUnFollow = @"http://api.trakt.tv/network/unfollow/" + ApiKey;
    public const string NetworkApprove = @"http://api.trakt.tv/network/approve/" + ApiKey;
    public const string NetworkDeny = @"http://api.trakt.tv/network/deny/" + ApiKey;
  }
}
