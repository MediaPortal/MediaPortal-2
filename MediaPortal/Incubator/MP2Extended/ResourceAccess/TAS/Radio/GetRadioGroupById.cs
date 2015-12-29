using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Radio
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  internal class GetRadioGroupById : BaseChannelGroup, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string groupId = httpParam["groupId"].Value;
      if (groupId == null)
        throw new BadRequestException("GetRadioGroupById: groupId is null");

      int groupIdInt;
      if (!int.TryParse(groupId, out groupIdInt))
        throw new BadRequestException(string.Format("GetRadioGroupById: Couldn't parse groupId to int: {0}", groupId));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetRadioGroupById: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      IList<IChannelGroup> channelGroups = new List<IChannelGroup>();
      channelAndGroupInfo.GetChannelGroups(out channelGroups);

      // select the channel Group we are looking for
      IChannelGroup group = channelGroups.First(x => x.ChannelGroupId == groupIdInt);

      if (group == null)
        throw new BadRequestException(string.Format("GetRadioGroupById: group with id: {0} not found", groupIdInt));

      WebChannelGroup webChannelGroup = ChannelGroup(group);


      return webChannelGroup;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}