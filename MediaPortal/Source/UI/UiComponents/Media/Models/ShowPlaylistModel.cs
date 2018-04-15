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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
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
  /// Attends the ShowPlaylist workflow state.
  /// </summary>
  /// <remarks>
  /// We only provide one single playlist workflow state for audio and video/image playlists for development time reasons.
  /// Later, we can split this state up into two different states with two different screens.
  /// </remarks>
  public class ShowPlaylistModel : BasePlaylistModel, IDisposable, IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "E30AA448-C1D1-4d8e-B08F-CF569624B51C";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected ItemsList _playlistItems = new ItemsList();
    protected bool _disableEditMode = false;

    protected ItemsList _playModeItems = new ItemsList();
    protected ItemsList _repeatModeItems = new ItemsList();

    protected AbstractProperty _currentPlayModeProperty = new WProperty(typeof(string), null);
    protected AbstractProperty _currentRepeatModeProperty = new WProperty(typeof(string), null);

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
          case PlayerManagerMessaging.MessageType.PlayerSlotClosed:
            IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
            ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>() ;
            IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
            if (pc == null && sss.CurrentState == SystemState.Running)
            {
              IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
              workflowManager.NavigatePopToState(Consts.WF_STATE_ID_SHOW_PLAYLIST, true);
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
      UpdatePlaylistHeader(pc == null ? null : (AVType?) pc.AVType, pc == null ? true : pc.IsPrimaryPlayerContext);
      lock (_syncObj)
      {
        _playlist = playlist;
        _playlistItems.Clear();
        if (playlist != null)
        {
          int ct = 0;
          int currentItemIdx = playlist.ItemListIndex;
          foreach (MediaItem mediaItem in playlist.ItemList)
          {
            int idx = ct++;
            PlayableMediaItem item = PlayableMediaItem.CreateItem(mediaItem);

            item.SetLabel(Consts.KEY_NUMBERSTR, (idx + 1) + ".");
            item.AdditionalProperties[Consts.KEY_INDEX] = idx;
            item.AdditionalProperties[Consts.KEY_IS_CURRENT_ITEM] = currentItemIdx == idx;
            _playlistItems.Add(item);
          }
        }
        IsPlaylistEmpty = _playlistItems.Count == 0;
        NumPlaylistItemsStr = Utils.BuildNumItemsStr(_playlistItems.Count, null);
      }
      _playlistItems.FireChange();
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
      foreach (PlayableMediaItem item in _playlistItems)
      {
        bool isCurrentItem = idx-- == 0;
        bool? currentIsCurrentItem = (bool?) item.AdditionalProperties[Consts.KEY_IS_CURRENT_ITEM];
        if (isCurrentItem != (currentIsCurrentItem ?? false))
        {
          item.AdditionalProperties[Consts.KEY_IS_CURRENT_ITEM] = isCurrentItem;
          item.FireChange();
        }
      }
    }

    protected void UpdateProperties()
    {
      CurrentPlayMode = LocalizationHelper.Translate(Consts.RES_PLAYMODE_SUFFIX + _playlist.PlayMode + Consts.RES_PLAYMODE_PREFIX);
      CurrentRepeatMode = LocalizationHelper.Translate(Consts.RES_REPEATMODE_SUFFIX + _playlist.RepeatMode + Consts.RES_REPEATMODE_PREFIX);
    }

    protected void UpdateSubMenuLists()
    {
      UpdateRepeatModes();
      UpdatePlayModes();
    }

    protected void UpdateRepeatModes()
    {
      _repeatModeItems = new ItemsList();
      ListItem noneItem = new ListItem(Consts.KEY_NAME, Consts.RES_REPEATMODE_NONE)
        {
            Command = new MethodDelegateCommand(() => SetRepeatMode(RepeatMode.None))
        };
      noneItem.AdditionalProperties[Consts.KEY_REPEATMODE] = RepeatMode.None;
      _repeatModeItems.Add(noneItem);

      ListItem allItem = new ListItem(Consts.KEY_NAME, Consts.RES_REPEATMODE_ALL)
        {
            Command = new MethodDelegateCommand(() => SetRepeatMode(RepeatMode.All))
        };
      allItem.AdditionalProperties[Consts.KEY_REPEATMODE] = RepeatMode.All;
      _repeatModeItems.Add(allItem);

      ListItem oneItem = new ListItem(Consts.KEY_NAME, Consts.RES_REPEATMODE_ONE)
        {
            Command = new MethodDelegateCommand(() => SetRepeatMode(RepeatMode.One))
        };
      oneItem.AdditionalProperties[Consts.KEY_REPEATMODE] = RepeatMode.One;
      _repeatModeItems.Add(oneItem);
    }

    protected void UpdatePlayModes()
    {
      _playModeItems = new ItemsList();
      ListItem continuousItem = new ListItem(Consts.KEY_NAME, Consts.RES_PLAYMODE_CONTINUOUS)
        {
            Command = new MethodDelegateCommand(() => SetPlayMode(PlayMode.Continuous))
        };
      continuousItem.AdditionalProperties[Consts.KEY_PLAYMODE] = PlayMode.Continuous;
      _playModeItems.Add(continuousItem);

      ListItem shuffleItem = new ListItem(Consts.KEY_NAME, Consts.RES_PLAYMODE_SHUFFLE)
        {
            Command = new MethodDelegateCommand(() => SetPlayMode(PlayMode.Shuffle))
        };
      shuffleItem.AdditionalProperties[Consts.KEY_PLAYMODE] = PlayMode.Shuffle;
      _playModeItems.Add(shuffleItem);
    }

    protected void SetRepeatMode(RepeatMode mode)
    {
      IPlaylist playlist = _playlist;
      if (playlist == null)
        return;
      playlist.RepeatMode = mode;
      UpdateProperties();
    }

    protected void SetPlayMode(PlayMode mode)
    {
      IPlaylist playlist = _playlist;
      if (playlist == null)
        return;
      playlist.PlayMode = mode;
      UpdateProperties();
    }

    #region Static members to be called from other parts of the system

    /// <summary>
    /// Shows a dialog with all items of the current playlist.
    /// </summary>
    /// <param name="disableEditMode">If this parameter is set to <c>true</c>, the button to enter the edit
    /// playlist mode won't be available.</param>
    public static void ShowPlaylist(bool disableEditMode)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_SHOW_PLAYLIST, new NavigationContextConfig
        {
            AdditionalContextVariables = new Dictionary<string, object>
              {
                  {Consts.KEY_DISABLE_EDIT_MODE, disableEditMode}
              }
        });
    }

    #endregion

    #region Members to be accessed by the GUI

    public ItemsList PlaylistItems
    {
      get { return _playlistItems; }
    }

    public bool DisableEditMode
    {
      get { return _disableEditMode; }
    }

    public AbstractProperty CurrentPlayModeProperty
    {
      get { return _currentPlayModeProperty; }
    }

    public string CurrentPlayMode
    {
      get { return (string) _currentPlayModeProperty.GetValue(); }
      set { _currentPlayModeProperty.SetValue(value); }
    }

    public AbstractProperty CurrentRepeatModeProperty
    {
      get { return _currentRepeatModeProperty; }
    }

    public string CurrentRepeatMode
    {
      get { return (string) _currentRepeatModeProperty.GetValue(); }
      set { _currentRepeatModeProperty.SetValue(value); }
    }

    public ItemsList PlayModeItems
    {
      get { return _playModeItems; }
    }

    public ItemsList RepeatModeItems
    {
      get { return _repeatModeItems; }
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
      bool? disableEditMode = (bool?) newContext.GetContextVariable(Consts.KEY_DISABLE_EDIT_MODE, false);
      _disableEditMode = disableEditMode.HasValue && disableEditMode.Value;
      UpdatePlaylist();
      UpdateProperties();
      UpdateSubMenuLists();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _messageQueue.Shutdown();
      _playlist = null;
      _repeatModeItems = null;
      _playModeItems = null;
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
