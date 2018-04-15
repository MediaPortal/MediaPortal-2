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

using System.Device.Location;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.FreeGeoIP.Data
{
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
  [System.SerializableAttribute]
  [System.Diagnostics.DebuggerStepThroughAttribute]
  [System.ComponentModel.DesignerCategoryAttribute(@"code")]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
  [System.Xml.Serialization.XmlRootAttribute(ElementName = "Response", Namespace = "", IsNullable = true)]
  [DataContract]
  public class FreeGeoIPResponse
  {
    #region Public properties

    /// <remarks/>
    [DataMember(Name = "areacode")]
    public object AreaCode { get; set; }

    /// <remarks/>
    [DataMember(Name = "city")]
    public string City { get; set; }

    /// <remarks/>
    [DataMember(Name = "country_code")]
    public string CountryCode { get; set; }

    /// <remarks/>
    [DataMember(Name = "country_name")]
    public string CountryName { get; set; }

    /// <remarks/>
    [DataMember(Name = "ip")]
    public string Ip { get; set; }

    /// <remarks/>
    [DataMember(Name = "latitude")]
    public decimal Latitude { get; set; }

    /// <remarks/>
    [DataMember(Name = "longitude")]
    public decimal Longitude { get; set; }

    /// <remarks/>
    [DataMember(Name = "metro_code")]
    public object MetroCode { get; set; }

    /// <remarks/>
    [DataMember(Name = "region_code")]
    public string RegionCode { get; set; }

    /// <remarks/>
    [DataMember(Name = "region_name")]
    public string RegionName { get; set; }

    /// <remarks/>
    [DataMember(Name = "zipcode")]
    public object ZipCode { get; set; }

    #endregion Public properties

    #region Public methods

    public CivicAddress ToCivicAddress()
    {
      CivicAddress address = new CivicAddress();

      address.CountryRegion = CountryName;
      address.StateProvince = RegionName;
      address.City = City;
      if (ZipCode != null)
        address.PostalCode = ZipCode.ToString();

      return address;
    }

    public GeoCoordinate ToGeoCoordinates()
    {
      GeoCoordinate coordinates = new GeoCoordinate();

      coordinates.Latitude = (double)Latitude;
      coordinates.Longitude = (double)Longitude;

      return coordinates;
    }

    #endregion Public methods
  }
}