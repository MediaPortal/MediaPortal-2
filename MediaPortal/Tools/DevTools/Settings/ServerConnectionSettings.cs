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

using MediaPortal.Common.General;
using MediaPortal.Common.Settings;

namespace MediaPortal.DevTools.Settings
{
  public class ServerConnectionSettings
  {
    protected string _homeServerSystemId = null;
    protected string _lastHomeServerName;
    protected SystemName _lastHomeServerSystem;

    /// <summary>
    /// UUID of our home server. The server connection manager will always try to connect to a home server
    /// of this UUID.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string HomeServerSystemId
    {
      get { return _homeServerSystemId; }
      set { _homeServerSystemId = value; }
    }

    /// <summary>
    /// Cached display name of the last connected home server.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string LastHomeServerName
    {
      get { return _lastHomeServerName; }
      set { _lastHomeServerName = value; }
    }

    /// <summary>
    /// Computer name of the last connected home server.
    /// </summary>
    [Setting(SettingScope.Global)]
    public SystemName LastHomeServerSystem
    {
      get { return _lastHomeServerSystem; }
      set { _lastHomeServerSystem = value; }
    }
  }
}
