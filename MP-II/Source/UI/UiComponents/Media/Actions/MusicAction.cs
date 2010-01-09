#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using UiComponents.Media.Models;

namespace UiComponents.Media.Actions
{
  public class MusicAction : TrackServerConnectionBaseAction
  {
    #region Consts

    public const string MUSIC_CONTRIBUTOR_MODEL_ID_STR = "D8922F2B-5B56-4218-93B1-570616BAAEAD";

    public static readonly Guid MUSIC_CONTRIBUTOR_MODEL_ID = new Guid(MUSIC_CONTRIBUTOR_MODEL_ID_STR);

    public const string MUSIC_RESOURCE = "[Media.MusicMenuItem]";

    #endregion

    public MusicAction() :
        base(true, MediaModel.MUSIC_NAVIGATION_ROOT_STATE, MUSIC_RESOURCE) { }
  }
}