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
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using MediaPortal.Core;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Logging;

namespace MediaPortal.UiComponents.Weather.Grabbers
{
  /// <summary>
  /// Implementation of the IWeatherCatcher which grabs weather data from www.weather.com.
  /// TODO: Replace usage of XmlDocument by XPathDocument
  /// TODO: Rework completely.
  /// </summary>
  public class WeatherDotComCatcher : IWeatherCatcher
  {
    #region Private Variables

    // Private Variables
    private char _temperatureFarenheit = 'C';
    private string _windSpeed = "K";
    private char _unitTemperature;
    private string _unitSpeed = string.Empty;
    private string _parsefileLocation = string.Empty;
    private bool _skipConnectionTest;

    #endregion

    #region Constants

    // Constants
    private const char DEGREE_CHARACTER = (char) 176; //the degree 'o' character
    private const string PARTNER_ID = "1004124588"; //weather.com partner id
    private const string PARTNER_KEY = "079f24145f208494"; //weather.com partner key

    #endregion

    /// <summary>
    /// Creates a new instance of this class
    /// </summary>
    /// <param name="temperatureFahrenheit">Celsius or Fahrenheit, values <c>'C'</c> or <c>'F'</c>. TODO: Use enum, document values.</param>
    /// <param name="windSpeed">Windspeed, values <c>'K'</c>, <c>'M'</c> or <c>'S'</c>. TODO: Use enum, document values.</param>
    /// <param name="parseFilePath">Local file path where the downloaded file will be saved.</param>
    /// <param name="skipConnectionTest">If set to <c>true</c>, the download procedure will skip the connection test before
    /// downloading new data.</param>
    public WeatherDotComCatcher(char temperatureFahrenheit, string windSpeed, string parseFilePath, bool skipConnectionTest)
    {
      _temperatureFarenheit = temperatureFahrenheit;
      _windSpeed = windSpeed;
      _skipConnectionTest = skipConnectionTest;
      _parsefileLocation = parseFilePath;
    }

    #region IWeatherCatcher Members

    /// <summary>
    /// returns the name of the service used to fetch the data,
    /// f.e. weather.com
    /// </summary>
    /// <returns></returns>
    public string GetServiceName()
    {
      return "Weather.com";
    }

    /// <summary>
    /// downloads data from the internet and populates
    /// the city object with it.
    /// the searchId is the unique Id at the special
    /// service to identify the city
    /// </summary>
    /// <param name="city"></param>
    /// <returns>true if successful, false otherwise</returns>
    public bool GetLocationData(City city)
    {
      // begin getting the data
      string file;
      city.HasData = false;
      file = string.Format(_parsefileLocation, city.Id);
      // download the xml file to the given location
      if (!Download(city.Id, file))
      {
        ServiceRegistration.Get<ILogger>().Info("Models.Weather.WeatherDotComCatcher: Could not Download Data for {0}.", city.Name);
        return false;
      }
      // try to parse the file
      if (!ParseFile(city, file))
      {
        ServiceRegistration.Get<ILogger>().Info("Models.Weather.WeatherDotComCatcher: Could not Parse Data from {0} for City {1}.",
                                         file, city.Name);
        return false;
      }
      ServiceRegistration.Get<ILogger>().Info(
        "Models.Weather.WeatherDotComCatcher: Fetching of weather data was successful for {0}.", city.Name);
      city.HasData = true;
      return true;
    }

