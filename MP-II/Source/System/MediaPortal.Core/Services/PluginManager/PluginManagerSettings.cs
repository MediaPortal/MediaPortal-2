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

using System;
using System.Collections.Generic;
using MediaPortal.Core.Settings;

namespace MediaPortal.Core.Services.PluginManager
{
  public class PluginManagerSettings
  {
    #region Protected properties

    protected List<Guid> _userDisabledPlugins = new List<Guid>();

    #endregion

    public void AddUserDisabledPlugin(Guid pluginId)
    {
      if (!_userDisabledPlugins.Contains(pluginId))
        _userDisabledPlugins.Add(pluginId);
    }

    public void RemoveUserDisabledPlugin(Guid pluginId)
    {
      _userDisabledPlugins.Remove(pluginId);
    }

    [Setting(SettingScope.User)]
    public ICollection<Guid> UserDisabledPlugins
    {
      get { return _userDisabledPlugins; }
      set { _userDisabledPlugins = new List<Guid>(value); }
    }

    #region Additional members for the XML serialization

    public List<Guid> XML_UserDisabledPlugins
    {
      get { return _userDisabledPlugins; }
      set { _userDisabledPlugins = value; }
    }

    #endregion
  }
}
