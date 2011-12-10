#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Weather
{
  /// <summary>
  /// Holds information for a city.
  /// </summary>
  // TODO: Make the plugin cope with different numbers of forecasts, depending on the catcher.
  // It's not good that this class always asumes 5 forecasts in the forecast collection.
  public class City : CitySetupInfo
  {
    protected readonly AbstractProperty _locationInfo = new WProperty(typeof(LocInfo), new LocInfo());
    protected readonly AbstractProperty _currCondition = new WProperty(typeof(CurrentCondition), new CurrentCondition());
    protected readonly IList<DayForecast> _dayForecastCollection = new List<DayForecast>(5);
    protected bool _updateSuccessful;

    /// <summary>
    /// Parameterless constructor - needed for serialization.
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
        ForecastCollection.Add(new DayForecast());
    }

    public void Copy(City src)
    {
      Name = src.Name;
      Id = src.Id;
      LocInfo locInfo = LocationInfo;
      LocInfo sourceLocInfo = src.LocationInfo;
      locInfo.SunRise = sourceLocInfo.SunRise;
      locInfo.SunSet = sourceLocInfo.SunSet;
      CurrentCondition condition = Condition;
      CurrentCondition sourceCondition = src.Condition;
      condition.BigIcon = sourceCondition.BigIcon;
      condition.SmallIcon = sourceCondition.SmallIcon;
      condition.Temperature = sourceCondition.Temperature;
      condition.LastUpdate = sourceCondition.LastUpdate;
      condition.FeelsLikeTemp = sourceCondition.FeelsLikeTemp;
      condition.Humidity = sourceCondition.Humidity;
      condition.Wind = sourceCondition.Wind;
      condition.UVIndex = sourceCondition.UVIndex;
      condition.DewPoint = sourceCondition.DewPoint;
      condition.Condition = sourceCondition.Condition;
      IList<DayForecast> forecastCollection = ForecastCollection;
      for (int i = 0; i < forecastCollection.Count; i++)
      {
        DayForecast forecast = forecastCollection[i];
        DayForecast sourceForecast = src.ForecastCollection[i];
        forecast.Day = sourceForecast.Day;
        forecast.BigIcon = sourceForecast.BigIcon;
        forecast.SmallIcon = sourceForecast.SmallIcon;
        forecast.Low = sourceForecast.Low;
        forecast.High = sourceForecast.High;
        forecast.Humidity = sourceForecast.Humidity;
        forecast.Overview = sourceForecast.Overview;
        forecast.Precipitation = sourceForecast.Precipitation;
        forecast.SunRise = sourceForecast.SunRise;
        forecast.SunSet = sourceForecast.SunSet;
        forecast.Wind = sourceForecast.Wind;
      }
    }

    #region Properties

    public AbstractProperty NameProperty
    {
      get { return _name; }
    }

    public LocInfo LocationInfo
    {
      get { return (LocInfo) _locationInfo.GetValue(); }
      set { _locationInfo.SetValue(value); }
    }

    public AbstractProperty LocationInfoProperty
    {
      get { return _locationInfo; }
    }

    public CurrentCondition Condition
    {
      get { return (CurrentCondition) _currCondition.GetValue(); }
      set { _currCondition.SetValue(value); }
    }

    public AbstractProperty ConditionProperty
    {
      get { return _currCondition; }
    }

    public IList<DayForecast> ForecastCollection
    {
      get { return _dayForecastCollection; }
    }

    /// <summary>
    /// Returns the information if the update was successful.
    /// </summary>
    public bool HasData
    {
      get { return _updateSuccessful; }
      set { _updateSuccessful = value; }
    }

    #endregion
  }
}
