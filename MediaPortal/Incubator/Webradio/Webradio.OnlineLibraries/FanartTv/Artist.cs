using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Webradio.OnlineLibraries.FanartTv
{
  [DataContract]
  public class Albumcover
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "likes")]
    public string Likes { get; set; }
  }

  [DataContract]
  public class Cdart
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "likes")]
    public string Likes { get; set; }

    [DataMember(Name = "disc")]
    public string Disc { get; set; }

    [DataMember(Name = "size")]
    public string Size { get; set; }
  }

  public class Albums
  {
        [System.Runtime.Serialization.DataMember(Name = "cdart")]
    public System.Collections.Generic.List<Cdart> Cdart { get; set; }
    
        [System.Runtime.Serialization.DataMember(Name = "albumcover")]
    public System.Collections.Generic.List<Albumcover> Albumcover { get; set; }
  }

  [DataContract]
  public class Artistthumb
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "likes")]
    public string Likes { get; set; }
  }

  [DataContract]
  public class Hdmusiclogo
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "likes")]
    public string Likes { get; set; }
  }

  [DataContract]
  public class Musiclogo
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "likes")]
    public string Likes { get; set; }
  }

  [DataContract]
  public class Artistbackground
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "likes")]
    public string Likes { get; set; }
  }

  [DataContract]
  public class Musicbanner
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "likes")]
    public string Likes { get; set; }
  }

  [DataContract]
  public class Artist
  {

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "mbid_id")]
    public string MbidId { get; set; }

    [DataMember(Name = "albums")]
    public Albums Albums { get; set; }

    [DataMember(Name = "artistthumb")]
    public IList<Artistthumb> Artistthumb { get; set; }

    [DataMember(Name = "hdmusiclogo")]
    public IList<Hdmusiclogo> Hdmusiclogo { get; set; }

    [DataMember(Name = "musiclogo")]
    public IList<Musiclogo> Musiclogo { get; set; }

    [DataMember(Name = "artistbackground")]
    public IList<Artistbackground> Artistbackground { get; set; }

    [DataMember(Name = "musicbanner")]
    public IList<Musicbanner> Musicbanner { get; set; }
  }
}
