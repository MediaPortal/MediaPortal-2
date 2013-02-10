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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation.Data
{
  [DataContract]
  class GoogleResults
  {
    [DataMember(Name = "results")]
    public List<GoogleResult> Results { get; set; }
  }

  [DataContract]
  class GoogleResult
  {
    [DataMember(Name = "address_components")]
    public List<AddressComponent> AddressComponents { get; set; }

    public LocationInfo ToLocation ()
    {
      LocationInfo result = new LocationInfo();
      var city = GetAddressComponent("locality");
      if (city != null)
        result.City = city.LongName;
      var state = GetAddressComponent("administrative_area_level_1");
      if (state != null)
        result.State = state.LongName;
      var country = GetAddressComponent("country");
      if (country != null)
        result.Country = country.LongName;
      return result;
    }

    public AddressComponent GetAddressComponent(string type)
    {
      return AddressComponents.FirstOrDefault(addressComponent => addressComponent.Types != null && ((IList) addressComponent.Types).Contains(type));
    }
  }

  [DataContract]
  class AddressComponent
  {
    [DataMember(Name = "long_name")]
    public string LongName { get; set; }
    [DataMember(Name = "short_name")]
    public string ShortName { get; set; }
    [DataMember(Name = "types")]
    public string[] Types { get; set; }
    
    public override string ToString()
    {
      return LongName;
    }
  }
}
