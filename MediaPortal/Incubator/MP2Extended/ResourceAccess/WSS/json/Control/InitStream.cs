using System;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  internal class InitStream : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string itemId = httpParam["itemId"].Value;
      string clientDescription = httpParam["clientDescription"].Value;
      string identifier = httpParam["identifier"].Value;
      string idleTimeout = httpParam["idleTimeout"].Value;

      if (itemId == null)
        throw new BadRequestException("InitStream: itemId is null");
      if (clientDescription == null)
        throw new BadRequestException("InitStream: clientDescription is null");
      if (identifier == null)
        throw new BadRequestException("InitStream: identifier is null");

      Guid itemGuid;
      if (!Guid.TryParse(itemId, out itemGuid))
        throw new BadRequestException(string.Format("InitStream: Couldn't parse itemId: {0}", itemId));

      int idleTimeoutInt = -1;
      if (idleTimeout != null)
        int.TryParse(idleTimeout, out idleTimeoutInt);

      StreamItem streamItem = new StreamItem
      {
        ItemId = itemGuid,
        ClientDescription = clientDescription,
        IdleTimeout = idleTimeoutInt
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