    /// <summary>
    /// returns a new List of City objects if the searched
    /// location was found... the unique id (City.Id) and the
    /// location name will be set for each City...
    /// </summary>
    /// <param name="locationName">name of the location to search for</param>
    /// <returns>new City List</returns>
    public List<CitySetupInfo> FindLocationsByName(string locationName)
    {
      // create list that will hold all found cities
      List<CitySetupInfo> locations = new List<CitySetupInfo>();

      try
      {
        string searchURI = string.Format("http://xoap.weather.com/search/search?where={0}", HttpUtility.UrlEncode(locationName));

        //
        // Create the request and fetch the response
        //
        WebRequest request = WebRequest.Create(searchURI);
        WebResponse response = request.GetResponse();

        //
        // Read data from the response stream
        //
        Stream responseStream = response.GetResponseStream();
        Encoding iso8859 = Encoding.GetEncoding("iso-8859-1");
        StreamReader streamReader = new StreamReader(responseStream, iso8859);

        XPathDocument document = new XPathDocument(streamReader);
        XPathNavigator nav = document.CreateNavigator();
        nav.MoveToChild(XPathNodeType.Element);
        XPathNodeIterator locIt = nav.Select("/search/loc");
        //
        // Iterate through our results
        //
        while (locIt.MoveNext())
        {
          XPathNavigator locNav = locIt.Current;
          string name = locNav.Value;
          string id = locNav.GetAttribute("id", string.Empty);

          locations.Add(new CitySetupInfo(name, id));
        }
        return locations;
      }
      catch (Exception e)
      {
        //
        // Failed to perform search
        //
        ServiceRegistration.Get<ILogger>().Error("Failed to perform city search, make sure you are connected to the internet... Error: {0}",
            e.Message);
        return locations;
      }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// download weather information to an xml file
    /// </summary>
    /// <param name="locationCode"></param>
    /// <param name="weatherFile">xml file to be downloaded to</param>
    /// <returns>success status</returns>
    private bool Download(string locationCode, string weatherFile)
    {
      int code = 0;

      if (!Helper.IsConnectedToInternet(ref code))
      {
        if (File.Exists(weatherFile))
          return true;

        ServiceRegistration.Get<ILogger>().Info("Models.Weather.WeatherDotComCatcher.Download: No internet connection {0}", code);

        if (!_skipConnectionTest)
          return false;
      }

      char units = _temperatureFarenheit; //convert from temp units to metric/standard
      //we'll convert the speed later depending on what thats set to
      units = units == 'F' ? 's' : 'm';

      string url = string.Format("http://xoap.weather.com/weather/local/{0}?cc=*&dayf=5&link=xoap&prod=xoap&par={1}&key={2}&unit={3}", 
          locationCode, PARTNER_ID, PARTNER_KEY, units);

      using (WebClient client = new WebClient())
      {
        try
        {
          client.DownloadFile(url, weatherFile);
          return true;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Info("WeatherDotComCatcher: Failed to download weather data", ex);
        }
      }
      return false;
    }

    /// <summary>
    /// parses the downloaded XML file
    /// </summary>
    /// <param name="c"></param>
    /// <param name="weatherFile">the xml file that contains weather information</param>
    /// <returns>success state</returns>
    private bool ParseFile(City c, string weatherFile)
    {
      if (!File.Exists(weatherFile))
        return false;
      try
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(weatherFile);
        if (doc.DocumentElement == null)
          return false;
        XmlNode xmlElement = doc.DocumentElement;

        if (doc.DocumentElement.Name == "error")
        {
          ParseError(xmlElement);
          return false;
        }

        _unitTemperature = _temperatureFarenheit;
        if (_windSpeed[0] == 'M')
          _unitSpeed = "mph";
        else if (_windSpeed[0] == 'K')
          _unitSpeed = "km/h";
        else
          _unitSpeed = "m/s";

        if (!ParseLocation(c, xmlElement.SelectSingleNode("loc")))
          return false;
        if (!ParseCurrentCondition(c, xmlElement.SelectSingleNode("cc")))
          return false;
        if (!ParseDayForeCast(c, xmlElement.SelectSingleNode("dayf")))
          return false;
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Info("WeatherDotComCatcher: Failed to parse weather document", ex);
        return false;
      }
    }

    /// <summary>
    /// writes an error message to the log
    /// if parsing went wrong
    /// </summary>
    /// <param name="xmlElement"></param>
    private static void ParseError(XmlNode xmlElement)
    {
      ServiceRegistration.Get<ILogger>().Info("WeatherForecast.ParseFile: Error = '{0}'",
          GetString(xmlElement, "err", "Unknown Error"));
    }

