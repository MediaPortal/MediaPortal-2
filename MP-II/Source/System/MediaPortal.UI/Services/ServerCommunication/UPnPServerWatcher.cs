#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.UPnP;
using MediaPortal.UI.ServerCommunication;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.UI.Services.ServerCommunication
{
  public delegate void AvailableBackendServersChangedDlgt(ICollection<ServerDescriptor> allAvailableServers, bool serversWereAdded);

  /// <summary>
  /// Watches the network for available MediaPortal-II servers.
  /// </summary>
  public class UPnPServerWatcher
  {
    protected ICollection<ServerDescriptor> _availableServers = new List<ServerDescriptor>();
    protected UPnPNetworkTracker _networkTracker;

    public UPnPServerWatcher()
    {
      CPData cpData = new CPData();
      _networkTracker = new UPnPNetworkTracker(cpData);
      _networkTracker.RootDeviceAdded += OnUPnPRootDeviceAdded;
      _networkTracker.RootDeviceRemoved += OnUPnPRootDeviceRemoved;
    }

    ~UPnPServerWatcher()
    {
      Stop();
    }

    public ICollection<ServerDescriptor> AvailableServers
    {
      get { return _availableServers; }
    }

    public event AvailableBackendServersChangedDlgt AvailableBackendServersChanged;

    public void Start()
    {
      _networkTracker.Start();
    }

    public void Stop()
    {
      _networkTracker.Close();
    }

    public static ServerDescriptor GetMPBackendServerDescriptor(RootDescriptor rootDescriptor)
    {
      try
      {
        XPathNavigator deviceElementNav = rootDescriptor.FindFirstDeviceElement(
            UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        if (deviceElementNav == null)
          return null;
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(deviceElementNav.NameTable);
        nsmgr.AddNamespace("d", UPnP.Infrastructure.UPnPConsts.NS_DEVICE_DESCRIPTION);
        string udn = RootDescriptor.GetDeviceUDN(deviceElementNav, nsmgr);
        string friendlyName = ParserHelper.SelectText(deviceElementNav, "d:friendlyName/text()", nsmgr);
        SystemName system = new SystemName(new Uri(rootDescriptor.SSDPRootEntry.DescriptionLocation).Host);
        return new ServerDescriptor(rootDescriptor, ParserHelper.ExtractUUIDFromUDN(udn), friendlyName, system);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("Error parsing UPnP device description", e);
        return null;
      }
    }

    void OnUPnPRootDeviceAdded(RootDescriptor rootDescriptor)
    {
      ICollection<ServerDescriptor> availableServers;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        ServerDescriptor serverDescriptor = GetMPBackendServerDescriptor(rootDescriptor);
        if (serverDescriptor == null || _availableServers.Contains(serverDescriptor))
          return;
        ServiceScope.Get<ILogger>().Debug("UPnPServerWatcher: Found MP-II BackendServer '{0}' at host '{1}'",
            serverDescriptor.ServerName, serverDescriptor.System.HostName);
        _availableServers.Add(serverDescriptor);
        availableServers = _availableServers;
      }
      InvokeAvailableBackendServersChanged(availableServers, true);
    }

    void OnUPnPRootDeviceRemoved(RootDescriptor rootDescriptor)
    {
      ICollection<ServerDescriptor> availableServers;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        ServerDescriptor serverDescriptor = GetMPBackendServerDescriptor(rootDescriptor);
        if (serverDescriptor == null || !_availableServers.Contains(serverDescriptor))
          return;
        ServiceScope.Get<ILogger>().Debug("UPnPServerWatcher: MP-II BackendServer '{0}' at host '{1}' was removed from the network",
            serverDescriptor.ServerName, serverDescriptor.System.HostName);
        _availableServers.Remove(serverDescriptor);
        availableServers = _availableServers;
      }
      InvokeAvailableBackendServersChanged(availableServers, false);
    }

    protected void InvokeAvailableBackendServersChanged(ICollection<ServerDescriptor> allAvailableServers, bool serversWereAdded)
    {
      AvailableBackendServersChangedDlgt dlgt = AvailableBackendServersChanged;
      if (dlgt != null)
        dlgt(allAvailableServers, serversWereAdded);
    }
  }
}
