#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;

namespace Models.Weather.Grabbers
{
  /// <summary>
  /// Implementation of the IWeatherCatcher
  /// Interface which grabs the Data from
  /// www.weather.com
  /// </summary>
  public class WeatherDotComCatcher : IWeatherCatcher
  {
    #region Private Variables

    // Private Variables
    private string _temperatureFarenheit = "C";
    private string _windSpeed = "K";
    private string unitTemperature = String.Empty;
    private string _unitSpeed = String.Empty;
    private string _parsefileLocation = String.Empty;
    private bool _skipConnectionTest;

    #endregion

    #region Constants

    // Constants
    private const int NUM_DAYS = 4;
    private const char DEGREE_CHARACTER = (char) 176; //the degree 'o' character
    private const string PARTNER_ID = "1004124588"; //weather.com partner id
    private const string PARTNER_KEY = "079f24145f208494"; //weather.com partner key

    #endregion

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
      // update variables from settings
      LoadSettings();
      // begin getting the data
      string file;
      city.HasData = false;
      file = String.Format(_parsefileLocation, city.Id);
      // download the xml file to the given location
      if (!Download(city.Id, file))
      {
        ServiceScope.Get<ILogger>().Info("Models.Weather.WeatherDotComCatcher: Could not Download Data for {0}.", city.Name);
        return false;
      }
      // try to parse the file
      if (!ParseFile(city, file))
      {
        ServiceScope.Get<ILogger>().Info("Models.Weather.WeatherDotComCatcher: Could not Parse Data from {0} for City {1}.",
                                         file, city.Name);
        return false;
      }
      ServiceScope.Get<ILogger>().Info(
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
        string searchURI =
          String.Format("http://xoap.weather.com/search/search?where={0}", Helper.UrlEncode(locationName));

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

        //
        // Fetch information from our stream
        //
        string data = streamReader.ReadToEnd();

        XmlDocument document = new XmlDocument();
        document.LoadXml(data);

        XmlNodeList nodes = document.DocumentElement.SelectNodes("/search/loc");

        if (nodes != null)
        {
          //
          // Iterate through our results
          //
          foreach (XmlNode node in nodes)
          {
            string name = node.InnerText;
            string id = node.Attributes["id"].Value;

            locations.Add(new CitySetupInfo(name, id));
          }
        }
        return locations;
      }
      catch (Exception e)
      {
        //
        // Failed to perform search
        //
        ServiceScope.Get<ILogger>().Error(
          "Failed to perform city search, make sure you are connected to the internet... Error: {0}", e.Message);
        return locations;
      }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Load Settings needed for this catcher
    /// </summary>
    private void LoadSettings()
    {
      WeatherSettings settings = ServiceScope.Get<ISettingsManager>().Load<WeatherSettings>();
      _temperatureFarenheit = settings.TemperatureFahrenheit;
      _windSpeed = settings.WindSpeed;
      _skipConnectionTest = settings.SkipConnectionTest;
      _parsefileLocation = settings.ParsefileLocation;
    }

