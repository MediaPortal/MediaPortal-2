using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb.Data
{
  [DataContract]
  public class Image
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "side")]
    public string Side { get; set; }

    [DataMember(Name = "filename")]
    public string Filename { get; set; }

    [DataMember(Name = "resolution")]
    public string Resolution { get; set; }
  }
}
