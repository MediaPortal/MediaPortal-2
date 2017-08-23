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
  public class ImagesAction : VisibilityDependsOnServerConnectStateAction
  {
    #region Consts

    public const string IMAGES_CONTRIBUTOR_MODEL_ID_STR = "7190C5BB-430E-48be-A6B2-C651A840C54A";

    public static readonly Guid IMAGES_CONTRIBUTOR_MODEL_ID = new Guid(IMAGES_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    public ImagesAction() :
        base(true, Consts.WF_STATE_ID_IMAGES_NAVIGATION_ROOT, Consts.RES_IMAGES_MENU_ITEM) { }
  }
}