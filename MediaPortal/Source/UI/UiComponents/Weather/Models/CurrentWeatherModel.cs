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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UiComponents.Weather.Settings;
using System;
using System.Threading;

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

    protected readonly SettingsChangeWatcher<WeatherSettings> _settings = new SettingsChangeWatcher<WeatherSettings>();

    protected readonly AbstractProperty _currentLocationProperty = new WProperty(typeof(City), City.NoData);

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
      : base(true, WEATHER_UPDATE_INTERVAL)
    {
      // do initial update in its own thread to avoid delay during startup of MP2
      ServiceRegistration.Get<IThreadPool>().Add(SetAndUpdatePreferredLocation, "SetAndUpdatePreferredLocation", QueuePriority.Normal, ThreadPriority.BelowNormal);
      SubscribeToMessages();
      _settings.SettingsChanged += Update;
    }

    private void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(LocalizationMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == LocalizationMessaging.CHANNEL &&
          ((LocalizationMessaging.MessageType) message.MessageType) == LocalizationMessaging.MessageType.LanguageChanged)
        Update();
    }

    protected void Update(object sender, EventArgs e)
    {
      // Only react on city changes here. Regular weather updates are done timer based.
      if (_settings.Settings.LocationCode != CurrentLocation.Id)
        Update();
    }

    protected override void Update()
    {
      // Do update in its own thread to avoid delay
      ServiceRegistration.Get<IThreadPool>().Add(SetAndUpdatePreferredLocation, "SetAndUpdatePreferredLocation", QueuePriority.Normal, ThreadPriority.BelowNormal);
    }

    protected void SetAndUpdatePreferredLocation()
    {
      CurrentLocation.Copy(City.NoData);

      if (_settings.Settings.LocationsList == null)
        return;

      CitySetupInfo city = _settings.Settings.LocationsList.Find(loc => loc.Id == _settings.Settings.LocationCode);
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
      {
      }

      ServiceRegistration.Get<ILogger>().Info(result
                                                ? "CurrentWeatherModel: Loaded weather data for {0}, {1}"
                                                : "CurrentWeatherModel: Failed to load weather data for {0}, {1}",
                                              CurrentLocation.Name, CurrentLocation.Id);
    }

    public override void Dispose()
    {
      _settings.Dispose();
      base.Dispose();
    }
  }
}
