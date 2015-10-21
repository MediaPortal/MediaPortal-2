using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses
{
  class BaseChannelGroup
  {
    internal WebChannelGroup ChannelGroup(IChannelGroup group)
    {
      WebChannelGroup webChannelGroup = new WebChannelGroup
      {
        GroupName = @group.Name,
        Id = @group.ChannelGroupId,
        IsRadio = @group.MediaType == MediaType.Radio,
        IsTv = @group.MediaType == MediaType.TV,
        SortOrder = @group.SortOrder,
        IsChanged = true,
      };
      //webChannelGroup.IsChanged;

      return webChannelGroup;
    }
  }
}
