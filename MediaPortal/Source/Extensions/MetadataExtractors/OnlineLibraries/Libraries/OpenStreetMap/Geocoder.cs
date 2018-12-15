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
using MediaPortal.Extensions.OnlineLibraries.Libraries.OpenStreetMap.Data;
using System.Device.Location;
using System.Globalization;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.OpenStreetMap
{
  /// <summary>
  /// OpenStreetMap library.
  /// </summary>
  public class Geocoder : IAddressResolver
  {
    #region Ctor

    public Geocoder()
    {
    }

    #endregion Ctor

    #region Private methods

    private string BuildUrl(double latitude, double longitude)
    {
      var mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture.TwoLetterISOLanguageName;
      return string.Format("http://nominatim.openstreetmap.org/reverse?format=json&lat={0}&lon={1}&zoom=10&accept-language={2}",
          latitude.ToString(CultureInfo.InvariantCulture),
          longitude.ToString(CultureInfo.InvariantCulture),
          mpLocal);
    }

    #endregion Private methods

    #region IAddressResolver implementation

    /// <summary>
    /// Lookup the address at the given coordinates.
    /// </summary>
    /// <param name="coordinates">Coordinates to lookup.</param>
    /// <returns>Address corresponding to the coordinates or <c>null</c>.</returns>
    public async Task<CivicAddress> TryResolveCivicAddressAsync(GeoCoordinate coordinates)
    {
      var downloader = new Downloader { EnableCompression = true };
      GeocoderResponse result = await downloader.DownloadAsync<GeocoderResponse>(BuildUrl(coordinates.Latitude, coordinates.Longitude)).ConfigureAwait(false);
      return result?.ToCivicAddress();
    }

    #endregion IAddressResolver implementation
  }
}