    /// <summary>
    /// parses the locations
    /// </summary>
    /// <param name="c"></param>
    /// <param name="xmlElement"></param>
    /// <returns></returns>
    private static bool ParseLocation(City c, XmlNode xmlElement)
    {
      if (xmlElement == null)
        return false;
      c.LocationInfo = new LocInfo();
      c.LocationInfo.Time = GetString(xmlElement, "tm", string.Empty); // <tm>1:12 AM</tm>
      c.LocationInfo.Lat = GetString(xmlElement, "lat", string.Empty); // <lat>49.02</lat>
      c.LocationInfo.Lon = GetString(xmlElement, "lon", string.Empty); // <lon>12.1</lon>
      
      c.LocationInfo.SunRise = GetString(xmlElement, "sunr", string.Empty); // <sunr>7:14 AM</sunr> 
      if (c.LocationInfo.SunRise == "N/A")
        c.LocationInfo.SunRise = string.Empty;
      else
        c.LocationInfo.SunRise = string.Format("{0}", RelocalizeTime(c.LocationInfo.SunRise));

      c.LocationInfo.SunSet = GetString(xmlElement, "suns", string.Empty); // <suns>5:38 PM</suns>
      if (c.LocationInfo.SunSet == "N/A")
        c.LocationInfo.SunSet = string.Empty;
      else
        c.LocationInfo.SunSet = string.Format("{0}", RelocalizeTime(c.LocationInfo.SunSet));
      
      c.LocationInfo.Zone = GetString(xmlElement, "zone", string.Empty); // <zone>1</zone>
      return true;
    }

    /// <summary>
    /// parses the current condition
    /// </summary>
    /// <param name="c"></param>
    /// <param name="xmlElement"></param>
    /// <returns></returns>
    private bool ParseCurrentCondition(City c, XmlNode xmlElement)
    {
      if (xmlElement == null)
        return false;

      c.Condition.LastUpdate = RelocalizeDateTime(GetString(xmlElement, "lsup", string.Empty));
      c.Condition.City = GetString(xmlElement, "obst", string.Empty);
      c.Condition.BigIcon = string.Format(@"Weather\128x128\{0}.png", GetIntegerDef(xmlElement, "icon", 0));
      string condition = LocalizeOverview(GetString(xmlElement, "t", string.Empty));
      SplitLongString(ref condition, 8, 15); //split to 2 lines if needed
      c.Condition.Condition = condition;
      c.Condition.Temperature =
        string.Format("{0}{1}{2}", GetIntegerDef(xmlElement, "tmp", 0), DEGREE_CHARACTER, _unitTemperature);
      c.Condition.FeelsLikeTemp =
        string.Format("{0}{1}{2}", GetIntegerDef(xmlElement, "flik", 0), DEGREE_CHARACTER, _unitTemperature);
      c.Condition.Wind = ParseWind(xmlElement.SelectSingleNode("wind"), _unitSpeed);
      c.Condition.Humidity = string.Format("{0}%", GetIntegerDef(xmlElement, "hmid", 0));
      c.Condition.UVIndex = ParseUVIndex(xmlElement.SelectSingleNode("uv"));
      c.Condition.DewPoint =
        string.Format("{0}{1}{2}", GetIntegerDef(xmlElement, "dewp", 0), DEGREE_CHARACTER, _unitTemperature);
      return true;
    }

