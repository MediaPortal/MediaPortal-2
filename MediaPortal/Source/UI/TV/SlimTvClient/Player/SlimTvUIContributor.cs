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
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  public class SlimTvUIContributor : BaseVideoPlayerUIContributor
  {
    private readonly AbstractProperty _isZappingProperty;
    private LiveTvPlayer _player;

    public const string SCREEN_FULLSCREEN_TV = "FullscreenContentTv";
    public const string SCREEN_CURRENTLY_PLAYING_TV = "CurrentlyPlayingTv";

    public override bool BackgroundDisabled
    {
      get { return false; }
    }

    public AbstractProperty IsZappingProperty
    {
      get { return _isZappingProperty; }
    }

    public bool IsZapping
    {
      get { return (bool)_isZappingProperty.GetValue(); }
      set { _isZappingProperty.SetValue(value); }
    }

    public override string Screen
    {
      get
      {
        if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
          return SCREEN_CURRENTLY_PLAYING_TV;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return SCREEN_FULLSCREEN_TV;
        return null;
      }
    }

    public SlimTvUIContributor()
    {
      _isZappingProperty = new WProperty(typeof(bool), false);
    }

    public override void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      base.Initialize(stateType, player);
      _player = player as LiveTvPlayer;
      if (_player != null)
      {
        _player.OnBeginZap += OnBeginZap;
        _player.OnEndZap += OnEndZap;
      }
    }

    private void OnBeginZap(object sender, EventArgs e)
    {
      IsZapping = true;
    }

    private void OnEndZap(object sender, EventArgs e)
    {
      IsZapping = false;
    }

    public override void Dispose()
    {
      base.Dispose();
      if (_player != null)
      {
        _player.OnBeginZap -= OnBeginZap;
        _player.OnEndZap -= OnEndZap;
      }
    }
  }
}