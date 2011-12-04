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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.UiComponents.Weather.Settings;

namespace MediaPortal.UiComponents.Weather.Grabbers
{
  class WorldWeatherOnlineCatcher : IWeatherCatcher
  {
    #region IWeatherCatcher Member

    private const string CELCIUS = " °C";
    private const string FAHRENHEIT = " °F";
    private const string KPH = " km/h";
    private const string MPH = " mph";
    private const string PERCENT = " %";
    private const string MM = " mm";

    private readonly bool _preferCelcius = true;
    private readonly bool _preferKph = true;
    private readonly string _parsefileLocation;

    public WorldWeatherOnlineCatcher()
    {
      WeatherSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<WeatherSettings>();
      _preferCelcius = settings.TemperatureUnit == WeatherSettings.TempUnit.Celcius;
      _preferKph = settings.WindSpeedUnit == WeatherSettings.SpeedUnit.Kph;
      _parsefileLocation = ServiceRegistration.Get<IPathManager>().GetPath(settings.ParsefileLocation);
    }

    public bool GetLocationData(City city)
    {
      string locationKey = city.Id;
      if (string.IsNullOrEmpty(locationKey))
        return false;

      city.HasData = false;
      string cachefile = string.Format(_parsefileLocation, locationKey.Replace(',', '_'));

      XPathDocument doc;
      if (File.Exists(cachefile))
      {
        doc = new XPathDocument(cachefile);
      }
      else
      {
        Dictionary<string, string> args = new Dictionary<string, string>();
        args["q"] = city.Id;
        args["num_of_days"] = "5";

        string url = BuildRequest("weather.ashx", args);
        doc = WorldWeatherOnlineHelper.GetOnlineContent(url);

        // Save cache file
        using (XmlWriter xw = XmlWriter.Create(cachefile))
        {
          doc.CreateNavigator().WriteSubtree(xw);
          xw.Close();
        }
      }
      return Parse(city, doc);
    }

    /// <summary>
    /// Returns a new List of City objects if the searched
    /// location was found... the unique id (City.Id) and the
    /// location name will be set for each City...
    /// </summary>
    /// <param name="locationName">Name of the location to search for</param>
    /// <returns>New City List</returns>
    public List<CitySetupInfo> FindLocationsByName(string locationName)
    {
      // create list that will hold all found cities
      List<CitySetupInfo> locations = new List<CitySetupInfo>();

      Dictionary<string, string> args = new Dictionary<string, string>();
      args["query"] = locationName;
      args["num_of_results"] = "5";

      string url = BuildRequest("search.ashx", args);
      XPathDocument doc = WorldWeatherOnlineHelper.GetOnlineContent(url);
      XPathNavigator navigator = doc.CreateNavigator();
      XPathNodeIterator nodes = navigator.Select("/search_api/result");

      while (nodes.MoveNext())
      {
        CitySetupInfo city = new CitySetupInfo();
        XPathNavigator nodesNavigator = nodes.Current;
        if (nodesNavigator == null)
          continue;

        XPathNavigator areaNode = nodesNavigator.SelectSingleNode("areaName");
        if (areaNode != null)
          city.Name = areaNode.Value;

        XPathNavigator countryNode = nodesNavigator.SelectSingleNode("country");
        if (countryNode != null)
          city.Name += " (" + countryNode.Value + ")";

        // Build a unique key based on latitude & longitude
        XPathNavigator latNode = nodesNavigator.SelectSingleNode("latitude");
        XPathNavigator lonNode = nodesNavigator.SelectSingleNode("longitude");
        if (latNode != null && lonNode != null)
          city.Id = latNode.Value + "," + lonNode.Value;

        city.Grabber = GetServiceName();
        locations.Add(city);
      }
      return locations;
    }

    public string GetServiceName()
    {
      return "WorldWeatherOnline.com";
    }

    private bool Parse(City city, XPathDocument doc)
    {
      if (city == null || doc == null)
        return false;
      XPathNavigator navigator = doc.CreateNavigator();
      XPathNavigator condition = navigator.SelectSingleNode("/data/current_condition");
      if (condition != null)
      {
        string nodeName = _preferCelcius ? "temp_C" : "temp_F";
        string unit = _preferCelcius ? CELCIUS : FAHRENHEIT;

        XPathNavigator node = condition.SelectSingleNode(nodeName);
        if (node != null)
          city.Condition.Temperature = node.Value + unit;

        nodeName = _preferKph ? "windspeedKmph" : "windspeedMiles";
        unit = _preferKph ? KPH : MPH;
        node = condition.SelectSingleNode(nodeName);
        if (node != null)
          city.Condition.Wind = node.Value + unit;

        node = condition.SelectSingleNode("humidity");
        if (node != null)
          city.Condition.Humidity = node.Value + PERCENT;

        node = condition.SelectSingleNode("weatherDesc");
        if (node != null)
          city.Condition.Condition = node.Value;

        node = condition.SelectSingleNode("weatherIconUrl");
        if (node != null)
          city.Condition.BigIcon = city.Condition.SmallIcon = node.Value;
      }

      city.ForecastCollection.Clear();
      XPathNodeIterator forecasts = navigator.Select("/data/weather");
      while (forecasts.MoveNext())
      {
        if (forecasts.Current == null)
          continue;

        DayForeCast dayForeCast = new DayForeCast();

        XPathNavigator node = forecasts.Current.SelectSingleNode("date");
        if (node != null)
          dayForeCast.Day = node.Value;

        string nodeName = _preferCelcius ? "tempMinC" : "tempMinF";
        string unit = _preferCelcius ? CELCIUS : FAHRENHEIT;

        node = forecasts.Current.SelectSingleNode(nodeName);
        if (node != null)
          dayForeCast.Low = node.Value + unit;

        nodeName = _preferCelcius ? "tempMaxC" : "tempMaxF";
        node = forecasts.Current.SelectSingleNode(nodeName);
        if (node != null)
          dayForeCast.High = node.Value + unit;

        nodeName = _preferKph ? "windspeedKmph" : "windspeedMiles";
        unit = _preferKph ? KPH : MPH;
        node = forecasts.Current.SelectSingleNode(nodeName);
        if (node != null)
          dayForeCast.Wind = node.Value + unit;

        node = forecasts.Current.SelectSingleNode("winddirection");
        if (node != null)
          dayForeCast.Wind += node.Value;

        node = forecasts.Current.SelectSingleNode("weatherIconUrl");
        if (node != null)
          dayForeCast.BigIcon = dayForeCast.SmallIcon = node.Value;

        node = forecasts.Current.SelectSingleNode("weatherDesc");
        if (node != null)
          dayForeCast.Overview = node.Value;

        node = forecasts.Current.SelectSingleNode("precipMM");
        if (node != null)
          dayForeCast.Precipitation = node.Value + MM;

        city.ForecastCollection.Add(dayForeCast);
      }
      return true;
    }

    private static string BuildRequest(string relativePage, Dictionary<string, string> args)
    {
      return string.Format("http://free.worldweatheronline.com/feed/{0}?{1}", relativePage, BuildRequestArgs(args));
    }

    private static string BuildRequestArgs(Dictionary<string, string> args)
    {
      StringBuilder requestArgs = new StringBuilder();
      foreach (KeyValuePair<string, string> keyValuePair in args)
      {
        if (requestArgs.Length > 0)
          requestArgs.Append("&");
        requestArgs.Append(string.Format("{0}={1}", keyValuePair.Key, Uri.EscapeUriString(keyValuePair.Value)));
      }
      return requestArgs.ToString();
    }

    #endregion
  }
}
