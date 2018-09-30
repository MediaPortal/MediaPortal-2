#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FreeGeoIP.Data;
using System.Device.Location;
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.Services.ServerCommunication;

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
      return string.Format("http://ipinfo.io/json/?ip={0}&token=ee8866dc3d53c1", address);
    }

    private async Task<AsyncResult<Tuple<CivicAddress, GeoCoordinate>>> TryLookupInternal()
    {
      var ipResult = await ExternalIPResolver.GetExternalIPAddressAsync().ConfigureAwait(false);
      if (!ipResult.Success)
        return new AsyncResult<Tuple<CivicAddress, GeoCoordinate>>(false, null);

      return await TryLookupInternal(ipResult.Result).ConfigureAwait(false);
    }

    private async Task<AsyncResult<Tuple<CivicAddress, GeoCoordinate>>> TryLookupInternal(IPAddress ip)
    {
      var downloader = new Downloader { EnableCompression = true };
      downloader.Headers["Accept"] = "application/json";
      downloader.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:17.0) Gecko/20100101 Firefox/17.0";
      IpInfoResponse result = await downloader.DownloadAsync<IpInfoResponse>(BuildUrl(ip)).ConfigureAwait(false);

      bool success;
      CivicAddress address;
      GeoCoordinate coordinates;
      if (result == null)
      {
        address = null;
        coordinates = null;
        success = false;
      }
      else
      {
        address = result.ToCivicAddress();
        coordinates = result.ToGeoCoordinates();
        success = true;
      }
      return new AsyncResult<Tuple<CivicAddress, GeoCoordinate>>(success, new Tuple<CivicAddress, GeoCoordinate>(address, coordinates));
    }

    #endregion Private methods

    #region IAddressResolver implementation

    /// <summary>
    /// Determine the address at the specified coordinates.
    /// </summary>
    /// <param name="coordinates">Coordinates to lookup.</param>
    /// <returns>Address corresponding to the coordinates or <c>null</c>.</returns>
    public async Task<CivicAddress> TryResolveCivicAddressAsync(GeoCoordinate coordinates)
    {
      var result = await TryLookupInternal().ConfigureAwait(false);
      if (result.Success)
        return result.Result.Item1;
      return null;
    }

    #endregion IAddressResolver implementation

    #region ICoordinateResolver implementation

    /// <summary>
    /// Determine the current location of the device.
    /// </summary>
    /// <returns>Coordinates of the device if successful.</returns>
    public async Task<GeoCoordinate> TryResolveCoordinatesAsync()
    {
      var result = await TryLookupInternal().ConfigureAwait(false);
      if (result.Success)
        return result.Result.Item2;
      return null;
    }

    #endregion ICoordinateResolver implementation
  }
}
