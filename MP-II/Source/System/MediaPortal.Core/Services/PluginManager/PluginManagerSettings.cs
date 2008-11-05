#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

namespace MediaPortal.Core.Services.PluginManager
{
  public class PluginManagerSettings
  {
    #region Protected properties

    protected List<string> _userDisabledPlugins = new List<string>();

    #endregion

    public void AddUserDisabledPlugin(string pluginName)
    {
      if (!_userDisabledPlugins.Contains(pluginName))
        _userDisabledPlugins.Add(pluginName);
    }

    public void RemoveUserDisabledPlugin(string pluginName)
    {
      _userDisabledPlugins.Remove(pluginName);
    }

    public ICollection<string> UserDisabledPlugins
    {
      get { return _userDisabledPlugins; }
      set { _userDisabledPlugins = new List<string>(value); }
    }

    /// <summary>
    /// Only used for the settings system to serialize/deserialize the plugin list.
    /// </summary>
    [Setting(SettingScope.User, null)]
    public string[] UserDisabledPlugins_SettingInternal
    {
      get { return _userDisabledPlugins.ToArray(); }
      set
      {
        _userDisabledPlugins.Clear();
        _userDisabledPlugins.AddRange(value);
      }
    }
  }
}
