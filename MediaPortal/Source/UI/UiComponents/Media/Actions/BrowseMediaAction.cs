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
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class BrowseMediaAction : VisibilityDependsOnServerConnectStateAction
  {
    #region Consts

    public const string BROWSE_MEDIA_CONTRIBUTOR_MODEL_ID_STR = "92F6CE34-CB28-40f7-9136-AB576F479F94";

    public static readonly Guid BROWSE_MEDIA_CONTRIBUTOR_MODEL_ID = new Guid(BROWSE_MEDIA_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    public BrowseMediaAction() :
        base(true, Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT, Consts.RES_BROWSE_MEDIA_MENU_ITEM) { }
  }
}