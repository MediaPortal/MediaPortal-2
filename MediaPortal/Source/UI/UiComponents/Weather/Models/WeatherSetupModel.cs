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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Weather.Settings;


namespace MediaPortal.UiComponents.Weather.Models
{
  /// <summary>
  /// Workflow model for the weather setup.
  /// </summary>
  public class WeatherSetupModel : IWorkflowModel
  {
    public const string WEATHER_SETUP_MODEL_ID_STR = "CF0434F2-B319-48ff-A700-0BB7F0C2CD2A";

    // Locations that are already in the list
    private List<CitySetupInfo> _locations = null;
    // Locations that return as result of searching for a city
    private List<CitySetupInfo> _locationsSearch = null; 

    // Variants of the above that is exposed to the skin
    private ItemsList _locationsExposed = null; 
    private ItemsList _locationsSearchExposed = null;

    private AbstractProperty _searchCityProperty = null;

    public AbstractProperty SearchCityProperty
    {
      get { return _searchCityProperty; }
    }

    /// <summary>
    /// Exposes the current search string to the skin.
    /// </summary>
    public string SearchCity
    {
      get { return (string) _searchCityProperty.GetValue(); }
      set { _searchCityProperty.SetValue(value); }
    }

    /// <summary>
    /// Loads all locations from the settings.
    /// </summary>
    private void GetLocationsFromSettings()
    {
      WeatherSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<WeatherSettings>();
      Locations = settings.LocationsList ?? new List<CitySetupInfo>();
    }

    /// <summary>
    /// Search for a location name and fill up the _locationsSearch list.
    /// </summary>
    /// <param name="name"></param>
    public void SearchLocations(string name)
    {
      LocationsSearch = ServiceRegistration.Get<IWeatherCatcher>().FindLocationsByName(name);
    }

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      WeatherSettings settings = settingsManager.Load<WeatherSettings>();
      // Apply new locations list
      settings.LocationsList = Locations;
      settingsManager.Save(settings);
    }

    /// <summary>
    /// this will add a location
    /// to the _locationsExposed and _locations list
    /// </summary>
    /// <param name="item"></param>
    public void AddLocation(ListItem item)
    {
      // Don't add it if it's already in there
      foreach (ListItem i in _locationsExposed)
        if (i["Id"].Equals(item["Id"]))
          return;
      _locationsExposed.Add(item);
      // Create a CitySetupObject and add it to the loctions list
      CitySetupInfo c = new CitySetupInfo(item["Name"], item["Id"]);
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
          return;

        _locationsExposed.Clear();
        ListItem item;

        foreach (CitySetupInfo c in _locations)
        {
          if (c != null)
          {
            item = new ListItem();
            item.SetLabel("Name", c.Name);
            item.SetLabel("Id", c.Id);
            _locationsExposed.Add(item);
          }
        }
        _locationsExposed.FireChange();
      }
    }

    /// <summary>
    /// Gets or sets the locations which were found.
    /// </summary>
    public List<CitySetupInfo> LocationsSearch
    {
      get { return _locationsSearch; }
      set
      {
        _locationsSearch = value;
        if (_locationsSearch == null)
          return;

        _locationsSearchExposed.Clear();
        ListItem item;
        foreach (CitySetupInfo c in _locationsSearch)
        {
          if (c != null)
          {
            item = new ListItem();
            item.SetLabel("Name", c.Name);
            item.SetLabel("Id", c.Id);
            _locationsSearchExposed.Add(item);
          }
        }
        _locationsSearchExposed.FireChange();
      }
    }

    /// <summary>
    /// Exposes the available locations.
    /// </summary>
    public ItemsList SetupLocations
    {
      get { return _locationsExposed; }
    }

    /// <summary>
    /// Exposes the search result.
    /// </summary>
    public ItemsList SetupSearchLocations
    {
      get { return _locationsSearchExposed; }
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(WEATHER_SETUP_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _searchCityProperty = new WProperty(typeof(string), string.Empty);
      _locations = new List<CitySetupInfo>();
      _locationsExposed = new ItemsList();
      _locationsSearch = new List<CitySetupInfo>();
      _locationsSearchExposed = new ItemsList();
      // Load settings
      GetLocationsFromSettings();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _locationsExposed.Clear();
      _locationsExposed = null;
      _locationsSearchExposed.Clear();
      _locationsSearchExposed = null;
      _searchCityProperty = null;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // TODO
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}