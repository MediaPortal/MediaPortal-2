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

using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.Weather
{
  /// <summary>
  /// Day forecast
  /// </summary>
  public class DayForecast : ListItem
  {
    protected readonly AbstractProperty _smallIcon = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _bigIcon= new WProperty(typeof(string), string.Empty); 
    protected readonly AbstractProperty _overview = new WProperty(typeof(string), string.Empty); 
    protected readonly AbstractProperty _day = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _high = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _low = new WProperty(typeof(string), string.Empty); 
    protected readonly AbstractProperty _sunRise = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _sunSet = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _precipitation = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _humidity = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _wind = new WProperty(typeof(string), string.Empty); 

    // Public properties
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
    }

    public AbstractProperty BigIconProperty
    {
      get { return _bigIcon; }
    }

    public AbstractProperty OverviewProperty
    {
      get { return _overview; }
    }

    public AbstractProperty DayProperty
    {
      get { return _day; }
    }

    public AbstractProperty HighProperty
    {
      get { return _high; }
    }

    public AbstractProperty LowProperty
    {
      get { return _low; }
    }

    public AbstractProperty SunRiseProperty
    {
      get { return _sunRise; }
    }

    public AbstractProperty SunSetProperty
    {
      get { return _sunSet; }
    }

    public AbstractProperty PrecipitationProperty
    {
      get { return _precipitation; }
    }

    public AbstractProperty HumidityProperty
    {
      get { return _humidity; }
    }

    public AbstractProperty WindProperty
    {
      get { return _wind; }
    }
  }
}