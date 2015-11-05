using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal class TVAccessServiceHandler : IRequestModuleHandler
  {
    private readonly Dictionary<string, IRequestMicroModuleHandler> _requestModuleHandlers = new Dictionary<string, IRequestMicroModuleHandler>
    {
      // Misc
      { "GetActiveCards", new GetActiveCards()},
      { "GetServiceDescription", new GetServiceDescription()},
      // Tv
      { "GetChannelsBasic", new GetChannelsBasic()},
      { "GetGroupById", new GetGroupById()},
      { "GetGroupCount", new GetGroupCount()},
      { "GetGroups", new GetGroups()},
      // Schedule
      { "AddSchedule", new AddSchedule()},
    };

    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      string action = uriParts.Last();

      Logger.Info("TAS: AbsolutePath: {0}, uriParts.Length: {1}, Lastpart: {2}", request.Uri.AbsolutePath, uriParts.Length, action);

      // pass on to the micro processors
      IRequestMicroModuleHandler requestModuleHandler;
      dynamic returnValue = null;
      if (_requestModuleHandlers.TryGetValue(action, out requestModuleHandler))
        returnValue = requestModuleHandler.Process(request);

      if (returnValue == null)
      {
        Logger.Warn("MAS: Micromodule not found: {0}", action);
        throw new BadRequestException(String.Format("MAS: Micromodule not found: {0}", action));
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