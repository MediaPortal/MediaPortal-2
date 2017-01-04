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

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UPnP.Infrastructure
{
  /// <summary>
  /// Contains global consts of the UPnP system.
  /// </summary>
  public class UPnPConsts
  {
    /// <summary>
    /// Time in seconds until UPnP advertisments will expire.
    /// </summary>
    public const int DEFAULT_ADVERTISEMENT_EXPIRATION_TIME = 1800;

    /// <summary>
    /// Maximum random time in milliseconds to wait until an initial advertisement of all devices is made.
    /// </summary>
    public static int INITIAL_ADVERTISEMENT_MAX_WAIT_MS = 100;

    /// <summary>
    /// Minimum advertisement interval in seconds.
    /// </summary>
    public static int MIN_ADVERTISEMENT_INTERVAL = 600;

    /// <summary>
    /// Port for SSDP multicast messages.
    /// </summary>
    public const int SSDP_MULTICAST_PORT = 1900;

    /// <summary>
    /// Port for GENA multicast messages.
    /// </summary>
    public static int GENA_MULTICAST_PORT = 7900;

    /// <summary>
    /// Default timeout for GENA event subscriptions in seconds.
    /// </summary>
    public static int GENA_DEFAULT_SUBSCRIPTION_TIMEOUT = 600;

    /// <summary>
    /// Multicast address for GENA multicast sendings for IPv4.
    /// </summary>
    public static IPAddress GENA_MULTICAST_ADDRESS_V4 = new IPAddress(new byte[] {239, 255, 255, 246});

    /// <summary>
    /// Multicast address for GENA multicast sendings for IPv6 (node-local scope).
    /// </summary>
    public static IPAddress GENA_MULTICAST_ADDRESS_V6_NODE_LOCAL = IPAddress.Parse("FF01::130");

    /// <summary>
    /// Multicast address for GENA multicast sendings for IPv6 (link-local scope).
    /// </summary>
    public static IPAddress GENA_MULTICAST_ADDRESS_V6_LINK_LOCAL = IPAddress.Parse("FF02::130");

    /// <summary>
    /// Multicast address for GENA multicast sendings for IPv6 (site-local scope).
    /// </summary>
    public static IPAddress GENA_MULTICAST_ADDRESS_V6_SITE_LOCAL = IPAddress.Parse("FF05::130");

    /// <summary>
    /// Multicast address for GENA multicast sendings for IPv6 (global scope).
    /// </summary>
    public static IPAddress GENA_MULTICAST_ADDRESS_V6_GLOBAL = IPAddress.Parse("FF0E::130");

    /// <summary>
    /// Maximum number of variables which are evented in a single multicast (UDP) message.
    /// </summary>
    public const int GENA_MAX_MULTICAST_EVENT_VAR_COUNT = 5;

    /// <summary>
    /// Default port for the SSDP UDP socket which receives unicast requests.
    /// </summary>
    public static int DEFAULT_SSDP_SEARCH_PORT = 1900;

    /// <summary>
    /// Default time-to-live for SSDP and GENA UDP multicast packets for IPv4.
    /// </summary>
    public static short DEFAULT_UDP_TTL_V4 = 2;

    /// <summary>
    /// Default hop limit for SSDP and GENA UDP multicast packets for IPv6.
    /// </summary>
    public static short DEFAULT_HOP_LIMIT_V6 = 254;

    /// <summary>
    /// Multicast address for SSDP multicast sendings for IPv4.
    /// </summary>
    public static IPAddress SSDP_MULTICAST_ADDRESS_V4 = new IPAddress(new byte[] {239, 255, 255, 250});

    /// <summary>
    /// Multicast address for SSDP multicast sendings for IPv6 (node-local scope).
    /// </summary>
    public static IPAddress SSDP_MULTICAST_ADDRESS_V6_NODE_LOCAL = IPAddress.Parse("FF01::C");

    /// <summary>
    /// Multicast address for SSDP multicast sendings for IPv6 (link-local scope).
    /// </summary>
    public static IPAddress SSDP_MULTICAST_ADDRESS_V6_LINK_LOCAL = IPAddress.Parse("FF02::C");

    /// <summary>
    /// Multicast address for SSDP multicast sendings for IPv6 (site-local scope).
    /// </summary>
    public static IPAddress SSDP_MULTICAST_ADDRESS_V6_SITE_LOCAL = IPAddress.Parse("FF05::C");

    /// <summary>
    /// Multicast address for SSDP multicast sendings for IPv6 (global scope).
    /// </summary>
    public static IPAddress SSDP_MULTICAST_ADDRESS_V6_GLOBAL = IPAddress.Parse("FF0E::C");

    /// <summary>
    /// Receive buffer size for the UDP socket for the SSDP protocol handlers.
    /// </summary>
    public static int UDP_SSDP_RECEIVE_BUFFER_SIZE = 4096;

    /// <summary>
    /// Receive buffer size for the UDP socket for the GENA event notifications. Must be quite big to hold a complete
    /// event notification (unfortunately it is not specified how big it should be).
    /// </summary>
    public static int UDP_GENA_RECEIVE_BUFFER_SIZE = 16384;

    /// <summary>
    /// Factor to calculate the actual <see cref="Socket.ReceiveBufferSize"/> used for receiving UDP packets. The
    /// resulting size will be buffer size * <see cref="UDP_RECEIVE_BUFFER_FACTOR"/>.
    /// </summary>
    public static int UDP_RECEIVE_BUFFER_FACTOR = 10;

    /// <summary>
    /// Denotes the "infinite" timespan, used for <see cref="System.Threading.Timer.Change(System.TimeSpan,System.TimeSpan)"/>
    /// method, for example.
    /// </summary>
    public static readonly TimeSpan INFINITE_TIMESPAN = new TimeSpan(0, 0, 0, 0, -1);

    /// <summary>
    /// If deadlocks happen, don't wait for locks infinitely in periodic timer events.
    /// </summary>
    public static TimeSpan TIMEOUT_TIMER_LOCK_ACCESS = TimeSpan.FromSeconds(30);

    #region Mutlicast event levels

    /// <summary>
    /// Multicast event level. The event carries critical information that the device SHOULD act upon immediately.
    /// </summary>
    public const string MEL_UPNP_EMERGENCY = "upnp:/emergency";

    /// <summary>
    /// Multicast Event level. The event carries information related to an error case.
    /// </summary>
    public const string MEL_UPNP_FAULT = "upnp:/fault";

    /// <summary>
    /// Multicast Event level. The event carries information that is a non-critical condition that the device MAY
    /// want to process or pass to the user.
    /// </summary>
    public const string MEL_UPNP_WARNING = "upnp:/warning";

    /// <summary>
    /// Multicast Event level. The event carries information about the normal operation of the device that may be of interest to
    /// end-users. This information is simply informative and does not indicate any abnormal condition or status such as a
    /// warning or fault. Other event levels are defined for those purposes.
    /// </summary>
    public const string MEL_UPNP_INFO = "upnp:/info";

    /// <summary>
    /// Multicast Event level. The event carries debug information typically used by programmers and test engineers to evaluate
    /// the internal operation of the device. This information is typically not displayed to end users.
    /// </summary>
    public const string MEL_UPNP_DEBUG = "upnp:/debug";

    /// <summary>
    /// Multicast Event level. For events that fit into no other defined category.
    /// </summary>
    public const string MEL_UPNP_GENERAL = "upnp:/general";

    #endregion

    public const uint UPNP_VERSION_MAJOR = 1;
    public const uint UPNP_VERSION_MINOR = 1;

    #region Namespace URIs

    /// <summary>
    /// XML namespace to be used for the SOAP envelope.
    /// </summary>
    public const string NS_SOAP_ENVELOPE = "http://schemas.xmlsoap.org/soap/envelope/";

    /// <summary>
    /// XML namespace to be used for the SOAP encoding.
    /// </summary>
    public const string NS_SOAP_ENCODING = "http://schemas.xmlsoap.org/soap/encoding/";

    /// <summary>
    /// XML namespace for the UPnP control schema.
    /// </summary>
    public const string NS_UPNP_CONTROL = "urn:schemas-upnp-org:control-1-0";

    /// <summary>
    /// XML namespace for the UPnP device description.
    /// </summary>
    public const string NS_DEVICE_DESCRIPTION = "urn:schemas-upnp-org:device-1-0";

    /// <summary>
    /// XML namespace for the UPnP service description.
    /// </summary>
    public const string NS_SERVICE_DESCRIPTION = "urn:schemas-upnp-org:service-1-0";

    /// <summary>
    /// XML namespace for UPnP GENA event notifications.
    /// </summary>
    public const string NS_UPNP_EVENT = "urn:schemas-upnp-org:event-1-0";

    /// <summary>
    /// XML namespace for the XSI scheme.
    /// </summary>
    public const string NS_XSI = "http://www.w3.org/2001/XMLSchema-instance";

    #endregion

    public static Encoding UTF8_NO_BOM = new UTF8Encoding(false);
  }
}
