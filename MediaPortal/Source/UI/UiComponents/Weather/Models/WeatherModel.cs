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
using MediaPortal.UiComponents.Weather.Grabbers;

namespace MediaPortal.UiComponents.Weather.Models
{
  /// <summary>
  /// Main view model of the Weather plugin.
  /// </summary>
  public class WeatherModel
  {
    #region Private/protected fields

    public const string WEATHER_MODEL_ID_STR = "92BDB53F-4159-4dc2-B212-6083C820A214";

    private readonly List<City> _locations = new List<City>();

    private readonly AbstractProperty _currentLocation;
    private readonly ItemsList _locationsList = new ItemsList();

    private String _preferredLocationCode;

    #endregion

    #region Ctor 

    public WeatherModel()
    {
      _currentLocation = new WProperty(typeof (City), new City("No Data", "No Data"));
      // for testing purposes add a weathercatcher
      ServiceRegistration.Add<IWeatherCatcher>(new WeatherDotComCatcher());
      // add citys from settings to the locations list
      GetLocationsFromSettings(true);
    }

    #endregion

    #region Public properties

    public AbstractProperty CurrentLocationProperty
    {
      get { return _currentLocation; }
    }

    /// <summary>
    /// Exposes the current location to the skin.
    /// </summary>
    public City CurrentLocation
    {
      get { return (City) _currentLocation.GetValue(); }
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
    /// <param name="cityToRefresh">City</param>
    private void StartBackgroundRefresh(City cityToRefresh)
    {
      ThreadPool.QueueUserWorkItem(BackgroundRefresh, cityToRefresh);
    }


    /// <summary>
    /// Updates the location with new data.
    /// </summary>
    /// <param name="threadArgument">City</param>
    private void BackgroundRefresh(object threadArgument)
    {
      ServiceRegistration.Get<ILogger>().Debug("Weather: background refresh start");

      City cityToRefresh = (City) threadArgument;

      bool result = ServiceRegistration.Get<IWeatherCatcher>().GetLocationData(cityToRefresh);

      ServiceRegistration.Get<ILogger>().Info(
        result ? "Loaded Weather Data for {0}, {1}" : "Failed to load Weather Data for {0}, {1}",
        cityToRefresh.Name,
        cityToRefresh.Id);

      // Copy the data to the skin property.
      if (cityToRefresh.Id.Equals(_preferredLocationCode))
        CurrentLocation.Copy(cityToRefresh);

      ServiceRegistration.Get<ILogger>().Debug("Weather: background refresh end");
    }

    /// <summary>
    /// Adds a single city to locations list.
    /// </summary>
    /// <param name="loc">city setup info</param>
    private void AddCityToLocations(CitySetupInfo loc)
    {
      if (loc == null) return;

      City buffLoc = new City(loc);
      _locations.Add(buffLoc);

      ListItem buffItem = new ListItem();
      buffItem.SetLabel("Name", loc.Name);
      buffItem.SetLabel("Id", loc.Id);
      _locationsList.Add(buffItem);

      StartBackgroundRefresh(buffLoc);
    }
    #endregion
  }
}