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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using OpenWeatherMap;

namespace MediaPortal.UiComponents.Weather.Grabbers
{
  public class OpenWeatherMapCatcher : IWeatherCatcher
  {
    public const string SERVICE_NAME = "OpenWeatherMap.com";
    internal const string SERVICE_KEY = "12ca330117b1b4e1e1b7ccbe69360883";

    private readonly Dictionary<int, int> _weatherCodeTranslation = new Dictionary<int, int>();

    private OpenWeatherMapLanguage _language;
    private MetricSystem _metricSystem;
    private DateTimeFormatInfo _dateFormat;

    public OpenWeatherMapCatcher()
    {
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _dateFormat = currentCulture.DateTimeFormat;
      _metricSystem = new RegionInfo(currentCulture.LCID).IsMetric ? MetricSystem.Metric : MetricSystem.Imperial;
      _language = OpenWeatherMapLanguage.EN;
      _metricSystem = MetricSystem.Metric;

      #region Weather code translation

      // See https://openweathermap.org/weather-conditions
      // TODO: Adopt mapping from here: https://dl.team-mediaportal.com/2016/10/280291_WorldWeatherIconMapping.xml

      // Done:
      _weatherCodeTranslation[800] = 32; // Clear/Sunny
      _weatherCodeTranslation[801] = 30; // Partly Cloudy (scattered clouds)
      _weatherCodeTranslation[802] = 30; // Partly Cloudy
      _weatherCodeTranslation[803] = 26; // Cloudy (broken clouds)
      _weatherCodeTranslation[804] = 26; // Overcast (overcast clouds)

      // TODO:
      _weatherCodeTranslation[395] = 42; //  Moderate or heavy snow in area with thunder
      _weatherCodeTranslation[392] = 14; //  Patchy light snow in area with thunder
      _weatherCodeTranslation[389] = 40; //  Moderate or heavy rain in area with thunder
      _weatherCodeTranslation[386] = 3; //  Patchy light rain in area with thunder
      _weatherCodeTranslation[377] = 18; //  Moderate or heavy showers of ice pellets
      _weatherCodeTranslation[374] = 18; //  Light showers of ice pellets
      _weatherCodeTranslation[371] = 16; //  Moderate or heavy snow showers
      _weatherCodeTranslation[368] = 14; //  Light snow showers
      _weatherCodeTranslation[365] = 6; //  Moderate or heavy sleet showers
      _weatherCodeTranslation[362] = 6; //  Light sleet showers
      _weatherCodeTranslation[359] = 12; //  Torrential rain shower
      _weatherCodeTranslation[356] = 40; //  Moderate or heavy rain shower
      _weatherCodeTranslation[353] = 39; //  Light rain shower
      _weatherCodeTranslation[350] = 18; //  Ice pellets
      _weatherCodeTranslation[338] = 42; //  Heavy snow
      _weatherCodeTranslation[335] = 16; //  Patchy heavy snow
      _weatherCodeTranslation[332] = 41; //  Moderate snow
      _weatherCodeTranslation[329] = 14; //  Patchy moderate snow
      _weatherCodeTranslation[326] = 14; //  Light snow
      _weatherCodeTranslation[323] = 14; //  Patchy light snow
      _weatherCodeTranslation[320] = 6; //  Moderate or heavy sleet
      _weatherCodeTranslation[317] = 6; //  Light sleet
      _weatherCodeTranslation[314] = 10; //  Moderate or Heavy freezing rain
      _weatherCodeTranslation[311] = 10; //  Light freezing rain
      _weatherCodeTranslation[308] = 40; //  Heavy rain
      _weatherCodeTranslation[305] = 39; //  Heavy rain at times
      _weatherCodeTranslation[302] = 40; //  Moderate rain
      _weatherCodeTranslation[299] = 39; //  Moderate rain at times
      _weatherCodeTranslation[296] = 11; //  Light rain
      _weatherCodeTranslation[293] = 11; //  Patchy light rain
      _weatherCodeTranslation[284] = 8; //  Heavy freezing drizzle
      _weatherCodeTranslation[281] = 8; //  Freezing drizzle
      _weatherCodeTranslation[266] = 9; //  Light drizzle
      _weatherCodeTranslation[263] = 9; //  Patchy light drizzle
      _weatherCodeTranslation[260] = 20; //  Freezing fog
      _weatherCodeTranslation[248] = 20; //  Fog
      _weatherCodeTranslation[230] = 42; //  Blizzard
      _weatherCodeTranslation[227] = 43; //  Blowing snow
      _weatherCodeTranslation[200] = 35; //  Thundery outbreaks in nearby
      _weatherCodeTranslation[185] = 8; //  Patchy freezing drizzle nearby
      _weatherCodeTranslation[182] = 6; //  Patchy sleet nearby
      _weatherCodeTranslation[179] = 41; //  Patchy snow nearby
      _weatherCodeTranslation[176] = 39; //  Patchy rain nearby
      _weatherCodeTranslation[143] = 20; //  Mist

      #endregion
    }

