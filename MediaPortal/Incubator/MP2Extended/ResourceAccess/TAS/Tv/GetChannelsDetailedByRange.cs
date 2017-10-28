using MediaPortal.Common;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetChannelsDetailedByRange : GetChannelsDetailed
  {
    public IList<WebChannelDetailed> Process(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelsDetailedByRange: ITvProvider not found");

      var output = Process(groupId, sort, order)
        .TakeRange(start, end);

      return output.ToList();
    }
  }
}
