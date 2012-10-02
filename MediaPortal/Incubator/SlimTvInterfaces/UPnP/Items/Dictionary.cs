using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items
{
  [KnownType(typeof(Program))]
  [KnownType(typeof(Channel))]
  [KnownType(typeof(ChannelGroup))]
  public class Dictionary: Dictionary<string, object>
  {
  }
}
