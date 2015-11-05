using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  internal class StreamingServiceStreamHandler : ISubRequestModuleHandler
  {
    private readonly Dictionary<string, IStreamRequestMicroModuleHandler> _requestModuleHandlers = new Dictionary<string, IStreamRequestMicroModuleHandler>
    {
      // Images
      { "ExtractImageResized", new ExtractImageResized() },
      { "GetArtwork", new GetArtwork() },
      { "GetArtworkResized", new GetArtworkResized() },
    };

    // these modules handle the response themselfe
    private readonly Dictionary<string, IStreamRequestMicroModuleHandler2> _requestModuleHandlers2 = new Dictionary<string, IStreamRequestMicroModuleHandler2>
    {
      // General
      { "GetMediaItem", new GetMediaItem() },
      // Images
      { "GetImage", new GetImage() },
      { "GetImageResized", new GetImageResized() },
    };

    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      string action = uriParts.Last();

      Logger.Info("WSS: AbsolutePath: {0}, uriParts.Length: {1}, Lastpart: {2}", request.Uri.AbsolutePath, uriParts.Length, action);

      // pass on to the micro processors
      IStreamRequestMicroModuleHandler requestModuleHandler;
      IStreamRequestMicroModuleHandler2 requestModuleHandler2;
      dynamic returnValue = null;
      if (_requestModuleHandlers.TryGetValue(action, out requestModuleHandler))
        returnValue = requestModuleHandler.Process(request);

      if (_requestModuleHandlers2.TryGetValue(action, out requestModuleHandler2))
        returnValue = requestModuleHandler2.Process(request, response, session);

      if (returnValue == null)
      {
        Logger.Warn("WSS streaming: Micromodule not found: {0}", action);
        throw new BadRequestException(String.Format("WSS streaming: Micromodule not found: {0}", action));
      }

      // the second block of micromodules handles everything byitself
      if (requestModuleHandler2 != null)
        return true;

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