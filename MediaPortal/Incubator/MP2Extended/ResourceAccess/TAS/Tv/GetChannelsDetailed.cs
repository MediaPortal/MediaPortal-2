using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetChannelsDetailed : BaseChannelDetailed, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string groupId = httpParam["groupId"].Value;
     

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelsDetailed: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
        

      IList<IChannelGroup> channelGroups = new List<IChannelGroup>();
      if (groupId == null)
        channelAndGroupInfo.GetChannelGroups(out channelGroups);
      else
      {
        int channelGroupIdInt;
        if (!int.TryParse(groupId, out channelGroupIdInt))
          throw new BadRequestException(string.Format("GetChannelsDetailed: Couldn't convert groupId to int: {0}", groupId));
        channelGroups.Add(new ChannelGroup() { ChannelGroupId = channelGroupIdInt, MediaType = MediaType.TV });
      }

      List<WebChannelDetailed> output = new List<WebChannelDetailed>();

      foreach (var group in channelGroups.Where(x => x.MediaType == MediaType.TV))
      {
        // get channel for goup
        IList<IChannel> channels = new List<IChannel>();
        if (!channelAndGroupInfo.GetChannels(group, out channels))
          continue;

        output.AddRange(channels.Where(x => x.MediaType == MediaType.TV).Select(channel => ChannelDetailed(channel)));
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

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}