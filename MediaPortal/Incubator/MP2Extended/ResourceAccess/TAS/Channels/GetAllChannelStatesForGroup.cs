using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MP2Extended.TAS.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Channels
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "userName", Type = typeof(string), Nullable = false)]
  internal class GetAllChannelStatesForGroup
  {
    public IList<WebChannelState> Process(int groupId, string userName)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetAllChannelStatesForGroup: ITvProvider not found");

      if (string.IsNullOrEmpty(userName))
        throw new BadRequestException("GetAllChannelStatesForGroup: userName is null");
      
      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      
      return channelAndGroupInfo.GetTvChannelsForGroup(groupId).Select(c => new WebChannelState
      {
        ChannelId = c.ChannelId,
        State = ChannelState.Tunable // TODO: implement in SlimTv
      }).ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
