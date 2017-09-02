using System.Net;
using System.Text;
using HttpServer;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.BaseClasses
{
  class BaseJsonHeader
  {
    internal void SendHeader(IHttpResponse response, int contentLength)
    {
      response.Status = HttpStatusCode.OK;
      response.Encoding = Encoding.UTF8;
      response.ContentType = "application/json; charset=UTF-8";
      response.ContentLength = contentLength;
      response.SendHeaders();
    }
  }
}
