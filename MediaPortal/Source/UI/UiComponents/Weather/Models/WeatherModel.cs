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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Weather.Settings;

namespace MediaPortal.UiComponents.Weather.Models
{
  /// <summary>
  /// Main model of the Weather plugin.
  /// </summary>
  public class WeatherModel : IWorkflowModel
  {
    #region Consts

    public const string WEATHER_MODEL_ID_STR = "92BDB53F-4159-4dc2-B212-6083C820A214";
    public readonly static Guid WEATHER_MODEL_ID = new Guid(WEATHER_MODEL_ID_STR);

    public const string LAST_UPDATE_TIME_RES = "[Weather.LastUpdateTime]";
    public const string NOT_UPDATED_YET_RES = "[Weather.NotUpdatedYet]";

    protected const string KEY_CITY = "City";

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected List<City> _locations = null;

    protected AbstractProperty _currentLocationProperty = null;
    protected ItemsList _locationsList = null;
    protected AbstractProperty _isUpdatingProperty = null;
    protected AbstractProperty _lastUpdateTimeProperty = null;

    protected String _preferredLocationCode = null;
    protected int? _refreshIntervalSec = null;
    protected IIntervalWork _refreshIntervalWork = null;

    #endregion

    #region Public properties

    /// <summary>
    /// Exposes the current location to the skin.
    /// </summary>
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

    /// <summary>
    /// Exposes the IsUpdating property.
    /// </summary>
    public AbstractProperty IsUpdatingProperty
    {
      get { return _isUpdatingProperty; }
    }

    /// <summary>
    /// Indicates if a background-update is in progress.
    /// </summary>
    public bool IsUpdating
    {
      get { return (bool) _isUpdatingProperty.GetValue(); }
      set { _isUpdatingProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the LastUpdateTimeProperty property.
    /// </summary>
    public AbstractProperty LastUpdateTimeProperty
    {
      get { return _lastUpdateTimeProperty; }
    }

    /// <summary>
    /// Indicates the time of last successful update.
    /// </summary>
    public string LastUpdateTime
    {
      get { return (string) _lastUpdateTimeProperty.GetValue(); }
      set { _lastUpdateTimeProperty.SetValue(value); }
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
    /// Provides command for the skin to change the current location.
    /// </summary>
    /// <param name="item">The location item.</param>
    public void ChangeLocation(ListItem item)
    {
      City city = (City) item.AdditionalProperties[KEY_CITY];

      _preferredLocationCode = city.Id;
      StartBackgroundRefresh(city);

      // also save the last selected city to settings
      WeatherSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<WeatherSettings>();
      settings.LocationCode = city.Id;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    #endregion

    #region Private members

    protected void SetLastUpdateTime(DateTime? updateTime)
    {
      WeatherSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<WeatherSettings>();
      settings.LastUpdate = updateTime;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);

      AbstractProperty lastUpdateTimeProperty;
      lock (_syncObj)
      {
        if (_lastUpdateTimeProperty == null)
          return;
        lastUpdateTimeProperty = _lastUpdateTimeProperty;
      }
      ILocalization localization = ServiceRegistration.Get<ILocalization>();
      CultureInfo culture = localization.CurrentCulture;

      string lastUpdate = LocalizationHelper.Translate(LAST_UPDATE_TIME_RES, updateTime.HasValue ?
          updateTime.Value.ToString(culture) : LocalizationHelper.Translate(NOT_UPDATED_YET_RES));
      lastUpdateTimeProperty.SetValue(lastUpdate);
    }

    protected void ReadSettings(bool shouldFire)
    {
      // add citys from settings to the locations list
      WeatherSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<WeatherSettings>();

      if (settings == null || settings.LocationsList == null)
        return;

      _preferredLocationCode = settings.LocationCode;
      _refreshIntervalSec = settings.RefreshInterval;
      SetLastUpdateTime(settings.LastUpdate);

      foreach (CitySetupInfo loc in settings.LocationsList)
      {
        City city = AddCityToLocations(loc);
        if (loc.Id == _preferredLocationCode)
          StartBackgroundRefresh(city);
      }

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
      ServiceRegistration.Get<ILogger>().Debug("WeatherModel: Background refresh");
      City currentLocation;
      AbstractProperty isUpdatingProperty;
      lock (_syncObj)
      {
        // Don't use properties outside this lock as the underlaying instances might have been asynchronously set to null
        currentLocation = _currentLocationProperty == null ? null : (City) _currentLocationProperty.GetValue();
        isUpdatingProperty = _isUpdatingProperty;
        if (currentLocation == null || isUpdatingProperty == null)
          // Asynchronously already disposed
          return;
      }
      isUpdatingProperty.SetValue(true);
      try
      {
        City cityToRefresh = (City) threadArgument;

        bool result = ServiceRegistration.Get<IWeatherCatcher>().GetLocationData(cityToRefresh);

        ServiceRegistration.Get<ILogger>().Info(result ?
            "WeatherModel: Loaded weather data for {0}, {1}" : "WeatherModel: Failed to load weather data for {0}, {1}",
            cityToRefresh.Name, cityToRefresh.Id);

        // Copy the data to the skin property...
        if (cityToRefresh.Id.Equals(_preferredLocationCode))
          currentLocation.Copy(cityToRefresh);

        // ... and save the last update time to settings
        SetLastUpdateTime(DateTime.Now);

        ServiceRegistration.Get<ILogger>().Debug("WeatherModel: Background refresh end");
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("WeatherModel: Error refreshing city '{0}'", e, threadArgument);
      }
      finally
      {
        isUpdatingProperty.SetValue(false);
      }
    }

    /// <summary>
    /// Adds a single city to locations list.
    /// </summary>
    /// <param name="loc">city setup info</param>
    private City AddCityToLocations(CitySetupInfo loc)
    {
      if (loc == null) return null;

      City city = new City(loc);
      _locations.Add(city);

      ListItem item = new ListItem();
      item.SetLabel("Name", loc.Name);
      item.SetLabel("Id", loc.Id);
      item.AdditionalProperties[KEY_CITY] = city;
      _locationsList.Add(item);
      return city;
    }

    #endregion

    #region Message and Tasks handling

    /// <summary>
    /// Creates a task for background refresh that gets executed every n seconds (refresh interval).
    /// </summary>
    void StartRefreshTask()
    {
      if (_refreshIntervalSec == null)
        return;
      if (_refreshIntervalWork != null)
        return;

      _refreshIntervalWork = new IntervalWork(Refresh, TimeSpan.FromSeconds((int) _refreshIntervalSec));
      ServiceRegistration.Get<IThreadPool>().AddIntervalWork(_refreshIntervalWork, false);
    }

    /// <summary>
    /// Deletes the created task.
    /// </summary>
    void EndRefreshTask()
    {
      if (_refreshIntervalWork == null)
        return;
      ServiceRegistration.Get<IThreadPool>().RemoveIntervalWork(_refreshIntervalWork);
      _refreshIntervalWork = null;
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return WEATHER_MODEL_ID; }
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
        _isUpdatingProperty = new WProperty(typeof(bool), false);
        _lastUpdateTimeProperty = new WProperty(typeof(string), null);
      }

      // Add citys from settings to the locations list
      ReadSettings(true);
      StartRefreshTask();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      EndRefreshTask();
      lock (_syncObj)
      {
        _currentLocationProperty = null;
        _locations = null;
        _locationsList = null;
        _isUpdatingProperty = null;
        _lastUpdateTimeProperty = null;
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

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
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