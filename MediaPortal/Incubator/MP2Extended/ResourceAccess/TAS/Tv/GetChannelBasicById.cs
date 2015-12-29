using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "channelId", Type = typeof(int), Nullable = false)]
  internal class GetChannelBasicById : BaseChannelBasic, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string channelId = httpParam["channelId"].Value;
      

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelBasicById: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;


      int channelIdInt;
      if (!int.TryParse(channelId, out channelIdInt))
        throw new BadRequestException(string.Format("GetChannelBasicById: Couldn't convert channelId to int: {0}", channelId));

      IChannel channel;
      if (!channelAndGroupInfo.GetChannel(channelIdInt, out channel))
        throw new BadRequestException(string.Format("GetChannelBasicById: Couldn't get channel with Id: {0}", channelIdInt));


      return ChannelBasic(channel);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}