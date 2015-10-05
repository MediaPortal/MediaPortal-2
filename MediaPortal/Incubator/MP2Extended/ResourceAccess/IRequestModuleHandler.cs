using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttpServer.Sessions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  interface IRequestModuleHandler
  {
    bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session);
  }
}
