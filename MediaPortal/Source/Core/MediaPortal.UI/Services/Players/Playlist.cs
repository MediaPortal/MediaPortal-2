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
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.Services.Players
{
  public class Playlist : IPlaylist
  {
    protected static readonly Random rnd = new Random();
    protected readonly object _syncObj = new object();
    protected readonly IPlayerContext _playerContext;
    protected readonly IList<MediaItem> _itemList = new List<MediaItem>();
    protected PlayMode _playMode = PlayMode.Continuous;
    protected RepeatMode _repeatMode = RepeatMode.None;
    protected IList<int> _playIndexList = null; // Index on _itemList, lazy initialized before playing
    protected int _currentPlayIndex = -1; // Index for the _playItemList
    protected int _inBatchUpdate = 0; // 0 ^= no batch update, >0 ^= in batch update mode

    public Playlist(IPlayerContext context)
    {
      _playerContext = context;
    }

    protected IList<int> BuildPlayIndexList()
    {
      lock (_syncObj)
      {
        IList<int> result = new List<int>(_itemList.Count);
        for (int i = 0; i < _itemList.Count; i++)
          result.Add(i);
        if (_playMode == PlayMode.Shuffle)
        {
          for (int i = 0; i < _itemList.Count; i++)
          {
            int swapTarget = rnd.Next(_itemList.Count);
            int tmp = result[i];
            result[i] = result[swapTarget];
            result[swapTarget] = tmp;
          }
        }
        return result;
      }
    }

    protected int GetItemListIndex(int relativeIndex)
    {
      lock (_syncObj)
      {
        if (_currentPlayIndex == -1)
          return -1;
        if (_playIndexList == null)
          _playIndexList = BuildPlayIndexList();
        if (_playIndexList.Count == 0)
          return -1;
        if (_repeatMode == RepeatMode.One)
          return _playIndexList[_currentPlayIndex];
        int playIndex = _currentPlayIndex + relativeIndex;
        if (playIndex < 0 || playIndex >= _playIndexList.Count)
        {
          if (_repeatMode == RepeatMode.None)
            return -1;
          while (playIndex >= _playIndexList.Count)
            playIndex -= _playIndexList.Count;
          while (playIndex < 0)
            playIndex += _playIndexList.Count;
        }
        int index = _playIndexList[playIndex];
        if (index < 0 || index >= _itemList.Count)
          // Should never happen if we didn't break our index list
          return -1;
        return index;
      }
    }

    protected void SetCurrentPlayIndexByItemIndex(int currentItemIndex)
    {
      lock (_syncObj)
        if (currentItemIndex == -1)
          _currentPlayIndex = -1;
        else
        { // Find current played item in shuffled index list
          if (_playIndexList == null)
            _playIndexList = BuildPlayIndexList();
          foreach (int i in _playIndexList)
            if (_playIndexList[i] == currentItemIndex)
            {
              _currentPlayIndex = i;
              break;
            }
        }
      if (!InBatchUpdateMode)
        PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.CurrentItemChange, _playerContext);
    }

    #region IPlaylist implementation

    public object SyncObj
    {
      get { return _syncObj; }
    }

    public PlayMode PlayMode
    {
      get { return _playMode; }
      set
      {
        lock (_syncObj)
        {
          if (_playMode == value)
            return;
          _playMode = value;
          PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.PropertiesChange, _playerContext);
          if (_playIndexList == null)
            return;
          int currentItemIndex = _currentPlayIndex > -1 ? _playIndexList[_currentPlayIndex] : -1;
          _playIndexList = BuildPlayIndexList();
          SetCurrentPlayIndexByItemIndex(currentItemIndex);
        }
      }
    }

    public RepeatMode RepeatMode
    {
      get { return _repeatMode; }
      set
      {
        lock (_syncObj)
          _repeatMode = value;
        PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.PropertiesChange, _playerContext);
      }
    }

    public IList<MediaItem> ItemList
    {
      get { return _itemList; }
    }

    public int PlayableItemsCount
    {
      get { return _itemList.Sum(mi => mi.MaximumResourceLocatorIndex + 1); }
    }

    public int ItemListIndex
    {
      get { return GetItemListIndex(0); }
      set { SetCurrentPlayIndexByItemIndex(value); }
    }

    public MediaItem Current
    {
      get { return this[0]; }
    }

    public bool HasPrevious
    {
      get
      {
        lock (_syncObj)
        {
          if (_repeatMode == RepeatMode.One)
            return _currentPlayIndex > -1;
          return _currentPlayIndex > 0 || _repeatMode == RepeatMode.All || HasPreviousResourceIndex;
        }
      }
    }

    public bool HasNext
    {
      get
      {
        lock (_syncObj)
        {
          if (_repeatMode == RepeatMode.One)
            return _currentPlayIndex > -1;
          return _currentPlayIndex < _itemList.Count - 1 || _repeatMode == RepeatMode.All || HasNextResourceIndex;
        }
      }
    }

    public MediaItem this[int relativeIndex]
    {
      get
      {
        lock (_syncObj)
        {
          int index = GetItemListIndex(relativeIndex);
          return index == -1 ? null : _itemList[index];
        }
      }
    }

    public bool AllPlayed
    {
      get
      {
        lock (_syncObj)
          return _currentPlayIndex >= _itemList.Count && IsLastResourceIndex;
      }
    }

    public bool InBatchUpdateMode
    {
      get
      {
        lock (_syncObj)
          return _inBatchUpdate > 0;
      }
    }

    public MediaItem MoveAndGetPrevious()
    {
      lock (_syncObj)
      {
        if (_repeatMode == RepeatMode.One)
          return Current;
        // Skip back for multi-resource media items
        if (HasPreviousResourceIndex)
        {
          Current.ActiveResourceLocatorIndex--;
          return Current;
        }
        int oldPlayIndex = _currentPlayIndex;
        if (_currentPlayIndex > -1)
          _currentPlayIndex--;
        if (_currentPlayIndex == -1 && _repeatMode == RepeatMode.All)
          _currentPlayIndex = _itemList.Count - 1;
        if (_currentPlayIndex != oldPlayIndex)
          PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.CurrentItemChange, _playerContext);
        return Current;
      }
    }

    public MediaItem MoveAndGetNext()
    {
      lock (_syncObj)
      {
        if (_repeatMode == RepeatMode.One)
          return Current;
        // Skip forward for multi-resource media items
        if (HasNextResourceIndex)
        {
          Current.ActiveResourceLocatorIndex++;
          return Current;
        }
        if (_currentPlayIndex < _itemList.Count)
          _currentPlayIndex++;
        if (AllPlayed && _repeatMode == RepeatMode.All)
          _currentPlayIndex = 0;
        PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.CurrentItemChange, _playerContext);
        return Current;
      }
    }

    public void Clear()
    {
      lock (_syncObj)
      {
        _itemList.Clear();
        _playIndexList = null;
        _currentPlayIndex = -1;
        if (!InBatchUpdateMode)
          PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.PlaylistUpdate, _playerContext);
      }
    }

    public void Add(MediaItem mediaItem)
    {
      lock (_syncObj)
        Insert(_playMode == PlayMode.Shuffle ? rnd.Next(_itemList.Count) : _itemList.Count, mediaItem);
    }

    public void AddAll(IEnumerable<MediaItem> mediaItems)
    {
      StartBatchUpdate();
      int selectionIndex = -1;
      lock (_syncObj)
      {
        foreach (MediaItem mediaItem in mediaItems)
        {
          Add(mediaItem);
          if (CheckAndRemoveSelection(mediaItem))
            selectionIndex = _itemList.IndexOf(mediaItem) - 1;
        }
      }
      ItemListIndex = selectionIndex;
      EndBatchUpdate();
    }

    private bool HasPreviousResourceIndex
    {
      get { return Current != null && Current.ActiveResourceLocatorIndex > 0; }
    }

    private bool HasNextResourceIndex
    {
      get { return Current != null && Current.ActiveResourceLocatorIndex < Current.MaximumResourceLocatorIndex; }
    }

    private bool IsLastResourceIndex
    {
      get { return Current == null || Current.ActiveResourceLocatorIndex == Current.MaximumResourceLocatorIndex; }
    }

    /// <summary>
    /// Checks if the given <paramref name="mediaItem"/> contains the <see cref="SelectionMarkerAspect"/>.
    /// If so it removes it and returns <c>true</c>, otherwise <c>false</c>.
    /// </summary>
    /// <param name="mediaItem">MediaItem</param>
    /// <returns><c>true</c> if SelectionMarkerAspect was present</returns>
    private bool CheckAndRemoveSelection(MediaItem mediaItem)
    {
      if (!mediaItem.Aspects.ContainsKey(SelectionMarkerAspect.ASPECT_ID))
        return false;
      mediaItem.Aspects.Remove(SelectionMarkerAspect.ASPECT_ID);
      return true;
    }

    public void Remove(MediaItem mediaItem)
    {
      lock (_syncObj)
      {
        int index = _itemList.IndexOf(mediaItem);
        RemoveAt(index);
      }
    }

    public void RemoveAt(int index)
    {
      RemoveRange(index, index+1);
    }

    public void RemoveRange(int fromIndex, int toIndex)
    {
      lock (_syncObj)
      {
        if (fromIndex < 0)
          fromIndex = 0;
        if (toIndex > _itemList.Count)
          toIndex = _itemList.Count;
        if (toIndex <= fromIndex)
          return;
        for (int i = toIndex - 1; i >= fromIndex; i--)
          _itemList.RemoveAt(i);
        if (!InBatchUpdateMode)
          PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.PlaylistUpdate, _playerContext);
        if (_playIndexList == null)
          return;
        // Adapt play index list
        int removeCount = toIndex - fromIndex;
        for (int i = _playIndexList.Count - 1; i >= 0; i--)
        {
          int playIndex = _playIndexList[i];
          if (playIndex < fromIndex)
            continue;
          if (playIndex < toIndex)
            _playIndexList.RemoveAt(i);
          else
            _playIndexList[i] -= removeCount;
        }
        // Fix current play index
        if (_currentPlayIndex >= _playIndexList.Count)
          _currentPlayIndex = 0;
      }
    }

    public void Swap(int index1, int index2)
    {
      lock (_syncObj)
      {
        if (index1 == index2)
          return;
        if (index1 < 0 || index1 >= _itemList.Count || index2 < 0 || index2 >= _itemList.Count)
          return;
        CollectionUtils.Swap(_itemList, index1, index2);
        if (!InBatchUpdateMode)
          PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.PlaylistUpdate, _playerContext);
        if (_playIndexList == null)
          // If index list isn't yet initialized, we don't need to adapt the play index positions
          return;
        // Adapt play index list
        int[] piIndices = new int[2];
        int numFound = 0;
        for (int i = 0; i < _playIndexList.Count; i++)
        {
          int tmpIndex = _playIndexList[i];
          if (tmpIndex == index1 || tmpIndex == index2)
            piIndices[numFound++] = i;
          if (numFound == 2)
            break;
        }
        if (numFound != 2)
        { // Playlist and index list are out-of-sync. This should never happen...
          _playIndexList = BuildPlayIndexList();
          _currentPlayIndex = -1;
          return;
        }
        int tmp = _playIndexList[piIndices[0]];
        _playIndexList[piIndices[0]] = _playIndexList[piIndices[1]];
        _playIndexList[piIndices[1]] = tmp;
        // We don't need to update the _currentPlayIndex because the change only happens on the underlaying data
      }
    }

    public bool Insert(int index, MediaItem mediaItem)
    {
      lock (_syncObj)
      {
        if (index < 0 || index > _itemList.Count)
          return false;
        _itemList.Insert(index, mediaItem);
        if (!InBatchUpdateMode)
          PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.PlaylistUpdate, _playerContext);
        if (_playIndexList == null)
          return true;
        // Adapt play index list...
        // ... patch old play indices
        for (int i = 0; i < _playIndexList.Count; i++)
          if (_playIndexList[i] >= index)
            _playIndexList[i] += 1;
        // ... and add new item
        if (_playMode == PlayMode.Shuffle)
        {
          // Shuffle mode: insert an index entry for the new item at a random position
          int insertPos = rnd.Next(_itemList.Count - 1);
          _playIndexList.Insert(insertPos, index);
          if (_currentPlayIndex > -1 && insertPos <= _currentPlayIndex)
            _currentPlayIndex++;
        }
        else
          // Continuous mode: Simply add an index entry at the end of the index list
          _playIndexList.Add(index);
        return true;
      }
    }

    public void ResetStatus()
    {
      lock (_syncObj)
      {
        _currentPlayIndex = -1;
        _playIndexList = null;
        PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.PropertiesChange, _playerContext);
        PlaylistMessaging.SendPlaylistMessage(PlaylistMessaging.MessageType.CurrentItemChange, _playerContext);
      }
    }

    public void StartBatchUpdate()
    {
      lock (_syncObj)
        _inBatchUpdate ++;
    }

    public void EndBatchUpdate()
    {
      lock (_syncObj)
        if (InBatchUpdateMode)
          _inBatchUpdate--;
        else
          throw new IllegalCallException("The playlist is currently not in batch update mode");
    }

    public void ExportPlaylistRawData(PlaylistRawData data)
    {
      IList<Guid> idList = data.MediaItemIds;
      idList.Clear();
      lock (_syncObj)
        CollectionUtils.AddAll(idList, _itemList.Select(item => item.MediaItemId));
    }

    public void ExportPlaylistContents(PlaylistContents data)
    {
      IList<MediaItem> items = data.ItemList;
      items.Clear();
      lock (_syncObj)
        CollectionUtils.AddAll(items, _itemList);
    }

    #endregion
  }
}
