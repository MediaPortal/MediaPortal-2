using System;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using Microsoft.AspNet.Http;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "clientDescription", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "idleTimeout", Type = typeof(int), Nullable = true)]
  internal class InitStream
  {
    public WebBoolResult Process(HttpContext httpContext, string itemId, string clientDescription, string identifier, int? idleTimeout)
    {
      if (itemId == null)
        throw new BadRequestException("InitStream: itemId is null");
      if (clientDescription == null)
        throw new BadRequestException("InitStream: clientDescription is null");
      if (identifier == null)
        throw new BadRequestException("InitStream: identifier is null");

      Guid itemGuid;
      if (!Guid.TryParse(itemId, out itemGuid))
        throw new BadRequestException(string.Format("InitStream: Couldn't parse itemId: {0}", itemId));


      StreamItem streamItem = new StreamItem
      {
        ItemId = itemGuid,
        ClientDescription = clientDescription,
        IdleTimeout = idleTimeout ?? -1,
        ClientIp = httpContext.Request.Headers["remote_addr"]
      };

      // Add the stream to the stream controler
      StreamControl.AddStreamItem(identifier, streamItem);

      return new WebBoolResult { Result = true};
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}