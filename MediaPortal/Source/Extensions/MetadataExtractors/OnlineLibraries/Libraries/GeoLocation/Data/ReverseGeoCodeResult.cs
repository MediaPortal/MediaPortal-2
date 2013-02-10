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

#endregion

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation.Data
{
  [DataContract]
  public class ReverseGeoCodeResult
  {
    const string KEY_CITY = "city";
    const string KEY_STATE = "state";
    const string KEY_COUNTRY = "country";

    [DataMember(Name = "place_id")]
    public int PlaceId { get; set; }

    [DataMember(Name = "lat")]
    public double Latitude { get; set; }

    [DataMember(Name = "lon")]
    public double Longitude { get; set; }

    [DataMember(Name = "address")]
    public Dictionary<string, string> AddressInfo { get; internal set; }

    public ReverseGeoCodeResult()
    {
      AddressInfo = new Dictionary<string, string>();
    }

    public LocationInfo ToLocation()
    {
      var loc = new LocationInfo { Latitude = Latitude, Longitude = Longitude };
      if (AddressInfo.ContainsKey(KEY_CITY))
        loc.City = AddressInfo[KEY_CITY];
      if (AddressInfo.ContainsKey(KEY_STATE))
        loc.State = AddressInfo[KEY_STATE];
      if (AddressInfo.ContainsKey(KEY_COUNTRY))
        loc.Country = AddressInfo[KEY_COUNTRY];
      return loc;
    }
  }
}