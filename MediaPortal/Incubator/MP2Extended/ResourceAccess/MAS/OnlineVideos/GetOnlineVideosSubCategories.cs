using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebOnlineVideosSiteCategory>), Summary = "This function returns a list of Subcategories available in a selected Category.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetOnlineVideosSubCategories
  {
    public List<WebOnlineVideosSiteCategory> Process(string id)
    {
      if (id == null)
        throw new BadRequestException("GetOnlineVideosSubCategories: id is null");

      string siteName;
      string categoryRecursiveName;
      OnlineVideosIdGenerator.DecodeCategoryId(id, out siteName, out categoryRecursiveName);

      return MP2Extended.OnlineVideosManager.GetSubCategories(siteName, categoryRecursiveName).Select(subCategory => new WebOnlineVideosSiteCategory
      {
        Id = OnlineVideosIdGenerator.BuildCategoryId(siteName, subCategory.RecursiveName()),
        Title = subCategory.Name,
        Description = subCategory.Description,
        HasSubCategories = subCategory.HasSubCategories
      }).ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}