#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

namespace MediaPortal.UiComponents.SkinBase.General
{
  public class Consts
  {
    // Screens
    public static string SCREEN_DEFAULT_BACKGROUND = "default-background";
    public static string SCREEN_VIDEO_BACKGROUND = "video-background";
    public static string SCREEN_PICTURE_BACKGROUND = "picture-background";

    // Language resources
    public const string RES_SYSTEM_HINT = "[System.Hint]";
    public const string RES_SYSTEM_INFORMATION = "[System.Information]";
    public const string RES_SYSTEM_WARNING = "[System.Warning]";
    public const string RES_SYSTEM_ERROR = "[System.Error]";

    public const string RES_ONE_MORE_NOTIFICATION = "[Notifications.OneMoreNotification]";
    public const string RES_N_MORE_NOTIFICATIONS = "[Notifications.NMoreNotifications]";

    // Images and icons
    public const string REL_PATH_USER_INTERACTION_REQUIRED_ICON = "user-interaction-required-icon.png";
    public const string REL_PATH_INFO_ICON = "info-icon.png";
    public const string REL_PATH_WARNING_ICON = "warning-icon.png";
    public const string REL_PATH_ERROR_ICON = "error-icon.png";

    // Models
    public const string STR_MODEL_ID_NOTIFICATIONS = "843F373D-0B4B-47ba-8DD1-0D18F00FAAD3";
    public static readonly Guid MODEL_ID_NOTIFICATIONS = new Guid(STR_MODEL_ID_NOTIFICATIONS);

    // Workflow states
    public const string STR_STATE_ID_WATCH_NOTIFICATIONS = "9B1EADDC-C5CD-4a3a-B26A-91B943F680AD";
    public static readonly Guid STATE_ID_WATCH_NOTIFICATIONS = new Guid(STR_STATE_ID_WATCH_NOTIFICATIONS);
  }
}