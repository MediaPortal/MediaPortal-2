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

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.Settings
{
  public class NetworkNeighborhoodResourceProviderSettings
  {
    /// <summary>
    /// Gets or sets an indicator, if the server should try to impersonate the current interactive session.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool ImpersonateInteractive { get; set; }

    /// <summary>
    /// Gets or sets an indicator, if the server should use username and password to access network resources.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool UseCredentials { get; set; }

    /// <summary>
    /// Username to access network resources.
    /// </summary>
    [Setting(SettingScope.Global, null)]
    public string NetworkUserName { get; set; }

    /// <summary>
    /// Password to access network resources.
    /// </summary>
    [Setting(SettingScope.Global, null)]
    public string NetworkPassword { get; set; }
  }
}
