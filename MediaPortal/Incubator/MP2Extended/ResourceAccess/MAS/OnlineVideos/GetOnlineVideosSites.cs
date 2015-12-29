using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.Utils;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebOnlineVideosSite>), Summary = "This function returns a list of all locally available Sites in OnlineVideos.")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetOnlineVideosSites : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      List<WebOnlineVideosSite> output = MP2Extended.OnlineVideosManager.GetSites().Select(site => new WebOnlineVideosSite
      {
        Id = OnlineVideosIdGenerator.BuildSiteId(site.Settings.Name),
        Title = site.Settings.Name,
        Description = site.Settings.Description,
        Language = site.Settings.Language,
        LastUpdated = site.Settings.LastUpdated
      }).ToList();

      // sort and filter
      HttpParam httpParam = request.Param;
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      string filter = httpParam["filter"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.AsQueryable().Filter(filter).SortMediaItemList(webSortField, webSortOrder).ToList();
      }
      else
        output = output.Filter(filter).ToList();
      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}