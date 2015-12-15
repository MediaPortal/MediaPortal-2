using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.html.Api.Pages;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.html.Api
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Html, ReturnType = typeof(string), Summary = "")]
  internal class GetApi : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string handler = httpParam["handler"].Value;

      if (handler != null)
      {
        ServiceHandlerFunctionsTemplate functionPage = new ServiceHandlerFunctionsTemplate(handler);
        return functionPage.TransformText();
      }
      
      ServiceHandlerTemplate page = new ServiceHandlerTemplate();
      return page.TransformText();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}