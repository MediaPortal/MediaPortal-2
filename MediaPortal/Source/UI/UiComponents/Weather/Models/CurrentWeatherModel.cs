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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UiComponents.Weather.Settings;

namespace MediaPortal.UiComponents.Weather.Models
{
  /// <summary>
  /// CurrentWeatherModel exposes the weather data for the selected location. It refreshes the information automatically in given interval (<see cref="WEATHER_UPDATE_INTERVAL"/>).
  /// </summary>
  public class CurrentWeatherModel : BaseTimerControlledModel
  {
    /// <summary>
    /// Update interval for current location.
    /// </summary>
    private const int WEATHER_UPDATE_INTERVAL = 30 * 60 * 1000;
    protected readonly AbstractProperty _currentLocationProperty = new WProperty(typeof(City), new City("No Data", "No Data"));

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
      set { _currentLocationProperty.SetValue(value); }
    }

    /// <summary>
    /// Contructs a new instance of <see cref="CurrentWeatherModel"/>. 
    /// </summary>
    public CurrentWeatherModel()
      : base(WEATHER_UPDATE_INTERVAL)
    {
      SetAndUpdatePreferredLocation();
      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(WeatherMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WeatherMessaging.CHANNEL)
      {
        if (((WeatherMessaging.MessageType) message.MessageType) == WeatherMessaging.MessageType.LocationChanged)
          Update();
      }
    }
    protected override void Update()
    {
      SetAndUpdatePreferredLocation();
    }

    protected void SetAndUpdatePreferredLocation()
    {
      WeatherSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<WeatherSettings>();
      if (settings == null || settings.LocationsList == null)
        return;

      CitySetupInfo city = settings.LocationsList.Find(loc => loc.Id == settings.LocationCode);
      if (city == null)
        return;
      bool result = false;
      try
      {
        City newLocation = new City(city);
        if (ServiceRegistration.Get<IWeatherCatcher>().GetLocationData(newLocation))
        {
          CurrentLocation.Copy(newLocation);
          result = true;
        }
      }
      catch (Exception)
      { }

      ServiceRegistration.Get<ILogger>().Info(result ?
          "WeatherModel: Loaded weather data for {0}, {1}" : "WeatherModel: Failed to load weather data for {0}, {1}",
          CurrentLocation.Name, CurrentLocation.Id);
    }
  }
}
