using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttpServer.Sessions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  interface IStreamRequestMicroModuleHandler
  {
    byte[] Process(IHttpRequest request);
  }
}
