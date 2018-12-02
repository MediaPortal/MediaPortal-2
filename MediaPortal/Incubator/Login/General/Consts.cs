#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.UiComponents.Login.General
{
  public class Consts
  {
    // Keys for the ListItem's Labels in the ItemsLists
    public const string KEY_NAME = "Name";
    public const string KEY_USER = "User";
    public const string KEY_SHARE = "Share";
    public const string KEY_PROFILE_TEMPLATE_ID = "TemplateId";
    public const string KEY_COUNTRY = "Country";
    public const string KEY_RESTRICTION_GROUP = "RestrictionGroup";

    public const string RES_CLIENT_PROFILE_TEXT = "[UserConfig.ClientProfileType]";
    public const string RES_USER_PROFILE_TEXT = "[UserConfig.UserProfileType]";
    public const string RES_ADMIN_PROFILE_TEXT = "[UserConfig.AdminProfileType]";
    public const string RES_SHARES_TEXT = "[UserConfig.SharesText]";
    public const string RES_RESTRICTIONS_NUMBERS = "[UserConfig.RestrictedNumbers]";
    public const string RES_RESTRICTIONS_ALL = "[UserConfig.RestrictedAll]";
    public const string RES_RESTRICTIONS_NONE = "[UserConfig.RestrictedNone]";
    public const string RES_ANY_TEXT = "[UserConfig.AnyText]";
    public const string RES_NEW_USER_TEXT = "[UserConfig.NewUserText]";
    public const string RES_SELECT_USER_IMAGE = "[UserConfig.SelectImage]";
    public const string RES_DISABLE = "[UserConfig.Disable]";

    public const string RES_SYSTEM_DEFAULT_TEXT = "[Settings.Users.Config.SystemDefault]";

    public static Guid WF_STATE_ID_HOME_SCREEN = new Guid("7F702D9C-F2DD-42da-9ED8-0BA92F07787F");
    public static Guid WF_STATE_ID_LOGIN_SCREEN = new Guid("2529B0F0-8415-4A4E-971B-38D6CDD2406A");
  }
}