    /// <summary>
    /// parses the dayforecast
    /// </summary>
    /// <param name="c"></param>
    /// <param name="xmlElement"></param>
    /// <returns></returns>
    private bool ParseDayForeCast(City c, XmlNode xmlElement)
    {
      if (xmlElement == null)
        return false;

      XmlNode element = xmlElement.SelectSingleNode("day");
      foreach(DayForeCast forecast in c.ForecastCollection)
      {
        if (element != null)
        {
          forecast.Day = LocalizeDay(element.Attributes.GetNamedItem("t").InnerText);

          forecast.High = GetString(element, "hi", string.Empty); //string cause i've seen it return N/A
          forecast.High = forecast.High == "N/A" ? "N/A" :
              string.Format("{0}{1}{2}", forecast.High, DEGREE_CHARACTER, _unitTemperature);

          forecast.Low = GetString(element, "low", string.Empty);
          forecast.Low = forecast.Low == "N/A" ? "N/A" :
              string.Format("{0}{1}{2}", forecast.Low, DEGREE_CHARACTER, _unitTemperature);

          forecast.SunRise = GetString(element, "sunr", string.Empty);
          forecast.SunRise = forecast.SunRise == "N/A" ? string.Empty :
              string.Format("{0}", RelocalizeTime(forecast.SunRise));

          forecast.SunSet = GetString(element, "suns", string.Empty);
          forecast.SunSet = forecast.SunSet == "N/A" ? string.Empty :
              string.Format("{0}", RelocalizeTime(forecast.SunSet));

          XmlNode dayElement = element.SelectSingleNode("part"); //grab the first day/night part (should be day)
          if (dayElement != null) 
          {
            // If day forecast is not available (at the end of the day), show night forecast
            if (GetString(dayElement, "t", string.Empty) == "N/A")
              dayElement = dayElement.NextSibling;
          }

          if (dayElement != null)
          {
            forecast.SmallIcon = string.Format(@"Weather\64x64\{0}.png", GetIntegerDef(dayElement, "icon", 0));
            forecast.BigIcon = string.Format(@"Weather\128x128\{0}.png", GetIntegerDef(dayElement, "icon", 0));
            string overview = LocalizeOverview(GetString(dayElement, "t", string.Empty));
            SplitLongString(ref overview, 6, 15);
            forecast.Overview = overview;
            forecast.Humidity = string.Format("{0}%", GetIntegerDef(dayElement, "hmid", 0));
            forecast.Precipitation = string.Format("{0}%", GetIntegerDef(dayElement, "ppcp", 0));
            forecast.Wind = ParseWind(dayElement.SelectSingleNode("wind"), _unitSpeed);
          }
          element = element.NextSibling; //Element("day");
        }
      }
      return true;
    }

    #region Helper Methods

