#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core.General;

namespace UiComponents.Weather
{
  /// <summary>
  /// this is a basic field where
  /// the information for the provider
  /// are stored (usually the location name and unique id)
  /// </summary>
  public class CitySetupInfo
  {
    private string id, grabber;
    protected AbstractProperty _name = new WProperty(typeof(string), "");

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
    private AbstractProperty _locationInfo = new WProperty(typeof(LocInfo), new LocInfo());
    private AbstractProperty _currCondition = new WProperty(typeof(CurrentCondition), new CurrentCondition());
    private AbstractProperty _dayForecastCollection = new WProperty(typeof(DayForeCastCollection), new DayForeCastCollection());
    private bool _updateSuccessful;

    /// <summary>
    /// parameterless constructor
    /// needed for serialization
    /// </summary>
    public City()
    {
      _updateSuccessful = false;
      Init();
    }

    public City(CitySetupInfo info)
    {
      Name = info.Name;
      Id = info.Id;
      Grabber = info.Grabber;
      _updateSuccessful = false;
      Init();
    }

    public City(string name, string id)
    {
      Name = name;
      Id = id;
      _updateSuccessful = false;
      Init();
    }

    private void Init()
    {
      for (int i = 0; i < 5; i++)
        ForecastCollection.Add(new DayForeCast());
    }

    public void Copy(City src)
    {
      Name = src.Name;
      Id = src.Id;
      LocationInfo.SunRise = src.LocationInfo.SunRise;
      LocationInfo.SunSet = src.LocationInfo.SunSet;
      Condition.BigIcon = src.Condition.BigIcon;
      Condition.SmallIcon = src.Condition.SmallIcon;
      Condition.Temperature = src.Condition.Temperature;
      Condition.LastUpdate = src.Condition.LastUpdate;
      Condition.FeelsLikeTemp = src.Condition.FeelsLikeTemp;
      Condition.Humidity = src.Condition.Humidity;
      Condition.Wind = src.Condition.Wind;
      Condition.UVIndex = src.Condition.UVIndex;
      Condition.DewPoint = src.Condition.DewPoint;
      Condition.Condition = src.Condition.Condition;
      for (int i = 0; i < 5; i++)
      {
        ForecastCollection[i].Day = src.ForecastCollection[i].Day;
        ForecastCollection[i].BigIcon = src.ForecastCollection[i].BigIcon;
        ForecastCollection[i].SmallIcon = src.ForecastCollection[i].SmallIcon;
        ForecastCollection[i].Low = src.ForecastCollection[i].Low;
        ForecastCollection[i].High = src.ForecastCollection[i].High;
        ForecastCollection[i].Humidity = src.ForecastCollection[i].Humidity;
        ForecastCollection[i].Overview = src.ForecastCollection[i].Overview;
        ForecastCollection[i].Precipitation = src.ForecastCollection[i].Precipitation;
        ForecastCollection[i].SunRise = src.ForecastCollection[i].SunRise;
        ForecastCollection[i].SunSet = src.ForecastCollection[i].SunSet;
        ForecastCollection[i].Wind = src.ForecastCollection[i].Wind;
      }

    }

    #region properties

    public AbstractProperty NameProperty
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

    public AbstractProperty LocationInfoProperty
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

    public AbstractProperty ConditionProperty
    {
      get { return _currCondition; }
      set { _currCondition = value; }
    }

    /// <summary>
    /// Current Condition
    /// </summary>
    public DayForeCastCollection ForecastCollection
    {
      get { return (DayForeCastCollection)_dayForecastCollection.GetValue(); }
      set { _dayForecastCollection.SetValue(value); }
    }

