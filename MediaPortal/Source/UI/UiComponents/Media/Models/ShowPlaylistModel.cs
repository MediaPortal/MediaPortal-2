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
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Runtime;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Models.Navigation;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Attends the ShowPlaylist workflow state.
  /// </summary>
  /// <remarks>
  /// We only provide one single playlist workflow state for audio and video playlists for development time reasons.
  /// Later, we can split this state up into two different states with two different screens.
  /// </remarks>
  public class ShowPlaylistModel : BasePlaylistModel, IDisposable, IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "E30AA448-C1D1-4d8e-B08F-CF569624B51C";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string SHOW_PLAYLIST_WORKFLOW_STATE_ID_STR = "95E38A80-234C-4494-9F7A-006D8E4D6FDA";
    public static readonly Guid SHOW_PLAYLIST_WORKFLOW_STATE_ID = new Guid(SHOW_PLAYLIST_WORKFLOW_STATE_ID_STR);

    public const string KEY_IS_CURRENT_ITEM = "IsCurrentItem";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected ItemsList _items = new ItemsList();

    #endregion

    public ShowPlaylistModel()
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
          case PlaylistMessaging.MessageType.CurrentItemChange:
            UpdateCurrentItem();
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
          case PlayerManagerMessaging.MessageType.PlayerSlotDeactivated:
            IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
            ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>() ;
            IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
            if (pc == null && sss.CurrentState == SystemState.Running)
            {
              IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
              workflowManager.NavigatePopToState(SHOW_PLAYLIST_WORKFLOW_STATE_ID, true);
            }
            break;
          // We don't need to track the PlayerSlotActivated or PlayerSlotsChanged messages, because all information
          // we need is contained in the CurrentPlayer information
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

    protected void UpdatePlaylist()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      UpdatePlaylistHeader(pc == null ? null : (AVType?) pc.AVType,
          pc == null ? PlayerManagerConsts.PRIMARY_SLOT : pc.PlayerSlotController.SlotIndex);
      lock (_syncObj)
      {
        _playlist = playlist;
        _items.Clear();
        if (playlist != null)
        {
          int ct = 0;
          int currentItemIdx = playlist.ItemListIndex;
          foreach (MediaItem mediaItem in playlist.ItemList)
          {
            int idx = ct++;
            PlayableItem item = new PlayableItem(mediaItem);

            item.SetLabel(KEY_NUMBERSTR, (idx + 1) + ".");
            item.AdditionalProperties[KEY_INDEX] = idx;
            item.AdditionalProperties[KEY_IS_CURRENT_ITEM] = currentItemIdx == idx;
            _items.Add(item);
          }
        }
        IsPlaylistEmpty = _items.Count == 0;
        NumItemsStr = General.Utils.BuildNumItemsStr(_items.Count, null);
      }
      _items.FireChange();
    }

    protected void UpdateCurrentItem()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      lock (_syncObj)
        if (playlist == null || playlist != _playlist)
          return;
      int idx = playlist.ItemListIndex;
      foreach (PlayableItem item in _items)
      {
        bool isCurrentItem = idx-- == 0;
        bool? currentIsCurrentItem = (bool?) item.AdditionalProperties[KEY_IS_CURRENT_ITEM];
        if (isCurrentItem != (currentIsCurrentItem ?? false))
        {
          item.AdditionalProperties[KEY_IS_CURRENT_ITEM] = isCurrentItem;
          item.FireChange();
        }
      }
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

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
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
