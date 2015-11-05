using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General
{
  internal class TestConnection : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      return new WebBoolResult { Result = true };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}