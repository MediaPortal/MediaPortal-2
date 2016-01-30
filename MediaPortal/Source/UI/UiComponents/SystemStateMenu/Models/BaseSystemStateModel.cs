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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SystemStateMenu.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SystemStateMenu.Models
{
  public class BaseSystemStateModel
  {
    protected ItemsList _shutdownItems = new ItemsList();

    public ItemsList ShutdownItems
    {
      get
      {
        UpdateShutdownItems();
        return _shutdownItems;
      }
    }

    protected void UpdateShutdownItems()
    {
      ISettingsManager sm = ServiceRegistration.Get<ISettingsManager>();
      List<SystemStateItem> systemStateItems = sm.Load<SystemStateDialogSettings>().ShutdownItemList;

      _shutdownItems.Clear();
      if (systemStateItems != null)
      {
        for (int i = 0; i < systemStateItems.Count; i++)
        {
          SystemStateItem systemStateItem = systemStateItems[i];
          if (!systemStateItem.Enabled)
            continue;
          ListItem item = new ListItem(Consts.KEY_NAME, Consts.GetResourceIdentifierForMenuItem(systemStateItem.Action));
          item.Command = new MethodDelegateCommand(() => DoAction(systemStateItem.Action));
          _shutdownItems.Add(item);
        }
      }
      _shutdownItems.FireChange();
    }

    protected void DoAction(SystemStateAction action)
    {
      switch (action)
      {
        case SystemStateAction.Suspend:
          ServiceRegistration.Get<ISystemStateService>().Suspend();
          return;
        case SystemStateAction.Hibernate:
          ServiceRegistration.Get<ISystemStateService>().Hibernate();
          return;
        case SystemStateAction.Shutdown:
          ServiceRegistration.Get<ISystemStateService>().Shutdown();
          return;
        case SystemStateAction.Restart:
          ServiceRegistration.Get<ISystemStateService>().Restart();
          return;
        case SystemStateAction.Logoff:
          ServiceRegistration.Get<ISystemStateService>().Logoff();
          return;
        case SystemStateAction.CloseMP:
          ServiceRegistration.Get<IScreenControl>().Shutdown();
          return;
        case SystemStateAction.MinimizeMP:
          ServiceRegistration.Get<IScreenControl>().Minimize();
          return;
      }
    }
  }
}
