using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktUser
  {
    [DataMember(Name = "username")]
    public string Username { get; set; }

    [DataMember(Name = "protected")]
    public bool Protected { get; set; }

    [DataMember(Name = "full_name")]
    public string FullName { get; set; }

    [DataMember(Name = "gender")]
    public string Gender { get; set; }

    [DataMember(Name = "age")]
    public string Age { get; set; }

    [DataMember(Name = "location")]
    public string Location { get; set; }

    [DataMember(Name = "about")]
    public string About { get; set; }

    [DataMember(Name = "joined")]
    public long JoinDate { get; set; }

    [DataMember(Name = "avatar")]
    public string Avatar { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "vip")]
    public bool VIP { get; set; }
  }
}