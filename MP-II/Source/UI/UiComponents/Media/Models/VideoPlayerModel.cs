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
using System.Collections.Generic;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using UiComponents.SkinBase.Models;

namespace UiComponents.Media.Models
{
  /// <summary>
  /// Attends the CurrentlyPlaying and FullscreenContent states for video players.
  /// </summary>
  public class VideoPlayerModel : BaseTimerControlledUIModel, IWorkflowModel
  {
    public const string MODEL_ID_STR = "4E2301B4-3C17-4a1d-8DE5-2CEA169A0256";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string CURRENTLY_PLAYING_STATE_ID_STR = "5764A810-F298-4a20-BF84-F03D16F775B1";
    public const string FULLSCREEN_CONTENT_STATE_ID_STR = "882C1142-8028-4112-A67D-370E6E483A33";

    public static readonly Guid CURRENTLY_PLAYING_STATE_ID = new Guid(CURRENTLY_PLAYING_STATE_ID_STR);
    public static readonly Guid FULLSCREEN_CONTENT_STATE_ID = new Guid(FULLSCREEN_CONTENT_STATE_ID_STR);

    public const string FULLSCREENVIDEO_SCREEN_NAME = "FullscreenContentVideo";
    public const string CURRENTLY_PLAYING_SCREEN_NAME = "CurrentlyPlayingVideo";

    public const string VIDEOCONTEXTMENU_DIALOG_NAME = "DialogVideoContextMenu";

    protected static TimeSpan VIDEO_INFO_TIMEOUT = new TimeSpan(0, 0, 0, 5);

    public static float DEFAULT_PIP_HEIGHT = 108;
    public static float DEFAULT_PIP_WIDTH = 192;

    protected DateTime _lastVideoInfoDemand = DateTime.MinValue;
    protected bool _inactive = false;
    protected VideoStateType _currentVideoStateType = VideoStateType.None;

    protected AbstractProperty _isOSDVisibleProperty;
    protected AbstractProperty _pipWidthProperty;
    protected AbstractProperty _pipHeightProperty;
    protected AbstractProperty _isPipProperty;

    public VideoPlayerModel() : base(300)
    {
      _isOSDVisibleProperty = new WProperty(typeof(bool), false);
      _pipWidthProperty = new WProperty(typeof(float), 0f);
      _pipHeightProperty = new WProperty(typeof(float), 0f);
      _isPipProperty = new WProperty(typeof(bool), false);
      // Don't StartTimer here, since that will be done in method EnterModelContext
    }

    protected override void Update()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext secondaryPlayerContext = playerContextManager.GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT);
      IVideoPlayer pipPlayer = secondaryPlayerContext == null ? null : secondaryPlayerContext.CurrentPlayer as IVideoPlayer;
      IInputManager inputManager = ServiceScope.Get<IInputManager>();

      IsOSDVisible = inputManager.IsMouseUsed || DateTime.Now - _lastVideoInfoDemand < VIDEO_INFO_TIMEOUT || _inactive;
      IsPip = pipPlayer != null;
      PipHeight = DEFAULT_PIP_HEIGHT;
      PipWidth = pipPlayer == null ? DEFAULT_PIP_WIDTH : PipHeight*pipPlayer.VideoAspectRatio.Width/pipPlayer.VideoAspectRatio.Height;
    }

    protected static bool CanHandlePlayer(IPlayer player)
    {
      return player is IVideoPlayer;
    }

    protected void UpdateVideoStateType(NavigationContext newContext)
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      if (newContext.WorkflowState.StateId == CURRENTLY_PLAYING_STATE_ID)
      {
        screenManager.BackgroundDisabled = true;
        _currentVideoStateType = VideoStateType.CurrentlyPlaying;
      }
      else if (newContext.WorkflowState.StateId == FULLSCREEN_CONTENT_STATE_ID)
      {
        screenManager.BackgroundDisabled = true;
        _currentVideoStateType = VideoStateType.FullscreenContent;
      }
      else
      {
        screenManager.BackgroundDisabled = false;
        _currentVideoStateType = VideoStateType.None;
      }
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

    public void ShowVideoInfo()
    {
      if (IsOSDVisible)
        // Pressing the info button twice will bring up the context menu
        PlayerConfigurationDialogModel.OpenPlayerConfigurationDialog();
      _lastVideoInfoDemand = DateTime.Now;
      Update();
    }

    public void ShowZoomModeDialog()
    {
      IPlayerContextManager pcm = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = pcm.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      PlayerConfigurationDialogModel.OpenChooseGeometryDialog(pc);
    }

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = null;
      if (newContext.WorkflowState.StateId == CURRENTLY_PLAYING_STATE_ID)
        // The "currently playing" screen is always bound to the "current player"
        pc = playerContextManager.CurrentPlayerContext;
      else if (newContext.WorkflowState.StateId == FULLSCREEN_CONTENT_STATE_ID)
        // The "fullscreen content" screen is always bound to the "primary player"
        pc = playerContextManager.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      return pc != null && CanHandlePlayer(pc.CurrentPlayer);
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      StartTimer(); // Lazily start our timer
      UpdateVideoStateType(newContext);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      StopTimer(); // Reduce workload when none of our states is used
      UpdateVideoStateType(newContext);
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      UpdateVideoStateType(newContext);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _inactive = true;
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _inactive = false;
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      switch (_currentVideoStateType)
      {
        case VideoStateType.CurrentlyPlaying:
          screen = CURRENTLY_PLAYING_SCREEN_NAME;
          break;
        case VideoStateType.FullscreenContent:
          screen = FULLSCREENVIDEO_SCREEN_NAME;
          break;
      }
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
