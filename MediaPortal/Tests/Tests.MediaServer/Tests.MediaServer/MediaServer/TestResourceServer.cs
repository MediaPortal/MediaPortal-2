using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Server.MediaServer
{
  class TestResourceServer : IResourceServer
  {

    public string AddressIPv4
    {
      get { return IPAddress.Loopback.ToString(); }
    }

    public int PortIPv4
    {
      get { return 0; }
    }

    public string AddressIPv6
    {
      get { return IPAddress.IPv6Loopback.ToString(); }
    }

    public int PortIPv6
    {
      get { return -1; }
    }

    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void RestartHttpServers()
    {
    }

    public string GetServiceUrl(IPAddress ipAddress)
    {
      return $"http://{AddressIPv4}:{PortIPv4}";
    }

    public void AddHttpModule(Type moduleType)
    {
    }

    public void RemoveHttpModule(Type moduleType)
    {
    }

    /// <summary>
    /// The assembly types that the code using this stub declared as sources of API controllers.
    /// </summary>
    public List<Type> ControllerAssemblyTypes { get; } = new List<Type>();

    public void AddHttpControllerAssembly(Type controllerType)
    {
      ControllerAssemblyTypes.Add(controllerType);
    }
  }
}
