using HttpServer;
using HttpServer.Sessions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal interface IRequestMicroModuleHandler
  {
    dynamic Process(IHttpRequest request, IHttpSession session);
  }
}