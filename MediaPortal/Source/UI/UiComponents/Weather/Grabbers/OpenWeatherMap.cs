#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UiComponents.Weather.Settings;
using OpenWeatherMap;

namespace MediaPortal.UiComponents.Weather.Grabbers
{
  public class OpenWeatherMapCatcher : IWeatherCatcher, IApiToken, IDisposable
  {
    public const string SERVICE_NAME = "OpenWeatherMap.com";

    internal static string[] KEYS = {
      "91bab7dff33dbb8f3b2ac644272188fa",
      "dd1ec26c30b60782b76b5cd4beb2cd0b",
      "c700353fc67ddb8e6dee46b7f6ab64eb"
    };

    public string ApiToken { get; set; }

    private readonly Dictionary<int, int> _weatherCodeTranslation = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _weatherCodeTranslationNight = new Dictionary<int, int>();

    private OpenWeatherMapLanguage _language;
    private MetricSystem _metricSystem;
    private DateTimeFormatInfo _dateFormat;
    private int _keyIndex = 0;
    private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private Dictionary<int, Tuple<City, DateTime>> _cache = new Dictionary<int, Tuple<City, DateTime>>();
    private TimeSpan MAX_CACHE_DURATION = TimeSpan.FromMinutes(30);
    private SettingsChangeWatcher<WeatherSettings> _settings = new SettingsChangeWatcher<WeatherSettings>(true);

