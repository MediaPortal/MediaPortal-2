using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnP.Infrastructure.Dv.SSDP
{
  /// <summary>
  /// Callback interface for the <see cref="DeviceTreeNotificationProducer"/>.
  /// </summary>
  /// <remarks>
  /// Implementations of this interface will produce a special kind of notify message for each of the objects in the
  /// device tree.
  /// </remarks>
  internal interface ISSDPDiscoveryMessageSender
  {
    /// <summary>
    /// Callback method which will be called for each of the NT/USN combinations for each device and service
    /// as specified in the UPnP architecture document.
    /// </summary>
    /// <param name="NT">NT parameter for NOTIFY messages, ST for search response.</param>
    /// <param name="USN">USN parameter.</param>
    /// <param name="rootDevice">Root device for that the message should be sent.</param>
    void SendMessage(string NT, string USN, DvDevice rootDevice);
  }
}