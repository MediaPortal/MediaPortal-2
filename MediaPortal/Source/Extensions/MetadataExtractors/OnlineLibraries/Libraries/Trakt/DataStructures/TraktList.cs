using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktList : TraktListSlug
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "privacy")]
    public string Privacy { get; set; }

    [DataMember(Name = "show_numbers")]
    public bool ShowNumbers { get; set; }

    [DataMember(Name = "allow_shouts")]
    public bool AllowShouts { get; set; }

    [DataMember(Name = "items")]
    public List<TraktListItem> Items { get; set; }
  }
}
