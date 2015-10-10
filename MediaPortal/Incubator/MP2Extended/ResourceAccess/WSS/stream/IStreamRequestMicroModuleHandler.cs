using HttpServer;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal interface IStreamRequestMicroModuleHandler
  {
    byte[] Process(IHttpRequest request);
  }
}