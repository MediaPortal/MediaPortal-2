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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Channels
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "channelId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "userName", Type = typeof(string), Nullable = false)]
  internal class GetChannelState
  {
    public WebChannelState Process(int channelId, string userName)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelState: ITvProvider not found");

      if (channelId == null)
        throw new BadRequestException("GetChannelState: channelId is null");
      if (userName == null)
        throw new BadRequestException("GetChannelState: userName is null");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;


      IChannel channel;
      if (!channelAndGroupInfo.GetChannel(channelId, out channel))
        throw new BadRequestException(string.Format("GetChannelState: Couldn't get channel with id: {0}", channelId));

      WebChannelState webChannelState = new WebChannelState
      {
        ChannelId = channel.ChannelId,
        State = ChannelState.Tunable // TODO: implement in SlimTv
      };

      return webChannelState;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}