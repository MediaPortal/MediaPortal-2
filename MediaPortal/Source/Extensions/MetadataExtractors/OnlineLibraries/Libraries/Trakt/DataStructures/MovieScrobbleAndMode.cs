namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  /// <summary>
  /// Class to pass scrobbling data and state to background worker
  /// </summary>
  class MovieScrobbleAndMode
  {
    public TraktMovieScrobble MovieScrobble { get; set; }
    public TraktScrobbleStates ScrobbleState { get; set; }
  }
}
