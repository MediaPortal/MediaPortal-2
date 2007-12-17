#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.Properties;

namespace MyWeather
{
  /// <summary>
  /// this is a basic field where
  /// the information for the provider
  /// are stored (usually the location name and unique id)
  /// </summary>
  public class CitySetupInfo
  {
    private string id, grabber;
    protected Property _name = new Property("");

    public CitySetupInfo(string name, string id, string grabber)
    {
      Name = name;
      this.id = id;
      this.grabber = grabber;
    }

    public CitySetupInfo(string name, string id)
    {
      Name = name;
      this.id = id;
      grabber = "Weather.com";
    }

    public CitySetupInfo() {}

    /// <summary>
    /// Get the Name of the City
    /// </summary>
    public string Name
    {
      get { return (string) _name.GetValue(); }
      set { _name.SetValue(value); }
    }

    /// <summary>
    /// Get the Location ID
    /// </summary>
    public string Id
    {
      get { return id; }
      set { id = value; }
    }

    /// <summary>
    /// Get the Grabber Service that should
    /// be used to fetch data for this town
    /// </summary>
    public string Grabber
    {
      get { return grabber; }
      set { grabber = value; }
    }

    public override string ToString()
    {
      return Name;
    }
  }

  #region City class

  /// <summary>
  /// holds Information on the City
  /// </summary>
  public class City : CitySetupInfo
  {
    private Property _locationInfo = new Property(new LocInfo());
    private Property _currCondition = new Property(new CurrentCondition());
    public List<DayForeCast> _forecast;
    private bool _updateSuccessful;

    /// <summary>
    /// parameterless constructor
    /// needed for serialization
    /// </summary>
    public City()
    {
      _updateSuccessful = false;
    }

    public City(CitySetupInfo info)
    {
      Name = info.Name;
      Id = info.Id;
      Grabber = info.Grabber;
      _updateSuccessful = false;
    }

    public City(string name, string id)
    {
      Name = name;
      Id = id;
      _updateSuccessful = false;
    }

    #region properties

    public Property NameProperty
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// LocationInfo
    /// </summary>
    public LocInfo LocationInfo
    {
      get { return (LocInfo) _locationInfo.GetValue(); }
      set { _locationInfo.SetValue(value); }
    }

    public Property LocationInfoProperty
    {
      get { return _locationInfo; }
      set { _locationInfo = value; }
    }

    /// <summary>
    /// Current Condition
    /// </summary>
    public CurrentCondition Condition
    {
      get { return (CurrentCondition) _currCondition.GetValue(); }
      set { _currCondition.SetValue(value); }
    }

    public Property ConditionProperty
    {
      get { return _currCondition; }
      set { _currCondition = value; }
    }

    /// <summary>
    /// Current Condition
    /// </summary>
    public List<DayForeCast> Forecast
    {
      get { return _forecast; }
    }

    /// <summary>
    /// Identifies if the update was successful
    /// </summary>
    public bool HasData
    {
      get { return _updateSuccessful; }
      set { _updateSuccessful = value; }
    }

    #endregion
  }

  #endregion

  #region structs

  #region LocInfo struct

  /// <summary>
  /// provides info of the Location
  /// </summary>
  public class LocInfo // <loc>
  {
    public void FillWithDummyData()
    {
      CityCode = "DUMMY002351";
      City = "Dummytown, Dummyland";
      Time = "23:15";
      Lat = "49.02";
      Lon = "12.1";
      SunRise = "7:14 AM";
      SunSet = "5:38 PM";
      Zone = "1";
    }

    private Property _cityCode = new Property(""); // <loc id="GMXX0223">
    private Property _city = new Property(""); // <dnam>Regensburg, Germany</dnam>
    private Property _time = new Property(""); // <tm>1:12 AM</tm>
    private Property _lat = new Property(""); // <lat>49.02</lat>
    private Property _lon = new Property(""); // <lon>12.1</lon>
    private Property _sunRise = new Property(""); // <sunr>7:14 AM</sunr> 
    private Property _sunSet = new Property(""); // <suns>5:38 PM</suns>
    private Property _zone = new Property(""); // <zone>1</zone>
    // Getters
    public string CityCode
    {
      get { return (string) _cityCode.GetValue(); }
      set { _cityCode.SetValue(value); }
    }

    public string City
    {
      get { return (string) _city.GetValue(); }
      set { _city.SetValue(value); }
    }

    public string Time
    {
      get { return (string) _time.GetValue(); }
      set { _time.SetValue(value); }
    }

    public string Lat
    {
      get { return (string) _lat.GetValue(); }
      set { _lat.SetValue(value); }
    }

    public string Lon
    {
      get { return (string) _lon.GetValue(); }
      set { _lon.SetValue(value); }
    }

    public string SunRise
    {
      get { return (string) _sunRise.GetValue(); }
      set { _sunRise.SetValue(value); }
    }

    public string SunSet
    {
      get { return (string) _sunSet.GetValue(); }
      set { _sunSet.SetValue(value); }
    }

    public string Zone
    {
      get { return (string) _zone.GetValue(); }
      set { _zone.SetValue(value); }
    }

