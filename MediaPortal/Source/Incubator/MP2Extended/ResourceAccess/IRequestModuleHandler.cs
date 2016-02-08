using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal interface IRequestModuleHandler
  {
    bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session);
    Dictionary<string, object> GetRequestMicroModuleHandlers();
  }
}