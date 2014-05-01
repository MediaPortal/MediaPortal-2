using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktListSlug
  {
    [DataMember(Name = "username")]
    public string UserName { get; set; }

    [DataMember(Name = "password")]
    public string Password { get; set; }

    /// <summary>
    /// Slug is an unique id for the list
    /// This can be null when creating a new list
    /// </summary>
    [DataMember(Name = "slug")]
    public string Slug { get; set; }
  }
}
