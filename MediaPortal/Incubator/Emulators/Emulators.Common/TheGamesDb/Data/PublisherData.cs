using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emulators.Common.TheGamesDb.Data
{
  [DataContract]
  public class PublisherData : ApiData
  {
    [DataMember(Name = "publishers")]
    public Dictionary<string, NamedItem> Publishers { get; set; }
  }
}
