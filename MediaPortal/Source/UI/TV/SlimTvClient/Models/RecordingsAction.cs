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
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.UiComponents.Media.Actions;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  public class RecordingsAction : VisibilityDependsOnServerConnectStateAction
  {
    #region Consts

    public const string RECORDINGS_CONTRIBUTOR_MODEL_ID_STR = "117A9807-8B10-47F1-8780-C748DBCF45BA";

    public static readonly Guid RECORDINGS_CONTRIBUTOR_MODEL_ID = new Guid(RECORDINGS_CONTRIBUTOR_MODEL_ID_STR);

    public const string RES_RECORDINGS_MENU_ITEM = "[SlimTvClient.RecordingsMenuItem]";

    #endregion

    public RecordingsAction() :
      base(true, SlimTvConsts.WF_MEDIA_NAVIGATION_ROOT_STATE, RES_RECORDINGS_MENU_ITEM) { }
  }
}