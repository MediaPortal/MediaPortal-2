using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  /// <summary>
  /// Authenticated Call will return user collection info
  /// </summary>
  [DataContract]
  public class TraktTrendingShow : TraktShow
  {
    [DataMember(Name = "watchers")]
    public int Watchers { get; set; }
  }
}