    public OpenWeatherMapCatcher()
    {
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _dateFormat = currentCulture.DateTimeFormat;
      _metricSystem = new RegionInfo(currentCulture.Name).IsMetric ? MetricSystem.Metric : MetricSystem.Imperial;
      if (!Enum.TryParse(currentCulture.TwoLetterISOLanguageName, true, out _language))
        _language = OpenWeatherMapLanguage.EN;

      // Start with a random key
      _keyIndex = new Random(DateTime.Now.Millisecond).Next(KEYS.Length);
      _settings.SettingsChanged += (sender, args) => ApiToken = _settings.Settings.ApiKey;

      #region Weather code translation

      // See https://openweathermap.org/weather-conditions
      // See https://github.com/ronie/weather.openweathermap.extended/blob/master/resources/lib/utils.py#L118
      _weatherCodeTranslation[200] = 4;
      _weatherCodeTranslation[200] = 4;
      _weatherCodeTranslation[201] = 4;
      _weatherCodeTranslation[202] = 3;
      _weatherCodeTranslation[210] = 4;
      _weatherCodeTranslation[211] = 4;
      _weatherCodeTranslation[212] = 3;
      _weatherCodeTranslation[221] = 38;
      _weatherCodeTranslation[230] = 4;
      _weatherCodeTranslation[231] = 4;
      _weatherCodeTranslation[232] = 4;
      _weatherCodeTranslation[300] = 9;
      _weatherCodeTranslation[301] = 9;
      _weatherCodeTranslation[302] = 9;
      _weatherCodeTranslation[310] = 9;
      _weatherCodeTranslation[311] = 9;
      _weatherCodeTranslation[312] = 9;
      _weatherCodeTranslation[313] = 9;
      _weatherCodeTranslation[314] = 9;
      _weatherCodeTranslation[321] = 9;
      _weatherCodeTranslation[500] = 11;
      _weatherCodeTranslation[501] = 11;
      _weatherCodeTranslation[502] = 11;
      _weatherCodeTranslation[503] = 11;
      _weatherCodeTranslation[504] = 11;
      _weatherCodeTranslation[511] = 11;
      _weatherCodeTranslation[520] = 11;
      _weatherCodeTranslation[521] = 11;
      _weatherCodeTranslation[522] = 11;
      _weatherCodeTranslation[531] = 40;
      _weatherCodeTranslation[600] = 14;
      _weatherCodeTranslation[601] = 16;
      _weatherCodeTranslation[602] = 41;
      _weatherCodeTranslation[611] = 18;
      _weatherCodeTranslation[612] = 6;
      _weatherCodeTranslation[615] = 5;
      _weatherCodeTranslation[616] = 5;
      _weatherCodeTranslation[620] = 14;
      _weatherCodeTranslation[621] = 46;
      _weatherCodeTranslation[622] = 43;
      _weatherCodeTranslation[701] = 20;
      _weatherCodeTranslation[711] = 22;
      _weatherCodeTranslation[721] = 21;
      _weatherCodeTranslation[731] = 19;
      _weatherCodeTranslation[741] = 20;
      _weatherCodeTranslation[751] = 19;
      _weatherCodeTranslation[761] = 19;
      _weatherCodeTranslation[762] = 19;
      _weatherCodeTranslation[771] = 2;
      _weatherCodeTranslation[781] = 0;
      _weatherCodeTranslation[800] = 32;
      _weatherCodeTranslation[801] = 34;
      _weatherCodeTranslation[802] = 30;
      _weatherCodeTranslation[803] = 30;
      _weatherCodeTranslation[804] = 28;
      _weatherCodeTranslation[900] = 0;
      _weatherCodeTranslation[901] = 1;
      _weatherCodeTranslation[902] = 2;
      _weatherCodeTranslation[903] = 25;
      _weatherCodeTranslation[904] = 36;
      _weatherCodeTranslation[905] = 24;
      _weatherCodeTranslation[906] = 17;
      _weatherCodeTranslation[951] = 33;
      _weatherCodeTranslation[952] = 24;
      _weatherCodeTranslation[953] = 24;
      _weatherCodeTranslation[954] = 24;
      _weatherCodeTranslation[955] = 24;
      _weatherCodeTranslation[956] = 24;
      _weatherCodeTranslation[957] = 23;
      _weatherCodeTranslation[958] = 23;
      _weatherCodeTranslation[959] = 23;
      _weatherCodeTranslation[960] = 4;
      _weatherCodeTranslation[961] = 3;
      _weatherCodeTranslation[962] = 2;

      _weatherCodeTranslationNight[200] = 47;
      _weatherCodeTranslationNight[201] = 47;
      _weatherCodeTranslationNight[202] = 47;
      _weatherCodeTranslationNight[210] = 47;
      _weatherCodeTranslationNight[211] = 47;
      _weatherCodeTranslationNight[212] = 47;
      _weatherCodeTranslationNight[221] = 47;
      _weatherCodeTranslationNight[230] = 47;
      _weatherCodeTranslationNight[231] = 47;
      _weatherCodeTranslationNight[232] = 47;
      _weatherCodeTranslationNight[300] = 45;
      _weatherCodeTranslationNight[301] = 45;
      _weatherCodeTranslationNight[302] = 45;
      _weatherCodeTranslationNight[310] = 45;
      _weatherCodeTranslationNight[311] = 45;
      _weatherCodeTranslationNight[312] = 45;
      _weatherCodeTranslationNight[313] = 45;
      _weatherCodeTranslationNight[314] = 45;
      _weatherCodeTranslationNight[321] = 45;
      _weatherCodeTranslationNight[500] = 45;
      _weatherCodeTranslationNight[501] = 45;
      _weatherCodeTranslationNight[502] = 45;
      _weatherCodeTranslationNight[503] = 45;
      _weatherCodeTranslationNight[504] = 45;
      _weatherCodeTranslationNight[511] = 45;
      _weatherCodeTranslationNight[520] = 45;
      _weatherCodeTranslationNight[521] = 45;
      _weatherCodeTranslationNight[522] = 45;
      _weatherCodeTranslationNight[531] = 45;
      _weatherCodeTranslationNight[600] = 46;
      _weatherCodeTranslationNight[601] = 46;
      _weatherCodeTranslationNight[602] = 46;
      _weatherCodeTranslationNight[611] = 46;
      _weatherCodeTranslationNight[612] = 46;
      _weatherCodeTranslationNight[615] = 46;
      _weatherCodeTranslationNight[616] = 46;
      _weatherCodeTranslationNight[620] = 46;
      _weatherCodeTranslationNight[621] = 46;
      _weatherCodeTranslationNight[622] = 46;
      _weatherCodeTranslationNight[701] = 29;
      _weatherCodeTranslationNight[711] = 29;
      _weatherCodeTranslationNight[721] = 29;
      _weatherCodeTranslationNight[731] = 29;
      _weatherCodeTranslationNight[741] = 29;
      _weatherCodeTranslationNight[751] = 29;
      _weatherCodeTranslationNight[761] = 29;
      _weatherCodeTranslationNight[762] = 29;
      _weatherCodeTranslationNight[771] = 29;
      _weatherCodeTranslationNight[781] = 29;
      _weatherCodeTranslationNight[800] = 31;
      _weatherCodeTranslationNight[801] = 33;
      _weatherCodeTranslationNight[802] = 29;
      _weatherCodeTranslationNight[803] = 29;
      _weatherCodeTranslationNight[804] = 27;
      _weatherCodeTranslationNight[900] = 29;
      _weatherCodeTranslationNight[901] = 29;
      _weatherCodeTranslationNight[902] = 27;
      _weatherCodeTranslationNight[903] = 33;
      _weatherCodeTranslationNight[904] = 31;
      _weatherCodeTranslationNight[905] = 27;
      _weatherCodeTranslationNight[906] = 45;
      _weatherCodeTranslationNight[951] = 31;
      _weatherCodeTranslationNight[952] = 31;
      _weatherCodeTranslationNight[953] = 33;
      _weatherCodeTranslationNight[954] = 33;
      _weatherCodeTranslationNight[955] = 29;
      _weatherCodeTranslationNight[956] = 29;
      _weatherCodeTranslationNight[957] = 29;
      _weatherCodeTranslationNight[958] = 27;
      _weatherCodeTranslationNight[959] = 27;
      _weatherCodeTranslationNight[960] = 27;
      _weatherCodeTranslationNight[961] = 45;
      _weatherCodeTranslationNight[962] = 45;

      #endregion
    }

