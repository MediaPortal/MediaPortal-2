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
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Attends the EditPlaylist workflow state.
  /// </summary>
  public class EditPlaylistModel : BasePlaylistModel, IDisposable, IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "0AFD5E3A-2CB6-44d6-827F-72A7193595E2";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected ItemsList _items = new ItemsList();
    protected int _topIndex = 0;
    protected int _focusedDownButton = -1;
    protected int _focusedUpButton = -1;

    #endregion

    public EditPlaylistModel()
    {
      InitializeMessageQueue();
      // Don't _messageQueue.Start() here, as this will be done in EnterModelContext
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    private void InitializeMessageQueue()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            PlaylistMessaging.CHANNEL,
            PlayerManagerMessaging.CHANNEL,
            PlayerContextManagerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlaylistMessaging.CHANNEL)
      {
        PlaylistMessaging.MessageType messageType = (PlaylistMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlaylistMessaging.MessageType.PlaylistUpdate:
            UpdatePlaylist();
            break;
          case PlaylistMessaging.MessageType.PropertiesChange:
            UpdateProperties();
            break;
        }
      }
      else if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerSlotClosed:
            IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
            ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>() ;
            IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
            if (pc == null && sss.CurrentState == SystemState.Running)
            {
              IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
              workflowManager.NavigatePopToState(Consts.WF_STATE_ID_EDIT_PLAYLIST, true);
            }
            break;
        }
      }
      else if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
      {
        PlayerContextManagerMessaging.MessageType messageType = (PlayerContextManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged:
            UpdatePlaylist();
            break;
        }
      }
    }

    protected override bool MoveItemUp(int index, ListItem item)
    {
      bool result = base.MoveItemUp(index, item);
      if (!result)
        return false;
      lock (_syncObj)
      {
        _focusedDownButton = -1;
        _focusedUpButton = index - 1;
      }
      return true;
    }

    protected override bool MoveItemDown(int index, ListItem item)
    {
      bool result = base.MoveItemDown(index, item);
      if (!result)
        return false;
      lock (_syncObj)
      {
        _focusedDownButton = index + 1;
        _focusedUpButton = -1;
      }
      return true;
    }

    protected void UpdatePlaylist()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      UpdatePlaylistHeader(pc == null ? null : (AVType?) pc.AVType, pc == null ? true : pc.IsPrimaryPlayerContext);
      lock (_syncObj)
      {
        // TODO: If playlist objects differ, leave state EditPlaylist?
        _playlist = playlist;
        _items.Clear();
        if (playlist != null)
        {
          IList<MediaItem> items = playlist.ItemList;
          for (int i = 0; i < items.Count; i++)
          {
            MediaItem mediaItem = items[i];
            PlayableMediaItem item = PlayableMediaItem.CreateItem(mediaItem);

            item.SetLabel(Consts.KEY_NUMBERSTR, (i + 1) + ".");
            item.AdditionalProperties[Consts.KEY_INDEX] = i;

            item.AdditionalProperties[Consts.KEY_IS_DOWN_BUTTON_FOCUSED] = i == _focusedDownButton;
            item.AdditionalProperties[Consts.KEY_IS_UP_BUTTON_FOCUSED] = i == _focusedUpButton;
            _items.Add(item);
          }
        }
        _focusedDownButton = -1;
        _focusedUpButton = -1;
        IsPlaylistEmpty = _items.Count == 0;
        NumPlaylistItemsStr = Utils.BuildNumItemsStr(_items.Count, null);
      }
      _items.FireChange();
    }

    protected void UpdateProperties()
    {
      // TODO: Other properties
    }

    #region Members to be accessed by the GUI

    public ItemsList Items
    {
      get { return _items; }
    }

    public void ClearPlaylist()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      if (playlist == null)
        return;
      playlist.Clear();
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      return pc != null && (pc.AVType == AVType.Audio || pc.AVType == AVType.Video);
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _playlist = null;
      _messageQueue.Start();
      UpdatePlaylist();
      UpdateProperties();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _messageQueue.Shutdown();
      _playlist = null;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _messageQueue.Shutdown();
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _messageQueue.Start();
      UpdatePlaylist();
      UpdateProperties();
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}