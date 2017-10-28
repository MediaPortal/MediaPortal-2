using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MP2Extended.TAS.Extensions;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  internal class GetGroupById : BaseChannelGroup
  {
    public WebChannelGroup Process(int groupId)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelsBasic: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      // select the channel Group we are looking for
      IChannelGroup group = channelAndGroupInfo.GetTvGroups().FirstOrDefault(x => x.ChannelGroupId == groupId);

      if (group == null)
        throw new BadRequestException(string.Format("GetGroupById: group with id: {0} not found", groupId));
      
      return ChannelGroup(group);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
