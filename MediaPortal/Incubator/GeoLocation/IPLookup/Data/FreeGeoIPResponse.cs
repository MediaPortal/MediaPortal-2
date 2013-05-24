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

using System.Runtime.Serialization;

#endregion

namespace MediaPortal.Extensions.GeoLocation.IPLookup.Data
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
    #region Private variables

    private string _ipField;
    private string _countryCodeField;
    private string _countryNameField;
    private byte _regionCodeField;
    private string _regionNameField;
    private string _cityField;
    private object _zipCodeField;
    private decimal _latitudeField;
    private decimal _longitudeField;
    private object _metroCodeField;
    private object _areaCodeField;

    #endregion Private variables

    #region Public properties

    /// <remarks/>
    [DataMember(Name = "ip")]
    public string Ip
    {
      get
      {
        return _ipField;
      }
      set
      {
        _ipField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "country_code")]
    public string CountryCode
    {
      get
      {
        return _countryCodeField;
      }
      set
      {
        _countryCodeField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "country_name")]
    public string CountryName
    {
      get
      {
        return _countryNameField;
      }
      set
      {
        _countryNameField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "region_code")]
    public byte RegionCode
    {
      get
      {
        return _regionCodeField;
      }
      set
      {
        _regionCodeField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "region_name")]
    public string RegionName
    {
      get
      {
        return _regionNameField;
      }
      set
      {
        _regionNameField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "city")]
    public string City
    {
      get
      {
        return _cityField;
      }
      set
      {
        _cityField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "zipcode")]
    public object ZipCode
    {
      get
      {
        return _zipCodeField;
      }
      set
      {
        _zipCodeField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "latitude")]
    public decimal Latitude
    {
      get
      {
        return _latitudeField;
      }
      set
      {
        _latitudeField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "longitude")]
    public decimal Longitude
    {
      get
      {
        return _longitudeField;
      }
      set
      {
        _longitudeField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "metro_code")]
    public object MetroCode
    {
      get
      {
        return _metroCodeField;
      }
      set
      {
        _metroCodeField = value;
      }
    }

    /// <remarks/>
    [DataMember(Name = "areacode")]
    public object AreaCode
    {
      get
      {
        return _areaCodeField;
      }
      set
      {
        _areaCodeField = value;
      }
    }

    #endregion Public properties
  }
}