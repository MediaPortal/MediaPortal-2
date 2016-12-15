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
  /// Holds information for a city.
  /// </summary>
  public class City : CitySetupInfo
  {
    #region Const

    /// <summary>
    /// Represents "No Data" default location. This property always returns a new instance.
    /// </summary>
    public static City NoData
    {
      get { return new City("No Data", string.Empty); }
    }

    #endregion Const

    protected readonly AbstractProperty _locationInfo = new WProperty(typeof(LocInfo), new LocInfo());
    protected readonly AbstractProperty _currCondition = new WProperty(typeof(CurrentCondition), new CurrentCondition());
    protected readonly ItemsList _dayForecastCollection = new ItemsList();
    protected bool _updateSuccessful;

    /// <summary>
    /// Parameterless constructor - needed for serialization.
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

    public void Copy(City src)
    {
      if (src == this)
        return;

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
      condition.Precipitation = sourceCondition.Precipitation;
      condition.Pressure = sourceCondition.Pressure;
      condition.Wind = sourceCondition.Wind;
      condition.UVIndex = sourceCondition.UVIndex;
      condition.DewPoint = sourceCondition.DewPoint;
      condition.Condition = sourceCondition.Condition;

      ForecastCollection.Clear();

      // Simply copy the DayForecasts to the target
      foreach (DayForecast srcDayForecast in src.ForecastCollection)
        ForecastCollection.Add(srcDayForecast);

      ForecastCollection.FireChange();
    }

    #region Properties

    public AbstractProperty NameProperty
    {
      get { return _name; }
    }

    public LocInfo LocationInfo
    {
      get { return (LocInfo)_locationInfo.GetValue(); }
      set { _locationInfo.SetValue(value); }
    }

    public AbstractProperty LocationInfoProperty
    {
      get { return _locationInfo; }
    }

    public CurrentCondition Condition
    {
      get { return (CurrentCondition)_currCondition.GetValue(); }
      set { _currCondition.SetValue(value); }
    }

    public AbstractProperty ConditionProperty
    {
      get { return _currCondition; }
    }

    public ItemsList ForecastCollection
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

    #endregion Properties
  }
}