///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "channels", Namespace = "")]
  public class FavoritesRequest
  {
  }

  [CollectionDataContract(Name = "channels", ItemName = "channel", Namespace = "")]
  public class FavoriteChannels : List<string>
  {
  }

  [DataContract(Name = "favorite", Namespace = "")]
  public class Favorite
  {
    [DataMember(Name = "id", EmitDefaultValue = false, Order = 1)]
    public Guid Id { get; set; }

    [DataMember(Name = "name", EmitDefaultValue = false, Order = 2)]
    public string Name { get; set; }

    // TODO: Enum
    [DataMember(Name = "flags", EmitDefaultValue = false, Order = 3)]
    public int Flags { get; set; }

    [DataMember(Name = "channels", EmitDefaultValue = false, Order = 4)]
    public FavoriteChannels Channels { get; set; }
  }

  [CollectionDataContract(Name = "favorites", Namespace = "")]
  public class Favorites : List<Favorite>
  {
  }
}
