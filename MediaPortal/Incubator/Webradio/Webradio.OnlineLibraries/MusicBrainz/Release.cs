using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Webradio.OnlineLibraries.MusicBrainz
{
  [DataContract]
  public class Thumbnails
  {
    [DataMember(Name = "1200")]
    public string x1200 { get; set; }

    [DataMember(Name = "250")]
    public string x250 { get; set; }

    [DataMember(Name = "500")]
    public string x500 { get; set; }

    [DataMember(Name = "large")]
    public string Large { get; set; }

    [DataMember(Name = "small")]
    public string Small { get; set; }
  }

  [DataContract]
  public class Cover
  {
    [DataMember(Name = "approved")]
    public bool Approved { get; set; }

    [DataMember(Name = "back")]
    public bool Back { get; set; }

    [DataMember(Name = "comment")]
    public string Comment { get; set; }

    [DataMember(Name = "edit")]
    public int Edit { get; set; }

    [DataMember(Name = "front")]
    public bool Front { get; set; }

    [DataMember(Name = "id")]
    public long Id { get; set; }

    [DataMember(Name = "image")]
    public string Image { get; set; }

    [DataMember(Name = "thumbnails")]
    public Thumbnails Thumbnails { get; set; }

    [DataMember(Name = "types")]
    public IList<string> Types { get; set; }
  }

  [DataContract]
  public class CaRelease
  {
    [DataMember(Name = "images")]
    public IList<Cover> Images { get; set; }

    [DataMember(Name = "release")]
    public string Release { get; set; }
  }


}
