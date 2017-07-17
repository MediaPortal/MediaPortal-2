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
  public class FavoriteMediaAction : VisibilityDependsOnServerConnectStateAction
  {
    #region Consts

    public const string FAVORITE_MEDIA_CONTRIBUTOR_MODEL_ID_STR = "EB11ECA4-F7AB-42E7-AFF9-A0D0F547131E";

    public static readonly Guid FAVORITE_MEDIA_CONTRIBUTOR_MODEL_ID = new Guid(FAVORITE_MEDIA_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    public FavoriteMediaAction() :
      base(true, Consts.WF_STATE_ID_FAVORITE_MEDIA, Consts.RES_FAVORITE_MEDIA_MENU_ITEM)
    {
    }
  }
}
