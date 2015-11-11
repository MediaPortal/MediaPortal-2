using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  internal interface ISubRequestModuleHandler
  {
    bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session);
    Dictionary<string, object> GetRequestMicroModuleHandlers();
  }
}