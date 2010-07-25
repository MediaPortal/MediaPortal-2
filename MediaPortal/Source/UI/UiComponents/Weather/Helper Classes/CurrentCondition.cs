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

using MediaPortal.Core.General;

namespace MediaPortal.UiComponents.Weather
{
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

    public AbstractProperty SmallIconProperty
    {
      get { return _smallIcon; }
      set { _smallIcon = value; }
    }
  }
}