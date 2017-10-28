using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MP2Extended.TAS.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetChannelsBasic : BaseChannelBasic
  {
    public IList<WebChannelBasic> Process(WebSortField? sort, WebSortOrder? order, int? groupId = null)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelsBasic: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      var output = channelAndGroupInfo.GetTvChannelsForGroup(groupId)
        .Select(c => ChannelBasic(c));

      // sort
      if (sort != null)
        output = output.SortChannelList(sort, order);

      return output.ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
