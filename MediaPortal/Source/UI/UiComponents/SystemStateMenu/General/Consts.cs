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

using System;

namespace MediaPortal.Plugins.SystemStateMenu
{
  public class Consts
  {
    public const string STR_WF_STATE_ID_SYSTEM_STATE_DIALOG = "BBFA7DB7-5055-48D5-A904-0F0C79849369";
    public const string STR_WF_STATE_ID_SYSTEM_STATE_CONFIGURATION_DIALOG = "F499DC76-2BCE-4126-AF4E-7FEB9DB88E80";
    public const string STR_WF_STATE_ID_SLEEP_TIMER_DIALOG = "5BFE10D3-6D66-46D8-B0BE-74F4190DA6A9";
    public const string STR_WF_STATE_ID_SLEEP_TIMER_MODEL = "40FDD1C3-CFAB-4731-9636-96726301B648";

    public static readonly Guid WF_STATE_ID_SYSTEM_STATE_DIALOG = new Guid(STR_WF_STATE_ID_SYSTEM_STATE_DIALOG);
    public static readonly Guid WF_STATE_ID_SYSTEM_STATE_CONFIGURATION_DIALOG = new Guid(STR_WF_STATE_ID_SYSTEM_STATE_CONFIGURATION_DIALOG);
    public static readonly Guid WF_STATE_ID_SLEEP_TIMER_DIALOG = new Guid(STR_WF_STATE_ID_SLEEP_TIMER_DIALOG);
    public static readonly Guid WF_STATE_ID_SLEEP_TIMER_MODEL = new Guid(STR_WF_STATE_ID_SLEEP_TIMER_MODEL);

    // Localization resource identifiers
    public const string RES_SYSTEM_HIBERNATE_MENU_ITEM = "[SystemState.Hibernate]";
    public const string RES_SYSTEM_SHUTDOWN_MENU_ITEM = "[SystemState.Shutdown]";
    public const string RES_SYSTEM_SUSPEND_MENU_ITEM = "[SystemState.Suspend]";
    public const string RES_SYSTEM_RESTART_MENU_ITEM = "[SystemState.Restart]";
    public const string RES_SYSTEM_LOGOFF_MENU_ITEM = "[SystemState.Logoff]";
    public const string RES_SYSTEM_SLEEPTIMER_CFG_MENU_ITEM = "[SystemState.SleepTimerConfig]";
    public const string RES_SYSTEM_SLEEPTIMER_STOP_MENU_ITEM = "[SystemState.SleepTimerStop]";

    public const string RES_MEDIAPORTAL_MINIMIZE_MENU_ITEM = "[SystemState.MinimizeMP]";
    public const string RES_MEDIAPORTAL_SHUTDOWN_MENU_ITEM = "[SystemState.ShutdownMP]";

    // Accessor keys for GUI communication
    public const string KEY_NAME = "Name";
    public const string KEY_ACTION = "Action";
    public const string KEY_INDEX = "Sort-Index";

    // SleepTimer
    public static readonly int DEFAULT_MAX_SLEEPTIME = 300;

    // ShutdownConfigurationDialogModel
    public const string KEY_IS_CHECKED = "IsChecked";
    public const string KEY_IS_DOWN_BUTTON_FOCUSED = "IsDownButtonFocused";
    public const string KEY_IS_UP_BUTTON_FOCUSED = "IsUpButtonFocused";

    public static string GetResourceIdentifierForMenuItem(SystemStateAction systemStateAction, bool timerActive = false)
    {
      switch (systemStateAction)
      {
        case SystemStateAction.Suspend:
          return RES_SYSTEM_SUSPEND_MENU_ITEM;
        case SystemStateAction.Hibernate:
          return RES_SYSTEM_HIBERNATE_MENU_ITEM;
        case SystemStateAction.Shutdown:
          return RES_SYSTEM_SHUTDOWN_MENU_ITEM;
        case SystemStateAction.Logoff:
          return RES_SYSTEM_LOGOFF_MENU_ITEM;
        case SystemStateAction.Restart:
          return RES_SYSTEM_RESTART_MENU_ITEM;
        case SystemStateAction.SleepTimer:
          return timerActive ? RES_SYSTEM_SLEEPTIMER_STOP_MENU_ITEM : RES_SYSTEM_SLEEPTIMER_CFG_MENU_ITEM;
        case SystemStateAction.CloseMP:
          return RES_MEDIAPORTAL_SHUTDOWN_MENU_ITEM;
        case SystemStateAction.MinimizeMP:
          return RES_MEDIAPORTAL_MINIMIZE_MENU_ITEM;
        default:
          return string.Empty;
      }
    }
  }
}
