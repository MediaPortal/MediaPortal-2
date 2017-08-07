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

namespace MediaPortal.UiComponents.Login.General
{
  public class Consts
  {
    // Workflow states
    public const string STR_WF_STATE_ID_USERS_OVERVIEW = "75488A94-7BEC-44FF-836D-7A2A8C7AFEF0";
    public static readonly Guid WF_STATE_ID_USERS_OVERVIEW = new Guid(STR_WF_STATE_ID_USERS_OVERVIEW);

    // Keys for the ListItem's Labels in the ItemsLists
    public const string KEY_NAME = "Name";
    public const string KEY_USER_PROXY = "UserProxy";
  }
}
