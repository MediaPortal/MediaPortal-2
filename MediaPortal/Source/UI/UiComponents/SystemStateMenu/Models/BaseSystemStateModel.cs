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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SystemStateMenu.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using System.Collections.Generic;
using MediaPortal.UI.Presentation.Workflow;

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

      bool timerActive = false;
      SleepTimerModel stm = ServiceRegistration.Get<IWorkflowManager>().GetModel(Consts.WF_STATE_ID_SLEEP_TIMER_MODEL) as SleepTimerModel;
      if (stm != null && stm.IsSleepTimerActive)
      {
        timerActive = true;
      }

      _shutdownItems.Clear();
      if (systemStateItems != null)
      {
        for (int i = 0; i < systemStateItems.Count; i++)
        {
          SystemStateItem systemStateItem = systemStateItems[i];
          if (!systemStateItem.Enabled)
            continue;
          ListItem item = new ListItem(Consts.KEY_NAME, Consts.GetResourceIdentifierForMenuItem(systemStateItem.Action, timerActive))
          {
            Command = new MethodDelegateCommand(() => DoAction(systemStateItem.Action))
          };
          item.AdditionalProperties[Consts.KEY_ACTION] = systemStateItem.Action;
          _shutdownItems.Add(item);
        }
      }
      _shutdownItems.FireChange();
    }

    protected void DoClose(SystemStateAction action)
    {
      switch (action)
      {
        case SystemStateAction.SleepTimer:
          return;
        default:
          ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
          return;
      }
    }

    protected void DoAction(SystemStateAction action)
    {
      // I don't like this way, but I need it...
      DoClose(action);

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
        case SystemStateAction.SleepTimer:
          SleepTimerModel stm = ServiceRegistration.Get<IWorkflowManager>().GetModel(Consts.WF_STATE_ID_SLEEP_TIMER_MODEL) as SleepTimerModel;
          if (stm == null || stm.IsSleepTimerActive == false)
          {
            ServiceRegistration.Get<IWorkflowManager>().NavigatePop(1);

            ServiceRegistration.Get<IWorkflowManager>().NavigatePush(
              Consts.WF_STATE_ID_SLEEP_TIMER_DIALOG);
          }
          else
          {
            stm.Stop();
            UpdateShutdownItems();
          }
          return;
      }
    }
  }
}
