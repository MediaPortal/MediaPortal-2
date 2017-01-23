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
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.UiComponents.SkinBase.General
{
  public class Consts
  {
    // Screens
    public const string SCREEN_SHARES_CONFIG_RELOCATE_DIALOG = "shares_config_relocate_dialog";
    public const string SCREEN_SUPERLAYER_VOLUME = "Volume";
    public const string SCREEN_SHARES_CONFIG_PROBLEM = "shares_config_problem";

    public const string SCREEN_DEFAULT_BACKGROUND = "default-background";
    public const string SCREEN_VIDEO_BACKGROUND = "video-background";
    public const string SCREEN_IMAGE_BACKGROUND = "image-background";

    // Dialogs
    public const string DIALOG_ATTACH_TO_SERVER = "AttachToServerDialog";

    public const string DIALOG_PATH_BROWSER = "DialogPathBrowser";

    // Language resources
    public const string RES_SYSTEM_HINT = "[System.Hint]";
    public const string RES_SYSTEM_INFORMATION = "[System.Information]";
    public const string RES_SYSTEM_WARNING = "[System.Warning]";
    public const string RES_SYSTEM_ERROR = "[System.Error]";

    public const string RES_ONE_MORE_NOTIFICATION = "[Notifications.OneMoreNotification]";
    public const string RES_N_MORE_NOTIFICATIONS = "[Notifications.NMoreNotifications]";

    public const string RES_NOTIFICATION_HOME_SERVER_AVAILABLE_IN_NETWORK_TITLE = "[ServerConnection.NotificationServerAvailableTitle]";
    public const string RES_NOTIFICATION_HOME_SERVER_AVAILABLE_IN_NETWORK_TEXT = "[ServerConnection.NotificationServerAvailableText]";

    public const string RES_SERVER_FORMAT_TEXT = "[ServerConnection.ServerFormatText]";
    public const string RES_UNKNOWN_SERVER_NAME = "[ServerConnection.UnknownServerName]";
    public const string RES_UNKNOWN_SERVER_SYSTEM = "[ServerConnection.UnknownServerSystem]";

    public const string RES_ATTACH_INFO_DIALOG_HEADER = "[ServerConnection.AttachInfoDialogHeader]";
    public const string RES_ATTACH_INFO_DIALOG_TEXT = "[ServerConnection.AttachInfoDialogText]";

    public const string RES_DETACH_CONFIRM_DIALOG_HEADER = "[ServerConnection.DetachConfirmDialogHeader]";
    public const string RES_DETACH_CONFIRM_DIALOG_TEXT = "[ServerConnection.DetachConfirmDialogText]";

    public const string RES_SEARCH_FOR_SERVERS = "[ServerConnection.SearchForServers]";
    public const string RES_DETACH_FROM_SERVER = "[ServerConnection.DetachFromServer]";

    public const string RES_IMPORT_STARTED_TITLE = "[ImporterMessages.ImportStartedTitle]";
    public const string RES_IMPORT_STARTED_TEXT = "[ImporterMessages.ImportStartedText]";
    public const string RES_IMPORT_COMPLETED_TITLE = "[ImporterMessages.ImportCompletedTitle]";
    public const string RES_IMPORT_COMPLETED_TEXT = "[ImporterMessages.ImportCompletedText]";

    public const string RES_SHARES_CONFIG_LOCAL_SHARE = "[SharesConfig.LocalShare]";
    public const string RES_SHARES_CONFIG_GLOBAL_SHARE = "[SharesConfig.GlobalShare]";
    public const string RES_INVALID_PATH = "[Shares.InvalidPath]";

    public const string RES_CANNOT_ADD_SHARES_TITLE = "[SharesConfig.CannotAddSharesTitle]";
    public const string RES_CANNOT_ADD_SHARE_LOCAL_HOME_SERVER_NOT_CONNECTED = "[SharesConfig.CannotAddShareLocalHomeServerNotConnected]";

    public const string RES_ADD_SHARE_TITLE = "[SharesConfig.AddLocalShare]";
    public const string RES_EDIT_SHARE_TITLE = "[SharesConfig.EditLocalShare]";

    public const string RES_SHARE_NAME_EXISTS = "[SharesConfig.ShareNameExists]";
    public const string RES_SHARE_PATH_EXISTS = "[SharesConfig.SharePathExists]";
    public const string RES_SHARE_NAME_EMPTY = "[SharesConfig.ShareNameEmpty]";

    public const string RES_EDIT_SHARE_PROBLEM = "[SharesConfig.EditShareProblemHeader]";

    public const string RES_REIMPORT_ALL_SHARES = "[SharesOverview.ReImportAllShares]";

    public const string RES_CURRENT_MEDIA = "[Players.CurrentMediaInfo]";

    public const string RES_FULLSCREEN_VIDEO = "[Players.FullscreenVideo]";
    public const string RES_AUDIO_VISUALIZATION = "[Players.AudioVisualization]";
    public const string RES_FULLSCREEN_IMAGE = "[Players.FullscreenImage]";

    public const string RES_PLAYER_CONFIGURATION = "[Players.PlayerConfiguration]";

    public const string RES_PLAYER_OF_TYPE = "[Players.PlayerOfType]";
    public const string RES_SLOT_NO = "[Players.SlotNo]";
    public const string RES_FOCUS_PLAYER = "[Players.FocusPlayer]";
    public const string RES_SWITCH_PIP_PLAYERS = "[Players.SwitchPipPlayers]";
    public const string RES_CHOOSE_AUDIO_STREAM = "[Players.ChooseAudioStream]";
    public const string RES_MUTE = "[Players.Mute]";
    public const string RES_MUTE_OFF = "[Players.MuteOff]";
    public const string RES_CLOSE_PLAYER_CONTEXT = "[Players.ClosePlayerContext]";
    public const string RES_CHOOSE_PLAYER_GEOMETRY = "[Players.ChoosePlayerGeometry]";
    public const string RES_CHOOSE_PLAYER_EFFECT = "[Players.ChoosePlayerEffect]";
    public const string RES_CHOOSE_PLAYER_SUBTITLE = "[Players.ChoosePlayerSubtitle]";

    public const string RES_PLAYER_SLOT_AUDIO_MENU = "[Players.PlayerSlotAudioMenu]";

    // Images and icons
    public const string REL_PATH_USER_INTERACTION_REQUIRED_ICON = "user-interaction-required-icon.png";
    public const string REL_PATH_INFO_ICON = "info-icon.png";
    public const string REL_PATH_WARNING_ICON = "warning-icon.png";
    public const string REL_PATH_ERROR_ICON = "error-icon.png";

    // Action ids
    public const string STR_ACTION_ID_REIMPORT_ALL_SHARES = "FD35E282-5563-4D79-B600-A752DD8A57D2";
    public static readonly Guid ACTION_ID_REIMPORT_ALL_SHARES = new Guid(STR_ACTION_ID_REIMPORT_ALL_SHARES);

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

    public const string STR_WF_STATE_ID_SHARES_OVERVIEW = "36B3F24A-29B4-4cb4-BC7D-434C51491CD2";
    public static readonly Guid WF_STATE_ID_SHARES_OVERVIEW = new Guid(STR_WF_STATE_ID_SHARES_OVERVIEW);

    public const string STR_WF_STATE_ID_SHARES_REMOVE = "900BA520-F989-48c0-B076-5DAD61945845";
    public static readonly Guid WF_STATE_ID_SHARES_REMOVE = new Guid(STR_WF_STATE_ID_SHARES_REMOVE);

    public const string STR_WF_STATE_ID_SHARE_INFO = "1D5618C2-61F4-403c-8946-E80B043BA021";
    public static readonly Guid WF_STATE_ID_SHARE_INFO = new Guid(STR_WF_STATE_ID_SHARE_INFO);

    public const string STR_WF_STATE_ID_SHARE_ADD_CHOOSE_SYSTEM = "6F7EB06A-2AC6-4bcb-9003-F5DA44E03C26";
    public static readonly Guid WF_STATE_ID_SHARE_ADD_CHOOSE_SYSTEM = new Guid(STR_WF_STATE_ID_SHARE_ADD_CHOOSE_SYSTEM);

    public const string STR_WF_STATE_ID_SHARE_EDIT_CHOOSE_RESOURCE_PROVIDER = "F3163500-3015-4a6f-91F6-A3DA5DC3593C";
    public static readonly Guid WF_STATE_ID_SHARE_EDIT_CHOOSE_RESOURCE_PROVIDER = new Guid(STR_WF_STATE_ID_SHARE_EDIT_CHOOSE_RESOURCE_PROVIDER);

    public const string STR_WF_STATE_ID_SHARE_EDIT_EDIT_PATH = "652C5A9F-EA50-4076-886B-B28FD167AD66";
    public static readonly Guid WF_STATE_ID_SHARE_EDIT_EDIT_PATH = new Guid(STR_WF_STATE_ID_SHARE_EDIT_EDIT_PATH);

    public const string STR_WF_STATE_ID_SHARE_EDIT_CHOOSE_PATH = "5652A9C9-6B20-45f0-889E-CFBF6086FB0A";
    public static readonly Guid WF_STATE_ID_SHARE_EDIT_CHOOSE_PATH = new Guid(STR_WF_STATE_ID_SHARE_EDIT_CHOOSE_PATH);

    public const string STR_WF_STATE_ID_SHARE_EDIT_EDIT_NAME = "ACDD705B-E60B-454a-9671-1A12A3A3985A";
    public static readonly Guid WF_STATE_ID_SHARE_EDIT_EDIT_NAME = new Guid(STR_WF_STATE_ID_SHARE_EDIT_EDIT_NAME);

    public const string STR_WF_STATE_ID_SHARE_EDIT_CHOOSE_CATEGORIES = "6218FE5B-767E-48e6-9691-65E466B6020B";
    public static readonly Guid WF_STATE_ID_SHARE_EDIT_CHOOSE_CATEGORIES = new Guid(STR_WF_STATE_ID_SHARE_EDIT_CHOOSE_CATEGORIES);

    public const string STR_WF_STATE_ID_IMPORT_OVERVIEW = "53139C8E-F7CE-4D49-AEAF-7B061DE175F8";
    public static readonly Guid WF_STATE_ID_IMPORT_OVERVIEW = new Guid(STR_WF_STATE_ID_IMPORT_OVERVIEW);

    public const string STR_WF_STATE_ID_PLAYER_CONFIGURATION_DIALOG = "D0B79345-69DF-4870-B80E-39050434C8B3";
    public static readonly Guid WF_STATE_ID_PLAYER_CONFIGURATION_DIALOG = new Guid(STR_WF_STATE_ID_PLAYER_CONFIGURATION_DIALOG);

    public const string STR_WF_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG = "A3F53310-4D93-4f93-8B09-D53EE8ACD829";
    public static Guid WF_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG = new Guid(STR_WF_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG);

    public const string STR_WF_STATE_ID_PLAYER_AUDIO_MENU_DIALOG = "428326CE-9DE1-41ff-A33B-BBB80C8AFAC5";
    public static Guid WF_STATE_ID_PLAYER_AUDIO_MENU_DIALOG = new Guid(STR_WF_STATE_ID_PLAYER_AUDIO_MENU_DIALOG);

    public const string STR_WF_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG = "D46F66DD-9E91-4788-ADFE-EBD96F1A489E";
    public static Guid WF_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG = new Guid(STR_WF_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG);

    public const string STR_WF_STATE_ID_PLAYER_CHOOSE_EFFECT_MENU_DIALOG = "DAD585DF-16FC-45AB-A6D7-FE5600080C7A";
    public static Guid WF_STATE_ID_PLAYER_CHOOSE_EFFECT_MENU_DIALOG = new Guid(STR_WF_STATE_ID_PLAYER_CHOOSE_EFFECT_MENU_DIALOG);

    public const string STR_WF_STATE_ID_PLAYER_CHOOSE_SUBTITLE_MENU_DIALOG = "29907D7A-2507-41C8-B082-D4BDA0728885";
    public static Guid WF_STATE_ID_PLAYER_CHOOSE_SUBTITLE_MENU_DIALOG = new Guid(STR_WF_STATE_ID_PLAYER_CHOOSE_SUBTITLE_MENU_DIALOG);

    // Keys for the ListItem's Labels in the ItemsLists
    public const string KEY_NAME = "Name";
    public const string KEY_SHARE = "Share";
    public const string KEY_RESOURCE_PROVIDER_METADATA = "ResourceProviderMetadata";
    public const string KEY_RESOURCE_PATH = "ResourcePath";
    public const string KEY_PATH = "Path";
    public const string KEY_MEDIA_CATEGORIES = "Categories";
    public const string KEY_MEDIA_CATEGORY = "Category";
    public const string KEY_PARENT_ITEM = "Parent";

    public const string KEY_SHARES_PROXY = "SharesProxy";
    public const string KEY_EXPANSION = "Expansion";
    public const string KEY_SYSTEM_SHARES = "SystemShares";
    public const string KEY_IS_IMPORTING = "IsImporting";
    public const string KEY_IMPORTING_PROGRESS = "ImportingProgress";
    public const string KEY_IS_CONNECTED = "IsConnected";
    public const string KEY_REIMPORT_ENABLED = "ReImportEnabled";

    public const string KEY_ITEM_ACTION = "MenuModel: Item-Action";
    public const string KEY_REGISTERED_ACTIONS = "MenuModel: RegisteredActions";
    public const string KEY_MENU_ITEMS = "MenuModel: MenuItems";

    public const string KEY_PLAYER_CONTEXT = "PlayerContext";
    public const string KEY_SHOW_MUTE = "ShowMute";

    public const string KEY_NAVIGATION_MODE = "NavigationMode";

    public const string KEY_SERVER_DESCRIPTOR = "ServerDescriptor";
    public const string KEY_SERVER_NAME = "ServerName";
    public const string KEY_SYSTEM = "System";
    public const string KEY_HOSTNAME = "HostName";
    public const string KEY_AUTO_CLOSE_ON_NO_SERVER = "AutoCloseOnNoServer";
  }
}
