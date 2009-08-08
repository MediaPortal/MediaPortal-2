using System;
using System.Net;

namespace UPnP.Infrastructure
{
  /// <summary>
  /// Contains global consts of the UPnP system.
  /// </summary>
  public class Consts
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
  }
}
