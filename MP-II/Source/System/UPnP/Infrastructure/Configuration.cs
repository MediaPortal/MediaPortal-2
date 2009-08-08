using MediaPortal.Utilities.SystemAPI;

namespace UPnP.Infrastructure
{
  /// <summary>
  /// Configuration class for global settings of the UPnP subsystem.
  /// </summary>
  /// <remarks>
  /// Configure the settings in this class before starting the UPnP system.
  /// </remarks>
  public class Configuration
  {
    #region Configuration settings

    /// <summary>
    /// Seconds a search response may be delayed. MUST be >=1 and SHOULD be <=5. This setting is relevant for the
    /// UPnP control point.
    /// </summary>
    public static int SEARCH_MX = 1;

    /// <summary>
    /// Default time-to-live for UDP notification messages for IPv4. This setting is relevant for the UPnP server.
    /// </summary>
    public static short DEFAULT_SSDP_UDP_TTL_V4 = 2;

    /// <summary>
    /// Default time-to-live for UDP notification messages for IPv4. This setting is relevant for the UPnP server.
    /// </summary>
    public static short DEFAULT_GENA_UDP_TTL_V4 = DEFAULT_SSDP_UDP_TTL_V4;

    /// <summary>
    /// Product/version of the system which is represented by this SSDP handler. Must be set from the application to match the
    /// product to be represented. This setting is relevant for both the UPnP control point and UPnP server.
    /// </summary>
    public static string PRODUCT_VERSION = "UPnP_server/0.1";

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
