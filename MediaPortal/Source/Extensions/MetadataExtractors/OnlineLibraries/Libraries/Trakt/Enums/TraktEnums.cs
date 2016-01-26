using System;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Enums
{
  /// <summary>
  /// Trakt Connection States
  /// </summary>
  public enum ConnectionState
  {
    Connected,
    Connecting,
    Disconnected,
    Invalid,
    UnAuthorised,
    Pending
  }

  /// <summary>
  /// Media Types for syncing
  /// </summary>
  public enum TraktMediaType
  {
    digital,
    bluray,
    hddvd,
    dvd,
    vcd,
    vhs,
    betamax,
    laserdisc
  }

  /// <summary>
  /// Video resolution for syncing
  /// </summary>
  public enum TraktResolution
  {
    uhd_4k,
    hd_1080p,
    hd_1080i,
    hd_720p,
    sd_576p,
    sd_576i,
    sd_480p,
    sd_480i
  }

  /// <summary>
  /// Audio types for syncing
  /// </summary>
  public enum TraktAudio
  {
    lpcm,
    mp3,
    aac,
    dts,
    dts_ma,
    flac,
    ogg,
    wma,
    dolby_prologic,
    dolby_digital,
    dolby_digital_plus,
    dolby_truehd
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
    ten
  }

  /// <summary>
  /// List of Item Types
  /// </summary>
  public enum TraktItemType
  {
    episode,
    season,
    show,
    movie,
    person
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
    pause,
    created,
    item_added,
    updated,
    like
  }

  /// <summary>
  /// Defaults to all, but you can instead send a comma delimited list of types.
  /// For example, /all or /movie,show or /list.
  /// </summary>
  public enum ActivityType
  {
    all,
    episode,
    season,
    show,
    movie,
    person,
    list,
    comment
  }

  /// <summary>
  /// All possible search types
  /// </summary>
  [Flags]
  public enum SearchType
  {
    none = 0,
    movies = 1,
    shows = 2,
    episodes = 4,
    people = 8,
    users = 16,
    lists = 32
  }

  /// <summary>
  /// Extended info parameter used on GET requests
  /// </summary>
  public enum ExtendedInfo
  {
    min,
    images,
    full
  }
}
