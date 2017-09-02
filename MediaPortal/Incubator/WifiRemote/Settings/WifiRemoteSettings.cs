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

using System.Collections.Generic;
using MediaPortal.Common.Settings;

namespace MediaPortal.Plugins.WifiRemote.Settings
{
  /// <summary>
  /// Settings class for IrInputPlugin.
  /// </summary>
  public class WifiRemoteSettings
  {
    #region Variables

    protected string _serverHost;
    protected List<string> _remoteMap;

    #endregion Variables

    #region Properties

    /// <summary>
    /// Gets or sets the server host.
    /// </summary>
    [Setting(SettingScope.Global, "localhost")]
    public string ServerHost
    {
      get { return _serverHost; }
      set { _serverHost = value; }
    }

    /// <summary>
    /// Gets or sets the remote map.
    /// </summary>
    [Setting(SettingScope.User)]
    public ICollection<string> RemoteMap
    {
      get { return _remoteMap; }
    }

    #endregion Properties

    #region Additional members for the XML serialization

    public List<string> XML_RemoteMap
    {
      get { return _remoteMap; }
      set { _remoteMap = value; }
    }

    #endregion
  }
}
