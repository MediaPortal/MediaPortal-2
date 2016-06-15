using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPersonImages
  {
    [DataMember(Name = "headshot")]
    public TraktImage HeadShot { get; set; }

    [DataMember(Name = "fanart")]
    public TraktImage Fanart { get; set; }
  }
}
