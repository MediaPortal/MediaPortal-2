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

using System.Collections;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Google.Data
{
  [DataContract]
  public class MapsApiGeocodeAddress
  {
    #region Public properties

    [DataMember(Name = "long_name")]
    public string LongName { get; set; }

    [DataMember(Name = "short_name")]
    public string ShortName { get; set; }

    [DataMember(Name = "types")]
    public string[] Types { get; set; }

    #endregion Public properties

    #region Public methods

    public override string ToString()
    {
      return LongName;
    }

    #endregion Public methods
  }

  [DataContract]
  public class MapsApiGeocodeResponse
  {
    #region Public properties

    [DataMember(Name = "results")]
    public List<MapsApiGeocodeResult> Results { get; set; }

    #endregion Public properties
  }

  [DataContract]
  public class MapsApiGeocodeResult
  {
    #region Public properties

    [DataMember(Name = "address_components")]
    public List<MapsApiGeocodeAddress> AddressComponents { get; set; }

    #endregion Public properties

    #region Public methods

    public MapsApiGeocodeAddress GetAddressComponent(string type)
    {
      return AddressComponents.FirstOrDefault(addressComponent => addressComponent.Types != null && ((IList)addressComponent.Types).Contains(type));
    }

    public CivicAddress ToCivicAddress()
    {
      CivicAddress result = new CivicAddress();
      var city = GetAddressComponent("locality");
      if (city != null)
        result.City = city.LongName;
      var state = GetAddressComponent("administrative_area_level_1");
      if (state != null)
        result.StateProvince = state.LongName;
      var country = GetAddressComponent("country");
      if (country != null)
        result.CountryRegion = country.LongName;
      return result;
    }

    #endregion Public methods
  }
}