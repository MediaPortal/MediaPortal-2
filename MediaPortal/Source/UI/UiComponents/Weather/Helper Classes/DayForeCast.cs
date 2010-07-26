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
  /// Day forecast
  /// </summary>
  public class DayForeCast
  {
    private AbstractProperty _smallIcon = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _bigIcon= new WProperty(typeof(string), string.Empty); 
    private AbstractProperty _overview = new WProperty(typeof(string), string.Empty); 
    private AbstractProperty _day = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _high = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _low = new WProperty(typeof(string), string.Empty); 
    private AbstractProperty _sunRise = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _sunSet = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _precipitation = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _humidity = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _wind = new WProperty(typeof(string), string.Empty); 


   
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
}