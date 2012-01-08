#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.UiComponents.SkinBase.General
{
  public class Consts
  {
    // Screens
    public static string SCREEN_DEFAULT_BACKGROUND = "default-background";
    public static string SCREEN_VIDEO_BACKGROUND = "video-background";
    public static string SCREEN_IMAGE_BACKGROUND = "image-background";

    // Language resources
    public const string RES_SYSTEM_HINT = "[System.Hint]";
    public const string RES_SYSTEM_INFORMATION = "[System.Information]";
    public const string RES_SYSTEM_WARNING = "[System.Warning]";
    public const string RES_SYSTEM_ERROR = "[System.Error]";

    public const string RES_ONE_MORE_NOTIFICATION = "[Notifications.OneMoreNotification]";
    public const string RES_N_MORE_NOTIFICATIONS = "[Notifications.NMoreNotifications]";

    public const string RES_NOTIFICATION_HOME_SERVER_AVAILABLE_IN_NETWORK_TITLE = "[ServerConnection.NotificationServerAvailableTitle]";
    public const string RES_NOTIFICATION_HOME_SERVER_AVAILABLE_IN_NETWORK_TEXT = "[ServerConnection.NotificationServerAvailableText]";

    public const string RES_IMPORT_STARTED_TITLE = "[ImporterMessages.ImportStartedTitle]";
    public const string RES_IMPORT_STARTED_TEXT = "[ImporterMessages.ImportStartedText]";
    public const string RES_IMPORT_COMPLETED_TITLE = "[ImporterMessages.ImportCompletedTitle]";
    public const string RES_IMPORT_COMPLETED_TEXT = "[ImporterMessages.ImportCompletedText]";

    // Images and icons
    public const string REL_PATH_USER_INTERACTION_REQUIRED_ICON = "user-interaction-required-icon.png";
    public const string REL_PATH_INFO_ICON = "info-icon.png";
    public const string REL_PATH_WARNING_ICON = "warning-icon.png";
    public const string REL_PATH_ERROR_ICON = "error-icon.png";

    // Workflow states
    public const string STR_WF_STATE_ID_WATCH_NOTIFICATIONS = "9B1EADDC-C5CD-4a3a-B26A-91B943F680AD";
    public static readonly Guid WF_STATE_ID_WATCH_NOTIFICATIONS = new Guid(STR_WF_STATE_ID_WATCH_NOTIFICATIONS);

    public const string STR_WF_STATE_ID_ATTACH_TO_SERVER = "E834D0E0-BC35-4397-86F8-AC78C152E693";

    /// <summary>
    /// In this state, the <see cref="ServerAttachmentModel"/> shows configuration dialogs to choose one of the home server
    /// which are present in the network. This state is valid if there is no home server attached. Else, this state is invalid.
    /// </summary>
    public static Guid WF_STATE_ID_ATTACH_TO_SERVER = new Guid(STR_WF_STATE_ID_ATTACH_TO_SERVER);

    public const string STR_WF_STATE_ID_DETACH_FROM_SERVER = "BAC42991-5AB6-471f-A185-673D2E3B1EBA";

    /// <summary>
    /// In this state, the <see cref="ServerAttachmentModel"/> shows a dialog where it asks the user if he really
    /// wants to detach from the current home server. This state is valid if there is a home server attached. Else, this state
    /// is invalid.
    /// </summary>
    public static Guid WF_STATE_ID_DETACH_FROM_SERVER = new Guid(STR_WF_STATE_ID_DETACH_FROM_SERVER);

  }
}