#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Xml;
using MediaPortal.Utilities.SystemAPI;

namespace UPnP.Infrastructure
{
  /// <summary>
  /// Configuration class for global settings of the UPnP subsystem.
  /// </summary>
  /// <remarks>
  /// Configure the settings in this class before starting the UPnP system.
  /// </remarks>
  public class UPnPConfiguration
  {
    #region Configuration settings

    /// <summary>
    /// Controls if devices and control points use the IPv4 protocol. As specified by DevArch, Appendix A.2, the IPv4
    /// protocol MUST be supported, so this flag must be set to <c>true</c> for productive usage.
    /// </summary>
    public static bool USE_IPV4 = true;

    /// <summary>
    /// Controls if devices and control points use the IPv6 protocol.
    /// </summary>
    public static bool USE_IPV6 = true;

    /// <summary>
    /// Controls the UPnP advertisement and event sending scope. If set to <c>true</c>, the biggest scope is the site-local
    /// scope. If set to <c>false</c>, advertisements and event messages are sent in global scope.
    /// </summary>
    public static bool SITE_LOCAL_OPERATION = true;

    /// <summary>
    /// Denotes that a search response may be delayed. MUST be &gt;=1 and SHOULD be &lt;=5. This setting is relevant for the
    /// UPnP control point.
    /// </summary>
    public static int SEARCH_MX = 1;

    /// <summary>
    /// Time-to-live setting for UDP SSDP notification messages for IPv4. This setting is relevant for the UPnP server.
    /// </summary>
    public static short SSDP_UDP_TTL_V4 = UPnPConsts.DEFAULT_UDP_TTL_V4;

    /// <summary>
    /// Hop limit setting for the UDP SSDP notification messages for IPv6. This setting is relevant for the UPnP server.
    /// </summary>
    public static short SSDP_UDP_HOP_LIMIT_V6 = UPnPConsts.DEFAULT_HOP_LIMIT_V6;

    /// <summary>
    /// Time-to-live for UDP GENA notification messages for IPv4. This setting is relevant for the UPnP server.
    /// </summary>
    public static short GENA_UDP_TTL_V4 = SSDP_UDP_TTL_V4;

    /// <summary>
    /// Hop limit setting for the UDP GENA notification messages for IPv6. This setting is relevant for the UPnP server.
    /// </summary>
    public static short GENA_UDP_HOP_LIMIT_V6 = UPnPConsts.DEFAULT_HOP_LIMIT_V6;

    /// <summary>
    /// Product/version of the system which is represented by this SSDP handler. Must be set from the application to match the
    /// product to be represented. This setting is relevant for both the UPnP control point and UPnP server.
    /// </summary>
    public static string PRODUCT_VERSION = "UPnP_server/0.1";

    /// <summary>
    /// Denotes that <c>USER-AGENT</c> strings should be parsed strictly (<c>LAX_USER_AGENT_PARSING == false</c>) or lax.
    /// Many devices, especially smartphones, send a malformed <c>USER-AGENT</c> header which doesn't comply with the UPnP specification.
    /// </summary>
    public static bool LAX_USER_AGENT_PARSING = true;

    /// <summary>
    /// Logger instance used in all future calls of the UPnP system. Must not be <c>null</c>!
    /// </summary>
    public static ILogger LOGGER = new ConsoleLogger();

    /// <summary>
    /// Default settins to be used by XML writers.
    /// </summary>
    public static XmlWriterSettings DEFAULT_XML_WRITER_SETTINGS = new XmlWriterSettings
      {
          CheckCharacters = false,
          Encoding = UPnPConsts.UTF8_NO_BOM,
          Indent = false
      };

    /// <summary>
    /// Default settins to be used by XML readers.
    /// </summary>
    public static XmlReaderSettings DEFAULT_XML_READER_SETTINGS = new XmlReaderSettings
      {
          CheckCharacters = false,
          IgnoreComments = true,
          IgnoreWhitespace = true
      };

    #endregion

    protected static string _serverOsVersion = null; // Lazily initialized

    /// <summary>
    /// Returns the SERVER header (for UPnP devices) resp. the USER-AGENT header (for UPnP control points) content for
    /// UPnP SSDP discovery messages.
    /// </summary>
    internal static string UPnPMachineInfoHeader
    {
      get
      {
        if (_serverOsVersion == null)
          _serverOsVersion = WindowsAPI.GetOsVersionString();
        return _serverOsVersion + " UPnP/1.1 " + PRODUCT_VERSION;
      }
    }
  }
}
