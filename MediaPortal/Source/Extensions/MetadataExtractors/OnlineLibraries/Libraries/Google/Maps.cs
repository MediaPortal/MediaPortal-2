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

using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Google.Data;
using System.Device.Location;
using System.Globalization;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Google
{
  /// <summary>
  /// Google maps library.
  /// </summary>
  public class Maps : IAddressResolver
  {
    #region Ctor

    public Maps()
    {
    }

    #endregion Ctor

    #region Private methods

    private string BuildUrl(double latitude, double longitude)
    {
      var mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture.TwoLetterISOLanguageName;
      return string.Format("http://maps.googleapis.com/maps/api/geocode/json?latlng={0},{1}&sensor=false&language={2}",
          latitude.ToString(CultureInfo.InvariantCulture),
          longitude.ToString(CultureInfo.InvariantCulture),
          mpLocal);
    }

    #endregion Private methods

    #region IAddressResolver implementation

    /// <summary>
    /// Determine the address of the given coordinates.
    /// </summary>
    /// <param name="coordinates">Coordinates to lookup.</param>
    /// <returns>Address corresponding to the coordinates or <c>null</c>.</returns>
    public async Task<CivicAddress> TryResolveCivicAddressAsync(GeoCoordinate coordinates)
    {
      var downloader = new Downloader { EnableCompression = true };
      downloader.Headers["Accept"] = "application/json";
      // Google enables compressed output only, if a valid User-Agent is sent!
      downloader.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:17.0) Gecko/20100101 Firefox/17.0";
      MapsApiGeocodeResponse results = await downloader.DownloadAsync<MapsApiGeocodeResponse>(BuildUrl(coordinates.Latitude, coordinates.Longitude)).ConfigureAwait(false);
      if (results == null || results.Results == null || results.Results.Count == 0)
      {
        return null;
      }
      return results.Results[0].ToCivicAddress();
    }

    #endregion IAddressResolver implementation
  }
}
