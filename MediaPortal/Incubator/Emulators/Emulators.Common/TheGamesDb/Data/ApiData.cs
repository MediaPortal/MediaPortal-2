using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb.Data
{
  [DataContract]
  public class ApiData
  {
    [DataMember(Name = "count")]
    public int Count { get; set; }
  }
}
