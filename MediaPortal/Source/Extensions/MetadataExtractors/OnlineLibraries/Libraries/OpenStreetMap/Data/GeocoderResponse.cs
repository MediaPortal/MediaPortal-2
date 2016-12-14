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

using System.Collections.Generic;
using System.Device.Location;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.OpenStreetMap.Data
{
  [DataContract]
  public class GeocoderResponse
  {
    #region Consts

    private const string KEY_CITY = "city";
    private const string KEY_COUNTRY = "country";
    private const string KEY_STATE = "state";

    #endregion Consts

    #region Public properties

    [DataMember(Name = "address")]
    public Dictionary<string, string> AddressInfo { get; internal set; }

    [DataMember(Name = "lat")]
    public double Latitude { get; set; }

    [DataMember(Name = "lon")]
    public double Longitude { get; set; }

    [DataMember(Name = "place_id")]
    public string PlaceId { get; set; }

    #endregion Public properties

    #region Ctor

    public GeocoderResponse()
    {
      AddressInfo = new Dictionary<string, string>();
    }

    #endregion Ctor

    #region Public methods

    public CivicAddress ToCivicAddress()
    {
      CivicAddress result = new CivicAddress();
      if (AddressInfo.ContainsKey(KEY_CITY))
        result.City = AddressInfo[KEY_CITY];
      if (AddressInfo.ContainsKey(KEY_STATE))
        result.StateProvince = AddressInfo[KEY_STATE];
      if (AddressInfo.ContainsKey(KEY_COUNTRY))
        result.CountryRegion = AddressInfo[KEY_COUNTRY];

      return result;
    }

    #endregion Public methods
  }
}