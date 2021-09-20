///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "channels", Namespace = "")]
  public class ChannelsRequest
  {
  }

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
  }

  [CollectionDataContract(Name = "channels", Namespace = "")]
  public class Channels : List<Channel>
  {
  }
}
