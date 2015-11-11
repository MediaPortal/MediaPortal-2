using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Utilities.Network;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.Api.Pages
{
  partial class ServiceHandlerTemplate
  {
    private readonly Dictionary<string, ApiHandlerDescription> _serviceHandler = new Dictionary<string, ApiHandlerDescription>();
    private readonly string title = "Api overview - Service Handlers";
    private readonly string headLine = "API";
    private readonly string subHeadLine = "Service Handlers.";

    public ServiceHandlerTemplate()
    {
      foreach (var handler in MainRequestHandler.REQUEST_MODULE_HANDLERS)
      {
        Attribute[] attrs = Attribute.GetCustomAttributes(handler.Value.GetType());
        foreach (Attribute attr in attrs)
        {
          var description = attr as ApiHandlerDescription;
          if (description != null)
          {
            ApiHandlerDescription apiHandlerDescription = description;
            _serviceHandler.Add(handler.Key, apiHandlerDescription);
          }
        }
      }
    }

    private static IPAddress GetLocalIp()
    {
      bool useIPv4 = true;
      bool useIPv6 = false;
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      if (settings.UseIPv4) useIPv4 = true;
      if (settings.UseIPv6) useIPv6 = true;

      var host = Dns.GetHostEntry(Dns.GetHostName());
      IPAddress ip6 = null;
      foreach (var ip in host.AddressList)
      {
        if (IPAddress.IsLoopback(ip) == true)
        {
          continue;
        }
        if (useIPv4)
        {
          if (ip.AddressFamily == AddressFamily.InterNetwork)
          {
            return ip;
          }
        }
        if (useIPv6)
        {
          if (ip.AddressFamily == AddressFamily.InterNetworkV6)
          {
            ip6 = ip;
          }
        }
      }
      if (ip6 != null)
      {
        return ip6;
      }
      return null;
    }

    private static string GetBaseURL()
    {
      var rs = ServiceRegistration.Get<IResourceServer>();
      return "http://" + NetworkUtils.IPAddrToString(GetLocalIp()) + ":" + rs.GetPortForIP(GetLocalIp()) + MainRequestHandler.RESOURCE_ACCESS_PATH;
    }
  }
}
