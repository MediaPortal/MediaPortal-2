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

namespace MediaPortal.Plugins.SystemStateMenu.Settings
{
  /// <summary>
  /// Shutdown settings class.
  /// </summary>
  public class SystemStateDialogSettings
  {
    private List<SystemStateItem> _shutdownItemList;

    /// <summary>
    /// Constructor
    /// </summary>
    public SystemStateDialogSettings()
    {
      ShutdownItemList = new List<SystemStateItem>();
    }

    [Setting(SettingScope.User, 60)]
    public int? LastCustomSleepTimeout { get; set; }

    [Setting(SettingScope.User, SystemStateAction.Suspend)]
    public SystemStateAction? LastCustomSleepAction { get; set; }

    [Setting(SettingScope.User, 300)]
    public int? MaxSleepTimeout { get; set; }

    [Setting(SettingScope.User, SystemStateAction.Shutdown)]
    public SystemStateAction? LastSleepTimerAction { get; set; }

    [Setting(SettingScope.User, null)]
    public List<SystemStateItem> ShutdownItemList
    {
      get
      {
        if (_shutdownItemList == null)
          CreateDefaultShutdownMenu();

        return _shutdownItemList;
      }
      set
      {
        _shutdownItemList = value;
      }
    }

    private void CreateDefaultShutdownMenu()
    {
      _shutdownItemList = new List<SystemStateItem>
                            {
                              new SystemStateItem(SystemStateAction.Suspend, true),
                              new SystemStateItem(SystemStateAction.Hibernate, true),
                              new SystemStateItem(SystemStateAction.Shutdown, true),
                              new SystemStateItem(SystemStateAction.SleepTimer, true),
                              new SystemStateItem(SystemStateAction.Restart, true),
                              new SystemStateItem(SystemStateAction.CloseMP, true),
                              new SystemStateItem(SystemStateAction.MinimizeMP, false),
                              new SystemStateItem(SystemStateAction.Logoff, false)
                            };
    }
  }
}
