#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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

using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.Models;

namespace Cinema.Player
{
  public class CinemaUiContributor : BaseVideoPlayerUIContributor
  {
    public const string SCREEN_FULLSCREEN_VIDEO = "CinemaFullscreenContent";
    public const string SCREEN_CURRENTLY_PLAYING_VIDEO = "CinemaCurrentlyPlaying";

    public override bool BackgroundDisabled
    {
      get { return true; }
    }

    public override string Screen
    {
      get
      {
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return SCREEN_FULLSCREEN_VIDEO;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
          return SCREEN_CURRENTLY_PLAYING_VIDEO;
        return null;
      }
    }
  }
}