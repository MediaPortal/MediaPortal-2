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
using System.Collections.Generic;
using System.Threading;

namespace MediaPortal.UiComponents.Weather
{
  /// <summary>
  /// holds Information on the City
  /// </summary>
  public class City : CitySetupInfo
  {
    private AbstractProperty _locationInfo = new WProperty(typeof(LocInfo), new LocInfo());
    private AbstractProperty _currCondition = new WProperty(typeof(CurrentCondition), new CurrentCondition());
    private AbstractProperty _dayForecastCollection = new WProperty(typeof(List<DayForeCast>), new List<DayForeCast>());
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
      for (int i = 0; i < ForecastCollection.Count; i++)
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
      Thread.Sleep(2000);
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
    public List<DayForeCast> ForecastCollection
    {
      get { return (List<DayForeCast>)_dayForecastCollection.GetValue(); }
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
}
