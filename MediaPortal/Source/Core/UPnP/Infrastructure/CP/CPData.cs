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
using UPnP.Infrastructure.CP.SSDP;

namespace UPnP.Infrastructure.CP
{
  /// <summary>
  /// Contains data shared throughout the control point system.
  /// </summary>
  public class CPData
  {
    protected object _syncObj = new object();
    protected int _httpPortV4 = 0;
    protected int _httpPortV6 = 0;
    protected SSDPClientController _ssdpClientController = null;
    protected ICollection<EndpointConfiguration> _endpoints = new List<EndpointConfiguration>();
    protected IDictionary<string, RootEntry> _deviceEntries = new Dictionary<string, RootEntry>();

    /// <summary>
    /// Synchronization object for the UPnP control point system.
    /// </summary>
    public object SyncObj
    {
      get { return _syncObj; }
    }

    /// <summary>
    /// Gets or sets the HTTP listening port for IPv4 used for event messages.
    /// </summary>
    public int HttpPortV4
    {
      get { return _httpPortV4; }
      internal set { _httpPortV4 = value; }
    }

    /// <summary>
    /// Gets or sets the HTTP listening port for IPv6 used for event messages.
    /// </summary>
    public int HttpPortV6
    {
      get { return _httpPortV6; }
      internal set { _httpPortV6 = value; }
    }

    /// <summary>
    /// Gets a collection of all local IP endpoint configurations.
    /// </summary>
    public ICollection<EndpointConfiguration> Endpoints
    {
      get { return _endpoints; }
    }

    /// <summary>
    /// Returns dictionary of root device description locations mapped to their root device entry.
    /// </summary>
    public IDictionary<string, RootEntry> DeviceEntries
    {
      get { return _deviceEntries; }
    }

    /// <summary>
    /// Gets or sets the SSDP controller of the UPnP client.
    /// </summary>
    public SSDPClientController SSDPController
    {
      get { return _ssdpClientController; }
      internal set { _ssdpClientController = value; }
    }
  }
}
