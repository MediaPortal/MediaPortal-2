using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MP2Extended.TAS.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  internal class GetChannelCount
  {
    public WebIntResult Process(int? groupId = null)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelCount: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      return new WebIntResult { Result = channelAndGroupInfo.GetTvChannelsForGroup(groupId).Count };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