    public AbstractProperty ForecastCollectionProperty
    {
      get { return _dayForecastCollection; }
      set { _dayForecastCollection = value; }
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
    private AbstractProperty _time = new WProperty(typeof(string), ""); // <tm>1:12 AM</tm>
    private AbstractProperty _lat = new WProperty(typeof(string), ""); // <lat>49.02</lat>
    private AbstractProperty _lon = new WProperty(typeof(string), ""); // <lon>12.1</lon>
    private AbstractProperty _sunRise = new WProperty(typeof(string), ""); // <sunr>7:14 AM</sunr> 
    private AbstractProperty _sunSet = new WProperty(typeof(string), ""); // <suns>5:38 PM</suns>
    private AbstractProperty _zone = new WProperty(typeof(string), ""); // <zone>1</zone>
    // Getters

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

    public AbstractProperty TimeProperty
    {
      get { return _time; }
      set { _time = value; }
    }

    public AbstractProperty LatProperty
    {
      get { return _lat; }
      set { _lat = value; }
    }

    public AbstractProperty LonProperty
    {
      get { return _lon; }
      set { _lon = value; }
    }

    public AbstractProperty SunRiseProperty
    {
      get { return _sunRise; }
      set { _sunRise = value; }
    }

    public AbstractProperty SunSetProperty
    {
      get { return _sunSet; }
      set { _sunSet = value; }
    }

    public AbstractProperty ZoneProperty
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
      Temperature = "12�C";
      FeelsLikeTemp = "16�C";
      Condition = "Partly \n Cloudy";
      BigIcon = "30.png";
      Humidity = "70%";
      Wind = "200 mph";
      UVIndex = "10";
      DewPoint = "30%";
    }

    private AbstractProperty _city = new WProperty(typeof(string), ""); // <obst>
    private AbstractProperty _lastUpdate = new WProperty(typeof(string), ""); // <lsup>
    private AbstractProperty _temperature = new WProperty(typeof(string), ""); // <temp> 
    private AbstractProperty _feelsLikeTemp = new WProperty(typeof(string), ""); // <flik>
    private AbstractProperty _condition = new WProperty(typeof(string), ""); // <t>
    private AbstractProperty _bigIcon = new WProperty(typeof(string), ""); // <icon> 
    private AbstractProperty _smallIcon = new WProperty(typeof(string), ""); // <icon> 
    private AbstractProperty _humidity = new WProperty(typeof(string), ""); // <hmid>
    private AbstractProperty _wind = new WProperty(typeof(string), ""); // <wind>
    private AbstractProperty _uVindex = new WProperty(typeof(string), ""); // <uv> 
    private AbstractProperty _dewPoint = new WProperty(typeof(string), ""); // <dewp>
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

    public string BigIcon
    {
      get { return (string) _bigIcon.GetValue(); }
      set { _bigIcon.SetValue(value); }
    }

    public string SmallIcon
    {
      get { return (string)_smallIcon.GetValue(); }
      set { _smallIcon.SetValue(value); }
    }

    public AbstractProperty CityProperty
    {
      get { return _city; }
      set { _city = value; }
    }

    public AbstractProperty LastUpdateProperty
    {
      get { return _lastUpdate; }
      set { _lastUpdate = value; }
    }

    public AbstractProperty TemperatureProperty
    {
      get { return _temperature; }
      set { _temperature = value; }
    }

    public AbstractProperty FeelsLikeTempProperty
    {
      get { return _feelsLikeTemp; }
      set { _feelsLikeTemp = value; }
    }

    public AbstractProperty ConditionProperty
    {
      get { return _condition; }
      set { _condition = value; }
    }

    public AbstractProperty HumidityProperty
    {
      get { return _humidity; }
      set { _humidity = value; }
    }

    public AbstractProperty WindProperty
    {
      get { return _wind; }
      set { _wind = value; }
    }

    public AbstractProperty UVIndexProperty
    {
      get { return _uVindex; }
      set { _uVindex = value; }
    }

    public AbstractProperty DewPointProperty
    {
      get { return _dewPoint; }
      set { _dewPoint = value; }
    }

    public AbstractProperty BigIconProperty
    {
      get { return _bigIcon; }
      set { _bigIcon = value; }
    }
  }

  #endregion

  #region DayForeCast struct

