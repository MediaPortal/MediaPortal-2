///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "channels", Namespace = "")]
  public class ChannelsRequest
  {
    [DataMember(Name = "favorite_id", EmitDefaultValue = false)]
    public string FavoriteId { get; set; }
  }

  [DebuggerDisplay("{Number}: {Name} [{Id}, {ChannelType}, {Encrypted}]")]
  [DataContract(Name = "channel", Namespace = "")]
  public class Channel
  {
    [DataMember(Name = "channel_id", EmitDefaultValue = false)]
    public string Id { get; set; }

    [DataMember(Name = "channel_dvblink_id", EmitDefaultValue = false)]
    public string DvbLinkId { get; set; }

    [DataMember(Name = "channel_name", EmitDefaultValue = false)]
    public string Name { get; set; }

    [DataMember(Name = "channel_number", EmitDefaultValue = false)]
    public int Number { get; set; }

    [DataMember(Name = "channel_subnumber", EmitDefaultValue = false)]
    public int SubNumber { get; set; }

    /// <summary>
    /// mandatory, (RD_CHANNEL_TV = 0, RD_CHANNEL_RADIO = 1, RD_CHANNEL_OTHER = 2)
    /// </summary>
    [DataMember(Name = "channel_type", EmitDefaultValue = false)]
    public int ChannelType { get; set; }

    /// <summary>
    /// optional,1 if encrypted, 0 if FTA (default)
    /// </summary>
    [DataMember(Name = "channel_encrypted", EmitDefaultValue = false)]
    public int Encrypted { get; set; }
  }

  [CollectionDataContract(Name = "channels", Namespace = "")]
  public class Channels : List<Channel>
  {
  }
}
