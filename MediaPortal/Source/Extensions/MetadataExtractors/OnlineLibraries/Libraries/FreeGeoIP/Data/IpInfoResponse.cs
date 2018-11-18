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

using System.Device.Location;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.FreeGeoIP.Data
{
  public class IpInfoResponse
  {
    public string Latitude
    {
      get { return Location?.Split(',').FirstOrDefault(); }
    }
    public string Longitude
    {
      get { return Location?.Split(',').LastOrDefault(); }
    }

    [JsonProperty("ip")]
    public string Ip { get; set; }
    [JsonProperty("city")]
    public string City { get; set; }
    [JsonProperty("region")]
    public string Region { get; set; }
    [JsonProperty("country")]
    public string Country { get; set; }
    [JsonProperty("loc")]
    public string Location { get; set; } // loc	"50.8197,12.9403"
    [JsonProperty("postal")]
    public string Postal { get; set; }


    public CivicAddress ToCivicAddress()
    {
      CivicAddress address = new CivicAddress
      {
        CountryRegion = Country,
        StateProvince = Region,
        City = City,
        PostalCode = Postal
      };

      return address;
    }

    public GeoCoordinate ToGeoCoordinates()
    {
      GeoCoordinate coordinates = new GeoCoordinate();

      if (double.TryParse(Latitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
          double.TryParse(Longitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
      {
        coordinates.Latitude = lat;
        coordinates.Longitude = lon;
      }
      return coordinates;
    }

  }
}