    /// <summary>
    /// Helper function returning the text inside the child element of the given <paramref name="element"/>
    /// of the name <paramref name="tagName"/>.
    /// </summary>
    /// <param name="element">Element where to search the child element.</param>
    /// <param name="tagName">Child element name to search</param>
    /// <param name="defaultValue">Value to be returned if there is no child element of the given <paramref name="tagName"/>
    /// or if the text inside the child element is empty or equal to <c>"-"</c>.</param>
    /// <returns>String which was read.</returns>
    private static string GetString(XmlNode element, string tagName, string defaultValue)
    {
      string value = null;
      try
      {
        XmlNode node = element.SelectSingleNode(tagName);
        if (node != null)
          value = node.InnerText;
        if (value == "-")
          value = null;
      }
      catch (XPathException) {}
      return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    /// <summary>
    /// Helper function returning an integer from inside the child element of the given <paramref name="element"/>
    /// of the name <paramref name="tagName"/>.
    /// </summary>
    /// <param name="element">Element where to search the child element.</param>
    /// <param name="tagName">Child element name to search</param>
    /// <param name="defaultValue">Default value to be returned if the child element of the given <paramref name="tagName"/>
    /// could not be read or could not be parsed as an <c>int</c>.</param>
    /// <returns>Integer value which was read.</returns>
    private static int GetIntegerDef(XmlNode element, string tagName, int defaultValue)
    {
      int value;
      if (GetInteger(element, tagName, out value))
        return value;
      return defaultValue;
    }

    /// <summary>
    /// Helper function returning an integer from inside the child element of the given <paramref name="element"/>
    /// which of the name <paramref name="tagName"/>.
    /// </summary>
    /// <param name="element">Element where to search the child element.</param>
    /// <param name="tagName">Child element name to search</param>
    /// <param name="value">Integer value which was read.</param>
    /// <returns><c>true</c>, if the value could be read. <c>false</c> if there is no child element of the given
    /// <paramref name="tagName"/> or the text inside that element could not be parsed as <c>int</c>.</returns>
    private static bool GetInteger(XmlNode element, string tagName, out int value)
    {
      value = 0;
      XmlNode node = element.SelectSingleNode(tagName);
      string text = node == null ? null : node.InnerText;
      return !string.IsNullOrEmpty(text) && int.TryParse(text, out value);
    }

    //splitStart + End are the chars to search between for a space to replace with a \n
    private static void SplitLongString(ref string lineString, int splitStart, int splitEnd)
    {
      //search chars 10 to 15 for a space
      //if we find one, replace it with a newline
      for (int i = splitStart; i < splitEnd && i < lineString.Length; i++)
      {
        if (lineString[i] == ' ')
        {
          lineString = lineString.Substring(0, i) + "\n" + lineString.Substring(i + 1);
          return;
        }
      }
    }

    /// <summary>
    /// relocalizes the timezone
    /// </summary>
    /// <param name="usFormatTime"></param>
    /// <returns></returns>
    private static string RelocalizeTime(string usFormatTime)
    {
      string result = usFormatTime;

      string[] tokens = result.Split(' ');

      if (tokens.Length == 2)
      {
        try
        {
          string[] timePart = tokens[0].Split(':');
          DateTime now = DateTime.Now;
          DateTime time = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            Int32.Parse(timePart[0]) + (string.Compare(tokens[1], "PM", true) == 0 ? 12 : 0),
            Int32.Parse(timePart[1]),
            0
            );

          result = time.ToString("t", CultureInfo.CurrentCulture.DateTimeFormat);
        }
        catch
        {
          // default value is ok
        }
      }
      return result;
    }

    /// <summary>
    /// convert weather.com day strings into localized string id's
    /// </summary>
    /// <param name="dayName"></param>
    /// <returns></returns>
    private static string LocalizeDay(string dayName)
    {
      switch (dayName)
      {
        case "Monday":
          return ServiceRegistration.Get<ILocalization>().ToString("[DaysOfWeek.1]");
        case "Tuesday":
          return ServiceRegistration.Get<ILocalization>().ToString("[DaysOfWeek.2]");
        case "Wednesday":
          return ServiceRegistration.Get<ILocalization>().ToString("[DaysOfWeek.3]");
        case "Thursday":
          return ServiceRegistration.Get<ILocalization>().ToString("[DaysOfWeek.4]");
        case "Friday":
          return ServiceRegistration.Get<ILocalization>().ToString("[DaysOfWeek.5]");
        case "Saturday":
          return ServiceRegistration.Get<ILocalization>().ToString("[DaysOfWeek.6]");
        case "Sunday":
          return ServiceRegistration.Get<ILocalization>().ToString("[DaysOfWeek.7]");
        default:
          return string.Empty;
      }
    }

    /// <summary>
    /// relocalizes the date time
    /// </summary>
    /// <param name="usFormatDateTime"></param>
    /// <returns>localized string</returns>
    private static string RelocalizeDateTime(string usFormatDateTime)
    {
      string result = usFormatDateTime;

      string[] tokens = result.Split(' ');

      // A safety check
      if ((tokens.Length == 5) &&
          (string.Compare(tokens[3], "Local", true) == 0) && (string.Compare(tokens[4], "Time", true) == 0))
      {
        try
        {
          string[] datePart = tokens[0].Split('/');
          string[] timePart = tokens[1].Split(':');
          DateTime time = new DateTime(
            2000 + Int32.Parse(datePart[2]),
            Int32.Parse(datePart[0]),
            Int32.Parse(datePart[1]),
            Int32.Parse(timePart[0]) + (string.Compare(tokens[2], "PM", true) == 0 ? 12 : 0),
            Int32.Parse(timePart[1]),
            0
            );
          result = time.ToString("f", CultureInfo.CurrentCulture.DateTimeFormat);
        }
        catch
        {
          // default value is ok
        }
      }
      return result;
    }

