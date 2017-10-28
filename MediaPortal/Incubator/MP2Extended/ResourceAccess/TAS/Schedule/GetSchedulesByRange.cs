using MediaPortal.Common;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetSchedulesByRange : GetSchedules
  {
    public IList<WebScheduleBasic> Process(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetSchedulesByRange: ITvProvider not found");

      var output = Process(filter, sort, order)
      // Get Range
      .TakeRange(start, end).ToList();

      return output.ToList();
    }
  }
}
