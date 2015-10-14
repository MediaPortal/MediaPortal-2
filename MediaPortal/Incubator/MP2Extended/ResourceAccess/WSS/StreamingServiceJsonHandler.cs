using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.StreamInfo;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  internal class StreamingServiceJsonHandler : ISubRequestModuleHandler
  {
    private readonly Dictionary<string, IRequestMicroModuleHandler> _requestModuleHandlers = new Dictionary<string, IRequestMicroModuleHandler>
    {
      // General
      { "GetItemSupportStatus", new GetItemSupportStatus() },
      { "GetServiceDescription", new GetServiceDescription() },
      // Control
      { "InitStream", new InitStream() },
      { "StartStream", new StartStream() },
      { "StartStreamWithStreamSelection", new StartStreamWithStreamSelection() },
      // Profiles
      { "GetTranscoderProfilesForTarget", new GetTranscoderProfilesForTarget() },
      // StreamInfo
      { "GetMediaInfo", new GetMediaInfo() },
    };

    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      string action = uriParts.Last();

      Logger.Info("WSS: AbsolutePath: {0}, uriParts.Length: {1}, Lastpart: {2}", request.Uri.AbsolutePath, uriParts.Length, action);

      // pass on to the micro processors
      IRequestMicroModuleHandler requestModuleHandler;
      dynamic returnValue = null;
      if (_requestModuleHandlers.TryGetValue(action, out requestModuleHandler))
        returnValue = requestModuleHandler.Process(request);

      if (returnValue == null)
      {
        Logger.Warn("WSS json: Micromodule not found: {0}", action);
        throw new BadRequestException(String.Format("WSS json: Micromodule not found: {0}", action));
      }

      byte[] output = ResourceAccessUtils.GetBytesFromDynamic(returnValue);

      // Send the response
      response.Status = HttpStatusCode.OK;
      response.ContentType = "text/html";
      response.ContentLength = output.Length;
      response.SendHeaders();

      response.SendBody(output);

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}