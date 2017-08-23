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

using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FreeGeoIP.Data;
using System.Device.Location;
using System.Net;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.FreeGeoIP
{
  /// <summary>
  /// FreeGeoIP Library.
  /// </summary>
  public class IPLookup : IAddressResolver, ICoordinateResolver
  {
    #region Ctor

    public IPLookup()
    {
    }

    #endregion Ctor

    #region Private methods

    private string BuildUrl(IPAddress address)
    {
      return string.Format("http://freegeoip.net/json/{0}", address);
    }

    private bool TryLookupInternal(out CivicAddress address, out GeoCoordinate coordinates)
    {
      address = null;
      coordinates = null;
      IPAddress ip;
      return ExternalIPResolver.GetExternalIPAddress(out ip) && TryLookupInternal(ip, out address, out coordinates);
    }

    private bool TryLookupInternal(IPAddress ip, out CivicAddress address, out GeoCoordinate coordinates)
    {
      var downloader = new Downloader { EnableCompression = true };
      downloader.Headers["Accept"] = "application/json";
      downloader.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:17.0) Gecko/20100101 Firefox/17.0";
      FreeGeoIPResponse result = downloader.Download<FreeGeoIPResponse>(BuildUrl(ip));

      if (result == null)
      {
        address = null;
        coordinates = null;
        return false;
      }

      address = result.ToCivicAddress();
      coordinates = result.ToGeoCoordinates();

      return true;
    }

    #endregion Private methods

    #region IAddressResolver implementation

    /// <summary>
    /// Determine the address at the specified coordinates.
    /// </summary>
    /// <param name="coordinates">Coordinates to lookup.</param>
    /// <param name="address">Resultant address.</param>
    /// <returns>If lookup is successful.</returns>
    public bool TryResolveCivicAddress(GeoCoordinate coordinates, out CivicAddress address)
    {
      GeoCoordinate temp;
      return TryLookupInternal(out address, out temp);
    }

    #endregion IAddressResolver implementation

    #region ICoordinateResolver implementation

    /// <summary>
    /// Determine the current location of the device.
    /// </summary>
    /// <param name="coordinates">Coordinates of the device.</param>
    /// <returns>If lookup is successful.</returns>
    public bool TryResolveCoordinates(out GeoCoordinate coordinates)
    {
      CivicAddress temp;
      return TryLookupInternal(out temp, out coordinates);
    }

    #endregion ICoordinateResolver implementation
  }
}
