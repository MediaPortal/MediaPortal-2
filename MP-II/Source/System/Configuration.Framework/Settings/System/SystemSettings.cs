#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using MediaPortal.Core.Settings;

namespace MediaPortal.Configuration.ConfigurationClasses.System
{
  /// <summary>
  /// Class to save all systemsettings (registry, start menu, ...),
  /// so the user can migrate his configuration files to another system.
  /// </summary>
  class SystemSettings
  {

    #region Variables

    private bool _autostart;
    private bool _balloontips;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether MediaPortal should autostart on Windows startup.
    /// </summary>
    [Setting(SettingScope.User, false)]
    public bool Autostart
    {
      get { return _autostart; }
      set { _autostart = value; }
    }


    /// <summary>
    /// Gets or sets whether tray area's balloontips are enabled.
    /// (for all applications)
    /// </summary>
    [Setting(SettingScope.User, true)]
    public bool Balloontips
    {
      get { return _balloontips; }
      set { _balloontips = value; }
    }

    [Setting(SettingScope.User, new int[] { 2, 1 })]
    public int[] TestArray
    {
      get { return new int[] { 1, 2 }; }
      //get { return new int[0]; }
      set { }
    }

    #endregion

  }
}