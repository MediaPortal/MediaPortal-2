#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections;
using System.Net.NetworkInformation;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using ArkaneSystems.Arkane.Zeroconf;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class ZeroConfig
  {
    /// <summary>
    /// The Bonjour service publish object
    /// </summary>
    RegisterService _publishService;

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
        Logger.Debug("WifiRemote: Already in the process of publishing the Bonjour service. Aborting publish ...");
        return;
      }

      _servicePublishing = true;

      try
      {
        _publishService = new RegisterService();
        _publishService.Name = _serviceName;
        _publishService.RegType = serviceType;
        _publishService.ReplyDomain = _domain;
        _publishService.Port = Convert.ToInt16(_port);

        // Get the MAC addresses and set it as bonjour txt record
        // Needed by the clients to implement wake on lan
        TxtRecord txt_record = new TxtRecord();
        txt_record.Add("hwAddr", GetHardwareAddresses());
        _publishService.TxtRecord = txt_record;

        _publishService.Response += PublishService_Response;
        _publishService.Register();
      }
      catch (Exception ex)
      {
        Logger.Debug("WifiRemote: Bonjour enabled but failed to publish!", ex);
        Logger.Info("WifiRemote: Disabling Bonjour for this session. If not installed get it at http://support.apple.com/downloads/Bonjour_for_Windows");
        _disableBonjour = true;
        return;
      }
    }

    private void PublishService_Response(object o, RegisterServiceEventArgs args)
    {
      if (_servicePublishing)
      {
        if (args.IsRegistered)
        {
          Logger.Info("WifiRemote: Published Service via Bonjour!");
          _servicePublishing = false;
          _servicePublished = true;
        }
        else
        {
          Logger.Error(String.Format("WifiRemote: Bonjour publish error: {0}", args.ServiceError.ToString()));
          _servicePublishing = false;
        }
      }
    }

    /// <summary>
    /// Stops the ZeroConfig Server if it is running
    /// </summary>
    public void Stop()
    {
      if (!_servicePublished)
        return;

      _publishService.Dispose();
      _publishService = null;
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
        Logger.Error("WifiRemote: Could not get hardware address: {0}", e.Message);
      }

      return hardwareAddresses.ToString();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
