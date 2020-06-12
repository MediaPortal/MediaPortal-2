using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emulators.Common.TheGamesDb.Data
{
  [DataContract]
  public class GenreData : ApiData
  {
    [DataMember(Name = "genres")]
    public Dictionary<string, NamedItem> Genres { get; set; }
  }
}
