using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb.Data
{
  [DataContract]
  public class ImageData : ApiData
  {
    [DataMember(Name = "base_url")]
    public ImageBaseUrl BaseUrl { get; set; }

    [DataMember(Name = "images")]
    public Dictionary<string, Image[]> Images { get; set; }
  }
}
