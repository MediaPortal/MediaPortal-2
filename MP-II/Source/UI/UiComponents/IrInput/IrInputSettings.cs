#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core.Settings;

namespace UiComponents.IrInput
{
  /// <summary>
  /// Settings class for IrInputPlugin.
  /// </summary>
  public class IrInputSettings
  {
    #region Variables

    protected string _serverHost;
    protected List<MappedKeyCode> _remoteMap;

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
    public ICollection<MappedKeyCode> RemoteMap
    {
      get { return _remoteMap; }
    }

    #endregion Properties

    #region Additional members for the XML serialization

    public List<MappedKeyCode> XML_RemoteMap
    {
      get { return _remoteMap; }
      set { _remoteMap = value; }
    }

    #endregion
  }
}
