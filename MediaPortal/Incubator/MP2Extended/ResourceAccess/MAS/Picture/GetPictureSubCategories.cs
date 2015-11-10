using System.Collections.Generic;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.MAS;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture
{
  // TODO: not supported by MP2 -> return an empty list
  internal class GetPictureSubCategories : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      var output = new List<WebCategory>();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}