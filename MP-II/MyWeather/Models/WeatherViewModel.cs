#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

﻿#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Properties;
using MediaPortal.Core.Settings;
using MediaPortal.Core.WindowManager;
using MyWeather.Grabbers;

namespace MyWeather
{
  /// <summary>
  /// ViewModel Class for weather.xml
  /// </summary>
  public class WeatherViewModel
  {
    //City _currentLocation = new City("No Data", "No Data");
    private Property _currentLocation;
    private readonly List<City> _locations = new List<City>();

    private readonly ItemsCollection _locationsCollection = new ItemsCollection();
    // Used to select a city... Items hold Name and ID

    private readonly ItemsCollection _forecastCollection = new ItemsCollection();
    // Used to display the 4day Forecast for the currently selected Location

    public WeatherViewModel()
    {
      _currentLocation = new Property(new City("No Data", "No Data"));
      // for testing purposes add a weathercatcher
      ServiceScope.Add<IWeatherCatcher>(new WeatherDotComCatcher());
      // add citys from settings to the locations list
      GetLocationsFromSettings(true);
      Refresh();
    }

    /// <summary>
    /// this gets the locations from settings
    /// </summary>
    protected void GetLocationsFromSettings(bool shouldFire)
    {
      // empty lists
      _locationsCollection.Clear();
      _locations.Clear();
      CurrentLocation = null;
      // add citys from settings to the locations list
      WeatherSettings settings = new WeatherSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      ListItem buffItem;
      foreach (CitySetupInfo loc in settings.LocationsList)
      {
        if (loc != null)
        {
          City buffLoc = new City(loc);
          buffItem = new ListItem();
          buffItem.Add("Name", loc.Name);
          buffItem.Add("Id", loc.Id);
          _locations.Add(buffLoc);
          _locationsCollection.Add(buffItem);
          // set the currentlocation from settings
          if (loc.Id.Equals(settings.LocationCode))
          {
            CurrentLocation = buffLoc;
          }
        }
      }
      // if there is no city selected until yet, choose the first one
      if (settings.LocationCode.Equals("<none>"))
      {
        if (_locations.Count > 0)
        {
          CurrentLocation = _locations[0];
        }
          // no locations have been setup yet, guide to setup
        else
        {
          AddDummyCity(settings);
          ServiceScope.Get<IWindowManager>().ShowWindow("weathersetup");
        }
      }
      // we've added new citys, so update the locations collection
      if (shouldFire)
      {
        _locationsCollection.FireChange();
      }
    }

    protected static void AddDummyCity(WeatherSettings settings)
    {
      CitySetupInfo dummy = new CitySetupInfo("No Data", "UKXX0085");
      settings.LocationsList.Add(dummy);
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    /// <summary>
    /// this updates the Forecast itemcollection 
    /// (f.e. when the current location changed)
    /// </summary>
    protected void UpdateForecastsCollection()
    {
      if (CurrentLocation == null)
      {
        return;
      }
      // we need to generate the ItemsCollection from the Forecast here
      ListItem buff;
      _forecastCollection.Clear();
      if (CurrentLocation.Forecast == null)
      {
        return;
      }
      // okay, fill the ItemsCollection
      foreach (DayForeCast forecast in CurrentLocation.Forecast)
      {
        buff = new ListItem();
        buff.Add("IconLow", forecast.IconLow);
        buff.Add("IconHigh", forecast.IconHigh);
        buff.Add("Overview", forecast.Overview);
        buff.Add("Day", forecast.Day);
        buff.Add("High", forecast.High);
        buff.Add("Low", forecast.Low);
        buff.Add("SunRise", forecast.SunRise);
        buff.Add("SunSet", forecast.SunSet);
        buff.Add("Precipitation", forecast.Precipitation);
        buff.Add("Humidity", forecast.Humidity);
        buff.Add("Wind", forecast.Wind);
        _forecastCollection.Add(buff);
      }
      // tell the skin that something might have changed
      _forecastCollection.FireChange();
    }

    /// <summary>
    /// updates all locations with new data
    /// </summary>
    /// <returns></returns>
    public void Refresh()
    {
      ServiceScope.Get<IWindowManager>().CurrentWindow.WaitCursorVisible = true;
      GetLocationsFromSettings(true);
      foreach (City loc in _locations)
      {
        if (ServiceScope.Get<IWeatherCatcher>().GetLocationData(loc))
        {
          UpdateForecastsCollection();
          ServiceScope.Get<ILogger>().Info("Loaded Weather Data for " + loc.Name + ", " + loc.Id);
        }
        else
        {
          ServiceScope.Get<ILogger>().Info("Failded to load Weather Data for " + loc.Name + ", " + loc.Id);
        }
      }
      ServiceScope.Get<IWindowManager>().CurrentWindow.WaitCursorVisible = false;
    }


    /// <summary>
    /// provides command for the skin to change the current location
    /// </summary>
    /// <param name="item">The item.</param>
    public void ChangeLocation(ListItem item)
    {
      City found = null;
      // we need to find the correct city now... do this by searching for the id in the 
      // locations list
      string strLoc = item.Label("Id").Evaluate(null, null);

      foreach (City c in Locations)
      {
        if (c != null)
        {
          if (c.Id.Equals(strLoc))
          {
            found = c;
            break;
          }
        }
      }
      // okay, if we found the correct location, update the lists
      if (found != null)
      {
        CurrentLocation = found;
        // also save the last selected city to settings
        WeatherSettings settings = new WeatherSettings();
        ServiceScope.Get<ISettingsManager>().Load(settings);
        settings.LocationCode = found.Id;
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }
      Refresh();
    }

    /// <summary>
    /// exposes the current location to the skin
    /// </summary>
    public City CurrentLocation
    {
      set
      {
        if (value != null)
        {
          _currentLocation.SetValue(value);
        }
      }
      get { return (City) _currentLocation.GetValue(); }
    }

    public Property CurrentLocationProperty
    {
      set { _currentLocation = value; }
      get { return _currentLocation; }
    }

    /// <summary>
    /// gets a list of loaded locations
    /// </summary>
    public List<City> Locations
    {
      get { return _locations; }
    }

    /// <summary>
    /// exposes the loaded locations to the skin (Name, Id)
    /// </summary>
    public ItemsCollection LocationsCollection
    {
      get
      {
        GetLocationsFromSettings(false);
        return _locationsCollection;
      }
    }

    /// <summary>
    /// exposes the dayforecast (usually 4 days) to the skin
    /// </summary>
    public ItemsCollection ForecastCollection
    {
      get { return _forecastCollection; }
    }
  }
}
