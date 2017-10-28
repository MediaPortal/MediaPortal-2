using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using System.Collections.Generic;
using System.Linq;

namespace MP2Extended.TAS.Extensions
{
  public static class Extensions
  {
    public static MediaType GetMediaType(this IChannelGroup channelGroup)
    {
      return channelGroup.ChannelGroupId > 0 ? MediaType.TV : MediaType.Radio;
    }

    public static IList<IChannelGroup> GetTvGroups(this IChannelAndGroupInfo channelAndGroupInfo)
    {
      return GetGroups(channelAndGroupInfo, MediaType.TV);
    }

    public static IList<IChannelGroup> GetRadioGroups(this IChannelAndGroupInfo channelAndGroupInfo)
    {
      return GetGroups(channelAndGroupInfo, MediaType.Radio);
    }

    public static IList<IChannel> GetTvChannelsForGroup(this IChannelAndGroupInfo channelAndGroupInfo, int? groupId)
    {
      return GetChannelsForGroup(channelAndGroupInfo, MediaType.TV, groupId);
    }

    public static IList<IChannel> GetRadioChannelsForGroup(this IChannelAndGroupInfo channelAndGroupInfo, int? groupId)
    {
      return GetChannelsForGroup(channelAndGroupInfo, MediaType.Radio, groupId);
    }

    private static IList<IChannelGroup> GetGroups(IChannelAndGroupInfo channelAndGroupInfo, MediaType mediaType)
    {
      return channelAndGroupInfo.GetChannelGroups(out IList<IChannelGroup> groups) ?
        groups.Where(g => g.GetMediaType() == mediaType).ToList() : new List<IChannelGroup>();
    }

    private static IList<IChannel> GetChannelsForGroup(IChannelAndGroupInfo channelAndGroupInfo, MediaType mediaType, int? groupId)
    {
      List<IChannel> result = new List<IChannel>();
      if (groupId.HasValue)
      {
        ChannelGroup channelGroup = new ChannelGroup() { ChannelGroupId = groupId.Value };
        if (channelGroup.GetMediaType() == mediaType && channelAndGroupInfo.GetChannels(channelGroup, out IList<IChannel> channels))
          return channels;
        return result;
      }

      //Get all channels

      IList<IChannelGroup> channelGroups;
      if (!channelAndGroupInfo.GetChannelGroups(out channelGroups))
        return result;

      foreach (IChannelGroup group in channelGroups.Where(g => g.GetMediaType() == mediaType))
        if (channelAndGroupInfo.GetChannels(group, out IList<IChannel> groupChannels))
          result.AddRange(groupChannels);

      return result;
    }
  }
}