    /// <summary>
    /// download weather information to an xml file
    /// </summary>
    /// <param name="locationCode"></param>
    /// <param name="weatherFile">xml file to be downloaded to</param>
    /// <returns>success status</returns>
    private bool Download(string locationCode, string weatherFile)
    {
      string url;
      // update variables from the settings
      LoadSettings();

      int code = 0;

      if (!Helper.IsConnectedToInternet(ref code))
      {
        if (File.Exists(weatherFile))
        {
          return true;
        }

        ServiceScope.Get<ILogger>().Info("Models.Weather.WeatherDotComCatcher.Download: No internet connection {0}", code);

        if (_skipConnectionTest == false)
        {
          return false;
        }
      }

      char units = _temperatureFarenheit[0]; //convert from temp units to metric/standard
      if (units == 'F') //we'll convert the speed later depending on what thats set to
      {
        units = 's';
      }
      else
      {
        units = 'm';
      }

      url = String.Format("http://xoap.weather.com/weather/local/{0}?cc=*&unit={1}&dayf=4",
                          locationCode, units);

      using (WebClient client = new WebClient())
      {
        try
        {
          client.DownloadFile(url, weatherFile);
          return true;
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Info("WeatherDotComCatcher: Failed to download weather:{0} {1} {2}", ex.Message,
                                           ex.Source, ex.StackTrace);
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
      {
        return false;
      }
      XmlDocument doc = new XmlDocument();
      doc.Load(weatherFile);
      if (doc.DocumentElement == null)
      {
        return false;
      }
      XmlNode xmlElement = doc.DocumentElement;

      if (doc.DocumentElement.Name == "error")
      {
        ParseError(xmlElement);
        return false;
      }

      unitTemperature = _temperatureFarenheit;
      if (_windSpeed[0] == 'M')
      {
        _unitSpeed = "mph";
      }
      else if (_windSpeed[0] == 'K')
      {
        _unitSpeed = "km/h";
      }
      else
      {
        _unitSpeed = "m/s";
      }

      if (!ParseLocation(c, xmlElement.SelectSingleNode("loc")))
      {
        return false;
      }
      if (!ParseCurrentCondition(c, xmlElement.SelectSingleNode("cc")))
      {
        return false;
      }
      if (!ParseDayForeCast(c, xmlElement.SelectSingleNode("dayf")))
      {
        return false;
      }
      return true;
    }

    /// <summary>
    /// writes an error message to the log
    /// if parsing went wrong
    /// </summary>
    /// <param name="xmlElement"></param>
    private static void ParseError(XmlNode xmlElement)
    {
      ServiceScope.Get<ILogger>().Info("WeatherForecast.ParseFile: Error = " +
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
      {
        return false;
      }
      c.LocationInfo = new LocInfo();
      c.LocationInfo.Time = GetString(xmlElement, "tm", String.Empty); // <tm>1:12 AM</tm>
      c.LocationInfo.Lat = GetString(xmlElement, "lat", String.Empty); // <lat>49.02</lat>
      c.LocationInfo.Lon = GetString(xmlElement, "lon", String.Empty); // <lon>12.1</lon>
      
      c.LocationInfo.SunRise = GetString(xmlElement, "sunr", String.Empty); // <sunr>7:14 AM</sunr> 
      if (c.LocationInfo.SunRise == "N/A")
        c.LocationInfo.SunRise = String.Empty;
      else
        c.LocationInfo.SunRise = String.Format("{0}", RelocalizeTime(c.LocationInfo.SunRise));

      c.LocationInfo.SunSet = GetString(xmlElement, "suns", String.Empty); // <suns>5:38 PM</suns>
      if (c.LocationInfo.SunSet == "N/A")
        c.LocationInfo.SunSet = String.Empty;
      else
        c.LocationInfo.SunSet = String.Format("{0}", RelocalizeTime(c.LocationInfo.SunSet));
      
      c.LocationInfo.Zone = GetString(xmlElement, "zone", String.Empty); // <zone>1</zone>
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
      {
        return false;
      }

      c.Condition.LastUpdate = RelocalizeDateTime(GetString(xmlElement, "lsup", String.Empty));
      c.Condition.City = GetString(xmlElement, "obst", String.Empty);
      c.Condition.BigIcon = String.Format(@"Weather\128x128\{0}.png", GetInteger(xmlElement, "icon"));
      string condition = LocalizeOverview(GetString(xmlElement, "t", String.Empty));
      SplitLongString(ref condition, 8, 15); //split to 2 lines if needed
      c.Condition.Condition = condition;
      c.Condition.Temperature =
        String.Format("{0}{1}{2}", GetInteger(xmlElement, "tmp"), DEGREE_CHARACTER, unitTemperature);
      c.Condition.FeelsLikeTemp =
        String.Format("{0}{1}{2}", GetInteger(xmlElement, "flik"), DEGREE_CHARACTER, unitTemperature);
      c.Condition.Wind = ParseWind(xmlElement.SelectSingleNode("wind"), _unitSpeed);
      c.Condition.Humidity = String.Format("{0}%", GetInteger(xmlElement, "hmid"));
      c.Condition.UVIndex = ParseUVIndex(xmlElement.SelectSingleNode("uv"));
      c.Condition.DewPoint =
        String.Format("{0}{1}{2}", GetInteger(xmlElement, "dewp"), DEGREE_CHARACTER, unitTemperature);
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
      {
        return false;
      }

      XmlNode element = xmlElement.SelectSingleNode("day");
      DayForeCast forecast;

      for (int i = 0; i < NUM_DAYS; i++)
      {
        forecast = c.ForecastCollection[i];
        if (element != null)
        {
          forecast.Day = LocalizeDay(element.Attributes.GetNamedItem("t").InnerText);

          forecast.High = GetString(element, "hi", String.Empty); //string cause i've seen it return N/A
          if (forecast.High == "N/A")
          {
            forecast.High = "N/A";
          }
          else
          {
            forecast.High = String.Format("{0}{1}{2}", forecast.High, DEGREE_CHARACTER, unitTemperature);
          }

          forecast.Low = GetString(element, "low", String.Empty);
          if (forecast.Low == "N/A")
          {
            forecast.Low = "N/A";
          }
          else
          {
            forecast.Low = String.Format("{0}{1}{2}", forecast.Low, DEGREE_CHARACTER, unitTemperature);
          }

          forecast.SunRise = GetString(element, "sunr", String.Empty);
          if (forecast.SunRise == "N/A")
          {
            forecast.SunRise = String.Empty;
          }
          else
          {
            forecast.SunRise = String.Format("{0}", RelocalizeTime(forecast.SunRise));
          }

          forecast.SunSet = GetString(element, "suns", String.Empty);
          if (forecast.SunSet == "N/A")
          {
            forecast.SunSet = String.Empty;
          }
          else
          {
            forecast.SunSet = String.Format("{0}", RelocalizeTime(forecast.SunSet));
          }

          XmlNode dayElement = element.SelectSingleNode("part"); //grab the first day/night part (should be day)
          if (dayElement != null && i == 0)
          {
            // If day forecast is not available (at the end of the day), show night forecast
            if (GetString(dayElement, "t", String.Empty) == "N/A")
            {
              dayElement = dayElement.NextSibling;
            }
          }

          if (dayElement != null)
          {
            string overview;
            forecast.SmallIcon = String.Format(@"Weather\64x64\{0}.png", GetInteger(dayElement, "icon"));
            forecast.BigIcon = String.Format(@"Weather\128x128\{0}.png", GetInteger(dayElement, "icon"));
            overview = LocalizeOverview(GetString(dayElement, "t", String.Empty));
            SplitLongString(ref overview, 6, 15);
            forecast.Overview = overview;
            forecast.Humidity = String.Format("{0}%", GetInteger(dayElement, "hmid"));
            forecast.Precipitation = String.Format("{0}%", GetInteger(dayElement, "ppcp"));
            forecast.Wind = ParseWind(dayElement.SelectSingleNode("wind"), _unitSpeed);
          }
          element = element.NextSibling; //Element("day");
        }
      }
      return true;
    }

    #region Helper Methods

    /// <summary>
    /// helper function
    /// </summary>
    /// <param name="xmlElement"></param>
    /// <param name="tagName"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    private static string GetString(XmlNode xmlElement, string tagName, string defaultValue)
    {
      string value = String.Empty;

      try
      {
        XmlNode node = xmlElement.SelectSingleNode(tagName);
        if (node != null)
        {
          if (node.InnerText != null)
          {
            if (node.InnerText != "-")
            {
              value = node.InnerText;
            }
          }
        }
      }
      catch {}
      if (value.Length == 0)
      {
        return defaultValue;
      }
      return value;
    }

    /// <summary>
    /// helper function
    /// </summary>
    /// <param name="xmlElement"></param>
    /// <param name="tagName"></param>
    /// <returns></returns>
    private static int GetInteger(XmlNode xmlElement, string tagName)
    {
      int value = 0;
      XmlNode node = xmlElement.SelectSingleNode(tagName);
      if (node != null)
      {
        if (node.InnerText != null)
        {
          try
          {
            value = Int32.Parse(node.InnerText);
          }
          catch {}
        }
      }
      return value;
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
            Int32.Parse(timePart[0]) + (String.Compare(tokens[1], "PM", true) == 0 ? 12 : 0),
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
        case "Sunday":
          return ServiceScope.Get<ILocalisation>().ToString("days", "0");
        case "Monday":
          return ServiceScope.Get<ILocalisation>().ToString("days", "1");
        case "Tuesday":
          return ServiceScope.Get<ILocalisation>().ToString("days", "2");
        case "Wednesday":
          return ServiceScope.Get<ILocalisation>().ToString("days", "3");
        case "Thursday":
          return ServiceScope.Get<ILocalisation>().ToString("days", "4");
        case "Friday":
          return ServiceScope.Get<ILocalisation>().ToString("days", "5");
        case "Saturday":
          return ServiceScope.Get<ILocalisation>().ToString("days", "6");
        default:
          return String.Empty;
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
          (String.Compare(tokens[3], "Local", true) == 0) && (String.Compare(tokens[4], "Time", true) == 0))
      {
        try
        {
          string[] datePart = tokens[0].Split('/');
          string[] timePart = tokens[1].Split(':');
          DateTime time = new DateTime(
            2000 + Int32.Parse(datePart[2]),
            Int32.Parse(datePart[0]),
            Int32.Parse(datePart[1]),
            Int32.Parse(timePart[0]) + (String.Compare(tokens[2], "PM", true) == 0 ? 12 : 0),
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
      string localizedLine = String.Empty;

      foreach (string tokenSplit in token.Split(' '))
      {
        string localizedWord = String.Empty;

        if (String.Compare(tokenSplit, "T-Storms", true) == 0 || String.Compare(tokenSplit, "T-Storm", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "370");
        }
        else if (String.Compare(tokenSplit, "Partly", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "371");
        }
        else if (String.Compare(tokenSplit, "Mostly", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "372");
        }
        else if (String.Compare(tokenSplit, "Sunny", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "373");
        }
        else if (String.Compare(tokenSplit, "Cloudy", true) == 0 || String.Compare(tokenSplit, "Clouds", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "374");
        }
        else if (String.Compare(tokenSplit, "Snow", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "375");
        }
        else if (String.Compare(tokenSplit, "Rain", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "376");
        }
        else if (String.Compare(tokenSplit, "Light", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "377");
        }
        else if (String.Compare(tokenSplit, "AM", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "378");
        }
        else if (String.Compare(tokenSplit, "PM", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "379");
        }
        else if (String.Compare(tokenSplit, "Showers", true) == 0 || String.Compare(tokenSplit, "Shower", true) == 0 ||
                 String.Compare(tokenSplit, "T-Showers", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "380");
        }
        else if (String.Compare(tokenSplit, "Few", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "381");
        }
        else if (String.Compare(tokenSplit, "Scattered", true) == 0 || String.Compare(tokenSplit, "Isolated", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "382");
        }
        else if (String.Compare(tokenSplit, "Wind", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "383");
        }
        else if (String.Compare(tokenSplit, "Strong", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "384");
        }
        else if (String.Compare(tokenSplit, "Fair", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "385");
        }
        else if (String.Compare(tokenSplit, "Clear", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "386");
        }
        else if (String.Compare(tokenSplit, "Early", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "387");
        }
        else if (String.Compare(tokenSplit, "and", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "388");
        }
        else if (String.Compare(tokenSplit, "Fog", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "389");
        }
        else if (String.Compare(tokenSplit, "Haze", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "390");
        }
        else if (String.Compare(tokenSplit, "Windy", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "391");
        }
        else if (String.Compare(tokenSplit, "Drizzle", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "392");
        }
        else if (String.Compare(tokenSplit, "Freezing", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "393");
        }
        else if (String.Compare(tokenSplit, "N/A", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "394");
        }
        else if (String.Compare(tokenSplit, "Mist", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "395");
        }
        else if (String.Compare(tokenSplit, "High", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "799");
        }
        else if (String.Compare(tokenSplit, "Low", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "798");
        }
        else if (String.Compare(tokenSplit, "Moderate", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "534");
        }
        else if (String.Compare(tokenSplit, "Late", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "553");
        }
        else if (String.Compare(tokenSplit, "Very", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "554");
        }
          // wind directions
        else if (String.Compare(tokenSplit, "N", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "535");
        }
        else if (String.Compare(tokenSplit, "E", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "536");
        }
        else if (String.Compare(tokenSplit, "S", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "537");
        }
        else if (String.Compare(tokenSplit, "W", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "538");
        }
        else if (String.Compare(tokenSplit, "NE", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "539");
        }
        else if (String.Compare(tokenSplit, "SE", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "540");
        }
        else if (String.Compare(tokenSplit, "SW", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "541");
        }
        else if (String.Compare(tokenSplit, "NW", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "542");
        }
        else if (String.Compare(tokenSplit, "Thunder", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "543");
        }
        else if (String.Compare(tokenSplit, "NNE", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "544");
        }
        else if (String.Compare(tokenSplit, "ENE", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "545");
        }
        else if (String.Compare(tokenSplit, "ESE", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "546");
        }
        else if (String.Compare(tokenSplit, "SSE", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "547");
        }
        else if (String.Compare(tokenSplit, "SSW", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "548");
        }
        else if (String.Compare(tokenSplit, "WSW", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "549");
        }
        else if (String.Compare(tokenSplit, "WNW", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "551");
        }
        else if (String.Compare(tokenSplit, "NNW", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "552");
        }
        else if (String.Compare(tokenSplit, "VAR", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "556");
        }
        else if (String.Compare(tokenSplit, "CALM", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "557");
        }
        else if (String.Compare(tokenSplit, "Storm", true) == 0 || String.Compare(tokenSplit, "Gale", true) == 0 ||
                 String.Compare(tokenSplit, "Tempest", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "599");
        }
        else if (String.Compare(tokenSplit, "in the Vicinity", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "559");
        }
        else if (String.Compare(tokenSplit, "Clearing", true) == 0)
        {
          localizedWord = ServiceScope.Get<ILocalisation>().ToString("weather", "560");
        }

        if (localizedWord == String.Empty)
        {
          localizedWord = tokenSplit; //if not found, let fallback
        }

        localizedLine = localizedLine + localizedWord;
        localizedLine += " ";
      }
      return localizedLine;
    }

    /// <summary>
    /// converts speed to other metrics
    /// </summary>
    /// <param name="curSpeed"></param>
    /// <returns></returns>
    private int ConvertSpeed(int curSpeed)
    {
      //we might not need to convert at all
      if ((_temperatureFarenheit[0] == 'C' && _windSpeed[0] == 'K') ||
          (_temperatureFarenheit[0] == 'F' && _windSpeed[0] == 'M'))
      {
        return curSpeed;
      }

      //got through that so if temp is C, speed must be M or S
      if (_temperatureFarenheit[0] == 'C')
      {
        if (_windSpeed[0] == 'S')
        {
          return (int) (curSpeed*(1000.0/3600.0) + 0.5); //mps
        }
        else
        {
          return (int) (curSpeed/(8.0/5.0)); //mph
        }
      }
      else
      {
        if (_windSpeed[0] == 'S')
        {
          return (int) (curSpeed*(8.0/5.0)*(1000.0/3600.0) + 0.5); //mps
        }
        else
        {
          return (int) (curSpeed*(8.0/5.0)); //kph
        }
      }
    }

    /// <summary>
    /// parses the uv index
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static string ParseUVIndex(XmlNode element)
    {
      if (element == null)
      {
        return String.Empty;
      }
      return String.Format("{0} {1}", GetInteger(element, "i"), LocalizeOverview(GetString(element, "t", String.Empty)));
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
      {
        return String.Empty;
      }

      string wind;
      int tempInteger = ConvertSpeed(GetInteger(node, "s")); //convert speed if needed
      string tempString = LocalizeOverview(GetString(node, "t", "N")); //current wind direction

      if (tempInteger != 0) // Have wind
      {
        //From <dir eg NW> at <speed> km/h	
        string format = ServiceScope.Get<ILocalisation>().ToString("weather", "555");
        if (format == "")
        {
          format = "From {0} at {1} {2}";
        }
        wind = String.Format(format, tempString, tempInteger, unitSpeed);
      }
      else // Calm
      {
        wind = ServiceScope.Get<ILocalisation>().ToString("weather", "558");
        if (wind == "")
        {
          wind = "No wind";
        }
      }
      return wind;
    }

    #endregion

    #endregion
  }
}
