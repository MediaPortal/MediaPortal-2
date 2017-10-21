using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
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
  internal class GetAllChannelStatesForGroup
  {
    public dynamic Process(int groupId, string userName)
    {

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetAllChannelStatesForGroup: ITvProvider not found");

      if (userName == String.Empty)
        throw new BadRequestException("GetAllChannelStatesForGroup: userName is null");


      List<WebChannelState> output = new List<WebChannelState>();


      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      IChannelGroup channelGroup = new ChannelGroup { ChannelGroupId = groupId };

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