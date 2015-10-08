using HttpServer;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal interface IRequestMicroModuleHandler
  {
    dynamic Process(IHttpRequest request);
  }
}