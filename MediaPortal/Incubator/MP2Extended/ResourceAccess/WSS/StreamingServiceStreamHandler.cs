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
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  class StreamingServiceStreamHandler : ISubRequestModuleHandler
  {
    private readonly Dictionary<string, IStreamRequestMicroModuleHandler> _requestModuleHandlers = new Dictionary<string, IStreamRequestMicroModuleHandler>
    {
      // Images
      { "GetArtworkResized", new GetArtworkResized()},
    };
    
    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      string action = uriParts.Last();

      Logger.Info("WSS: AbsolutePath: {0}, uriParts.Length: {1}, Lastpart: {2}", request.Uri.AbsolutePath, uriParts.Length, action);

      // pass on to the micro processors
      IStreamRequestMicroModuleHandler requestModuleHandler;
      dynamic returnValue = null;
      if (_requestModuleHandlers.TryGetValue(action, out requestModuleHandler))
        returnValue = requestModuleHandler.Process(request);

      if (returnValue == null)
      {
        Logger.Warn("WSS streaming: Micromodule not found: {0}", action);
        throw new BadRequestException(String.Format("WSS streaming: Micromodule not found: {0}", action));
      }

      // Send the response
      response.Status = HttpStatusCode.OK;
      response.ContentType = "application/octet-stream";
      response.ContentLength = returnValue.Length;
      response.SendHeaders();

      response.SendBody(returnValue);

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