    public async Task<bool> GetLocationData(City city)
    {
      int cityId;
      // Other grabbers store string IDs and this would fail here
      if (city.Grabber != GetServiceName() || !int.TryParse(city.Id, out cityId))
        return false;

      Tuple<City, DateTime> data;
      await _lock.WaitAsync();
      try
      {
        if (_cache.TryGetValue(cityId, out data) && DateTime.Now - data.Item2 <= MAX_CACHE_DURATION)
        {
          city.Copy(data.Item1);
          return true;
        }

        await new SynchronizationContextRemover();
        var client = new OpenWeatherMapClient(GetKey());
        var currentWeather = await client.CurrentWeather.GetByCityId(cityId, _metricSystem, _language);

        city.Condition.Temperature = FormatTemp(currentWeather.Temperature.Value, currentWeather.Temperature.Unit);
        city.Condition.Humidity = string.Format("{0} {1}", currentWeather.Humidity.Value, currentWeather.Humidity.Unit);
        city.Condition.Pressure = string.Format("{0:F0} {1}", currentWeather.Pressure.Value, currentWeather.Pressure.Unit);
        city.Condition.Precipitation = string.Format("{0} {1}", currentWeather.Precipitation.Value, currentWeather.Precipitation.Unit);
        city.Condition.Wind = string.Format("{0} {1}", currentWeather.Wind.Speed.Name, currentWeather.Wind.Direction.Name);
        city.Condition.Condition = currentWeather.Weather.Value;
        var now = DateTime.Now;
        bool isNight = now >= currentWeather.City.Sun.Set || now < currentWeather.City.Sun.Rise;
        city.Condition.BigIcon = @"Weather\128x128\" + GetWeatherIcon(currentWeather.Weather.Number, isNight);
        city.Condition.SmallIcon = @"Weather\64x64\" + GetWeatherIcon(currentWeather.Weather.Number, isNight);

        var forecasts = await client.Forecast.GetByCityId(cityId, true, _metricSystem, _language);
        foreach (var forecast in forecasts.Forecast)
        {
          DayForecast dayForecast = new DayForecast();
          dayForecast.High = FormatTemp(forecast.Temperature.Max, currentWeather.Temperature.Unit);
          dayForecast.Low = FormatTemp(forecast.Temperature.Min, currentWeather.Temperature.Unit);

          dayForecast.Humidity = string.Format("{0} {1}", forecast.Humidity.Value, forecast.Humidity.Unit);
          // TODO:
          //dayForecast.Pressure = string.Format("{0} {1}", forecast.Pressure.Value, forecast.Pressure.Unit);
          dayForecast.Precipitation = string.Format("{0} {1}", forecast.Precipitation.Value, forecast.Precipitation.Unit);
          dayForecast.Wind = string.Format("{0} {1}", forecast.WindSpeed.Mps, currentWeather.Wind.Direction.Name);
          dayForecast.Overview = forecast.Symbol.Name;
          dayForecast.BigIcon = @"Weather\128x128\" + GetWeatherIcon(forecast.Symbol.Number, false);
          dayForecast.SmallIcon = @"Weather\64x64\" + GetWeatherIcon(forecast.Symbol.Number, false);
          string fomattedDate = forecast.Day.ToString(_dateFormat.ShortDatePattern, _dateFormat);
          string day = _dateFormat.GetAbbreviatedDayName(forecast.Day.DayOfWeek);
          dayForecast.Day = String.Format("{0} {1}", day, fomattedDate);

          city.ForecastCollection.Add(dayForecast);
        }

        city.ForecastCollection.FireChange();
        _cache[cityId] = new Tuple<City, DateTime>(city, DateTime.Now);
      }
      finally
      {
        _lock.Release();
      }
      return true;
    }

