using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emulators.Common.TheGamesDb.Data
{
  [DataContract]
  public class DeveloperData : ApiData
  {
    [DataMember(Name = "developers")]
    public Dictionary<string, NamedItem> Developers { get; set; }
  }
}
