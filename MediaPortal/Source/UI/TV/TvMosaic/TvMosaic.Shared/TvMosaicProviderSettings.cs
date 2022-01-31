#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace TvMosaic.Shared
{
  public class TvMosaicProviderSettings
  {
    /// <summary>
    /// Holds the host name or IP address of the TvMosaic service, this is used on clients and the server
    /// so should be an address that is accessible from all machines on the network.
    /// </summary>
    [Setting(SettingScope.Global, "localhost")]
    public string Host { get; set; }

    /// <summary>
    /// Holds the port to use to connect of the TvMosaic service.
    /// </summary>
    [Setting(SettingScope.Global, 9270)]
    public int Port { get; set; }

    /// <summary>
    /// When TvMosaic is configured to require authentication, this Username is used for establishing the connection.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string Username { get; set; }

    /// <summary>
    /// When TvMosaic is configured to require authentication, this Password is used for establishing the connection.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string Password { get; set; }

    /// <summary>
    /// Holds the last selected channel group ID.
    /// </summary>
    [Setting(SettingScope.Global)]
    public int LastChannelGroupId { get; set; }
  }
}
