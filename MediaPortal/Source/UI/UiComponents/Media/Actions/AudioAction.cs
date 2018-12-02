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
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class AudioAction : VisibilityDependsOnServerConnectStateAction
  {
    #region Consts

    public const string AUDIO_CONTRIBUTOR_MODEL_ID_STR = "D8922F2B-5B56-4218-93B1-570616BAAEAD";

    public static readonly Guid AUDIO_CONTRIBUTOR_MODEL_ID = new Guid(AUDIO_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    public AudioAction() :
        base(true, Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT, Consts.RES_AUDIO_MENU_ITEM) { }
  }
}