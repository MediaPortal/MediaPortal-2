using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebBoolResult), Summary = "This function changes the value of a site property.")]
  [ApiFunctionParam(Name = "siteId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "property", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "value", Type = typeof(string), Nullable = false)]
  internal class SetOnlineVideosSiteSetting : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string siteId = httpParam["siteId"].Value;
      string property = httpParam["property"].Value;
      string value = httpParam["value"].Value;

      if (siteId == null)
        throw new BadRequestException("SetOnlineVideosSiteSetting: siteId is null");
      if (property == null)
        throw new BadRequestException("SetOnlineVideosSiteSetting: property is null");
      if (value == null)
        throw new BadRequestException("SetOnlineVideosSiteSetting: value is null");

      string siteName;
      OnlineVideosIdGenerator.DecodeSiteId(siteId, out siteName);

      return new WebBoolResult { Result = MP2Extended.OnlineVideosManager.SetSiteSetting(siteName, property, value) };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}