    /// <summary>
    /// localizes overview strings
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private static string LocalizeOverview(string token)
    {
      StringBuilder localizedLine = new StringBuilder();

      foreach (string tokenSplit in token.Split(' '))
      {
        string localizedWord = string.Empty;

        ILocalization localization = ServiceRegistration.Get<ILocalization>();
        // handle only mappings of different spellings
        if (string.Compare(tokenSplit, "T-Storms", true) == 0 || string.Compare(tokenSplit, "T-Storm", true) == 0)
          localizedWord = localization.ToString("[Weather.TStorm]");
        else if (string.Compare(tokenSplit, "Cloudy", true) == 0)
          localizedWord = localization.ToString("[Weather.Clouds]");
        else if (string.Compare(tokenSplit, "Shower", true) == 0 ||
                 string.Compare(tokenSplit, "T-Showers", true) == 0)
          localizedWord = localization.ToString("[Weather.Showers]");
        else if (string.Compare(tokenSplit, "Isolated", true) == 0)
          localizedWord = localization.ToString("[Weather.Scattered]");
        else if (string.Compare(tokenSplit, "Gale", true) == 0 ||
                 string.Compare(tokenSplit, "Tempest", true) == 0)
          localizedWord = localization.ToString("[Weather.Storm]");
        else 
          // for all other tokens do a direct lookup
          localizedWord = localization.ToString("[Weather."+tokenSplit+"]");

        localizedLine.AppendFormat("{0} ", localizedWord ?? tokenSplit); //if not found, let fallback
      }
      return localizedLine.ToString();
    }

    /// <summary>
    /// converts speed to other metrics
    /// </summary>
    /// <param name="curSpeed"></param>
    /// <returns></returns>
    private int ConvertSpeed(int curSpeed)
    {
      //we might not need to convert at all
      if ((_temperatureFarenheit == 'C' && _windSpeed[0] == 'K') ||
          (_temperatureFarenheit == 'F' && _windSpeed[0] == 'M'))
        return curSpeed;

      //got through that so if temp is C, speed must be M or S
      if (_temperatureFarenheit == 'C')
        return _windSpeed[0] == 'S' ? (int) (curSpeed*(1000.0/3600.0) + 0.5) : (int) (curSpeed/(8.0/5.0));
      else
        return _windSpeed[0] == 'S' ? (int) (curSpeed*(8.0/5.0)*(1000.0/3600.0) + 0.5) : (int) (curSpeed*(8.0/5.0));
    }

    /// <summary>
    /// parses the uv index
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static string ParseUVIndex(XmlNode element)
    {
      if (element == null)
        return string.Empty;
      return string.Format("{0} {1}", GetIntegerDef(element, "i", 0), LocalizeOverview(GetString(element, "t", string.Empty)));
    }

    /// <summary>
    /// parses the wind
    /// </summary>
    /// <param name="node"></param>
    /// <param name="unitSpeed"></param>
    /// <returns></returns>
    private string ParseWind(XmlNode node, string unitSpeed)
    {
      if (node == null)
        return string.Empty;

      string wind;
      int tempInteger = ConvertSpeed(GetIntegerDef(node, "s", 0)); //convert speed if needed
      string tempString = LocalizeOverview(GetString(node, "t", "N")); //current wind direction

      if (tempInteger != 0) // Have wind
      {
        //From <dir eg NW> at <speed> km/h	
        string format = ServiceRegistration.Get<ILocalization>().ToString("[Weather.From]");
        if (format == string.Empty)
          format = "From {0} at {1} {2}";
        wind = string.Format(format, tempString, tempInteger, unitSpeed);
      }
      else // Calm
      {
        wind = ServiceRegistration.Get<ILocalization>().ToString("[Weather.Calm]");
        if (wind == string.Empty)
          wind = "No wind";
      }
      return wind;
    }

    #endregion

    #endregion
  }
}
