using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.json.Settings;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS
{
  internal class DebugAccessServiceJsonHandler : BaseJsonHeader, ISubRequestModuleHandler
  {
    private readonly Dictionary<string, IRequestMicroModuleHandler> _requestModuleHandlers = new Dictionary<string, IRequestMicroModuleHandler>
    {
      // Settings
      { "CreateUser", new CreateUser() },
      { "DeleteUser", new DeleteUser() },
      { "ChangeSetting", new ChangeSetting() },
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
        returnValue = requestModuleHandler.Process(request, session);

      if (returnValue == null)
      {
        Logger.Warn("DAS json: Micromodule not found: {0}", action);
        throw new BadRequestException(String.Format("DAS json: Micromodule not found: {0}", action));
      }

      byte[] output = ResourceAccessUtils.GetBytesFromDynamic(returnValue);

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