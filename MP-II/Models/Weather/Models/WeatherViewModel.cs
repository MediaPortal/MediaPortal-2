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
using System;
using System.Collections.Generic;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;

using Models.Weather.Grabbers;


namespace Models.Weather
{
  /// <summary>
  /// ViewModel Class for weather.xml
  /// </summary>
  public class WeatherViewModel
  {
    private Property _currentLocation;
    private readonly List<City> _locations = new List<City>();

    private readonly ItemsCollection _locationsCollection = new ItemsCollection();
    // Used to select a city... Items hold Name and ID

    private ItemsCollection _mainMenu;

    public WeatherViewModel()
    {
      _currentLocation = new Property(typeof(City), new City("No Data", "No Data"));
      // for testing purposes add a weathercatcher
      ServiceScope.Add<IWeatherCatcher>(new WeatherDotComCatcher());
      // add citys from settings to the locations list
      GetLocationsFromSettings(true);
    }

    /// <summary>
    /// exposes the main menu to the skin
    /// </summary>
    /// <value>The main menu.</value>
    public ItemsCollection MainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        _mainMenu = MenuHelper.WrapMenu(menuCollect.GetMenu("weather-main"));

        return _mainMenu;
      }
    }

    /// <summary>
    /// this gets the locations from settings
    /// </summary>
    protected void GetLocationsFromSettings(bool shouldFire)
    {
      // empty lists
      _locationsCollection.Clear();
      _locations.Clear();
      // add citys from settings to the locations list
      WeatherSettings settings = ServiceScope.Get<ISettingsManager>().Load<WeatherSettings>();
      ListItem buffItem;
      foreach (CitySetupInfo loc in settings.LocationsList)
      {
        if (loc != null)
        {
          City buffLoc = new City(loc);
          _locations.Add(buffLoc);

          buffItem = new ListItem();
          buffItem.Add("Name", loc.Name);
          buffItem.Add("Id", loc.Id);
          _locationsCollection.Add(buffItem);

          // Is this the setting?
          if (loc.Id.Equals(settings.LocationCode))
          {
            // Fetch data
            RefreshData(buffLoc);
            // Copy the data to the skin property.
            CurrentLocation.Copy(buffLoc);
          }
        }
      }
      // if there is no city selected until yet, choose the first one
      if (settings.LocationCode.Equals("<none>"))
      {
        if (_locations.Count > 0)
        {
          // Fetch data
          RefreshData(_locations[0]);
          // Copy the data to the skin property.
          CurrentLocation.Copy(_locations[0]);
        }
        // no locations have been setup yet, guide to setup
        else
        {
          //  ServiceScope.Get<IScreenManager>().ShowScreen("weathersetup");
        }
      }
      // we've added new citys, so update the locations collection
      if (shouldFire)
      {
        _locationsCollection.FireChange();
      }
    }

    /// <summary>
    /// updates the location with new data
    /// </summary>
    /// <returns></returns>
    public void RefreshData(City loc)
    {
      //ServiceScope.Get<IScreenManager>().CurrentWindow.WaitCursorVisible = true;

      if (ServiceScope.Get<IWeatherCatcher>().GetLocationData(loc))
      {
        ServiceScope.Get<ILogger>().Info("Loaded Weather Data for " + loc.Name + ", " + loc.Id);
      }
      else
      {
        ServiceScope.Get<ILogger>().Info("Failded to load Weather Data for " + loc.Name + ", " + loc.Id);
      }

      //ServiceScope.Get<IScreenManager>().CurrentWindow.WaitCursorVisible = false;
    }

    /// <summary>
    /// provides command for the skin to update the location with new data
    /// </summary>
    /// <returns></returns>
    public void Refresh()
    {
      RefreshData(CurrentLocation);
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
      string strLoc = item["Id"];

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
        RefreshData(found);
        CurrentLocation.Copy(found);
        // also save the last selected city to settings
        WeatherSettings settings = ServiceScope.Get<ISettingsManager>().Load<WeatherSettings>();
        settings.LocationCode = found.Id;
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }

    }

    /// <summary>
    /// exposes the current location to the skin
    /// </summary>
    public City CurrentLocation
    {
      get { return (City)_currentLocation.GetValue(); }
    }

    public Property CurrentLocationProperty
    {
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
        return _locationsCollection;
      }
    }
  }
}
