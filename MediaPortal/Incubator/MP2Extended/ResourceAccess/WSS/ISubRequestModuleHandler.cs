using HttpServer;
using HttpServer.Sessions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal interface ISubRequestModuleHandler
  {
    bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session);
  }
}