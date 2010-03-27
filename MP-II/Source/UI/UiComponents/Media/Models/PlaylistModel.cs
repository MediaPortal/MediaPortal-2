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
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;
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

    public const string AUDIO_PLAYLIST_RES = "[Media.AudioPlaylist]";
    public const string VIDEO_PLAYLIST_RES = "[Media.VideoPlaylist]";
    public const string PIP_PLAYLIST_RES = "[Media.PiPPlaylist]";

    public const string KEY_IS_CURRENT_ITEM = "IsCurrentItem";
    public const string KEY_LENGTH = "Length";
    public const string KEY_INDEX = "Playlist-Index";
    public const string KEY_NUMBERSTR = "NumberStr";

    #endregion

    protected AsynchronousMessageQueue _messageQueue;
    protected readonly object _syncObj = new object();
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
            PlaylistHeader = AUDIO_PLAYLIST_RES;
            break;
          case PlayerContextType.Video:
            PlaylistHeader = pc.PlayerSlotController.SlotIndex == PlayerManagerConsts.PRIMARY_SLOT ?
                VIDEO_PLAYLIST_RES : PIP_PLAYLIST_RES;
            break;
          default:
            // Unknown player context type
            PlaylistHeader = null;
            break;
        }
      }
      else
        playlist = null;
      lock (_syncObj)
      {
        _playlist = playlist;
        int ct = 0;
        _items.Clear();
        int currentItemIdx = playlist.ItemListIndex;
        foreach (MediaItem mediaItem in playlist.ItemList)
        {
          int idx = ct++;
          PlayableItem item = new PlayableItem(mediaItem);
          MediaItemAspect mediaAspect;
          MediaItemAspect audioAspect;
          MediaItemAspect videoAspect;
          if (!mediaItem.Aspects.TryGetValue(MediaAspect.ASPECT_ID, out mediaAspect))
            mediaAspect = null;
          if (!mediaItem.Aspects.TryGetValue(AudioAspect.ASPECT_ID, out audioAspect))
            audioAspect = null;
          if (!mediaItem.Aspects.TryGetValue(VideoAspect.ASPECT_ID, out videoAspect))
            videoAspect = null;
          string title = mediaAspect == null ? null : mediaAspect[MediaAspect.ATTR_TITLE] as string;

          string artists = audioAspect == null ? null : StringUtils.Join(", ", (IEnumerable<string>) audioAspect[AudioAspect.ATTR_ARTISTS]);
          string name = title + (string.IsNullOrEmpty(artists) ? string.Empty : (" (" + artists + ")"));
          long? length = audioAspect == null ? null : (long?) audioAspect[AudioAspect.ATTR_DURATION];
          if (!length.HasValue)
            length = videoAspect == null ? null : (long?) videoAspect[VideoAspect.ATTR_DURATION];

          item.Name = name;
          item.SetLabel(KEY_NUMBERSTR, (idx + 1) + ".");
          item.SetLabel(KEY_LENGTH, length.HasValue ? FormattingUtils.FormatMediaDuration(new TimeSpan(0, 0, 0, (int) length.Value)) : string.Empty);
          item.AdditionalProperties[KEY_INDEX] = idx;
          item.AdditionalProperties[KEY_IS_CURRENT_ITEM] = currentItemIdx == idx;
          _items.Add(item);
        }
        IsPlaylistEmpty = _items.Count == 0;
        NumItemsStr = Utils.Utils.BuildNumItemsStr(_items.Count);
      }
      _items.FireChange();
    }

    protected void UpdateCurrentItem()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
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
        if (isCurrentItem != (currentIsCurrentItem.HasValue ? currentIsCurrentItem.Value : false))
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

    protected void PlayItem(int index)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      lock (_syncObj)
        if (pc == null || pc.Playlist != _playlist)
          return;
      playlist.ItemListIndex = index;
      pc.DoPlay(playlist.Current);
    }

    protected void RemoveItem(int index)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      lock (_syncObj)
        if (pc == null || pc.Playlist != _playlist)
          return;
      playlist.RemoveAt(index);
    }

    protected void MoveItemUp(int index, ListItem item)
    {
      IPlaylist playlist = _playlist;
      if (playlist == null)
        return;
      lock (playlist.SyncObj)
      {
        if (index > 0 && index < playlist.ItemList.Count)
          playlist.Swap(index, index - 1);
      }
    }

    protected void MoveItemDown(int index, ListItem item)
    {
      IPlaylist playlist = _playlist;
      if (playlist == null)
        return;
      lock (playlist.SyncObj)
      {
        if (index >= 0 && index < playlist.ItemList.Count - 1)
          playlist.Swap(index, index + 1);
      }
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

    protected bool TryGetIndex(ListItem item, out int index)
    {
      index = -1;
      if (item == null)
        return false;
      object oIndex;
      if (item.AdditionalProperties.TryGetValue(KEY_INDEX, out oIndex))
      {
        int? i = oIndex as int?;
        if (i.HasValue)
        {
          index = i.Value;
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Provides a callable method for the skin to select an item of the playlist.
    /// The item will be played.
    /// </summary>
    /// <param name="item">The choosen item. This item should be one of the items in the <see cref="Items"/> list.</param>
    public void Select(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        PlayItem(index);
    }

    /// <summary>
    /// Provides a callable method for the skin to remove an item from the playlist.
    /// </summary>
    /// <param name="item">The choosen item. This item should be one of the items in the <see cref="Items"/> list.</param>
    public void Remove(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        RemoveItem(index);
    }

    /// <summary>
    /// Provides a callable method for the skin to move the given playlist <paramref name="item"/> up in the playlist.
    /// </summary>
    /// <param name="item">The choosen item. This item should be one of the items in the <see cref="Items"/> list.</param>
    public void MoveUp(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        MoveItemUp(index, item);
    }

    /// <summary>
    /// Provides a callable method for the skin to move the given playlist <paramref name="item"/> down in the playlist.
    /// </summary>
    /// <param name="item">The choosen item. This item should be one of the items in the <see cref="Items"/> list.</param>
    public void MoveDown(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        MoveItemDown(index, item);
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
