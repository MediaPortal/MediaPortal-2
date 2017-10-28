using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MP2Extended.TAS.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses
{
  class BaseChannelGroup
  {
    internal WebChannelGroup ChannelGroup(IChannelGroup group)
    {
      return new WebChannelGroup
      {
        GroupName = group.Name,
        Id = group.ChannelGroupId,
        IsRadio = group.GetMediaType() == MediaType.Radio,
        IsTv = group.GetMediaType() == MediaType.TV,
        SortOrder = 0,
        IsChanged = false,
      };
    }
  }
}
