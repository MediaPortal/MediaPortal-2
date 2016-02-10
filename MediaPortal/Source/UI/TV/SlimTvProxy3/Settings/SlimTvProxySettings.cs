#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Plugins.SlimTv.Proxy.Settings
{
  class SlimTvProxySettings
  {
    public SlimTvProxySettings()
    {
      HostName = System.Net.Dns.GetHostName();
      DatabaseProvider = "MySQL";
      DatabaseConnectionString = "Server=localhost;Database=MpTvDb;User ID=root;Password=MediaPortal;charset=utf8;Connection Timeout=300;";
    }

    [Setting(SettingScope.Global)]
    public string HostName { get; private set; }
    [Setting(SettingScope.Global)]
    public string DatabaseProvider { get; private set; }
    [Setting(SettingScope.Global)]
    public string DatabaseConnectionString { get; private set; }
  }
}