    public Property CityCodeProperty
    {
      get { return _cityCode; }
      set { _cityCode = value; }
    }

    public Property CityProperty
    {
      get { return _city; }
      set { _city = value; }
    }

    public Property TimeProperty
    {
      get { return _time; }
      set { _time = value; }
    }

    public Property LatProperty
    {
      get { return _lat; }
      set { _lat = value; }
    }

    public Property LonProperty
    {
      get { return _lon; }
      set { _lon = value; }
    }

    public Property SunRiseProperty
    {
      get { return _sunRise; }
      set { _sunRise = value; }
    }

    public Property SunSetProperty
    {
      get { return _sunSet; }
      set { _sunSet = value; }
    }

    public Property ZoneProperty
    {
      get { return _zone; }
      set { _zone = value; }
    }
  }

  #endregion

  #region CurrentCondition struct

  /// <summary>
  /// current condition
  /// </summary>
  public class CurrentCondition
  {
    public void FillWithDummyData()
    {
      City = "Dummytown, Dummyland";
      LastUpdate = "Friday, 12 May 2007";
      Temperature = "12°C";
      FeelsLikeTemp = "16°C";
      Condition = "Partly \n Cloudy";
      Icon = "30.png";
      Humidity = "70%";
      Wind = "200 mph";
      UVIndex = "10";
      DewPoint = "30%";
    }

    private Property _city = new Property(""); // <obst>
    private Property _lastUpdate = new Property(""); // <lsup>
    private Property _temperature = new Property(""); // <temp> 
    private Property _feelsLikeTemp = new Property(""); // <flik>
    private Property _condition = new Property(""); // <t>
    private Property _icon = new Property(""); // <icon> 
    private Property _humidity = new Property(""); // <hmid>
    private Property _wind = new Property(""); // <wind>
    private Property _uVindex = new Property(""); // <uv> 
    private Property _dewPoint = new Property(""); // <dewp>
    // Getters :P
    public string City
    {
      get { return (string) _city.GetValue(); }
      set { _city.SetValue(value); }
    }

    public string LastUpdate
    {
      get { return (string) _lastUpdate.GetValue(); }
      set { _lastUpdate.SetValue(value); }
    }

    public string Temperature
    {
      get { return (string) _temperature.GetValue(); }
      set { _temperature.SetValue(value); }
    }

    public string FeelsLikeTemp
    {
      get { return (string) _feelsLikeTemp.GetValue(); }
      set { _feelsLikeTemp.SetValue(value); }
    }

    public string Condition
    {
      get { return (string) _condition.GetValue(); }
      set { _condition.SetValue(value); }
    }

    public string Humidity
    {
      get { return (string) _humidity.GetValue(); }
      set { _humidity.SetValue(value); }
    }

    public string Wind
    {
      get { return (string) _wind.GetValue(); }
      set { _wind.SetValue(value); }
    }

    public string UVIndex
    {
      get { return (string) _uVindex.GetValue(); }
      set { _uVindex.SetValue(value); }
    }

    public string DewPoint
    {
      get { return (string) _dewPoint.GetValue(); }
      set { _dewPoint.SetValue(value); }
    }

    public string Icon
    {
      get { return (string) _icon.GetValue(); }
      set { _icon.SetValue(value); }
    }


    public Property CityProperty
    {
      get { return _city; }
      set { _city = value; }
    }

    public Property LastUpdateProperty
    {
      get { return _lastUpdate; }
      set { _lastUpdate = value; }
    }

    public Property TemperatureProperty
    {
      get { return _temperature; }
      set { _temperature = value; }
    }

    public Property FeelsLikeTempProperty
    {
      get { return _feelsLikeTemp; }
      set { _feelsLikeTemp = value; }
    }

    public Property ConditionProperty
    {
      get { return _condition; }
      set { _condition = value; }
    }

    public Property HumidityProperty
    {
      get { return _humidity; }
      set { _humidity = value; }
    }

    public Property WindProperty
    {
      get { return _wind; }
      set { _wind = value; }
    }

    public Property UVIndexProperty
    {
      get { return _uVindex; }
      set { _uVindex = value; }
    }

    public Property DewPointProperty
    {
      get { return _dewPoint; }
      set { _dewPoint = value; }
    }

    public Property IconProperty
    {
      get { return _icon; }
      set { _icon = value; }
    }

    public Uri IconUri
    {
      get { return new Uri(IconPath); }
    }

    public string IconPath
    {
      get
      {
        string icon = (string) _icon.GetValue();
        if (icon == String.Empty)
        {
          return @"pack://siteoforigin:,,/Media/Weather/128x128/WEATHERALERT.png";
        }
        // return the data
        return (@"pack://siteoforigin:,,/Media/Weather/128x128/" + icon.Replace('\\', '/'));
      }
    }
  }

  #endregion

  #region DayForeCast struct

  /// <summary>
  /// day forecast
  /// </summary>
  public struct DayForeCast
  {
    public string iconImageNameLow;
    public string iconImageNameHigh;
    public string overview;
    public string day;
    public string high;
    public string low;
    public string sunRise;
    public string sunSet;
    public string precipitation;
    public string humidity;
    public string wind;


