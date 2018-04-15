#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.SystemCommunication;
using UPnP.Infrastructure.CP;

namespace MediaPortal.Common.Services.ServerCommunication
{
  public delegate void AvailableBackendServersChangedDlgt(ICollection<ServerDescriptor> allAvailableServers, bool serversWereAdded);

  /// <summary>
  /// Watches the network for available MediaPortal 2 servers.
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

    void OnUPnPRootDeviceAdded(RootDescriptor rootDescriptor)
    {
      ICollection<ServerDescriptor> availableServers;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        ServerDescriptor serverDescriptor = ServerDescriptor.GetMPBackendServerDescriptor(rootDescriptor);
        if (serverDescriptor == null || _availableServers.Contains(serverDescriptor))
          return;
        SystemName preferredLink = serverDescriptor.GetPreferredLink();
        ServiceRegistration.Get<ILogger>().Debug("UPnPServerWatcher: Found MediaPortal 2 BackendServer '{0}' at host '{1}' (IP address: '{2}')",
            serverDescriptor.ServerName, preferredLink.HostName, preferredLink.Address);
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
        ServerDescriptor serverDescriptor = ServerDescriptor.GetMPBackendServerDescriptor(rootDescriptor);
        if (serverDescriptor == null || !_availableServers.Contains(serverDescriptor))
          return;
        SystemName preferredLink = serverDescriptor.GetPreferredLink();
        ServiceRegistration.Get<ILogger>().Debug("UPnPServerWatcher: MediaPortal 2 BackendServer '{0}' at host '{1}' (IP address: '{2}') was removed from the network",
            serverDescriptor.ServerName, preferredLink.HostName, preferredLink.Address);
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
