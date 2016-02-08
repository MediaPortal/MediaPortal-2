using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.SlimTv.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetServiceDescription
  {
    public WebTVServiceDescription Process()
    { 
      WebTVServiceDescription webTVServiceDescription = new WebTVServiceDescription
      {
        ApiVersion = GlobalVersion.API_VERSION,
        HasConnectionToTVServer = ServiceRegistration.IsRegistered<ITvProvider>(),
        ServiceVersion = GlobalVersion.VERSION
      };

      return webTVServiceDescription;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}