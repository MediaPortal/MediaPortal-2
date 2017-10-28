using MediaPortal.Common;
using MediaPortal.Common.Logging;
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
  [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetGroupsByRange : GetGroups
  {
    public IList<WebChannelGroup> Process(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetGroupsByRange: ITvProvider not found");

      var output = Process(sort, order)
      // get range
        .TakeRange(start, end);

      return output.ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
