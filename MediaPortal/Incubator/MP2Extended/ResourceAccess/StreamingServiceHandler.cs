using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  [ApiHandlerDescription(FriendlyName = "Streaming Service", Summary = "The Streaming Service handles all streaming related tasks for MAS and TAS.")]
  internal class StreamingServiceHandler : IRequestModuleHandler
  {
    private readonly Dictionary<string, ISubRequestModuleHandler> _requestModuleHandlers = new Dictionary<string, ISubRequestModuleHandler>
    {
      // stream
      { "stream", new StreamingServiceStreamHandler() },
      // json
      { "json", new StreamingServiceJsonHandler() },
    };

    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      if (uriParts.Length < 3)
        throw new BadRequestException("WSS: path is not long enough");
      string action = uriParts[3];

      // pass on to the Sub processors
      ISubRequestModuleHandler requestModuleHandler;
      dynamic returnValue = null;
      if (_requestModuleHandlers.TryGetValue(action, out requestModuleHandler))
        returnValue = requestModuleHandler.Process(request, response, session);

      if (returnValue == null)
      {
        Logger.Warn("WSS: Submodule not found: {0}", action);
        throw new BadRequestException(String.Format("WSS: Submodule not found: {0}", action));
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