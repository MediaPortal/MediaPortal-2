using System.Collections.Generic;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Channels
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "userName", Type = typeof(string), Nullable = false)]
  internal class GetAllChannelStatesForGroup : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string groupId = httpParam["groupId"].Value;
      string userName = httpParam["userName"].Value;
     

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetAllChannelStatesForGroup: ITvProvider not found");

      if (groupId == null)
        throw new BadRequestException("GetAllChannelStatesForGroup: groupId is null");
      if (userName == null)
        throw new BadRequestException("GetAllChannelStatesForGroup: userName is null");

      int channelGroupIdInt;
      if (!int.TryParse(groupId, out channelGroupIdInt))
        throw new BadRequestException(string.Format("GetAllChannelStatesForGroup: Couldn't convert groupId to int: {0}", groupId));

      List<WebChannelState> output = new List<WebChannelState>();


      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      IChannelGroup channelGroup = new ChannelGroup { ChannelGroupId = channelGroupIdInt };

      IList<IChannel> channels = new List<IChannel>();
      if (!channelAndGroupInfo.GetChannels(channelGroup, out channels))
        throw new BadRequestException(string.Format("GetAllChannelStatesForGroup: Couldn't get channels for group: {0}", groupId));

      foreach (var channel in channels)
      {
        output.Add(new WebChannelState
        {
          ChannelId = channel.ChannelId,
          State = ChannelState.Tunable // TODO: implement in SlimTv
        });
      }
      

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}