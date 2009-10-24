#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
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
using System.Net;

namespace UPnP.Infrastructure
{
  /// <summary>
  /// Contains global consts of the UPnP system.
  /// </summary>
  public class UPnPConsts
  {
    /// <summary>
    /// Port for SSDP multicast messages.
    /// </summary>
    public const int SSDP_MULTICAST_PORT = 1900;

    /// <summary>
    /// Port for GENA multicast messages.
    /// </summary>
    public static int GENA_MULTICAST_PORT = 7900;

    /// <summary>
    /// Default port for the SSDP UDP socket which receives unicast requests.
    /// </summary>
    public static int DEFAULT_SSDP_SEARCH_PORT = 1900;

    /// <summary>
    /// Multicast address for SSDP multicast sendings for IPv4.
    /// </summary>
    public static IPAddress SSDP_MULTICAST_ADDRESS_V4 = new IPAddress(new byte[] {239, 255, 255, 250});

    /// <summary>
    /// Multicast address for SSDP multicast sendings for IPv6.
    /// </summary>
    public static IPAddress SSDP_MULTICAST_ADDRESS_V6 = IPAddress.Parse("FF02::C");

    /// <summary>
    /// Denotes the "infinite" timespan, used for <see cref="System.Threading.Timer.Change(System.TimeSpan,System.TimeSpan)"/>
    /// method, for example.
    /// </summary>
    public static TimeSpan INFINITE_TIMESPAN = new TimeSpan(0, 0, 0, 0, -1);

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
  }
}
