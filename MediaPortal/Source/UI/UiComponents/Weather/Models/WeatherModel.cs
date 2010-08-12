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

using System;
using System.Collections.Generic;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Weather.Grabbers;

namespace MediaPortal.UiComponents.Weather.Models
{
  /// <summary>
  /// Main model of the Weather plugin.
  /// </summary>
  public class WeatherModel : IWorkflowModel
  {
    #region Private/protected fields

    public const string WEATHER_MODEL_ID_STR = "92BDB53F-4159-4dc2-B212-6083C820A214";

    protected object _syncObj = new object();
    protected List<City> _locations = null;

    protected AbstractProperty _currentLocationProperty = null;
    protected ItemsList _locationsList = null;
    protected AbstractProperty _isRefreshingProperty = null;


    private String _preferredLocationCode;

    #endregion

    #region Ctor 

    public WeatherModel()
    {
      // See if we already have a weather catcher in ServiceRegistration, if not, add one
      if (!ServiceRegistration.IsRegistered<IWeatherCatcher>())
        ServiceRegistration.Add<IWeatherCatcher>(new WeatherDotComCatcher());
    }

    #endregion

    #region Public properties

    public AbstractProperty CurrentLocationProperty
    {
      get { return _currentLocationProperty; }
    }

    /// <summary>
    /// Exposes the current location to the skin.
    /// </summary>
    public City CurrentLocation
    {
      get { return (City) _currentLocationProperty.GetValue(); }
    }

    /// <summary>
    /// Gets the list of loaded locations.
    /// </summary>
    public List<City> Locations
    {
      get { return _locations; }
    }

    /// <summary>
    /// Exposes the loaded locations to the skin (Name, Id).
    /// </summary>
    public ItemsList LocationsList
    {
      get { return _locationsList; }
    }

    public AbstractProperty IsRefreshingProperty
    {
      get { return _isRefreshingProperty; }
    }

    public bool IsRefreshing
    {
      get { return (bool) _isRefreshingProperty.GetValue(); }
      set { _isRefreshingProperty.SetValue(value); }
    }

    #endregion

    #region Public methods


    /// <summary>
    /// provides command for the skin to update the location with new data
    /// </summary>
    /// <returns></returns>
    public void Refresh()
    {
      StartBackgroundRefresh(CurrentLocation);
    }

    /// <summary>
    /// provides command for the skin to change the current location
    /// </summary>
    /// <param name="item">The item.</param>
    public void ChangeLocation(ListItem item)
    {
      // we need to find the correct city now... do this by searching for the id in the 
      // locations list
      string strLoc = item["Id"];

      City found = Locations.Find(currentItem => currentItem.Id.Equals(strLoc));

      if (found == null) return;

      // okay, if we found the correct location, update the lists
      _preferredLocationCode = found.Id;
      StartBackgroundRefresh(found);

      // also save the last selected city to settings
      WeatherSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<WeatherSettings>();
      settings.LocationCode = found.Id;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    #endregion

    #region Private members

    /// <summary>
    /// this gets the locations from settings
    /// </summary>
    protected void GetLocationsFromSettings(bool shouldFire)
    {
      // empty lists
      _locationsList.Clear();
      _locations.Clear();
      // add citys from settings to the locations list
      WeatherSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<WeatherSettings>();

      if (settings == null || settings.LocationsList == null)
        return;

      _preferredLocationCode = settings.LocationCode;

      foreach (CitySetupInfo loc in settings.LocationsList)
        AddCityToLocations(loc);

      // if there is no city selected until yet, choose the first one
      if (settings.LocationCode.Equals("<none>") && _locations.Count > 0)
      {
        // Fetch data in background
        _preferredLocationCode = _locations[0].Id;
        StartBackgroundRefresh(_locations[0]);
      }

      // we've added new citys, so update the locations collection
      if (shouldFire)
        _locationsList.FireChange();
    }

    /// <summary>
    /// Starts the refresh of weather data in a background thread.
    /// </summary>
    /// <param name="cityToRefresh">City which should be refreshed.</param>
    private void StartBackgroundRefresh(City cityToRefresh)
    {
      ThreadPool.QueueUserWorkItem(BackgroundRefresh, cityToRefresh);
    }


    /// <summary>
    /// Updates the given location with new data.
    /// </summary>
    /// <param name="threadArgument">City which should be refreshed.</param>
    private void BackgroundRefresh(object threadArgument)
    {
      ServiceRegistration.Get<ILogger>().Debug("Weather: Background refresh");
      City currentLocation;
      AbstractProperty isRefreshingProperty;
      lock (_syncObj)
      {
        // Don't use properties outside this lock as the underlaying instances might have been asynchronously set to null
        currentLocation = _currentLocationProperty == null ? null : (City) _currentLocationProperty.GetValue();
        isRefreshingProperty = _isRefreshingProperty;
        if (currentLocation == null || isRefreshingProperty == null)
          // Asynchronously already disposed
          return;
      }
      isRefreshingProperty.SetValue(true);
      try
      {
        City cityToRefresh = (City) threadArgument;

        bool result = ServiceRegistration.Get<IWeatherCatcher>().GetLocationData(cityToRefresh);

        ServiceRegistration.Get<ILogger>().Info(
            result ? "WeatherModel: Loaded weather data for {0}, {1}" : "WeatherModel: Failed to load weather data for {0}, {1}",
            cityToRefresh.Name, cityToRefresh.Id);

        // Copy the data to the skin property.
        if (cityToRefresh.Id.Equals(_preferredLocationCode))
          currentLocation.Copy(cityToRefresh);

        ServiceRegistration.Get<ILogger>().Debug("Weather: Background refresh end");
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Weather: Error refreshing city '{0}'", e, threadArgument);
      }
      finally
      {
        isRefreshingProperty.SetValue(false);
      }
    }

    /// <summary>
    /// Adds a single city to locations list.
    /// </summary>
    /// <param name="loc">city setup info</param>
    private void AddCityToLocations(CitySetupInfo loc)
    {
      if (loc == null) return;

      City city = new City(loc);
      _locations.Add(city);

      ListItem item = new ListItem();
      item.SetLabel("Name", loc.Name);
      item.SetLabel("Id", loc.Id);
      _locationsList.Add(item);

      StartBackgroundRefresh(city);
    }
    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(WEATHER_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      lock (_syncObj)
      {
        _currentLocationProperty = new WProperty(typeof(City), new City("No Data", "No Data"));
        _locations = new List<City>();
        _locationsList = new ItemsList();
        _isRefreshingProperty = new WProperty(typeof(bool), false);
      }

      // Add citys from settings to the locations list
      GetLocationsFromSettings(true);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      lock (_syncObj)
      {
        _currentLocationProperty = null;
        _locations = null;
        _locationsList = null;
        _isRefreshingProperty = null;
      }
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do here
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