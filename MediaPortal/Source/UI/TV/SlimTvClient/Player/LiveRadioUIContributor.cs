#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  /// <summary>
  /// Class to tell the player model what screen to put up
  /// </summary>
  public class LiveRadioUIContributor : BaseVideoPlayerUIContributor
  {
    private LiveRadioPlayer _player;

    public const string SCREEN_FULLSCREEN_RADIO = "FullscreenContentRadio";
    public const string SCREEN_FULLSCREEN_RADIO_RECORDING = "FullscreenContentRadioRecording";
    public const string SCREEN_CURRENTLY_PLAYING_RADIO = "CurrentlyPlayingRadio";

    public override bool BackgroundDisabled
    {
      get { return true; }
    }

    public override string Screen
    {
      get
      {
        if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
          return SCREEN_CURRENTLY_PLAYING_RADIO;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return _player == null || _player.IsLiveRadio ? SCREEN_FULLSCREEN_RADIO : SCREEN_FULLSCREEN_RADIO_RECORDING;
        return null;
      }
    }

    public override void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      base.Initialize(stateType, player);
      _player = player as LiveRadioPlayer;
    }

  }
}
