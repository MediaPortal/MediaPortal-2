using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using ZeroconfService;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class ZeroConfig
  {
    /// <summary>
    /// The Bonjour service publish object
    /// </summary>
    NetService _publishService;

    /// <summary>
    /// Bonjour service name (your hostname)
    /// </summary>
    private readonly string _serviceName;

    /// <summary>
    /// Bonjour service type
    /// </summary>
    private string serviceType = "_mepo-remote._tcp";

    /// <summary>
    /// Bonjour domain (empty = whole network)
    /// </summary>
    private readonly string _domain;

    /// <summary>
    /// Service port
    /// </summary>
    private readonly UInt16 _port;

    /// <summary>
    /// <code>true</code> if the publishService is in the process of publishing the NetService
    /// </summary>
    private bool _servicePublishing;

    /// <summary>
    /// <code>true</code> if the service is advertised via Bonjour
    /// </summary>
    private bool _servicePublished;

    /// <summary>
    /// <code>true</code> to not publish the bonjour service
    /// </summary>
    private bool _disableBonjour;

    internal ZeroConfig(UInt16 port, string serviceName, string domain)
    {
      _port = port;
      _serviceName = serviceName;
      _domain = domain;
    }

    /// <summary>
    /// Publish the service via Bonjour protocol to the network
    /// </summary>
    internal void PublishBonjourService()
    {
      if (_servicePublishing)
      {
        Logger.Debug("Already in the process of publishing the Bonjour service. Aborting publish ...");
        return;
      }

      // Test if Bonjour is installed
      try
      {
        //float bonjourVersion = NetService.GetVersion();
        Version bonjourVersion = NetService.DaemonVersion;
        Logger.Info("Bonjour version {0} found.", bonjourVersion.ToString());
      }
      catch
      {
        Logger.Error("Bonjour enabled but not installed! Get it at http://support.apple.com/downloads/Bonjour_for_Windows");
        Logger.Info("Disabling Bonjour for this session.");
        _disableBonjour = true;
        return;
      }

      _servicePublishing = true;

      _publishService = new NetService(_domain, serviceType, _serviceName, _port);

      // Get the MAC addresses and set it as bonjour txt record
      // Needed by the clients to implement wake on lan
      Hashtable dict = new Hashtable { { "hwAddr", GetHardwareAddresses() } };
      _publishService.TXTRecordData = NetService.DataFromTXTRecordDictionary(dict);
      _publishService.DidPublishService += publishService_DidPublishService;
      _publishService.DidNotPublishService += publishService_DidNotPublishService;

      _publishService.Publish();
    }

    /// <summary>
    /// Stops the ZeroConfig Server if it is running
    /// </summary>
    public void Stop()
    {
      if (!_servicePublished) return;
      _publishService.Stop();
      _publishService = null;
    }

    /// <summary>
    /// Service couldn't be published
    /// </summary>
    /// <param name="service"></param>
    /// <param name="exception"></param>
    private void publishService_DidNotPublishService(NetService service, DNSServiceException exception)
    {
      Logger.Error(String.Format("Bonjour publish error: {0}", exception.Message));
      _servicePublishing = false;
    }

    /// <summary>
    /// Service was published
    /// </summary>
    /// <param name="service"></param>
    private void publishService_DidPublishService(NetService service)
    {
      Logger.Info("Published Service via Bonjour!");
      _servicePublishing = false;
      _servicePublished = true;
    }

    /// <summary>
    /// Get the hardware (MAC) addresses of all available network adapters
    /// </summary>
    /// <returns>Seperated MAC addresses</returns>
    private static String GetHardwareAddresses()
    {
      StringBuilder hardwareAddresses = new StringBuilder();
      try
      {
        NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in nics)
        {
          if (adapter.OperationalStatus == OperationalStatus.Up)
          {
            String hardwareAddress = adapter.GetPhysicalAddress().ToString();
            if (!hardwareAddress.Equals(String.Empty) && hardwareAddress.Length == 12)
            {
              if (hardwareAddresses.Length > 0)
              {
                hardwareAddresses.Append(";");
              }

              hardwareAddresses.Append(hardwareAddress);
            }
          }
        }
      }
      catch (NetworkInformationException e)
      {
        Logger.Error("Could not get hardware address: {0}", e.Message);
      }

      return hardwareAddresses.ToString();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
