#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Globalization;
using MediaPortal.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.Workflow;

namespace Ui.Players.Video
{
  /// <summary>
  /// Attends the CurrentlyPlaying and FullscreenContent states for the video player.
  /// </summary>
  public class VideoPlayerModel : BaseTimerControlledUIModel, IWorkflowModel
  {
    public const string MODEL_ID_STR = "4E2301B4-3C17-4a1d-8DE5-2CEA169A0256";
    public static Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string CURRENTLY_PLAYING_STATE_ID_STR = "5764A810-F298-4a20-BF84-F03D16F775B1";
    public const string FULLSCREEN_CONTENT_STATE_ID_STR = "882C1142-8028-4112-A67D-370E6E483A33";
    public const string PLAYER_CONFIGURATION_DIALOG_STATE_ID = "D0B79345-69DF-4870-B80E-39050434C8B3";

    public static Guid CURRENTLY_PLAYING_STATE_ID = new Guid(CURRENTLY_PLAYING_STATE_ID_STR);
    public static Guid FULLSCREEN_CONTENT_STATE_ID = new Guid(FULLSCREEN_CONTENT_STATE_ID_STR);
    public static Guid PLAYER_CONFIGURATION_DIALOG_STATE = new Guid(PLAYER_CONFIGURATION_DIALOG_STATE_ID);

    public const string FULLSCREENVIDEO_SCREEN_NAME = "FullscreenContentVideo";
    public const string CURRENTLY_PLAYING_SCREEN_NAME = "CurrentlyPlayingVideo";

    public const string VIDEOCONTEXTMENU_DIALOG_NAME = "DialogVideoContextMenu";

    public const string NO_VIDEO_RESOURCE = "[VideoPlayer.NoVideo]";

    protected static TimeSpan VIDEO_INFO_TIMEOUT = new TimeSpan(0, 0, 0, 5);

    public static float DEFAULT_PIP_HEIGHT = 108;
    public static float DEFAULT_PIP_WIDTH = 192;

    protected DateTime _lastVideoInfoDemand = DateTime.MinValue;
    protected bool _inactive = false;
    protected VideoStateType _currentVideoStateType = VideoStateType.None;

    protected Property _isOSDVisibleProperty;
    protected Property _isPrimaryPlayerActiveProperty;
    protected Property _isSecondaryPlayerActiveProperty;
    protected Property _primaryPercentPlayedProperty;
    protected Property _primaryCurrentTimeProperty;
    protected Property _primaryDurationProperty;
    protected Property _primaryPlayerStateTextProperty;
    protected Property _primaryVideoNameProperty;
    protected Property _secondaryMediaNameProperty;
    protected Property _pipWidthProperty;
    protected Property _pipHeightProperty;
    protected Property _osdPipWidthProperty;
    protected Property _osdPipHeightProperty;
    protected Property _isPipProperty;

    public VideoPlayerModel() : base(300)
    {
      _isOSDVisibleProperty = new Property(typeof(bool), false);
      _isPrimaryPlayerActiveProperty = new Property(typeof(bool), false);
      _isSecondaryPlayerActiveProperty = new Property(typeof(bool), false);
      _primaryPercentPlayedProperty = new Property(typeof(float), 0f);
      _primaryCurrentTimeProperty = new Property(typeof(string), string.Empty);
      _primaryDurationProperty = new Property(typeof(string), string.Empty);
      _primaryPlayerStateTextProperty = new Property(typeof(string), string.Empty);
      _primaryVideoNameProperty = new Property(typeof(string), string.Empty);
      _secondaryMediaNameProperty = new Property(typeof(string), string.Empty);
      _pipWidthProperty = new Property(typeof(float), 0f);
      _pipHeightProperty = new Property(typeof(float), 0f);
      _osdPipWidthProperty = new Property(typeof(float), 0f);
      _osdPipHeightProperty = new Property(typeof(float), 0f);
      _isPipProperty = new Property(typeof(bool), false);
      // Don't StartListening here, since that will be done in method EnterModelContext
    }

