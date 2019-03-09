#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UI.Presentation.Players
{
  public enum PlayMode
  {
    Continuous,
    Shuffle
  }

  public enum RepeatMode
  {
    None,
    One,
    All
  }

  /// <summary>
  /// List of media items to be played.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The playlist is responsible to manage the list of media items to be played as well as the current play state of the items.
  /// </para>
  /// <para>
  /// To manage that state, the playlist maintains two lists:
  /// <list>
  /// <item>A list containing <see cref="MediaItem"/> instances (called <strong>Items list</strong>)</item>
  /// <item>A list which contains the play order; this is a list containing indices to the first list (called <strong>Play order index list</strong>)</item>
  /// </list>
  /// The items list simply contains all media items which are in the playlist.
  /// The index list contains all numbers from <c>0</c> to the size of the items list minus one, which are interpreted as indices to the items list.
  /// The play order is determined by that index list; the entries from the index list are read in the order of the index list. To determine the
  /// n-th item to be played, the n-th entry of the index list is taken and used as index in the items list.
  /// Depending on the <see cref="PlayMode"/>, the index list's indices are ordered or shuffled.
  /// </para>
  /// <para>
  /// Furthermore, the playlist maintains a pointer to the <see cref="Current"/> media item, which is maintained as a pointer to the play order
  /// index list. That pointer is automatically updated when the list is changed, for example if the item which is currently played is removed,
  /// the <see cref="Current"/> pointer will point to the next item at once.
  /// </para>
  /// <para>
  /// This component is multithreading safe.
  /// </para>
  /// </remarks>
  public interface IPlaylist
  {
    /// <summary>
    /// Returns the synchronization object to synchronize thread access.
    /// </summary>
    object SyncObj { get; }

    /// <summary>
    /// Gets or sets the play mode for the playlist.
    /// </summary>
    PlayMode PlayMode { get; set; }

    /// <summary>
    /// Gets or sets the repeat mode for the playlist.
    /// </summary>
    RepeatMode RepeatMode { get; set; }

    /// <summary>
    /// Gets a list of all queued media items to play. The order of the returned list is the original playlist
    /// order, not the order in which the items will be played.
    /// </summary>
    IList<MediaItem> ItemList { get; }

    /// <summary>
    /// Returns the number of playable resources. Usually this equals to <see cref="ItemList"/>'s count property.
    /// Exceptions are multi-resource media items that consists of multiple physical sources.
    /// </summary>
    int PlayableItemsCount { get; }

    /// <summary>
    /// Gets or sets the index of the current item in the <see cref="ItemList"/> or <c>-1</c>, if no item
    /// is currently being played.
    /// </summary>
    /// <remarks>
    /// This index is valid in the <see cref="ItemList"/>, which might not represent the play order. The play order is
    /// determined by the current <see cref="RepeatMode"/> and the current <see cref="PlayMode"/>. If the play mode is
    /// <see cref="Players.PlayMode.Shuffle"/> for example, the play order (given by <see cref="Item(int)"/>) is random
    /// while the <see cref="ItemList"/> is still in the order which was built by the playlist build methods.
    /// </remarks>
    int ItemListIndex { get; set; }

    /// <summary>
    /// Gets the currently active media item. This is a convenience property for calling
    /// <see cref="this"/> with a relative index of <c>0</c>.
    /// </summary>
    MediaItem Current { get; }

    /// <summary>
    /// Gets a media item in the play order relative to the current item.
    /// </summary>
    /// <remarks>
    /// The current item can be retrieved with the <paramref name="relativeIndex"/> <c>0</c>, for the next item
    /// use a <paramref name="relativeIndex"/> of <c>1</c>, for the last item use a <paramref name="relativeIndex"/>
    /// of <c>-1</c>, etc.
    /// This property heeds the <see cref="RepeatMode"/> property, i.e. it returns the playlist item which will be
    /// played at the <paramref name="relativeIndex"/>'th position.
    /// </remarks>
    /// <param name="relativeIndex">Index relative to the current item.</param>
    /// <returns>Media item at the specified relative index or <c>null</c>, if there is no media item at the specified
    /// index.</returns>
    MediaItem this[int relativeIndex] { get; }

    /// <summary>
    /// Gets a value indicating whether all items have been played.
    /// </summary>
    /// <value><c>true</c> if all items have been played; otherwise, <c>false</c>.</value>
    bool AllPlayed { get; }

    /// <summary>
    /// Gets the information if this playlist is currently in batch update mode. In batch update mode, the playlist
    /// suppresses change events of type <see cref="PlaylistMessaging.MessageType.PlaylistUpdate"/>.
    /// </summary>
    bool InBatchUpdateMode { get; }

    /// <summary>
    /// Returns the information if we have a previous item.
    /// </summary>
    bool HasPrevious { get; }

    /// <summary>
    /// Returns the information if we have a next item.
    /// </summary>
    bool HasNext { get; }

    /// <summary>
    /// Noves the playlist to the previous media item to be played and returns it.
    /// </summary>
    /// <returns>Media item instance or <c>null</c>, if there are no previous items available (i.e. the playlist is
    /// not started at all or empty or the current item is already the first one).</returns>
    MediaItem MoveAndGetPrevious();

    /// <summary>
    /// Noves the playlist to the next media item to be played and returns it.
    /// </summary>
    /// <returns>Media item instance or <c>null</c>, if there are no more items to be played (i.e. the playlist is
    /// not started at all or empty or has reached its end (<see cref="AllPlayed"/> is <c>true</c>)).</returns>
    MediaItem MoveAndGetNext();

    /// <summary>
    /// Clears the playlist.
    /// </summary>
    void Clear();

    /// <summary>
    /// Adds the specified media item to the playlist.
    /// </summary>
    /// <param name="mediaItem">Media item to add.</param>
    void Add(MediaItem mediaItem);

    /// <summary>
    /// Adds all specified media items to the playlist.
    /// </summary>
    /// <param name="mediaItems">Media items to add.</param>
    void AddAll(IEnumerable<MediaItem> mediaItems);

    /// <summary>
    /// Removes the specified media item from the playlist.
    /// </summary>
    /// <param name="mediaItem">Media item to remove.</param>
    void Remove(MediaItem mediaItem);

    /// <summary>
    /// Removes the media at the specified index item from the playlist.
    /// </summary>
    /// <param name="index">Index of the item to remove, 0-based.</param>
    void RemoveAt(int index);

    /// <summary>
    /// Removes all media items between the playlist indices <paramref name="fromIndex"/> (inclusive)
    /// up to <paramref name="toIndex"/>, exclusive.
    /// </summary>
    /// <param name="fromIndex">Starting index of the range to remove (inclusive).</param>
    /// <param name="toIndex">End index of the range to remove (exclusive).</param>
    void RemoveRange(int fromIndex, int toIndex);

      /// <summary>
    /// Exchanges the media items at the specified indices.
    /// </summary>
    /// <param name="index1">First item index.</param>
    /// <param name="index2">Second item index.</param>
    void Swap(int index1, int index2);

    /// <summary>
    /// Inserts a new media item at the specified index.
    /// </summary>
    /// <param name="mediaItem">The new media item.</param>
    /// <param name="index">Index where the new item should be inserted.</param>
    /// <returns></returns>
    bool Insert(int index, MediaItem mediaItem);

    /// <summary>
    /// Resets the status for all items to not-played.
    /// </summary>
    void ResetStatus();

    /// <summary>
    /// Suppresses change events of type <see cref="PlaylistMessaging.MessageType.PlaylistUpdate"/>
    /// until <see cref="EndBatchUpdate"/> is called.
    /// </summary>
    void StartBatchUpdate();

    /// <summary>
    /// Must be called after a batch update when <see cref="StartBatchUpdate"/> was called. This will re-enable
    /// change events of type <see cref="PlaylistMessaging.MessageType.PlaylistUpdate"/>.
    /// </summary>
    void EndBatchUpdate();

    /// <summary>
    /// Exports the playlist raw data (i.e. the ids of the media items) to the given playlist <paramref name="data"/> structure.
    /// </summary>
    /// <param name="data">Data instance to export this playlist ids to.</param>
    void ExportPlaylistRawData(PlaylistRawData data);

    /// <summary>
    /// Exports the playlist contents to the given playlist <paramref name="data"/> structure.
    /// </summary>
    /// <param name="data">Data instance to export this playlist contents to.</param>
    void ExportPlaylistContents(PlaylistContents data);
  }
}
