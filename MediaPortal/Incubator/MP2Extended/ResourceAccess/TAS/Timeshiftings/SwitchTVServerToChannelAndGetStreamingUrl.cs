using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Timeshiftings
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "channelId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "userName", Type = typeof(string), Nullable = false)]
  internal class SwitchTVServerToChannelAndGetStreamingUrl : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string channelId = httpParam["channelId"].Value;
      string userName = httpParam["userName"].Value;
      

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("SwitchTVServerToChannelAndGetStreamingUrl: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;


      int channelIdInt;
      if (!int.TryParse(channelId, out channelIdInt))
        throw new BadRequestException(string.Format("SwitchTVServerToChannelAndGetStreamingUrl: Couldn't convert channelId to int: {0}", channelId));

      if (userName == null)
        throw new BadRequestException("SwitchTVServerToChannelAndGetStreamingUrl: userName is null");

      IChannel channel;
      if (!channelAndGroupInfo.GetChannel(channelIdInt, out channel))
        throw new BadRequestException(string.Format("SwitchTVServerToChannelAndGetStreamingUrl: Couldn't get channel with Id: {0}", channelIdInt));

      ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
      

      MediaItem item;
      if (!timeshiftControl.StartTimeshift(userName, SlotControl.GetSlotIndex(userName), channel, out item))
        throw new BadRequestException("SwitchTVServerToChannelAndGetStreamingUrl: Couldn't start timeshifting");

      string resourcePathStr = (string)item.Aspects[ProviderResourceAspect.ASPECT_ID][ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH];
      var resourcePath = ResourcePath.Deserialize(resourcePathStr);
      INetworkResourceAccessor stra = SlimTvResourceAccessor.GetResourceAccessor(resourcePath.BasePathSegment.Path);


      return new WebStringResult { Result = stra.URL };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}