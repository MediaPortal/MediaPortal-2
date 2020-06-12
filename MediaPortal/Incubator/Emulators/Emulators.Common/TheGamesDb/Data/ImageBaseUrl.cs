using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb.Data
{
  [DataContract]
  public class ImageBaseUrl
  {
    [DataMember(Name = "original")]
    public string Original { get; set; }

    [DataMember(Name = "small")]
    public string Small { get; set; }

    [DataMember(Name = "thumb")]
    public string Thumb { get; set; }

    [DataMember(Name = "cropped_center_thumb")]
    public string CroppedCenterThumb { get; set; }

    [DataMember(Name = "medium")]
    public string Medium { get; set; }

    [DataMember(Name = "large")]
    public string Large { get; set; }
  }
}
