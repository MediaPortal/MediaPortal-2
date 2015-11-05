using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.Plugins.MP2Extended.Extensions;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv
{
  internal class GetChannelsDetailedByRange : BaseChannelDetailed, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string groupId = httpParam["groupId"].Value;
      string start = httpParam["start"].Value;
      string end = httpParam["end"].Value;

      if (start == null || end == null)
        throw new BadRequestException("start or end parameter is missing");

      int startInt;
      if (!Int32.TryParse(start, out startInt))
      {
        throw new BadRequestException(String.Format("GetChannelsDetailedByRange: Couldn't convert start to int: {0}", start));
      }

      int endInt;
      if (!Int32.TryParse(end, out endInt))
      {
        throw new BadRequestException(String.Format("GetChannelsDetailedByRange: Couldn't convert end to int: {0}", end));
      }

      List<WebChannelDetailed> output = new List<WebChannelDetailed>();

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelsDetailedByRange: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
        

      IList<IChannelGroup> channelGroups = new List<IChannelGroup>();
      if (groupId == null)
        channelAndGroupInfo.GetChannelGroups(out channelGroups);
      else
      {
        int channelGroupIdInt;
        if (!int.TryParse(groupId, out channelGroupIdInt))
          throw new BadRequestException(string.Format("GetChannelsDetailedByRange: Couldn't convert groupId to int: {0}", groupId));
        channelGroups.Add(new ChannelGroup() { ChannelGroupId = channelGroupIdInt });
      }

      foreach (var group in channelGroups)
      {
        // get channel for goup
        IList<IChannel> channels = new List<IChannel>();
        if (!channelAndGroupInfo.GetChannels(group, out channels))
          continue;

        foreach (var channel in channels)
        {
          WebChannelDetailed webChannelDetailed = ChannelDetailed(channel);

          if (channel.MediaType == MediaType.TV)
            output.Add(webChannelDetailed);
        }
      }

      // sort
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.SortChannelList(webSortField, webSortOrder).ToList();
      }

      output = output.TakeRange(startInt, endInt).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}