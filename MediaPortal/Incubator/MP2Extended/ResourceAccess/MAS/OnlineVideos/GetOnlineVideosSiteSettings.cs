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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebOnlineVideosSiteSetting>), Summary = "This function returns a list of settings for the selected site.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetOnlineVideosSiteSettings
  {
    public List<WebOnlineVideosSiteSetting> Process(string id)
    {
      if (id == null)
        throw new BadRequestException("GetOnlineVideosSiteSettings: id is null");
      
      string siteName;
      OnlineVideosIdGenerator.DecodeSiteId(id, out siteName);

      return MP2Extended.OnlineVideosManager.GetSiteSettings(siteName);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}