#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.PackageManager.Settings
{
  public class PackageManagerClientSettings
  {
    /// <summary>
    /// TODO: Add proper Url to repository!
    /// </summary>
    [Setting(SettingScope.Global, "http://localhost:57235/")]
    public string PackageRepository { get; set; }

    /// <summary>
    /// Optional username to authenticate at repository. It's only require for write access like rating or review.
    /// </summary>
    [Setting(SettingScope.User, null)]
    public string UserName { get; set; }

    /// <summary>
    /// Optional password to authenticate at repository. It's only require for write access like rating or review.
    /// </summary>
    [Setting(SettingScope.User, null)]
    public string Password { get; set; }
  }
}
