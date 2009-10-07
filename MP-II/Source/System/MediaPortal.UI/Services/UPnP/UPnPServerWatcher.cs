#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using UPnP.Infrastructure.CP;

namespace MediaPortal.Services.UPnP
{
  public delegate void AvailableMediaServersChangedDlgt(IDictionary<SystemName, RootDescriptor> allAvailableServers);

  /// <summary>
  /// Watches the network for available MediaPortal-II servers.
  /// </summary>
  public class UPnPServerWatcher
  {
    protected IDictionary<SystemName, RootDescriptor> _availableServers =
        new Dictionary<SystemName, RootDescriptor>();
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

    public IDictionary<SystemName, RootDescriptor>  AvailableServers
    {
      get { return _availableServers; }
    }

    public event AvailableMediaServersChangedDlgt AvailableMediaServersChanged;

    public void Start()
    {
      _networkTracker.Start();
    }

    public void Stop()
    {
      _networkTracker.Close();
    }

    protected SystemName GetSystemOfMPMediaServer(RootDescriptor rootDescriptor)
    {
      try
      {
        XmlElement mediaServerDeviceElement = rootDescriptor.FindFirstDeviceElement(Consts.MEDIA_SERVER_DEVICE_TYPE, Consts.MEDIA_SERVER_DEVICE_TYPE_VERSION);
        if (mediaServerDeviceElement == null)
          return null;
        return new SystemName(new Uri(rootDescriptor.SSDPRootEntry.DescriptionLocation).Host);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("Error parsing UPnP MediaServer device location", e);
        return null;
      }
    }

    void OnUPnPRootDeviceAdded(RootDescriptor rootDescriptor)
    {
      IDictionary<SystemName, RootDescriptor>  availableServers;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        SystemName systemName = GetSystemOfMPMediaServer(rootDescriptor);
        if (systemName == null || _availableServers.ContainsKey(systemName))
          return;
        _availableServers.Add(systemName, rootDescriptor);
        availableServers = _availableServers;
      }
      InvokeAvailableMediaServersChanged(availableServers);
    }

    void OnUPnPRootDeviceRemoved(RootDescriptor rootDescriptor)
    {
      IDictionary<SystemName, RootDescriptor>  availableServers;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        SystemName systemName = GetSystemOfMPMediaServer(rootDescriptor);
        if (systemName == null || !_availableServers.ContainsKey(systemName))
          return;
        _availableServers.Remove(systemName);
        availableServers = _availableServers;
      }
      InvokeAvailableMediaServersChanged(availableServers);
    }

    protected void InvokeAvailableMediaServersChanged(IDictionary<SystemName, RootDescriptor> allAvailableServers)
    {
      AvailableMediaServersChangedDlgt dlgt = AvailableMediaServersChanged;
      if (dlgt != null)
        dlgt(allAvailableServers);
    }
  }
}