    public async Task<List<CitySetupInfo>> FindLocationsByName(string name)
    {
      await new SynchronizationContextRemover();

      var client = new OpenWeatherMapClient(GetKey());
      var parts = name.Split(',');
      bool isCoord = false;
      Coordinates coordinates = new Coordinates();
      double lat;
      double lon;
      if (parts.Length == 2 &&
        double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out lat) &&
        double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out lon))
      {
        coordinates.Latitude = lat;
        coordinates.Longitude = lon;
        isCoord = true;
      }

      var location = isCoord ?
          await client.Search.GetByCoordinates(coordinates, _metricSystem, _language).ConfigureAwait(false) :
          await client.Search.GetByName(name, _metricSystem, _language).ConfigureAwait(false);

      var cities = new List<CitySetupInfo>();
      bool useCoords = location.Count > 1;
      foreach (var city in location.List)
      {
        var setup = new CitySetupInfo(city.City.Name, city.City.Id.ToString(), SERVICE_NAME);
        if (useCoords)
          setup.Detail = string.Format("Lat. {0:F2} Lon. {1:F2}", city.City.Coordinates.Latitude, city.City.Coordinates.Longitude);
        setup.Grabber = GetServiceName();
        cities.Add(setup);
      }
      return cities;
    }

    public string GetServiceName()
    {
      return "OpenWeatherMap";
    }

    private string GetKey()
    {
      if (!String.IsNullOrEmpty(ApiToken))
        return ApiToken;
      var key = KEYS[_keyIndex];
      _keyIndex = (++_keyIndex) % KEYS.Length;
      return key;
    }

    private string FormatTemp(double temperatureValue, string temperatureUnit)
    {
      var temp = temperatureValue;
      string unit;
      switch (temperatureUnit)
      {
        case "metric":
          unit = "°C";
          break;
        case "fahrenheit":
          unit = "°F";
          break;
        default:
          unit = null;
          break;
      }
      temp = Math.Round(temp, 1);
      return string.Format("{0} {1}", temp, unit);
    }

    private string GetWeatherIcon(int weatherCode, bool isNight)
    {
      int translatedId;
      var dict = isNight ? _weatherCodeTranslationNight : _weatherCodeTranslation;
      if (dict.TryGetValue(weatherCode, out translatedId))
        return translatedId + ".png";
      return "na.png";
    }

    public static double ToDegree(double tempKelvin)
    {
      return tempKelvin - 273.15;
    }

    public static double ToFahrenheit(double tempKelvin)
    {
      return tempKelvin * 9 / 5 - 459.67;
    }

    public void Dispose()
    {
      _lock?.Dispose();
      _settings?.Dispose();
    }
  }
}
