using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  class StreamingServiceHandler : IRequestModuleHandler
  {
    private readonly Dictionary<string, ISubRequestModuleHandler> _requestModuleHandlers = new Dictionary<string, ISubRequestModuleHandler>
    {
      // stream
      { "stream", new StreamingServiceStreamHandler()},
    };
    
    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      if (uriParts.Length < 3)
        throw new BadRequestException("WSS: path is not long enough");
      string action = uriParts[3];

      Logger.Debug("WSS: AbsolutePath: {0}, uriParts.Length: {1}, Lastpart: {2}", request.Uri.AbsolutePath, uriParts.Length, action);

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

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
