#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Attends the CurrentlyPlaying and FullscreenContent states for video players.
  /// </summary>
  public class VideoPlayerModel : BasePlayerModel
  {
    public const string MODEL_ID_STR = "4E2301B4-3C17-4a1d-8DE5-2CEA169A0256";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    protected DateTime _lastVideoInfoDemand = DateTime.MinValue;

    protected AbstractProperty _isOSDVisibleProperty;
    protected AbstractProperty _pipWidthProperty;
    protected AbstractProperty _pipHeightProperty;
    protected AbstractProperty _isPipProperty;

    public VideoPlayerModel() : base(Consts.CURRENTLY_PLAYING_VIDEO_WORKFLOW_STATE_ID, Consts.FULLSCREEN_VIDEO_WORKFLOW_STATE_ID)
    {
      _isOSDVisibleProperty = new WProperty(typeof(bool), false);
      _pipWidthProperty = new WProperty(typeof(float), 0f);
      _pipHeightProperty = new WProperty(typeof(float), 0f);
      _isPipProperty = new WProperty(typeof(bool), false);
      // Don't StartTimer here, since that will be done in method EnterModelContext
    }

    protected override void Update()
    {
      base.Update();
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext secondaryPlayerContext = playerContextManager.GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT);
      IVideoPlayer pipPlayer = secondaryPlayerContext == null ? null : secondaryPlayerContext.CurrentPlayer as IVideoPlayer;
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();

      IsOSDVisible = inputManager.IsMouseUsed || DateTime.Now - _lastVideoInfoDemand < Consts.VIDEO_INFO_TIMEOUT || _inactive;
      IsPip = pipPlayer != null;
      PipHeight = Consts.DEFAULT_PIP_HEIGHT;
      PipWidth = pipPlayer == null ? Consts.DEFAULT_PIP_WIDTH : PipHeight*pipPlayer.VideoAspectRatio.Width/pipPlayer.VideoAspectRatio.Height;
      CurrentPlayerIndex = playerContextManager.CurrentPlayerIndex;
    }

    protected override Type GetPlayerUIContributorType(IPlayer player, MediaWorkflowStateType stateType)
    {
      if (!(player is IVideoPlayer))
        return null;
      if (player is IDVDPlayer)
        return typeof(DVDPlayerUIContributor);
      // else TODO: More specific UI contributor implementations for specific players: Subtitle, ...
      return typeof(DefaultVideoPlayerUIContributor);
    }

    #region Members to be accessed from the GUI

    public AbstractProperty IsOSDVisibleProperty
    {
      get { return _isOSDVisibleProperty; }
    }

    public bool IsOSDVisible
    {
      get { return (bool) _isOSDVisibleProperty.GetValue(); }
      set { _isOSDVisibleProperty.SetValue(value); }
    }

    public AbstractProperty IsPipProperty
    {
      get { return _isPipProperty; }
    }

    public bool IsPip
    {
      get { return (bool) _isPipProperty.GetValue(); }
      set { _isPipProperty.SetValue(value); }
    }

    public AbstractProperty PipWidthProperty
    {
      get { return _pipWidthProperty; }
    }

    public float PipWidth
    {
      get { return (float) _pipWidthProperty.GetValue(); }
      set { _pipWidthProperty.SetValue(value); }
    }

    public AbstractProperty PipHeightProperty
    {
      get { return _pipHeightProperty; }
    }

    public float PipHeight
    {
      get { return (float) _pipHeightProperty.GetValue(); }
      set { _pipHeightProperty.SetValue(value); }
    }

    public IPlayerUIContributor PlayerUIContributor
    {
      get { return _playerUIContributor; }
    }

    public void ShowVideoInfo()
    {
      if (IsOSDVisible)
        // Pressing the info button twice will bring up the context menu
        PlayerConfigurationDialogModel.OpenPlayerConfigurationDialog();
      _lastVideoInfoDemand = DateTime.Now;
      Update();
    }

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    #endregion
  }
}
