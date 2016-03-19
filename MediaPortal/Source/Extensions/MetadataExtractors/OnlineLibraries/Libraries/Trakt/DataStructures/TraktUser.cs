using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktUser
  {
    [DataMember(Name = "username")]
    public string Username { get; set; }

    [DataMember(Name = "name")]
    public string FullName { get; set; }

    [DataMember(Name = "vip")]
    public bool IsVip { get; set; }

    [DataMember(Name = "vip_ep")]
    public bool IsVipEP { get; set; }

    [DataMember(Name = "private")]
    public bool IsPrivate { get; set; }
  }
}