using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc
{
  internal class TestConnectionToTVService : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      return new WebBoolResult { Result = ServiceRegistration.IsRegistered<ITvProvider>() };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}