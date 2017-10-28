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
  internal class GetChannelsBasicByRange : GetChannelsBasic
  {
    public IList<WebChannelBasic> Process(int start, int end, WebSortField? sort, WebSortOrder? order, int? groupId = null)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelsBasicByRange: ITvProvider not found");

      var output = Process(sort, order, groupId)
      // get range
        .TakeRange(start, end);

      return output.ToList();
    }
  }
}
