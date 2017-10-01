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
using System.Collections.Generic;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.SkinBase.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Attends the CurrentlyPlaying and FullscreenContent states for video players.
  /// Contains the UI contributor and general properties about OSD.
  /// </summary>
  /// <remarks>
  /// <seealso cref="IPlayerUIContributor"/>
  /// </remarks>
  public class VideoPlayerModel : BasePlayerModel
  {
    public const string MODEL_ID_STR = "4E2301B4-3C17-4a1d-8DE5-2CEA169A0256";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    private bool _isPlayerConfigOpen;
    private DateTime _lastVideoInfoDemand = DateTime.MinValue;
    private bool _isOsdOpenOnDemand;

    protected AbstractProperty _isOSDVisibleProperty;
    protected AbstractProperty _isPipProperty;

    public VideoPlayerModel() : base(Consts.WF_STATE_ID_CURRENTLY_PLAYING_VIDEO, Consts.WF_STATE_ID_FULLSCREEN_VIDEO)
    {
      _isOSDVisibleProperty = new WProperty(typeof(bool), false);
      _isPipProperty = new WProperty(typeof(bool), false);
      
      SubscribeToMessages();

      // Don't StartTimer here, since that will be done in method EnterModelContext
    }

    protected override void Update()
    {
      // base.Update is abstract
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext secondaryPlayerContext = playerContextManager.SecondaryPlayerContext;
      IVideoPlayer pipPlayer = secondaryPlayerContext == null ? null : secondaryPlayerContext.CurrentPlayer as IVideoPlayer;
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();

      if (!_isOsdOpenOnDemand && !_isPlayerConfigOpen)
      {
        if (DateTime.Now - _lastVideoInfoDemand > DateTime.Now.AddSeconds(5) - DateTime.Now)
        {
          IsOSDVisible = inputManager.IsMouseUsed;
        }
      }

      IsPip = pipPlayer != null;
    }

    private void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
      {
        WorkflowManagerMessaging.CHANNEL,
        PlayerManagerMessaging.CHANNEL,
        PlayerContextManagerMessaging.CHANNEL,
      });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        if ((WorkflowManagerMessaging.MessageType)message.MessageType == WorkflowManagerMessaging.MessageType.StatePushed)
        {
          bool isPlayerConfigDialog = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext.WorkflowState.StateId.ToString().Equals("D0B79345-69DF-4870-B80E-39050434C8B3", StringComparison.OrdinalIgnoreCase);
          if (isPlayerConfigDialog)
          {
            _isPlayerConfigOpen = true;
            IsOSDVisible = false;
          }
        }

        if ((WorkflowManagerMessaging.MessageType)message.MessageType == WorkflowManagerMessaging.MessageType.StatesPopped)
        {
          ICollection<Guid> statesRemoved = new List<Guid>(((IDictionary<Guid, NavigationContext>)message.MessageData[WorkflowManagerMessaging.CONTEXTS]).Keys);
          if (statesRemoved.Contains(new Guid("D0B79345-69DF-4870-B80E-39050434C8B3")))
          {
            _isPlayerConfigOpen = false;
            _lastVideoInfoDemand = DateTime.Now;
            _isOsdOpenOnDemand = false;
          }
        }
      }
    }

    protected override Type GetPlayerUIContributorType(IPlayer player, MediaWorkflowStateType stateType)
    {
      // First check if the player provides an own UI contributor.
      IUIContributorPlayer uicPlayer = player as IUIContributorPlayer;
      if (uicPlayer != null)
        return uicPlayer.UIContributorType;

      // Return the more specific player types first
      if (player is IImagePlayer)
        return typeof(ImagePlayerUIContributor);

      if (player is IDVDPlayer)
        return typeof(DVDVideoPlayerUIContributor);

      if ((player is IVideoPlayer))
        return typeof(DefaultVideoPlayerUIContributor);

      return null;
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

    public void ShowVideoInfo()
    {
      if (IsOSDVisible)
      {
        MediaModelSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MediaModelSettings>();
        if (settings.OpenPlayerConfigInOsd)
        {
          PlayerConfigurationDialogModel.OpenPlayerConfigurationDialog();
        }
        else
        {
          IsOSDVisible = !IsOSDVisible;
          _lastVideoInfoDemand = DateTime.Now;
        }
      }
      else
      {
        IsOSDVisible = !IsOSDVisible;
      }
      _isOsdOpenOnDemand = IsOSDVisible;
    }


      _lastVideoInfoDemand = DateTime.Now;
      _isOsdOpenOnDemand = false;

    public void OpenPlayerConfigurationDialog()
    {
      PlayerConfigurationDialogModel.OpenPlayerConfigurationDialog();
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
