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
using MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  [ApiHandlerDescription(FriendlyName = "Debug Access Service", Summary = "The Debug Access Service allows you to access various debugging information like a list of Api functions etc.")]
  internal class DebugAccessServiceHandler : BaseHtmlHeader, IRequestModuleHandler
  {
    private readonly Dictionary<string, ISubRequestModuleHandler> _requestModuleHandlers = new Dictionary<string, ISubRequestModuleHandler>
    {
      // html
      { "html", new DebugAccessServiceHtmlHandler() },
      // json
      { "json", new DebugAccessServiceJsonHandler() },
    };

    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      if (uriParts.Length < 3)
        throw new BadRequestException("DAS: path is not long enough");
      string action = uriParts[3];

      Logger.Debug("DAS: AbsolutePath: {0}, uriParts.Length: {1}, Lastpart: {2}", request.Uri.AbsolutePath, uriParts.Length, action);

      // pass on to the Sub processors
      ISubRequestModuleHandler requestModuleHandler;
      dynamic returnValue = null;
      if (_requestModuleHandlers.TryGetValue(action, out requestModuleHandler))
        returnValue = requestModuleHandler.Process(request, response, session);

      if (returnValue == null)
      {
        Logger.Warn("DAS: Submodule not found: {0}", action);
        throw new BadRequestException(String.Format("DAS: Submodule not found: {0}", action));
      }

      return true;
    }

    public Dictionary<string, object> GetRequestMicroModuleHandlers()
    {
      return _requestModuleHandlers.SelectMany(handler => handler.Value.GetRequestMicroModuleHandlers()).ToDictionary(module => module.Key, module => module.Value);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}