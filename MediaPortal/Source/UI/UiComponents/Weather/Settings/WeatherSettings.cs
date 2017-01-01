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
using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.Weather.Settings
{
  /// <summary>
  /// Weather settings class.
  /// </summary>
  public class WeatherSettings
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public WeatherSettings()
    {
      LocationsList = new List<CitySetupInfo>();
    }

    /// <summary>
    /// RefreshInterval in Seconds.
    /// </summary>
    [Setting(SettingScope.User, 600)]
    public int? RefreshInterval { get; set; }

    /// <summary>
    /// Date and time of last update.
    /// </summary>
    [Setting(SettingScope.User, null)]
    public DateTime? LastUpdate { get; set; }

    /// <summary>
    /// Location of the XML files to parse to.
    /// </summary>
    [Setting(SettingScope.User, @"<CONFIG>\weather.location{0}.xml")]
    public string ParsefileLocation { get; set; }

    /// <summary>
    /// Selected location code.
    /// </summary>
    [Setting(SettingScope.User, "<none>")]
    public string LocationCode { get; set; }

    /// <summary>
    /// Skip internet connection test.
    /// </summary>
    [Setting(SettingScope.User, false)]
    public bool SkipConnectionTest { get; set; }

    /// <summary>
    /// List of all locations.
    /// </summary>
    [Setting(SettingScope.User, null)]
    public List<CitySetupInfo> LocationsList { get; set; }
  }
}