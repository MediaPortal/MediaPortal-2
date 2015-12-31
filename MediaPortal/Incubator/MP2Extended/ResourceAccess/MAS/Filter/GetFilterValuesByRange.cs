using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Filter
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<string>),
    Summary = "Get all available values for a given field")]
  [ApiFunctionParam(Name = "mediaType", Type = typeof(WebMediaType), Nullable = false)]
  [ApiFunctionParam(Name = "filterField", Type = typeof(string), Nullable = false)]
  //[ApiFunctionParam(Name = "provider", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "op", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "limit", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
  internal class GetFilterValuesByRange
  {
    public IList<string> Process(int start, int end, WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
     IList<string> output = new GetFilterValues().Process(mediaType, filterField, op, limit, order);

      // Get Range
      output = output.TakeRange(start, end).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}