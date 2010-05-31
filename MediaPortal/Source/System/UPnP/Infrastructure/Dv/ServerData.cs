#region Copyright (C) 2007-2010 Team MediaPortal

/* 
 *  Copyright (C) 2007-2010 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using HttpServer;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Dv.GENA;
using UPnP.Infrastructure.Dv.SSDP;

namespace UPnP.Infrastructure.Dv
{
  /// <summary>
  /// Runtime configuration data for the UPnP subsystem.
  /// </summary>
  public class ServerData
  {
    /// <summary>
    /// HTTP listening port for description and control requests.
    /// </summary>
    public static uint DEFAULT_HTTP_PORT = 8081;

    /// <summary>
    /// State of the UPnP subsystem.
    /// </summary>
    public bool IsActive = false;

    /// <summary>
    /// Synchronization object for the UPnP server system.
    /// </summary>
    public object SyncObj = new object();

    /// <summary>
    /// Collection of UPnP endpoints, the UPnP system is bound to.
    /// </summary>
    public ICollection<EndpointConfiguration> UPnPEndPoints = new List<EndpointConfiguration>();

    /// <summary>
    /// The handler instance which attends the SSDP subsystem.
    /// </summary>
    public SSDPServerController SSDPController;

    /// <summary>
    /// The controller instance which attends the GENA subsystem.
    /// </summary>
    public GENAServerController GENAController;

    /// <summary>
    /// HTTP listener to answer description, control and eventing requests for IPv4.
    /// </summary>
    public HttpListener HTTPListenerV4;

    /// <summary>
    /// HTTP listener to answer description, control and eventing requests for IPv6.
    /// </summary>
    public HttpListener HTTPListenerV6;

    /// <summary>
    /// Port where the HTTP server is listening for IPv4.
    /// </summary>
    public uint HTTP_PORTv4 = DEFAULT_HTTP_PORT;

    /// <summary>
    /// Port where the HTTP server is listening for IPv6.
    /// </summary>
    public uint HTTP_PORTv6 = DEFAULT_HTTP_PORT;

    /// <summary>
    /// The UPnP server which is handled by the UPnP subsystem.
    /// </summary>
    public UPnPServer Server;

    /// <summary>
    /// Time in seconds after that a UPnP advertisment expires.
    /// </summary>
    public int AdvertisementExpirationTime = SSDPServerController.DEFAULT_ADVERTISEMENT_EXPIRATION_TIME;

    /// <summary>
    /// UPnP BOOTID. Contains a value that is increased at every startup of the UPnP subsystem.
    /// </summary>
    public Int32 BootId = NextBootId();

    /// <summary>
    /// Collection of pending searches which will be answered some milliseconds later, as specified in the UPnP device
    /// architecture.
    /// </summary>
    public ICollection<PendingSearchRequest> PendingSearches = new List<PendingSearchRequest>();

    /// <summary>
    /// Stores the sequence numbers per service for multicast events. The entries in the dictionary will be lazily initialized.
    /// </summary>
    public IDictionary<DvService, EventingState> ServiceMulticastEventingState = new Dictionary<DvService, EventingState>();

    /// <summary>
    /// Returns the next integer which can be used as BOOTID value at the current time.
    /// The return value depends on the current system time.
    /// </summary>
    /// <returns>Integer for BOOTID field which can be used at the current time.</returns>
    public static int NextBootId()
    {
      // As proposed in (1), 1.2.2
      return (int) (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
    }

    /// <summary>
    /// Returns the next event key for a multicast event for the given <paramref name="service"/>.
    /// </summary>
    /// <param name="service">Service for that the new multicast event key should be created.</param>
    /// <returns>New multicast event key.</returns>
    public uint GetNextMulticastEventKey(DvService service)
    {
      EventingState state;
      if (!ServiceMulticastEventingState.TryGetValue(service, out state))
        ServiceMulticastEventingState[service] = state = new EventingState();
      uint result = state.EventKey;
      state.IncEventKey();
      return result;
    }
  }
}
