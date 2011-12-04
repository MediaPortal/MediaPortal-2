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
using System.Text;
using System.Xml.XPath;

namespace MediaPortal.UiComponents.Weather.Grabbers
{
  class WorldWeatherOnlineCatcher : IWeatherCatcher
  {
    #region IWeatherCatcher Member

    private const string DEGREE = " °C";
    private const string KMH = " km/h";
    private const string PERCENT = " %";

    public bool GetLocationData(City city)
    {
      string locationKey = city.Id;
      if (string.IsNullOrEmpty(locationKey))
        return false;

      Dictionary<string, string> args = new Dictionary<string, string>();
      args["q"] = city.Id;
      args["num_of_days"] = "5";

      string url = BuildRequest("weather.ashx", args);
      XPathDocument doc = WorldWeatherOnlineHelper.GetOnlineContent(url);
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
    
    private static bool Parse(City city, XPathDocument doc)
    {
      if (city == null || doc == null)
        return false;
      XPathNavigator navigator = doc.CreateNavigator();
      XPathNavigator condition = navigator.SelectSingleNode("/data/current_condition");
      if (condition != null)
      {
        XPathNavigator node = condition.SelectSingleNode("temp_C");
        if (node != null)
          city.Condition.Temperature = node.Value + DEGREE;
        
        node = condition.SelectSingleNode("windspeedKmph");
        if (node != null)
          city.Condition.Wind = node.Value + KMH;
        
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
      
      XPathNodeIterator forecasts = navigator.Select("/data/weather");
      while (forecasts.MoveNext())
      {
        if (forecasts.Current == null)
          continue;

        DayForeCast dayForeCast = new DayForeCast();

        XPathNavigator node = forecasts.Current.SelectSingleNode("date");
        if (node != null)
          dayForeCast.Day = node.Value;

        node = forecasts.Current.SelectSingleNode("tempMinC");
        if (node != null)
          dayForeCast.Low = node.Value + DEGREE;

        node = forecasts.Current.SelectSingleNode("tempMaxC");
        if (node != null)
          dayForeCast.High = node.Value + DEGREE;

        node = forecasts.Current.SelectSingleNode("windspeedKmph");
        if (node != null)
          dayForeCast.Wind = node.Value + KMH;

        node = forecasts.Current.SelectSingleNode("winddirection");
        if (node != null)
          dayForeCast.Wind += node.Value;

        node = forecasts.Current.SelectSingleNode("weatherIconUrl");
        if (node != null)
          dayForeCast.BigIcon = dayForeCast.SmallIcon = node.Value;

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
