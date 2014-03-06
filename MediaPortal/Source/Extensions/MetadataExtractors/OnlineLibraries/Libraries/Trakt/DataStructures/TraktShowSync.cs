using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  /// <summary>
  /// Data structure for Syncing to Trakt
  /// </summary>
  [DataContract]
  public class TraktShowSync
  {
    [DataMember(Name = "username")]
    public string UserName { get; set; }

    [DataMember(Name = "password")]
    public string Password { get; set; }

    [DataMember(Name = "shows")]
    public List<Show> Shows { get; set; }

    [DataContract]
    public class Show
    {
      [DataMember(Name = "tvdb_id")]
      public string TVDBID { get; set; }

      [DataMember(Name = "title")]
      public string Title { get; set; }

      [DataMember(Name = "year")]
      public int Year { get; set; }
    }
  }
}
