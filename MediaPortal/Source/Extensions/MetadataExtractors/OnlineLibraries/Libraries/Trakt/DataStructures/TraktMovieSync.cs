using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  /// <summary>
  /// Data structure for Syncing to Trakt
  /// </summary>
  [DataContract]
  public class TraktMovieSync
  {
    [DataMember(Name = "username")]
    public string UserName { get; set; }

    [DataMember(Name = "password")]
    public string Password { get; set; }

    [DataMember(Name = "movies")]
    public List<Movie> MovieList { get; set; }

    [DataContract]
    public class Movie : TraktMovieBase, IEquatable<Movie>
    {
      [DataMember(Name = "last_played")]
      public string LastPlayed { get; set; }

      #region IEquatable
      public bool Equals(Movie other)
      {
        bool result = false;
        if (other != null)
        {
          if (this.Title.Equals(other.Title) && this.Year.Equals(other.Year) && (this.IMDBID ?? string.Empty).Equals(other.IMDBID ?? string.Empty))
          {
            result = true;
          }
        }
        return result;
      }
      #endregion
    }
  }
}
