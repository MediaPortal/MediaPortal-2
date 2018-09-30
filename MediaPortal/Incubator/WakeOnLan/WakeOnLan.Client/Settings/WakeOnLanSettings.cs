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
using WakeOnLan.Common;

namespace WakeOnLan.Client.Settings
{
  public class WakeOnLanSettings
  {
    public const int DEFAULT_PORT = 1234;
    public const int DEFAULT_WAKE_TIMEOUT = 10000;
    public const int DEFAULT_PING_TIMEOUT = 1000;
    public const int DEFAULT_NETWORK_CONNECTED_TIMEOUT = 20000;

    [Setting(SettingScope.Global, true)]
    public bool EnableWakeOnLan { get; set; }

    [Setting(SettingScope.Global)]
    public WakeOnLanAddress ServerWakeOnLanAddress { get; set; }

    [Setting(SettingScope.Global, DEFAULT_PORT)]
    public int Port { get; set; }

    [Setting(SettingScope.Global, DEFAULT_WAKE_TIMEOUT)]
    public int WakeTimeout { get; set; }

    [Setting(SettingScope.Global, DEFAULT_PING_TIMEOUT)]
    public int PingTimeout { get; set; }

    [Setting(SettingScope.Global, DEFAULT_NETWORK_CONNECTED_TIMEOUT)]
    public int NetworkConnectedTimeout { get; set; }
  }
}
