using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //{
  //  "images": {
  //      "backdrop_sizes": ["w300", "w780", "w1280", "original"],
  //      "base_url": "http://cf2.imgobject.com/t/p/",
  //      "poster_sizes": ["w92", "w154", "w185", "w342", "w500", "original"],
  //      "profile_sizes": ["w45", "w185", "h632", "original"]
  //  }
  //}
  [DataContract]
  public class Configuration
  {
    [DataMember(Name = "images")]
    public ImageConfiguration Images { get; set; }
  }

  [DataContract]
  public class ImageConfiguration
  {
    [DataMember(Name = "backdrop_sizes")]
    public List<string> BackdropSizes { get; set; }

    [DataMember(Name = "poster_sizes")]
    public List<string> PosterSizes { get; set; }

    [DataMember(Name = "profile_sizes")]
    public List<string> ProfileSizes { get; set; }

    [DataMember(Name = "base_url")]
    public string BaseUrl { get; set; }

    public override string ToString()
    {
      return BaseUrl;
    }
  }
}
