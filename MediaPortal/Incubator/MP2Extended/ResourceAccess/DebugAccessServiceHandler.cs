using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.Api;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  [ApiHandlerDescription(FriendlyName = "Debug Access Service", Summary = "The Debug Access Service allows you to access various debugging information like a list of Api functions etc.")]
  internal class DebugAccessServiceHandler : BaseHtmlHeader, IRequestModuleHandler
  {
    private readonly Dictionary<string, IRequestMicroModuleHandler> _requestModuleHandlers = new Dictionary<string, IRequestMicroModuleHandler>
    {
      // Api
      { "GetApi", new GetApi() },
    };

    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      string action = (uriParts.Length >= 5) ? uriParts[4] : uriParts.Last();

      Logger.Info("DAS: AbsolutePath: {0}, uriParts.Length: {1}, Lastpart: {2}", request.Uri.AbsolutePath, uriParts.Length, action);

      // pass on to the micro processors
      IRequestMicroModuleHandler requestModuleHandler;
      dynamic returnValue = null;
      if (_requestModuleHandlers.TryGetValue(action, out requestModuleHandler))
        returnValue = requestModuleHandler.Process(request);

      if (returnValue == null)
      {
        Logger.Warn("DAS: Micromodule not found: {0}", action);
        throw new BadRequestException(String.Format("DAS: Micromodule not found: {0}", action));
      }

      byte[] output = ResourceAccessUtils.GetBytes(returnValue);

      // Send the response
      SendHeader(response, output.Length);

      response.SendBody(output);

      return true;
    }

    public Dictionary<string, object> GetRequestMicroModuleHandlers()
    {
      return _requestModuleHandlers.ToDictionary<KeyValuePair<string, IRequestMicroModuleHandler>, string, object>(module => module.Key, module => module.Value);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}