    /// <summary>
    /// constructor for designer
    /// </summary>
    /// <param name="day"></param>
    public DayForeCast(int day)
    {
      if (day == 0)
      {
        iconImageNameLow = "16.png";
        iconImageNameHigh = "16.png";
        overview = "Partly\nCloudy";
        this.day = "Monday";
        high = "13°C";
        low = "8°C";
        sunRise = "5:00 AM";
        sunSet = "7:00 PM";
        precipitation = "Partly\nCloudy";
        humidity = "54%";
        wind = "30 mph";
      }
      else if (day == 1)
      {
        iconImageNameLow = "36.png";
        iconImageNameHigh = "36.png";
        overview = "Sunny";
        this.day = "Tuesday";
        high = "25°C";
        low = "20°C";
        sunRise = "5:10 AM";
        sunSet = "7:10 PM";
        precipitation = "Sunny";
        humidity = "30%";
        wind = "0 mph";
      }
      else if (day == 2)
      {
        iconImageNameLow = "34.png";
        iconImageNameHigh = "34.png";
        overview = "Fog";
        this.day = "Wednesday";
        high = "20°C";
        low = "11°C";
        sunRise = "5:20 AM";
        sunSet = "7:20 PM";
        precipitation = "Fog";
        humidity = "84%";
        wind = "55 mph";
      }
      else
      {
        iconImageNameLow = "38.png";
        iconImageNameHigh = "38.png";
        overview = "Storm and\nThunder";
        this.day = "Thursday";
        high = "12°C";
        low = "0°C";
        sunRise = "5:40 AM";
        sunSet = "7:40 PM";
        precipitation = "Storm and\nThunder";
        humidity = "89%";
        wind = "100 mph";
      }
    }

    #region Dummy Data for Designer

    /// <summary>
    /// fills up some dummy data
    /// </summary>
    /// <param name="day"></param>
    public void FillWithDummyData(int day)
    {
      if (day == 0)
      {
        iconImageNameLow = "16.png";
        iconImageNameHigh = "16.png";
        overview = "this is an overview";
        this.day = "Monday";
        high = "13°C";
        low = "8°C";
        sunRise = "5:00 AM";
        sunSet = "7:00 PM";
        precipitation = "Partly\nCloudy";
        humidity = "54%";
        wind = "30 mph";
      }
      else if (day == 1)
      {
        iconImageNameLow = "36.png";
        iconImageNameHigh = "36.png";
        overview = "this is an overview";
        this.day = "Tuesday";
        high = "25°C";
        low = "20°C";
        sunRise = "5:10 AM";
        sunSet = "7:10 PM";
        precipitation = "Sunny";
        humidity = "30%";
        wind = "0 mph";
      }
      else if (day == 2)
      {
        iconImageNameLow = "34.png";
        iconImageNameHigh = "34.png";
        overview = "this is an overview";
        this.day = "Wednesday";
        high = "20°C";
        low = "11°C";
        sunRise = "5:20 AM";
        sunSet = "7:20 PM";
        precipitation = "Fog";
        humidity = "84%";
        wind = "55 mph";
      }
      else
      {
        iconImageNameLow = "38.png";
        iconImageNameHigh = "38.png";
        overview = "this is an overview";
        this.day = "Thursday";
        high = "12°C";
        low = "0°C";
        sunRise = "5:40 AM";
        sunSet = "7:40 PM";
        precipitation = "Storm and\nThunder";
        humidity = "89%";
        wind = "100 mph";
      }
    }

    #endregion

    // Getters :P
    public Uri IconImageNameLow
    {
      get { return new Uri(IconLow); }
    }

    public string IconLow
    {
      get
      {
        if (iconImageNameLow == String.Empty)
        {
          return (@"pack://siteoforigin:,,/Media/Weather/128x128/WEATHERALERT.png");
        }
        // return the data
        return (@"pack://siteoforigin:,,/Media/Weather/128x128/" + iconImageNameLow.Replace('\\', '/'));
      }
    }

    public Uri IconImageNameHigh
    {
      get { return new Uri(IconHigh); }
    }

    public string IconHigh
    {
      get
      {
        if (iconImageNameHigh == String.Empty)
        {
          return (@"pack://siteoforigin:,,/Media/Weather/128x128/WEATHERALERT.png");
        }
        // return the data
        return (@"pack://siteoforigin:,,/Media/Weather/128x128/" + iconImageNameHigh.Replace('\\', '/'));
      }
    }

    public string Overview
    {
      get { return overview; }
    }

    public string Day
    {
      get { return day; }
    }

    public string High
    {
      get { return high; }
    }

    public string Low
    {
      get { return low; }
    }

    public string SunRise
    {
      get { return sunRise; }
    }

    public string SunSet
    {
      get { return sunSet; }
    }

    public string Precipitation
    {
      get { return precipitation; }
    }

    public string Humidity
    {
      get { return humidity; }
    }

    public string Wind
    {
      get { return wind; }
    }
  } ;

  #endregion

  #endregion
}