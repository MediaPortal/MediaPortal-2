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
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using UiComponents.Media.Navigation;

namespace UiComponents.Media.Models
{
  /// <summary>
  /// Attends the Playlist state.
  /// </summary>
  /// <remarks>
  /// We only provide one single playlist workflow state for audio and video playlists for development time reasons.
  /// Later, we can split this state up in two different states with two different screens.
  /// </remarks>
  public class PlaylistModel : IDisposable, IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "E30AA448-C1D1-4d8e-B08F-CF569624B51C";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string SHOW_AUDIO_PLAYLIST_RES = "[Media.ShowAudioPlaylist]";
    public const string SHOW_VIDEO_PLAYLIST_RES = "[Media.ShowVideoPlaylist]";
    public const string SHOW_PIP_PLAYLIST_RES = "[Media.ShowPiPPlaylist]";

    public const string IS_CURRENT_ITEM_KEY = "CurrentItem";

    #endregion

    protected AsynchronousMessageQueue _messageQueue;
    protected ItemsList _items = new ItemsList();
    protected IPlaylist _playlist = null;
    protected AbstractProperty _playlistHeaderProperty;
    protected AbstractProperty _numItemsStrProperty;
    protected AbstractProperty _isPlaylistEmptyProperty;

    public PlaylistModel()
    {
      InitializeMessageQueue();
      _playlistHeaderProperty = new WProperty(typeof(string), null);
      _numItemsStrProperty = new WProperty(typeof(string), null);
      _isPlaylistEmptyProperty = new WProperty(typeof(bool), false);
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
          case PlaylistMessaging.MessageType.PlaylistAdvance:
            UpdateCurrentItem();
            break;
          case PlaylistMessaging.MessageType.PropertiesChanged:
            UpdateProperties();
            break;
        }
      }
      else if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerSlotsChanged:
            // We don't need to track PlayerSlotActivated and PlayerSlotDeactivated messages, because all information
            // we need is contained in the CurrentPlayer information
            UpdatePlaylist();
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

    protected void UpdatePlaylist()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist;
      if (pc != null)
      {
        playlist = pc.Playlist;
        switch (pc.MediaType)
        {
          case PlayerContextType.Audio:
            PlaylistHeader = SHOW_AUDIO_PLAYLIST_RES;
            break;
          case PlayerContextType.Video:
            PlaylistHeader = pc.PlayerSlotController.SlotIndex == PlayerManagerConsts.PRIMARY_SLOT ?
                SHOW_VIDEO_PLAYLIST_RES : SHOW_PIP_PLAYLIST_RES;
            break;
          default:
            // Unknown player context type
            PlaylistHeader = null;
            break;
        }
      }
      else
        playlist = null;
      if (playlist != _playlist)
      {
        int ct = 0;
        foreach (MediaItem mediaItem in _playlist.ItemList)
        {
          int idx = ct++;
          PlayableItem item = new PlayableItem(mediaItem)
            {
                Command = new MethodDelegateCommand(() => Select(idx))
            };
          _items.Add(item);
        }
        IsPlaylistEmpty = _items.Count == 0;
        NumItemsStr = Utils.Utils.BuildNumItemsStr(_items.Count);
        _items.FireChange();
      }
      UpdateCurrentItem();
    }

    protected void UpdateCurrentItem()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      if (playlist == null)
        return;
      int idx = playlist.ItemListIndex;
      foreach (PlayableItem item in _items)
      {
        bool isCurrentItem = idx-- == 0;
        if (((bool) item.AdditionalProperties[IS_CURRENT_ITEM_KEY]) != isCurrentItem)
        {
          item.AdditionalProperties[IS_CURRENT_ITEM_KEY] = isCurrentItem;
          item.FireChange();
        }
      }
    }

    protected void UpdateProperties()
    {
      // TODO: Other properties
    }

    #region Members to be accessed from the GUI

    public ItemsList Items
    {
      get { return _items; }
    }

    public AbstractProperty PlaylistHeaderProperty
    {
      get { return _playlistHeaderProperty; }
    }

    public string PlaylistHeader
    {
      get { return (string) _playlistHeaderProperty.GetValue(); }
      internal set { _playlistHeaderProperty.SetValue(value); }
    }

    public AbstractProperty NumItemsStrProperty
    {
      get { return _numItemsStrProperty; }
    }

    public string NumItemsStr
    {
      get { return (string) _numItemsStrProperty.GetValue(); }
      internal set { _numItemsStrProperty.SetValue(value); }
    }

    public AbstractProperty IsPlaylistEmptyProperty
    {
      get { return _isPlaylistEmptyProperty; }
    }

    public bool IsPlaylistEmpty
    {
      get { return (bool) _isPlaylistEmptyProperty.GetValue(); }
      internal set { _isPlaylistEmptyProperty.SetValue(value); }
    }

    public void Select(int index)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      if (pc == null || pc.Playlist != _playlist)
        return;
      playlist.ItemListIndex = index;
      pc.DoPlay(playlist.Current);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      return pc.MediaType == PlayerContextType.Audio || pc.MediaType == PlayerContextType.Video;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _messageQueue.Start();
      UpdatePlaylist();
      UpdateProperties();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _messageQueue.Shutdown();
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
