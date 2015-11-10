using System;
using System.Net;
using System.Text;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess;

namespace MediaPortal.Plugins.MP2Extended.ErrorPages
{
  class SendErrorPage
  {
    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session, Exception ex)
    {
      InternalServerExceptionTemplate page = new InternalServerExceptionTemplate(ex);
      byte[] output = ResourceAccessUtils.GetBytes(page.TransformText());
      response.Status = HttpStatusCode.InternalServerError;
      response.Encoding = Encoding.UTF8;
      response.ContentType = "text/html; charset=UTF-8";
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
