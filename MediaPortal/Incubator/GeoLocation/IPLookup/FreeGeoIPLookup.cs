#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

#endregion Copyright (C) 2007-2013 Team MediaPortal

#region Imports

using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Extensions.GeoLocation.IPLookup.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation.Data;
using System.Net;

#endregion Imports

namespace MediaPortal.Extensions.GeoLocation.IPLookup
{
  internal class FreeGeoIPLookup
  {
    #region Private methods

    private string BuildUrl(IPAddress address)
    {
      var mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture.TwoLetterISOLanguageName;
      return string.Format("http://freegeoip.net/json/{0}", address.ToString());
    }

    #endregion Private methods

    #region Internal methods

    internal bool TryLookup(IPAddress address, out LocationInfo locationInfo)
    {
      var downloader = new Downloader { EnableCompression = true };
      downloader.Headers["Accept"] = "application/json";
      downloader.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:17.0) Gecko/20100101 Firefox/17.0";
      FreeGeoIPResponse result = downloader.Download<FreeGeoIPResponse>(BuildUrl(address));

      if (result == null)
      {
        locationInfo = null;
        return false;
      }

      locationInfo = new LocationInfo()
        {
          City = result.City,
          Country = result.CountryName,
          Latitude = (double)result.Latitude,
          Longitude = (double)result.Longitude,
          State = result.RegionName
        };
      return true;
    }

    #endregion Internal methods
  }
}