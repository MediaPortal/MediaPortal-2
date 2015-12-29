using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Authentication;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.html.Settings.Pages;
using MediaPortal.Plugins.MP2Extended.Settings;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.html.Settings
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Html, ReturnType = typeof(string), Summary = "")]
  internal class ShowSettings : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      // Security
      CheckRights.AccessAllowed(session, UserTypes.Admin, true);

      SettingsTemplate page = new SettingsTemplate();
      return page.TransformText();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}