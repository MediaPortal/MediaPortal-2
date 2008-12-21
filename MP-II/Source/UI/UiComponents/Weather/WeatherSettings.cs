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

using System.Collections.Generic;
using MediaPortal.Core.Settings;

namespace UiComponents.Weather
{
  /// <summary>
  /// Sample settings class wich will implement your own settings object in your code/plugin
  /// Only public properties are stored/retrieved
  /// </summary>
  public class WeatherSettings
  {
    private int? _refreshInterval;
    private List<CitySetupInfo> _locationsList = new List<CitySetupInfo>();
    private string _parseFileLocation;
    private string _temperatureFahrenheit;
    private string _windSpeed;
    private string _locationCode;
    private bool _skipConnectionTest;

    /// <summary>
    /// Scope and default value attribute
    /// </summary>
    // RefreshInterval in Seconds
    [Setting(SettingScope.User, 600)]
    public int? RefreshInterval
    {
      get { return _refreshInterval; }
      set { _refreshInterval = value; }
    }

    // Location of the XML files to parse to
    [Setting(SettingScope.User, "Media/Users/location{0}.xml")]
    public string ParsefileLocation
    {
      get { return _parseFileLocation; }
      set { _parseFileLocation = value; }
    }

    [Setting(SettingScope.User, "C")]
    public string TemperatureFahrenheit
    {
      get { return _temperatureFahrenheit; }
      set { _temperatureFahrenheit = value; }
    }

    [Setting(SettingScope.User, "mph")]
    public string WindSpeed
    {
      get { return _windSpeed; }
      set { _windSpeed = value; }
    }

    [Setting(SettingScope.User, "<none>")]
    public string LocationCode
    {
      get { return _locationCode; }
      set { _locationCode = value; }
    }

    [Setting(SettingScope.User, false)]
    public bool SkipConnectionTest
    {
      get { return _skipConnectionTest; }
      set { _skipConnectionTest = value; }
    }

    [Setting(SettingScope.User, null)]
    public List<CitySetupInfo> LocationsList
    {
      get { return _locationsList; }
      set { _locationsList = value; }
    }
  }
}
