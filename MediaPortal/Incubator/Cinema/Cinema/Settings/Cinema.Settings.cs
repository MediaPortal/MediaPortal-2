#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using MediaPortal.Common.Settings;

namespace Cinema.Settings
{
  internal class CinemaSettings
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public CinemaSettings()
    {
    }

    /// <summary>
    /// List of all Locations
    /// </summary>
    [Setting(SettingScope.User, "DE")]
    public string LocationCountryCode { get; set; }

    /// <summary>
    /// Locations changed
    /// </summary>
    [Setting(SettingScope.User, "10119")]
    public string LocationPostalCode { get; set; }

    /// <summary>
    /// Selected ContentLanguage
    /// </summary>
    [Setting(SettingScope.User, "de")]
    public string ContentLanguage { get; set; }

    /// <summary>
    /// Date of Datalist
    /// </summary>
    [Setting(SettingScope.User)]
    public DateTime LastUpdate { get; set; } = new DateTime(2023, 01, 01);
  }
}
