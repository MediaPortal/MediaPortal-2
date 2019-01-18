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
  /// Settings class for WifiRemote.
  /// </summary>
  public class WifiRemoteSettings
  {
    #region Variables

    protected string _passCode = "mediaportal";
    protected int _port = 8017;
    protected bool _enableBonjour = true;
    protected string _serviceName = "MP2 Wifi Remote";
    protected int _authenticationMethod = 1;
    protected int _autoLoginTimeout = 0;

    #endregion Variables

    #region Properties

    /// <summary>
    /// Gets or sets the pass code to use for athentication.
    /// </summary>
    [Setting(SettingScope.Global, "mediaportal")]
    public string PassCode
    {
      get { return _passCode; }
      set { _passCode = value; }
    }

    /// <summary>
    /// Gets or sets the server port to use.
    /// </summary>
    [Setting(SettingScope.Global, 8017)]
    public int Port
    {
      get { return _port; }
      set { _port = value; }
    }

    /// <summary>
    /// Gets or sets whether Bonjour (Zero config) should be enabled.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool EnableBonjour
    {
      get { return _enableBonjour; }
      set { _enableBonjour = value; }
    }

    /// <summary>
    /// Gets or sets the pass code to use for athentication.
    /// </summary>
    [Setting(SettingScope.Global, "MP2 Wifi Remote")]
    public string ServiceName
    {
      get { return _serviceName; }
      set { _serviceName = value; }
    }

    /// <summary>
    /// Gets or sets the authentication method to use.
    /// </summary>
    [Setting(SettingScope.Global, 1)]
    public int AuthenticationMethod
    {
      get { return _authenticationMethod; }
      set { _authenticationMethod = value; }
    }

    /// <summary>
    /// Gets or sets the auto login timeout.
    /// </summary>
    [Setting(SettingScope.Global, 0)]
    public int AutoLoginTimeout
    {
      get { return _autoLoginTimeout; }
      set { _autoLoginTimeout = value; }
    }

    #endregion Properties
  }
}
