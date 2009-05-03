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
using MediaPortal.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
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
    public enum VideoScreenState
    {
      None,
      CurrentlyPlaying,
      FullscreenContent,
    }

    public const string MODEL_ID_STR = "4E2301B4-3C17-4a1d-8DE5-2CEA169A0256";
    public static Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string CURRENTLY_PLAYING_STATE_ID_STR = "5764A810-F298-4a20-BF84-F03D16F775B1";
    public const string FULLSCREEN_CONTENT_STATE_ID_STR = "882C1142-8028-4112-A67D-370E6E483A33";

    public static Guid CURRENTLY_PLAYING_STATE_ID = new Guid(CURRENTLY_PLAYING_STATE_ID_STR);
    public static Guid FULLSCREEN_CONTENT_STATE_ID = new Guid(FULLSCREEN_CONTENT_STATE_ID_STR);

    public const string FULLSCREENVIDEO_SCREEN_NAME = "FullscreenContentVideo";
    public const string CURRENTLY_PLAYING_SCREEN_NAME = "CurrentlyPlayingVideo";

    public const string VIDEOCONTEXTMENU_DIALOG_NAME = "DialogVideoContextMenu";

    protected static TimeSpan VIDEO_INFO_TIMEOUT = new TimeSpan(0, 0, 0, 5);

    protected IPlayerContext _playerContext = null; // Assigned and cleared in workflow model methods
    protected DateTime _lastVideoInfoDemand = DateTime.MinValue;
    protected bool _subscribedToMessages = false;
    protected VideoScreenState _currentScreenState = VideoScreenState.None;


    protected Property _isVideoInfoVisibleProperty;

    public VideoPlayerModel() : base(500)
    {
      _isVideoInfoVisibleProperty = new Property(typeof(bool), false);
      // Don't SubscribeToMessages and StartListening here, that will be done in method EnterModelContext
    }

    protected override void SubscribeToMessages()
    {
      if (_subscribedToMessages)
        return;
      _subscribedToMessages = true;
      base.SubscribeToMessages();
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived += OnPlayerManagerMessageReceived;
    }

    protected override void UnsubscribeFromMessages()
    {
      if (!_subscribedToMessages)
        return;
      _subscribedToMessages = false;
      base.UnsubscribeFromMessages();
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived -= OnPlayerManagerMessageReceived;
    }

    protected void OnPlayerManagerMessageReceived(QueueMessage message)
    {
      PlayerManagerMessaging.MessageType messageType =
          (PlayerManagerMessaging.MessageType) message.MessageData[PlayerManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case PlayerManagerMessaging.MessageType.PlayerSlotDeactivated:
          if (_playerContext != null && !_playerContext.IsValid)
          {
            IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
            // Maybe we should do more handling in this case if we are in FSC state - show dialogs "do you want to delete"
            // etc.? At the moment we'll simply return to the last workflow state.
            workflowManager.NavigatePop(1);
          }
          break;
      }
    }

    protected override void Update()
    {
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      IsVideoInfoVisible = inputManager.IsMouseUsed || DateTime.Now - _lastVideoInfoDemand < VIDEO_INFO_TIMEOUT;
    }

    protected void UpdateScreen()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      switch (_currentScreenState)
      {
        case VideoScreenState.CurrentlyPlaying:
          screenManager.ShowScreen(CURRENTLY_PLAYING_SCREEN_NAME);
          break;
        case VideoScreenState.FullscreenContent:
          screenManager.ShowScreen(FULLSCREENVIDEO_SCREEN_NAME);
          break;
      }
    }

    protected static bool CanHandlePlayer(IPlayer player)
    {
      return player is IVideoPlayer;
    }

    protected void UpdateVideoScreenState(NavigationContext newContext)
    {
      if (newContext.WorkflowState.StateId == CURRENTLY_PLAYING_STATE_ID)
        _currentScreenState = VideoScreenState.CurrentlyPlaying;
      else if (newContext.WorkflowState.StateId == FULLSCREEN_CONTENT_STATE_ID)
        _currentScreenState = VideoScreenState.FullscreenContent;
      else
        _currentScreenState = VideoScreenState.None;
      UpdateScreen();
    }

    #region Members to be accessed from the GUI

    public Property IsVideoInfoVisibleProperty
    {
      get { return _isVideoInfoVisibleProperty; }
    }

    public bool IsVideoInfoVisible
    {
      get { return (bool) _isVideoInfoVisibleProperty.GetValue(); }
      set { _isVideoInfoVisibleProperty.SetValue(value); }
    }

    public void ShowVideoInfo()
    {
      _lastVideoInfoDemand = DateTime.Now;
      if (IsVideoInfoVisible)
      { // Pressing the info button twice will bring up the context menu
        IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
        screenManager.ShowDialog(VIDEOCONTEXTMENU_DIALOG_NAME);
      }
      Update();
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
      IPlayerContext playerContext = _playerContext ?? playerContextManager.CurrentPlayerContext;
      return playerContext != null && CanHandlePlayer(playerContext.CurrentPlayer);
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      _playerContext = playerContextManager.CurrentPlayerContext;
      StartListening();
      SubscribeToMessages();
      UpdateVideoScreenState(newContext);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _playerContext = null;
      StopListening();
      UnsubscribeFromMessages();
      UpdateVideoScreenState(newContext);
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      UpdateVideoScreenState(newContext);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Not implemented
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Not implemented
    }

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowAction> actions)
    {
      // Not implemented yet
    }

    #endregion
  }
}
