using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt
{
  public class TraktSettings
  {
    private readonly Object _lockObject = new object();

    #region Settings
    int SettingsVersion = 1;

    public List<TraktAuthentication> UserLogins { get; set; }
    public bool KeepTraktLibraryClean { get; set; }
    public List<String> BlockedFilenames { get; set; }
    public List<String> BlockedFolders { get; set; }
    public SyncMovieCheck SkippedMovies { get; set; }
    public SyncMovieCheck AlreadyExistMovies { get; set; }
    public static int LogLevel { get; set; }
    public int SyncTimerLength { get; set; }
    public int SyncStartDelay { get; set; }
    public int TrendingMoviesDefaultLayout { get; set; }
    public int TrendingShowsDefaultLayout { get; set; }
    public int ShowSeasonsDefaultLayout { get; set; }
    public int SeasonEpisodesDefaultLayout { get; set; }
    public int RecommendedMoviesDefaultLayout { get; set; }
    public int RecommendedShowsDefaultLayout { get; set; }
    public int WatchListMoviesDefaultLayout { get; set; }
    public int WatchListShowsDefaultLayout { get; set; }
    public int WatchListEpisodesDefaultLayout { get; set; }
    public int ListsDefaultLayout { get; set; }
    public int ListItemsDefaultLayout { get; set; }
    public int RelatedMoviesDefaultLayout { get; set; }
    public int RelatedShowsDefaultLayout { get; set; }
    public int SearchMoviesDefaultLayout { get; set; }
    public int SearchShowsDefaultLayout { get; set; }
    public int SearchEpisodesDefaultLayout { get; set; }
    public int SearchPeopleDefaultLayout { get; set; }
    public int SearchUsersDefaultLayout { get; set; }
    public int DefaultCalendarView { get; set; }
    public int DefaultCalendarStartDate { get; set; }
    public bool DownloadFullSizeFanart { get; set; }
    public bool DownloadFanart { get; set; }
    public int WebRequestCacheMinutes { get; set; }
    public bool GetFollowerRequestsOnStartup { get; set; }
    public int MovingPicturesCategoryId { get; set; }
    public bool MovingPicturesCategories { get; set; }
    public int MovingPicturesFiltersId { get; set; }
    public bool MovingPicturesFilters { get; set; }
    public bool CalendarHideTVShowsInWatchList { get; set; }
    public bool HideWatchedRelatedMovies { get; set; }
    public bool HideWatchedRelatedShows { get; set; }
    public int WebRequestTimeout { get; set; }
    public bool HideSpoilersOnShouts { get; set; }
    public bool SyncRatings { get; set; }
    public bool ShowRateDialogOnWatched { get; set; }
    public bool ShowCommunityActivity { get; set; }
    public bool IncludeMeInFriendsActivity { get; set; }
    public TraktActivity LastActivityLoad { get; set; }
    public IEnumerable<TraktTrendingMovie> LastTrendingMovies { get; set; }
    public IEnumerable<TraktTrendingShow> LastTrendingShows { get; set; }
    public int DashboardActivityPollInterval { get; set; }
    public int DashboardTrendingPollInterval { get; set; }
    public int DashboardLoadDelay { get; set; }
    public TraktUserProfile.Statistics LastStatistics { get; set; }
    public bool DashboardMovieTrendingActive { get; set; }
    public string MovieRecommendationGenre { get; set; }
    public bool MovieRecommendationHideCollected { get; set; }
    public bool MovieRecommendationHideWatchlisted { get; set; }
    public int MovieRecommendationStartYear { get; set; }
    public int MovieRecommendationEndYear { get; set; }
    public string ShowRecommendationGenre { get; set; }
    public bool ShowRecommendationHideCollected { get; set; }
    public bool ShowRecommendationHideWatchlisted { get; set; }
    public int ShowRecommendationStartYear { get; set; }
    public int ShowRecommendationEndYear { get; set; }
    //public SortBy SortByTrendingMovies { get; set; }
    //public SortBy SortByRecommendedMovies { get; set; }
    //public SortBy SortByWatchListMovies { get; set; }
    //public SortBy SortByTrendingShows { get; set; }
    //public SortBy SortByRecommendedShows { get; set; }
    //public SortBy SortByWatchListShows { get; set; }
    public bool EnableJumpToForTVShows { get; set; }
    public bool MyFilmsCategories { get; set; }
    public bool SortSeasonsAscending { get; set; }
    public bool RememberLastSelectedActivity { get; set; }
    public int MovPicsRatingDlgDelay { get; set; }
    public bool ShowRateDlgForPlaylists { get; set; }
    public string DefaultTVShowTrailerSite { get; set; }
    public string DefaultMovieTrailerSite { get; set; }
    public bool TrendingMoviesHideWatched { get; set; }
    public bool TrendingMoviesHideWatchlisted { get; set; }
    public bool TrendingMoviesHideCollected { get; set; }
    public bool TrendingMoviesHideRated { get; set; }
    public bool TrendingShowsHideWatched { get; set; }
    public bool TrendingShowsHideWatchlisted { get; set; }
    public bool TrendingShowsHideCollected { get; set; }
    public bool TrendingShowsHideRated { get; set; }
    public List<string> ShowsInCollection { get; set; }
    public int DefaultNetworkView { get; set; }
    public int RecentWatchedMoviesDefaultLayout { get; set; }
    public int RecentWatchedEpisodesDefaultLayout { get; set; }
    public int RecentAddedMoviesDefaultLayout { get; set; }
    public int RecentAddedEpisodesDefaultLayout { get; set; }
    public bool SyncLibrary { get; set; }
    public int SearchTypes { get; set; }
    public bool ShowSearchResultsBreakdown { get; set; }
    public int MaxSearchResults { get; set; }
    public bool FilterTrendingOnDashboard { get; set; }
    public bool UseTrailersPlugin { get; set; }
    public bool IgnoreWatchedPercentOnDVD { get; set; }
    #endregion

    #region Constants

    //private string cLastActivityFileCache = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\Dashboard\Activity.json");
    //private string cLastTrendingMovieFileCache = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\Dashboard\TrendingMovies.json");
    //private string cLastTrendingShowFileCache = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\Dashboard\TrendingShows.json");
    //private string cLastStatisticsFileCache = Path.Combine(Config.GetFolder(Config.Dir.Config), @"Trakt\Dashboard\UserStatistics.json");

    #endregion

    #region Properties

    public static string Username
    {
      get
      {
        return _username;
      }
      set
      {
        _username = value;
        TraktAPI.Username = _username;
      }
    }

    static string _username = null;

    public static string Password
    {
      get
      {
        return _password;
      }
      set
      {
        _password = value;
        TraktAPI.Password = _password;
      }
    }

    static string _password = null;

    /// <summary>
    /// Show Advanced or Simple Ratings Dialog
    /// Settings is Synced from Server
    /// </summary>
    public bool ShowAdvancedRatingsDialog
    {
      get
      {
        return _showAdvancedRatingsDialogs;
      }
      set
      {
        // allow last saved setting to be available immediately
        _showAdvancedRatingsDialogs = value;

        // sync setting - delay on startup
        Thread syncSetting = new Thread(o =>
        {
          if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            return;

          Thread.Sleep(5000);
          TraktLogger.Info("Loading Online Settings");

          TraktAccountSettings settings = TraktAPI.GetAccountSettings();
          if (settings != null && settings.Status == "success")
          {
            _showAdvancedRatingsDialogs = settings.ViewingSettings.RatingSettings.Mode == "advanced";
          }
          else
          {
            TraktLogger.Error("Failed to retrieve trakt settings online.");
          }
        })
        {
          IsBackground = true,
          Name = "Settings"
        };
        syncSetting.Start();
      }
    }
    bool _showAdvancedRatingsDialogs;

    /// <summary>
    /// Version of Plugin
    /// </summary>
    public static string Version
    {
      get
      {
        return Assembly.GetCallingAssembly().GetName().Version.ToString();
      }
    }

    /// <summary>
    /// UserAgent used for Web Requests
    /// </summary>
    public string UserAgent
    {
      get
      {
        return string.Format("TraktForMediaPortal/{0}", Version);
      }
    }

    /// <summary>
    /// The current connection status to trakt.tv
    /// </summary>
    public ConnectionState AccountStatus
    {
      get
      {
        lock (_lockObject)
        {
          if (_accountStatus == ConnectionState.Pending)
          {
            // update state, to inform we are connecting now
            _accountStatus = ConnectionState.Connecting;

            TraktLogger.Info("Signing into trakt.tv");

            if (string.IsNullOrEmpty(TraktSettings.Username) || string.IsNullOrEmpty(TraktSettings.Password))
            {
              TraktLogger.Info("Username and/or Password is empty in settings!");
              return ConnectionState.Disconnected;
            }

            // test connection
            TraktAccount account = new TraktAccount
            {
              Username = Username,
              Password = Password
            };

            TraktResponse response = TraktAPI.TestAccount(account);
            if (response != null && response.Status == "success")
            {
              TraktLogger.Info("User {0} signed into trakt.", Username);
              _accountStatus = ConnectionState.Connected;

              if (!UserLogins.Exists(u => u.Username == Username))
              {
                UserLogins.Add(new TraktAuthentication { Username = Username, Password = Password });
              }
            }
            else
            {
              TraktLogger.Info("Username and/or Password is Invalid!");
              _accountStatus = ConnectionState.Invalid;
            }
          }
        }
        return _accountStatus;
      }
      set
      {
        lock (_lockObject)
        {
          _accountStatus = value;
        }
      }
    }
    ConnectionState _accountStatus = ConnectionState.Pending;

    #endregion

    #region Methods
    /// <summary>
    /// Loads the Settings
    /// </summary>
    internal void LoadSettings()
    {
      TraktLogger.Info("Loading Local Settings");

      // initialise API settings
      TraktAPI.UserAgent = UserAgent;

      //using (Settings xmlreader = new MPSettings())
      //{
      //    Username = xmlreader.GetValueAsString(cTrakt, cUsername, "");
      //    Password = xmlreader.GetValueAsString(cTrakt, cPassword, "");
      //    UserLogins = xmlreader.GetValueAsString(cTrakt, cUserLogins, "").FromJSONArray<TraktAuthentication>().ToList();
      //    MovingPictures = xmlreader.GetValueAsInt(cTrakt, cMovingPictures, -1);
      //    TVSeries = xmlreader.GetValueAsInt(cTrakt, cTVSeries, -1);
      //    MyVideos = xmlreader.GetValueAsInt(cTrakt, cMyVideos, -1);
      //    MyFilms = xmlreader.GetValueAsInt(cTrakt, cMyFilms, -1);
      //    OnlineVideos = xmlreader.GetValueAsInt(cTrakt, cOnlineVideos, -1);
      //    MyAnime = xmlreader.GetValueAsInt(cTrakt, cMyAnime, -1);
      //    MyTVRecordings = xmlreader.GetValueAsInt(cTrakt, cMyTVRecordings, -1);
      //    MyTVLive = xmlreader.GetValueAsInt(cTrakt, cMyTVLive, -1);
      //    ForTheRecordRecordings = xmlreader.GetValueAsInt(cTrakt, cForTheRecordRecordings, -1);
      //    ForTheRecordTVLive = xmlreader.GetValueAsInt(cTrakt, cForTheRecordTVLive, -1);
      //    ArgusRecordings = xmlreader.GetValueAsInt(cTrakt, cArgusRecordings, -1);
      //    ArgusTVLive = xmlreader.GetValueAsInt(cTrakt, cArgusTVLive, -1);
      //    KeepTraktLibraryClean = xmlreader.GetValueAsBool(cTrakt, cKeepTraktLibraryClean, false);
      //    BlockedFilenames = xmlreader.GetValueAsString(cTrakt, cBlockedFilenames, "").FromJSONArray<string>().ToList();
      //    BlockedFolders = xmlreader.GetValueAsString(cTrakt, cBlockedFolders, "").FromJSONArray<string>().ToList();
      //    SkippedMovies = xmlreader.GetValueAsString(cTrakt, cSkippedMovies, "{}").FromJSON<SyncMovieCheck>();
      //    AlreadyExistMovies = xmlreader.GetValueAsString(cTrakt, cAlreadyExistMovies, "{}").FromJSON<SyncMovieCheck>();
      //    LogLevel = xmlreader.GetValueAsInt("general", "loglevel", 1);
      //    SyncTimerLength = xmlreader.GetValueAsInt(cTrakt, cSyncTimerLength, 86400000);
      //    SyncStartDelay = xmlreader.GetValueAsInt(cTrakt, cSyncStartDelay, 0);
      //    TrendingMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cTrendingMoviesDefaultLayout, 0);
      //    TrendingShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cTrendingShowsDefaultLayout, 0);
      //    RecommendedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecommendedMoviesDefaultLayout, 0);
      //    RecommendedShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecommendedShowsDefaultLayout, 0);
      //    WatchListMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cWatchListMoviesDefaultLayout, 0);
      //    WatchListShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cWatchListShowsDefaultLayout, 0);
      //    WatchListEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cWatchListEpisodesDefaultLayout, 0);
      //    ListsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cListsDefaultLayout, 0);
      //    ListItemsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cListItemsDefaultLayout, 0);
      //    RelatedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRelatedMoviesDefaultLayout, 0);
      //    RelatedShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRelatedShowsDefaultLayout, 0);
      //    ShowSeasonsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cShowSeasonsDefaultLayout, 0);
      //    SeasonEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSeasonEpisodesDefaultLayout, 0);
      //    DefaultCalendarView = xmlreader.GetValueAsInt(cTrakt, cDefaultCalendarView, 0);
      //    DefaultCalendarStartDate = xmlreader.GetValueAsInt(cTrakt, cDefaultCalendarStartDate, 0);
      //    DownloadFullSizeFanart = xmlreader.GetValueAsBool(cTrakt, cDownloadFullSizeFanart, false);
      //    DownloadFanart = xmlreader.GetValueAsBool(cTrakt, cDownloadFanart, true);
      //    WebRequestCacheMinutes = xmlreader.GetValueAsInt(cTrakt, cWebRequestCacheMinutes, 15);
      //    WebRequestTimeout = xmlreader.GetValueAsInt(cTrakt, cWebRequestTimeout, 30000);
      //    GetFollowerRequestsOnStartup = xmlreader.GetValueAsBool(cTrakt, cGetFollowerRequestsOnStartup, true);
      //    MovingPicturesCategoryId = xmlreader.GetValueAsInt(cTrakt, cMovingPicturesCategoryId, -1);
      //    MovingPicturesCategories = xmlreader.GetValueAsBool(cTrakt, cMovingPicturesCategories, false);
      //    MovingPicturesFiltersId = xmlreader.GetValueAsInt(cTrakt, cMovingPicturesFilterId, -1);
      //    MovingPicturesFilters = xmlreader.GetValueAsBool(cTrakt, cMovingPicturesFilters, false);
      //    CalendarHideTVShowsInWatchList = xmlreader.GetValueAsBool(cTrakt, cCalendarHideTVShowsInWatchList, false);
      //    HideWatchedRelatedMovies = xmlreader.GetValueAsBool(cTrakt, cHideWatchedRelatedMovies, false);
      //    HideWatchedRelatedShows = xmlreader.GetValueAsBool(cTrakt, cHideWatchedRelatedShows, false);
      //    HideSpoilersOnShouts = xmlreader.GetValueAsBool(cTrakt, cHideSpoilersOnShouts, false);
      //    ShowAdvancedRatingsDialog = xmlreader.GetValueAsBool(cTrakt, cShowAdvancedRatingsDialog, false);
      //    SyncRatings = xmlreader.GetValueAsBool(cTrakt, cSyncRatings, false);
      //    ShowRateDialogOnWatched = xmlreader.GetValueAsBool(cTrakt, cShowRateDialogOnWatched, false);
      //    ShowCommunityActivity = xmlreader.GetValueAsBool(cTrakt, cShowCommunityActivity, false);
      //    IncludeMeInFriendsActivity = xmlreader.GetValueAsBool(cTrakt, cIncludeMeInFriendsActivity, false);
      //    DashboardActivityPollInterval = xmlreader.GetValueAsInt(cTrakt, cDashboardActivityPollInterval, 15000);
      //    DashboardTrendingPollInterval = xmlreader.GetValueAsInt(cTrakt, cDashboardTrendingPollInterval, 300000);
      //    DashboardLoadDelay = xmlreader.GetValueAsInt(cTrakt, cDashboardLoadDelay, 500);
      //    DashboardMovieTrendingActive = xmlreader.GetValueAsBool(cTrakt, cDashboardMovieTrendingActive, false);
      //    MovieRecommendationGenre = xmlreader.GetValueAsString(cTrakt, cMovieRecommendationGenre, "All");
      //    MovieRecommendationHideCollected = xmlreader.GetValueAsBool(cTrakt, cMovieRecommendationHideCollected, false);
      //    MovieRecommendationHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cMovieRecommendationHideWatchlisted, false);
      //    MovieRecommendationStartYear = xmlreader.GetValueAsInt(cTrakt, cMovieRecommendationStartYear, 0);
      //    MovieRecommendationEndYear = xmlreader.GetValueAsInt(cTrakt, cMovieRecommendationEndYear, 0);
      //    ShowRecommendationGenre = xmlreader.GetValueAsString(cTrakt, cShowRecommendationGenre, "All");
      //    ShowRecommendationHideCollected = xmlreader.GetValueAsBool(cTrakt, cShowRecommendationHideCollected, false);
      //    ShowRecommendationHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cShowRecommendationHideWatchlisted, false);
      //    ShowRecommendationStartYear = xmlreader.GetValueAsInt(cTrakt, cShowRecommendationStartYear, 0);
      //    ShowRecommendationEndYear = xmlreader.GetValueAsInt(cTrakt, cShowRecommendationEndYear, 0);
      //    SortByRecommendedMovies = xmlreader.GetValueAsString(cTrakt, cSortByRecommendedMovies, "{\"Field\": 0,\"Direction\": 0}").FromJSON<SortBy>();
      //    SortByRecommendedShows = xmlreader.GetValueAsString(cTrakt, cSortByRecommendedShows, "{\"Field\": 0,\"Direction\": 0}").FromJSON<SortBy>();
      //    SortByTrendingMovies = xmlreader.GetValueAsString(cTrakt, cSortByTrendingMovies, "{\"Field\": 5,\"Direction\": 1}").FromJSON<SortBy>();
      //    SortByTrendingShows = xmlreader.GetValueAsString(cTrakt, cSortByTrendingShows, "{\"Field\": 5,\"Direction\": 1}").FromJSON<SortBy>();
      //    SortByWatchListMovies = xmlreader.GetValueAsString(cTrakt, cSortByWatchListMovies, "{\"Field\": 6,\"Direction\": 1}").FromJSON<SortBy>();
      //    SortByWatchListShows = xmlreader.GetValueAsString(cTrakt, cSortByWatchListShows, "{\"Field\": 6,\"Direction\": 1}").FromJSON<SortBy>();
      //    EnableJumpToForTVShows = xmlreader.GetValueAsBool(cTrakt, cEnableJumpToForTVShows, false);
      //    MyFilmsCategories = xmlreader.GetValueAsBool(cTrakt, cMyFilmsCategories, false);
      //    SortSeasonsAscending = xmlreader.GetValueAsBool(cTrakt, cSortSeasonsAscending, false);
      //    RememberLastSelectedActivity = xmlreader.GetValueAsBool(cTrakt, cRememberLastSelectedActivity, true);
      //    MovPicsRatingDlgDelay = xmlreader.GetValueAsInt(cTrakt, cMovPicsRatingDlgDelay, 500);
      //    ShowRateDlgForPlaylists = xmlreader.GetValueAsBool(cTrakt, cShowRateDlgForPlaylists, true);
      //    DefaultTVShowTrailerSite = xmlreader.GetValueAsString(cTrakt, cDefaultTVShowTrailerSite, "YouTube");
      //    DefaultMovieTrailerSite = xmlreader.GetValueAsString(cTrakt, cDefaultMovieTrailerSite, "YouTube");
      //    TrendingMoviesHideWatched = xmlreader.GetValueAsBool(cTrakt, cTrendingMoviesHideWatched, false);
      //    TrendingMoviesHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cTrendingMoviesHideWatchlisted, false);
      //    TrendingMoviesHideCollected = xmlreader.GetValueAsBool(cTrakt, cTrendingMoviesHideCollected, false);
      //    TrendingMoviesHideRated = xmlreader.GetValueAsBool(cTrakt, cTrendingMoviesHideRated, false);
      //    TrendingShowsHideWatched = xmlreader.GetValueAsBool(cTrakt, cTrendingShowsHideWatched, false);
      //    TrendingShowsHideWatchlisted = xmlreader.GetValueAsBool(cTrakt, cTrendingShowsHideWatchlisted, false);
      //    TrendingShowsHideCollected = xmlreader.GetValueAsBool(cTrakt, cTrendingShowsHideCollected, false);
      //    TrendingShowsHideRated = xmlreader.GetValueAsBool(cTrakt, cTrendingShowsHideRated, false);
      //    ShowsInCollection = xmlreader.GetValueAsString(cTrakt, cShowsInCollection, "").FromJSONArray<string>().ToList();
      //    DefaultNetworkView = xmlreader.GetValueAsInt(cTrakt, cDefaultNetworkView, 1);
      //    RecentWatchedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecentWatchedMoviesDefaultLayout, 0);
      //    RecentWatchedEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecentWatchedEpisodesDefaultLayout, 0);
      //    RecentAddedMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecentAddedMoviesDefaultLayout, 0);
      //    RecentAddedEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cRecentAddedEpisodesDefaultLayout, 0);
      //    SyncLibrary = xmlreader.GetValueAsBool(cTrakt, cSyncLibrary, true);
      //    SearchMoviesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchMoviesDefaultLayout, 0);
      //    SearchShowsDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchShowsDefaultLayout, 0);
      //    SearchEpisodesDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchEpisodesDefaultLayout, 0);
      //    SearchPeopleDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchPeopleDefaultLayout, 0);
      //    SearchUsersDefaultLayout = xmlreader.GetValueAsInt(cTrakt, cSearchUsersDefaultLayout, 0);
      //    SearchTypes = xmlreader.GetValueAsInt(cTrakt, cSearchTypes, 1);
      //    ShowSearchResultsBreakdown = xmlreader.GetValueAsBool(cTrakt, cShowSearchResultsBreakdown, true);
      //    MaxSearchResults = xmlreader.GetValueAsInt(cTrakt, cMaxSearchResults, 30);
      //    FilterTrendingOnDashboard = xmlreader.GetValueAsBool(cTrakt, cFilterTrendingOnDashboard, false);
      //    UseTrailersPlugin = xmlreader.GetValueAsBool(cTrakt, cUseTrailersPlugin, false);
      //    IgnoreWatchedPercentOnDVD = xmlreader.GetValueAsBool(cTrakt, cIgnoreWatchedPercentOnDVD, true);
      //}

      TraktLogger.Info("Loading Persisted File Cache");
      //LastActivityLoad = LoadFileCache(cLastActivityFileCache, "{}").FromJSON<TraktActivity>();
      //LastTrendingMovies = LoadFileCache(cLastTrendingMovieFileCache, "{}").FromJSONArray<TraktTrendingMovie>();
      //LastTrendingShows = LoadFileCache(cLastTrendingShowFileCache, "{}").FromJSONArray<TraktTrendingShow>();
      //LastStatistics = LoadFileCache(cLastStatisticsFileCache, null).FromJSON<TraktUserProfile.Statistics>();
    }

    /// <summary>
    /// Saves the Settings
    /// </summary>
    internal void SaveSettings()
    {
      TraktLogger.Info("Saving Settings");
      //using (Settings xmlwriter = new MPSettings())
      //{
      //    xmlwriter.SetValue(cTrakt, cSettingsVersion, SettingsVersion);
      //    xmlwriter.SetValue(cTrakt, cUsername, Username);
      //    xmlwriter.SetValue(cTrakt, cPassword, Password);
      //    xmlwriter.SetValue(cTrakt, cUserLogins, UserLogins.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cMovingPictures, MovingPictures);
      //    xmlwriter.SetValue(cTrakt, cTVSeries, TVSeries);
      //    xmlwriter.SetValue(cTrakt, cMyVideos, MyVideos);
      //    xmlwriter.SetValue(cTrakt, cMyFilms, MyFilms);
      //    xmlwriter.SetValue(cTrakt, cOnlineVideos, OnlineVideos);
      //    xmlwriter.SetValue(cTrakt, cMyAnime, MyAnime);
      //    xmlwriter.SetValue(cTrakt, cMyTVRecordings, MyTVRecordings);
      //    xmlwriter.SetValue(cTrakt, cMyTVLive, MyTVLive);
      //    xmlwriter.SetValue(cTrakt, cForTheRecordRecordings, ForTheRecordRecordings);
      //    xmlwriter.SetValue(cTrakt, cForTheRecordTVLive, ForTheRecordTVLive);
      //    xmlwriter.SetValue(cTrakt, cArgusRecordings, ArgusRecordings);
      //    xmlwriter.SetValue(cTrakt, cArgusTVLive, ArgusTVLive);
      //    xmlwriter.SetValueAsBool(cTrakt, cKeepTraktLibraryClean, KeepTraktLibraryClean);
      //    xmlwriter.SetValue(cTrakt, cBlockedFilenames, BlockedFilenames.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cBlockedFolders, BlockedFolders.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cSkippedMovies, SkippedMovies.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cAlreadyExistMovies, AlreadyExistMovies.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cSyncTimerLength, SyncTimerLength);
      //    xmlwriter.SetValue(cTrakt, cSyncStartDelay, SyncStartDelay);
      //    xmlwriter.SetValue(cTrakt, cTrendingMoviesDefaultLayout, TrendingMoviesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cTrendingShowsDefaultLayout, TrendingShowsDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cRecommendedMoviesDefaultLayout, RecommendedMoviesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cRecommendedShowsDefaultLayout, RecommendedShowsDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cWatchListMoviesDefaultLayout, WatchListMoviesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cWatchListShowsDefaultLayout, WatchListShowsDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cWatchListEpisodesDefaultLayout, WatchListEpisodesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cRelatedMoviesDefaultLayout, RelatedMoviesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cRelatedShowsDefaultLayout, RelatedShowsDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cListsDefaultLayout, ListsDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cListItemsDefaultLayout, ListItemsDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cShowSeasonsDefaultLayout, ShowSeasonsDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cSeasonEpisodesDefaultLayout, SeasonEpisodesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cDefaultCalendarView, DefaultCalendarView);
      //    xmlwriter.SetValue(cTrakt, cDefaultCalendarStartDate, DefaultCalendarStartDate);
      //    xmlwriter.SetValueAsBool(cTrakt, cDownloadFullSizeFanart, DownloadFullSizeFanart);
      //    xmlwriter.SetValueAsBool(cTrakt, cDownloadFanart, DownloadFanart);
      //    xmlwriter.SetValue(cTrakt, cWebRequestCacheMinutes, WebRequestCacheMinutes);
      //    xmlwriter.SetValue(cTrakt, cWebRequestTimeout, WebRequestTimeout);
      //    xmlwriter.SetValueAsBool(cTrakt, cGetFollowerRequestsOnStartup, GetFollowerRequestsOnStartup);
      //    xmlwriter.SetValue(cTrakt, cMovingPicturesCategoryId, MovingPicturesCategoryId);
      //    xmlwriter.SetValueAsBool(cTrakt, cMovingPicturesCategories, MovingPicturesCategories);
      //    xmlwriter.SetValue(cTrakt, cMovingPicturesFilterId, MovingPicturesFiltersId);
      //    xmlwriter.SetValueAsBool(cTrakt, cMovingPicturesFilters, MovingPicturesFilters);
      //    xmlwriter.SetValueAsBool(cTrakt, cCalendarHideTVShowsInWatchList, CalendarHideTVShowsInWatchList);
      //    xmlwriter.SetValueAsBool(cTrakt, cHideWatchedRelatedMovies, HideWatchedRelatedMovies);
      //    xmlwriter.SetValueAsBool(cTrakt, cHideWatchedRelatedShows, HideWatchedRelatedShows);
      //    xmlwriter.SetValueAsBool(cTrakt, cHideSpoilersOnShouts, HideSpoilersOnShouts);
      //    xmlwriter.SetValueAsBool(cTrakt, cShowAdvancedRatingsDialog, ShowAdvancedRatingsDialog);
      //    xmlwriter.SetValueAsBool(cTrakt, cSyncRatings, SyncRatings);
      //    xmlwriter.SetValueAsBool(cTrakt, cShowRateDialogOnWatched, ShowRateDialogOnWatched);
      //    xmlwriter.SetValueAsBool(cTrakt, cShowCommunityActivity, ShowCommunityActivity);
      //    xmlwriter.SetValueAsBool(cTrakt, cIncludeMeInFriendsActivity, IncludeMeInFriendsActivity);
      //    xmlwriter.SetValue(cTrakt, cDashboardActivityPollInterval, DashboardActivityPollInterval);
      //    xmlwriter.SetValue(cTrakt, cDashboardTrendingPollInterval, DashboardTrendingPollInterval);
      //    xmlwriter.SetValue(cTrakt, cDashboardLoadDelay, DashboardLoadDelay);
      //    xmlwriter.SetValueAsBool(cTrakt, cDashboardMovieTrendingActive, DashboardMovieTrendingActive);
      //    xmlwriter.SetValue(cTrakt, cMovieRecommendationGenre, MovieRecommendationGenre);
      //    xmlwriter.SetValueAsBool(cTrakt, cMovieRecommendationHideCollected, MovieRecommendationHideCollected);
      //    xmlwriter.SetValueAsBool(cTrakt, cMovieRecommendationHideWatchlisted, MovieRecommendationHideWatchlisted);
      //    xmlwriter.SetValue(cTrakt, cMovieRecommendationStartYear, MovieRecommendationStartYear);
      //    xmlwriter.SetValue(cTrakt, cMovieRecommendationEndYear, MovieRecommendationEndYear);
      //    xmlwriter.SetValue(cTrakt, cShowRecommendationGenre, ShowRecommendationGenre);
      //    xmlwriter.SetValueAsBool(cTrakt, cShowRecommendationHideCollected, ShowRecommendationHideCollected);
      //    xmlwriter.SetValueAsBool(cTrakt, cShowRecommendationHideWatchlisted, ShowRecommendationHideWatchlisted);
      //    xmlwriter.SetValue(cTrakt, cShowRecommendationStartYear, ShowRecommendationStartYear);
      //    xmlwriter.SetValue(cTrakt, cShowRecommendationEndYear, ShowRecommendationEndYear);
      //    xmlwriter.SetValue(cTrakt, cSortByRecommendedMovies, SortByRecommendedMovies.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cSortByRecommendedShows, SortByRecommendedShows.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cSortByTrendingMovies, SortByTrendingMovies.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cSortByTrendingShows, SortByTrendingShows.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cSortByWatchListMovies, SortByWatchListMovies.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cSortByWatchListShows, SortByWatchListShows.ToJSON());
      //    xmlwriter.SetValueAsBool(cTrakt, cEnableJumpToForTVShows, EnableJumpToForTVShows);
      //    xmlwriter.SetValueAsBool(cTrakt, cMyFilmsCategories, MyFilmsCategories);
      //    xmlwriter.SetValueAsBool(cTrakt, cSortSeasonsAscending, SortSeasonsAscending);
      //    xmlwriter.SetValueAsBool(cTrakt, cRememberLastSelectedActivity, RememberLastSelectedActivity);
      //    xmlwriter.SetValueAsBool(cTrakt, cShowRateDlgForPlaylists, ShowRateDlgForPlaylists);
      //    xmlwriter.SetValue(cTrakt, cDefaultTVShowTrailerSite, DefaultTVShowTrailerSite);
      //    xmlwriter.SetValue(cTrakt, cDefaultMovieTrailerSite, DefaultMovieTrailerSite);
      //    xmlwriter.SetValueAsBool(cTrakt, cTrendingMoviesHideWatched, TrendingMoviesHideWatched);
      //    xmlwriter.SetValueAsBool(cTrakt, cTrendingMoviesHideWatchlisted, TrendingMoviesHideWatchlisted);
      //    xmlwriter.SetValueAsBool(cTrakt, cTrendingMoviesHideCollected, TrendingMoviesHideCollected);
      //    xmlwriter.SetValueAsBool(cTrakt, cTrendingMoviesHideRated, TrendingMoviesHideRated);
      //    xmlwriter.SetValueAsBool(cTrakt, cTrendingShowsHideWatched, TrendingShowsHideWatched);
      //    xmlwriter.SetValueAsBool(cTrakt, cTrendingShowsHideWatchlisted, TrendingShowsHideWatchlisted);
      //    xmlwriter.SetValueAsBool(cTrakt, cTrendingShowsHideCollected, TrendingShowsHideCollected);
      //    xmlwriter.SetValueAsBool(cTrakt, cTrendingShowsHideRated, TrendingShowsHideRated);
      //    xmlwriter.SetValue(cTrakt, cShowsInCollection, ShowsInCollection.ToJSON());
      //    xmlwriter.SetValue(cTrakt, cDefaultNetworkView, DefaultNetworkView);
      //    xmlwriter.SetValue(cTrakt, cRecentWatchedMoviesDefaultLayout, RecentWatchedMoviesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cRecentWatchedEpisodesDefaultLayout, RecentWatchedEpisodesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cRecentAddedMoviesDefaultLayout, RecentAddedMoviesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cRecentAddedEpisodesDefaultLayout, RecentAddedEpisodesDefaultLayout);
      //    xmlwriter.SetValueAsBool(cTrakt, cSyncLibrary, SyncLibrary);
      //    xmlwriter.SetValue(cTrakt, cSearchMoviesDefaultLayout, SearchMoviesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cSearchShowsDefaultLayout, SearchShowsDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cSearchEpisodesDefaultLayout, SearchEpisodesDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cSearchPeopleDefaultLayout, SearchPeopleDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cSearchUsersDefaultLayout, SearchUsersDefaultLayout);
      //    xmlwriter.SetValue(cTrakt, cSearchTypes, SearchTypes);
      //    xmlwriter.SetValueAsBool(cTrakt, cShowSearchResultsBreakdown, ShowSearchResultsBreakdown);
      //    xmlwriter.SetValue(cTrakt, cMaxSearchResults, MaxSearchResults);
      //    xmlwriter.SetValueAsBool(cTrakt, cFilterTrendingOnDashboard, FilterTrendingOnDashboard);
      //    xmlwriter.SetValueAsBool(cTrakt, cUseTrailersPlugin, UseTrailersPlugin);
      //    xmlwriter.SetValueAsBool(cTrakt, cIgnoreWatchedPercentOnDVD, IgnoreWatchedPercentOnDVD);
      //}

      //Settings.SaveCache();

      TraktLogger.Info("Saving Persistent File Cache");
      //SaveFileCache(cLastActivityFileCache, LastActivityLoad.ToJSON());
      //SaveFileCache(cLastTrendingShowFileCache, (LastTrendingShows ?? "{}".FromJSONArray<TraktTrendingShow>()).ToList().ToJSON());
      //SaveFileCache(cLastTrendingMovieFileCache, (LastTrendingMovies ?? "{}".FromJSONArray<TraktTrendingMovie>()).ToList().ToJSON());
      //SaveFileCache(cLastStatisticsFileCache, LastStatistics.ToJSON());
    }

    /// <summary>
    /// Modify External Plugin Settings
    /// </summary>
    internal void UpdateInternalPluginSettings()
    {
      //// disable internal plugin rate dialogs if we show trakt dialog
      //if (TraktSettings.ShowRateDialogOnWatched)
      //{
      //    if (TraktHelper.IsMovingPicturesAvailableAndEnabled)
      //        TraktHandlers.MovingPictures.UpdateSettingAsBool("auto_prompt_for_rating", false);

      //    if (TraktHelper.IsMPTVSeriesAvailableAndEnabled)
      //        TraktHandlers.TVSeries.UpdateSettingAsBool("askToRate", false);
      //}
    }

    #endregion
  }
}