    public bool GetLocationData(City city)
    {
      var client = new OpenWeatherMapClient(SERVICE_KEY);
      var currentWeather = Task.Run(async () => await client.CurrentWeather.GetByCityId(int.Parse(city.Id), _metricSystem, _language)).Result;

      city.Condition.Temperature = FormatTemp(currentWeather.Temperature.Value, currentWeather.Temperature.Unit);
      city.Condition.Humidity = string.Format("{0} {1}", currentWeather.Humidity.Value, currentWeather.Humidity.Unit);
      city.Condition.Pressure = string.Format("{0} {1}", currentWeather.Pressure.Value, currentWeather.Pressure.Unit);
      city.Condition.Precipitation = string.Format("{0} {1}", currentWeather.Precipitation.Value, currentWeather.Precipitation.Unit);
      city.Condition.Wind = string.Format("{0} {1}", currentWeather.Wind.Speed.Name, currentWeather.Wind.Direction.Name);
      city.Condition.Condition = currentWeather.Weather.Value;
      city.Condition.BigIcon = @"Weather\128x128\" + GetWeatherIcon(currentWeather.Weather.Number);
      city.Condition.SmallIcon = @"Weather\64x64\" + GetWeatherIcon(currentWeather.Weather.Number);

      var forecasts = Task.Run(async () => await client.Forecast.GetByCityId(int.Parse(city.Id), true, _metricSystem, _language)).Result;
      foreach (var forecast in forecasts.Forecast)
      {
        DayForecast dayForecast = new DayForecast();
        dayForecast.High = FormatTemp(forecast.Temperature.Max, forecast.Temperature.Unit);
        dayForecast.Low = FormatTemp(forecast.Temperature.Min, forecast.Temperature.Unit);

        dayForecast.Humidity = string.Format("{0} {1}", forecast.Humidity.Value, forecast.Humidity.Unit);
        // TODO:
        //dayForecast.Pressure = string.Format("{0} {1}", forecast.Pressure.Value, forecast.Pressure.Unit);
        dayForecast.Precipitation = string.Format("{0} {1}", forecast.Precipitation.Value, forecast.Precipitation.Unit);
        dayForecast.Wind = string.Format("{0} {1}", forecast.WindSpeed.Mps, currentWeather.Wind.Direction.Name);
        dayForecast.Overview = forecast.Symbol.Name;
        dayForecast.BigIcon = @"Weather\128x128\" + GetWeatherIcon(forecast.Symbol.Number);
        dayForecast.SmallIcon = @"Weather\64x64\" + GetWeatherIcon(forecast.Symbol.Number);
        string fomattedDate = forecast.Day.ToString(_dateFormat.ShortDatePattern, _dateFormat);
        string day = _dateFormat.GetAbbreviatedDayName(forecast.Day.DayOfWeek);
        dayForecast.Day = String.Format("{0} {1}", day, fomattedDate);

        city.ForecastCollection.Add(dayForecast);
      }

      return false;
    }

    private string FormatTemp(double temperatureValue, string temperatureUnit)
    {
      var temp = temperatureValue;
      var unit = temperatureUnit;
      if (temperatureUnit == "kelvin")
      {
        temp = ToDegree(temp);
        unit = "°C";
      }
      else if (temperatureUnit == "metric")
      {
        unit = "°C";
      }
      else
      {
        unit = "°F";
      }
      return string.Format("{0} {1}", temp, unit);
    }

    public List<CitySetupInfo> FindLocationsByName(string name)
    {
      var client = new OpenWeatherMapClient(SERVICE_KEY);
      var location = Task.Run(async () => await client.Search.GetByName(name)).Result;
      var cities = new List<CitySetupInfo>();
      foreach (var city in location.List)
      {
        var setup = new CitySetupInfo(city.City.Name, city.City.Id.ToString(), SERVICE_NAME);
        cities.Add(setup);
      }
      return cities;
    }

    public string GetServiceName()
    {
      return "OpenWeatherMap";
    }

    private string GetWeatherIcon(int weatherCode)
    {
      int translatedID;
      if (_weatherCodeTranslation.TryGetValue(weatherCode, out translatedID))
        return translatedID + ".png";
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
  }
}
