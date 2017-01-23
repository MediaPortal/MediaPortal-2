#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Plugins.MceRemoteReceiver.Hardware;

namespace MediaPortal.Plugins.MceRemoteReceiver
{
  /// <summary>
  /// Settings class for MceRemoteReceiver.
  /// </summary>
  public class MceRemoteSettings
  {
    #region Variables

    protected List<MappedKeyCode> _remoteMap;
    protected List<eHomeTransceiver> _transceivers;

    #endregion Variables

    #region Properties

    /// <summary>
    /// Gets the remote map.
    /// </summary>
    [Setting(SettingScope.User)]
    public ICollection<MappedKeyCode> RemoteMap
    {
      get { return _remoteMap; }
    }

    /// <summary>
    /// Gets the transceiver list.
    /// </summary>
    [Setting(SettingScope.User)]
    public ICollection<eHomeTransceiver> Transceivers
    {
      get { return _transceivers; }
    }

    #endregion Properties

    #region Additional members for the XML serialization

    public List<MappedKeyCode> XML_RemoteMap
    {
      get { return _remoteMap; }
      set { _remoteMap = value; }
    }

    public List<eHomeTransceiver> XML_Transceivers
    {
      get { return _transceivers; }
      set { _transceivers = value; }
    }

    #endregion
  }
}