    protected override void Update()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext primaryPlayerContext = playerContextManager.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      IPlayerContext secondaryPlayerContext = playerContextManager.GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT);
      IVideoPlayer primaryPlayer = primaryPlayerContext == null ? null : primaryPlayerContext.CurrentPlayer as IVideoPlayer;
      IVideoPlayer secondaryPlayer = secondaryPlayerContext == null ? null : secondaryPlayerContext.CurrentPlayer as IVideoPlayer;
      IVideoPlayer pipPlayer = secondaryPlayerContext == null ? null : secondaryPlayerContext.CurrentPlayer as IVideoPlayer;
      IInputManager inputManager = ServiceScope.Get<IInputManager>();

      IsOSDVisible = inputManager.IsMouseUsed || DateTime.Now - _lastVideoInfoDemand < VIDEO_INFO_TIMEOUT || _inactive;
      IsPrimaryPlayerActive = primaryPlayer != null;
      IsSecondaryPlayerActive = secondaryPlayer != null;
      if (primaryPlayer == null)
      {
        PrimaryPercentPlayed = 0f;
        PrimaryCurrentTime = string.Empty;
        PrimaryDuration = string.Empty;
        PrimaryPlayerStateText = string.Empty;
        PrimaryVideoName = NO_VIDEO_RESOURCE;
      }
      else
      {
        TimeSpan currentTime = primaryPlayer.CurrentTime;
        TimeSpan duration = primaryPlayer.Duration;
        if (duration.TotalMilliseconds == 0)
        {
          PrimaryPercentPlayed = 0;
          PrimaryCurrentTime = string.Empty;
          PrimaryDuration = string.Empty;
        }
        else
        {
          ILocalization localization = ServiceScope.Get<ILocalization>();
          CultureInfo culture = localization.CurrentCulture;
          PrimaryPercentPlayed = (float) (100*currentTime.TotalMilliseconds/duration.TotalMilliseconds);
          PrimaryCurrentTime = new DateTime().Add(currentTime).ToString("T", culture);
          PrimaryDuration = new DateTime().Add(duration).ToString("T", culture);
        }
        PrimaryPlayerStateText = primaryPlayer.State.ToString();
        PrimaryVideoName = primaryPlayer.MediaItemTitle;
      }
      SecondaryMediaName = secondaryPlayer == null ? string.Empty : secondaryPlayer.MediaItemTitle;
      IsPip = pipPlayer != null;
      PipHeight = DEFAULT_PIP_HEIGHT;
      PipWidth = pipPlayer == null ? DEFAULT_PIP_WIDTH :
          PipHeight*pipPlayer.VideoAspectRatio.Width/pipPlayer.VideoAspectRatio.Height;
      OsdPipHeight = PipHeight;
      OsdPipWidth = PipWidth;
    }

    protected void UpdateScreen()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      switch (_currentVideoStateType)
      {
        case VideoStateType.CurrentlyPlaying:
          screenManager.ShowScreen(CURRENTLY_PLAYING_SCREEN_NAME);
          break;
        case VideoStateType.FullscreenContent:
          screenManager.ShowScreen(FULLSCREENVIDEO_SCREEN_NAME);
          break;
      }
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
      UpdateScreen();
    }

    #region Members to be accessed from the GUI

    public Property IsOSDVisibleProperty
    {
      get { return _isOSDVisibleProperty; }
    }

    public bool IsOSDVisible
    {
      get { return (bool) _isOSDVisibleProperty.GetValue(); }
      set { _isOSDVisibleProperty.SetValue(value); }
    }

    public Property IsPrimaryPlayerActiveProperty
    {
      get { return _isPrimaryPlayerActiveProperty; }
    }

    public bool IsPrimaryPlayerActive
    {
      get { return (bool) _isPrimaryPlayerActiveProperty.GetValue(); }
      set { _isPrimaryPlayerActiveProperty.SetValue(value); }
    }

    public Property IsSecondaryPlayerActiveProperty
    {
      get { return _isSecondaryPlayerActiveProperty; }
    }

    public bool IsSecondaryPlayerActive
    {
      get { return (bool) _isSecondaryPlayerActiveProperty.GetValue(); }
      set { _isSecondaryPlayerActiveProperty.SetValue(value); }
    }

    public Property PrimaryPercentPlayedProperty
    {
      get { return _primaryPercentPlayedProperty; }
    }

    public float PrimaryPercentPlayed
    {
      get { return (float) _primaryPercentPlayedProperty.GetValue(); }
      set { _primaryPercentPlayedProperty.SetValue(value); }
    }

    public Property PrimaryCurrentTimeProperty
    {
      get { return _primaryCurrentTimeProperty; }
    }

    public string PrimaryCurrentTime
    {
      get { return (string) _primaryCurrentTimeProperty.GetValue(); }
      set { _primaryCurrentTimeProperty.SetValue(value); }
    }

    public Property PrimaryDurationProperty
    {
      get { return _primaryDurationProperty; }
    }

    public string PrimaryDuration
    {
      get { return (string) _primaryDurationProperty.GetValue(); }
      set { _primaryDurationProperty.SetValue(value); }
    }

    public Property PrimaryPlayerStateTextProperty
    {
      get { return _primaryPlayerStateTextProperty; }
    }

    public string PrimaryPlayerStateText
    {
      get { return (string) _primaryPlayerStateTextProperty.GetValue(); }
      set { _primaryPlayerStateTextProperty.SetValue(value); }
    }

    public Property PrimaryVideoNameProperty
    {
      get { return _primaryVideoNameProperty; }
    }

    public string PrimaryVideoName
    {
      get { return (string) _primaryVideoNameProperty.GetValue(); }
      set { _primaryVideoNameProperty.SetValue(value); }
    }

    public Property SecondaryMediaNameProperty
    {
      get { return _secondaryMediaNameProperty; }
    }

    public string SecondaryMediaName
    {
      get { return (string) _secondaryMediaNameProperty.GetValue(); }
      set { _secondaryMediaNameProperty.SetValue(value); }
    }

    public Property IsPipProperty
    {
      get { return _isPipProperty; }
    }

    public bool IsPip
    {
      get { return (bool) _isPipProperty.GetValue(); }
      set { _isPipProperty.SetValue(value); }
    }

    public Property PipWidthProperty
    {
      get { return _pipWidthProperty; }
    }

    public float PipWidth
    {
      get { return (float) _pipWidthProperty.GetValue(); }
      set { _pipWidthProperty.SetValue(value); }
    }

    public Property PipHeightProperty
    {
      get { return _pipHeightProperty; }
    }

    public float PipHeight
    {
      get { return (float) _pipHeightProperty.GetValue(); }
      set { _pipHeightProperty.SetValue(value); }
    }

    public Property OsdPipWidthProperty
    {
      get { return _osdPipWidthProperty; }
    }

    public float OsdPipWidth
    {
      get { return (float) _osdPipWidthProperty.GetValue(); }
      set { _osdPipWidthProperty.SetValue(value); }
    }

    public Property OsdPipHeightProperty
    {
      get { return _osdPipHeightProperty; }
    }

    public float OsdPipHeight
    {
      get { return (float) _osdPipHeightProperty.GetValue(); }
      set { _osdPipHeightProperty.SetValue(value); }
    }

    public void ShowVideoInfo()
    {
      if (IsOSDVisible)
      { // Pressing the info button twice will bring up the context menu
        IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
        workflowManager.NavigatePush(PLAYER_CONFIGURATION_DIALOG_STATE);
      }
      _lastVideoInfoDemand = DateTime.Now;
      Update();
    }

    /// <summary>
    /// To be called from the FSC state.
    /// </summary>
    public void SwitchPip()
    {
      if (_currentVideoStateType != VideoStateType.FullscreenContent)
        return;
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.SwitchSlots();
      // The workflow state will be changed to the new primary player's FSC- or CP-state automatically by the PCM,
      // if necessary
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
      if (newContext.WorkflowState.StateId == CURRENTLY_PLAYING_STATE_ID)
      {
        IPlayerContext pc = playerContextManager.CurrentPlayerContext;
        // The "currently playing" screen is always bound to the "current player"
        if (pc == null)
          return false;
        return CanHandlePlayer(pc.CurrentPlayer);
      }
      else if (newContext.WorkflowState.StateId == FULLSCREEN_CONTENT_STATE_ID)
      {
        // The "fullscreen content" screen is always bound to the "primary player"
        IPlayerContext playerContext = playerContextManager.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
        return playerContext != null && CanHandlePlayer(playerContext.CurrentPlayer);
      }
      else
        return false;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      StartListening(); // Lazily start our timer
      UpdateVideoStateType(newContext);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      StopListening(); // Reduce workload when none of our states is used
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

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowAction> actions)
    {
      // Nothing to do
    }

    #endregion
  }
}
