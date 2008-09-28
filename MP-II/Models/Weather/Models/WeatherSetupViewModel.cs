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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Settings;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;

using Models.Weather.Grabbers;


namespace Models.Weather
{
  public class WeatherSetupViewModel
  {
    // locations that are already in the list
    private List<CitySetupInfo> _locations;
    // locations that return as result of searching for a city
    private List<CitySetupInfo> _locationsSearch; 

    // vatiants of the above that is exposed to the skin
    private readonly ItemsCollection _locationsExposed = new ItemsCollection(); 
    private readonly ItemsCollection _locationsSearchExposed = new ItemsCollection();


    private ItemsCollection _mainMenu;
    private Property _searchCity;

    /// <summary>
    /// constructor
    /// </summary>
    public WeatherSetupViewModel()
    {
      _searchCity = new Property(typeof(string), "");
      // see if we already have a weather catcher in servicescope, if not, add one (for testing purposes)
      if (!ServiceScope.IsRegistered<IWeatherCatcher>())
      {
        ServiceScope.Add<IWeatherCatcher>(new WeatherDotComCatcher());
      }
      // load settings
      GetLocationsFromSettings();
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
        _mainMenu = MenuHelper.WrapMenu(menuCollect.GetMenu("settings-weather-main"));

        return _mainMenu;
      }
    }
    /// <summary>
    /// exposes the current search parameter to the skin
    /// </summary>
    public string SearchCity
    {
      get { return _searchCity.GetValue() as string; }
      set { _searchCity.SetValue(value); }
    }

    public Property SearchCityProperty
    {
      get { return _searchCity; }
    }

    /// <summary>
    /// load settings
    /// </summary>
    private void GetLocationsFromSettings()
    {
      WeatherSettings settings = new WeatherSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      Locations = settings.LocationsList;
    }

    /// <summary>
    /// search for a location name and fill up the _locationsSearch list
    /// </summary>
    /// <param name="name"></param>
    public void SearchLocations(string name)
    {
      LocationsSearch = ServiceScope.Get<IWeatherCatcher>().FindLocationsByName(name);
    }


    /// <summary>
    /// saves the current state to the settings
    /// </summary>
    public void SaveSettings()
    {
      //ServiceScope.Get<IScreenManager>().CurrentWindow.WaitCursorVisible = true;
      WeatherSettings settings = new WeatherSettings();
      // Load Settings
      ServiceScope.Get<ISettingsManager>().Load(settings);
      // apply new locations list
      settings.LocationsList = Locations;
      // save
      ServiceScope.Get<ISettingsManager>().Save(settings);
      //ServiceScope.Get<IScreenManager>().CurrentWindow.WaitCursorVisible = false;
    }

    /// <summary>
    /// this will add a location
    /// to the _locationsExposed and _locations list
    /// </summary>
    /// <param name="item"></param>
    public void AddLocation(ListItem item)
    {
      // we don't add it if it's already in there
      foreach (ListItem i in _locationsExposed)
      {
        if (i["Id"].Equals(item["Id"]))
        {
          return;
        }
      }
      _locationsExposed.Add(item);
      // create a CitySetupObject and add it to the loctions list
      CitySetupInfo c =
        new CitySetupInfo(item["Name"], item["Id"]);
      _locations.Add(c);
      _locationsExposed.FireChange();
    }

    /// <summary>
    /// this will delete a location from the _locationsExposed list
    /// </summary>
    /// <param name="item"></param>
    public void Delete(ListItem item)
    {
      if (_locationsExposed.Contains(item))
      {
        _locationsExposed.Remove(item);
        _locationsExposed.FireChange();
        string id = item["Id"];
        foreach (CitySetupInfo info in _locations)
        {
          if (info.Id == id)
          {
            _locations.Remove(info);
            return;
          }
        }
      }
    }

    /// <summary>
    /// gets or sets the Locations
    /// </summary>
    public List<CitySetupInfo> Locations
    {
      get { return _locations; }

      set
      {
        _locations = value;
        if (_locations == null)
        {
          return;
        }

        _locationsExposed.Clear();
        ListItem buff;

        foreach (CitySetupInfo c in _locations)
        {
          if (c != null)
          {
            buff = new ListItem();
            buff.Add("Name", c.Name);
            buff.Add("Id", c.Id);
            _locationsExposed.Add(buff);
          }
        }
        _locationsExposed.FireChange();
      }
    }

    /// <summary>
    /// gets or sets the found Locations
    /// </summary>
    public List<CitySetupInfo> LocationsSearch
    {
      get { return _locationsSearch; }

      set
      {
        _locationsSearch = value;
        if (_locationsSearch == null)
        {
          return;
        }

        _locationsSearchExposed.Clear();
        ListItem buff;
        foreach (CitySetupInfo c in _locationsSearch)
        {
          if (c != null)
          {
            buff = new ListItem();
            buff.Add("Name", c.Name);
            buff.Add("Id", c.Id);
            _locationsSearchExposed.Add(buff);
          }
        }
        _locationsSearchExposed.FireChange();
      }
    }

    /// <summary>
    /// exposes the available locations
    /// </summary>
    public ItemsCollection SetupLocations
    {
      get { return _locationsExposed; }
    }

    /// <summary>
    /// exposes the search result
    /// </summary>
    public ItemsCollection SetupSearchLocations
    {
      get { return _locationsSearchExposed; }
    }
  }
}