  /// <summary>
  /// Day forecast
  /// </summary>
  public class DayForeCast
  {
    private AbstractProperty _smallIcon = new WProperty(typeof(string), "");
    private AbstractProperty _bigIcon= new WProperty(typeof(string), ""); 
    private AbstractProperty _overview = new WProperty(typeof(string), ""); 
    private AbstractProperty _day = new WProperty(typeof(string), "");
    private AbstractProperty _high = new WProperty(typeof(string), "");
    private AbstractProperty _low = new WProperty(typeof(string), ""); 
    private AbstractProperty _sunRise = new WProperty(typeof(string), "");
    private AbstractProperty _sunSet = new WProperty(typeof(string), "");
    private AbstractProperty _precipitation = new WProperty(typeof(string), "");
    private AbstractProperty _humidity = new WProperty(typeof(string), "");
    private AbstractProperty _wind = new WProperty(typeof(string), ""); 


   
    // Getters :P

    public string SmallIcon
    {
      get { return (string)_smallIcon.GetValue(); }
      set { _smallIcon.SetValue(value); }
    }


    public string BigIcon
    {
      get { return (string)_bigIcon.GetValue(); }
      set { _bigIcon.SetValue(value); }
    }


    public string Overview
    {
      get { return (string)_overview.GetValue(); }
      set { _overview.SetValue(value); }
    }

    public string Day
    {
      get { return (string)_day.GetValue(); }
      set { _day.SetValue(value); }
    }

    public string High
    {
      get { return (string)_high.GetValue(); }
      set { _high.SetValue(value); }
    }

    public string Low
    {
      get { return (string)_low.GetValue(); }
      set { _low.SetValue(value); }
    }

    public string SunRise
    {
      get { return (string)_sunRise.GetValue(); }
      set { _sunRise.SetValue(value); }
    }

    public string SunSet
    {
      get { return (string)_sunSet.GetValue(); }
      set { _sunSet.SetValue(value); }
    }

    public string Precipitation
    {
      get { return (string)_precipitation.GetValue(); }
      set { _precipitation.SetValue(value); }
    }

    public string Humidity
    {
      get { return (string)_humidity.GetValue(); }
      set { _humidity.SetValue(value); }
    }

    public string Wind
    {
      get { return (string)_wind.GetValue(); }
      set { _wind.SetValue(value); }
    }

    public AbstractProperty SmallIconProperty
    {
      get { return _smallIcon; }
      set { _smallIcon = value; }
    }


    public AbstractProperty BigIconProperty
    {
      get { return _bigIcon; }
      set { _bigIcon = value; }
    }


    public AbstractProperty OverviewProperty
    {
      get { return _overview; }
      set { _overview = value; }
    }

    public AbstractProperty DayProperty
    {
      get { return _day; }
      set { _day = value; }
    }

    public AbstractProperty HighProperty
    {
      get { return _high; }
      set { _high = value; }
    }

    public AbstractProperty LowProperty
    {
      get { return _low; }
      set { _low = value; }
    }

    public AbstractProperty SunRiseProperty
    {
      get { return _sunRise; }
      set { _sunRise = value; }
    }

    public AbstractProperty SunSetProperty
    {
      get { return _sunSet; }
      set { _sunSet = value; }
    }

    public AbstractProperty PrecipitationProperty
    {
      get { return _precipitation; }
      set { _precipitation = value; }
    }

    public AbstractProperty HumidityProperty
    {
      get { return _humidity; }
      set { _humidity = value; }
    }

    public AbstractProperty WindProperty
    {
      get { return _wind; }
      set { _wind = value; }
    }
  } ;

   /// <summary>
  /// current condition
  /// </summary>
  public class DayForeCastCollection
  {
    List<DayForeCast> _elements;

    public DayForeCastCollection()
    {
      _elements = new List<DayForeCast>();
    }

    /// <summary>
    /// Adds the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    public void Add(DayForeCast element)
    {
      _elements.Add(element);
    }

    public DayForeCast this[int index]
    {
      get
      {
        return _elements[index];
      }
    }
  }

  #endregion

  #endregion
}
