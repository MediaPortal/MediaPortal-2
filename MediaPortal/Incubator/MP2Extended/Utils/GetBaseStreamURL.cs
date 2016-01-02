using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Utilities.Network;
using Microsoft.AspNet.Http;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
  internal static class GetBaseStreamUrl
  {
    public static string GetBaseStreamURL(HttpContext httpContext)
    {
      return "http://" + NetworkUtils.IPAddrToString(httpContext.Connection.LocalIpAddress) + ":" + httpContext.Connection.LocalPort;
    }
  }
}
