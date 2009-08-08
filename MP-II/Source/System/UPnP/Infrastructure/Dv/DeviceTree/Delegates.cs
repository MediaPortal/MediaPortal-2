using System.Globalization;
using System.Net;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Delegate which can return a URL which is available over the specified <paramref name="endPointIPAddress"/>.
  /// The URL may be localized for the given <paramref name="culture"/>.
  /// </summary>
  /// <param name="endPointIPAddress">IP address where the returned url must be reachable.</param>
  /// <param name="culture">The culture to localize the returned URL.</param>
  /// <returns>URL to an external resource. The URL/resource is not managed by the UPnP subsystem.</returns>
  public delegate string GetURLForEndpointDlgt(IPAddress endPointIPAddress, CultureInfo culture);
}
