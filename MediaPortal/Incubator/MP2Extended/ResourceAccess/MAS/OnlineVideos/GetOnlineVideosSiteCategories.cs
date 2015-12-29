using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebOnlineVideosSiteCategory>), Summary = "This function returns a list of Categories for a selected Site.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetOnlineVideosSiteCategories : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;

      if (id == null)
        throw new BadRequestException("GetOnlineVideosSiteCategories: id null");

      string siteName;
      OnlineVideosIdGenerator.DecodeSiteId(id, out siteName);

      return MP2Extended.OnlineVideosManager.GetSiteCategories(siteName).Select(category => new WebOnlineVideosSiteCategory
      {
        Id = OnlineVideosIdGenerator.BuildCategoryId(siteName, category.RecursiveName()),
        Title = category.Name,
        Description = category.Description,
        HasSubCategories = category.HasSubCategories
      }).ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}