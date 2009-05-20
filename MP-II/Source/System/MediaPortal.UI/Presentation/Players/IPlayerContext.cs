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
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Presentation.Players
{
  public enum PlayerContextType
  {
    /// <summary>
    /// No media type.
    /// </summary>
    None,

    /// <summary>
    /// Pure-audio type.
    /// </summary>
    Audio,

    /// <summary>
    /// A/V type (video or image).
    /// </summary>
    Video,
  }

  /// <summary>
  /// High-level descriptor, describing a typed "place" where a player can run.
  /// The player context can contain a typed playlist (A/V/AV) which will automatically be advanced.
  /// </summary>
  /// <remarks>
  /// This component is multithreading safe.
  /// </remarks>
  public interface IPlayerContext
  {
    /// <summary>
    /// Returns the information if this player context is still connected to a player slot. If <see cref="IsValid"/> is
    /// <c>false</c>, the player context was closed and cannot be used any more. Especially the underlaying data
    /// structure is not accessible any more in invalid player contexts, so the playlist and all context variables
    /// are gone then.
    /// </summary>
    /// <remarks>
    /// The return value of this property only has relevance when evaluated while holding the player manager's lock.<br/>
    /// So to read the value of this property, it is necessary to execute this sequence:
    /// <code>
    /// IPlayerManager pm = ...;
    /// lock (pm.SyncObj)
    /// {
    ///   ...
    ///   [Evaluation of IsValid property]
    ///   ...
    /// }
    /// </code>
    /// </remarks>
    bool IsValid { get; }

    /// <summary>
    /// Returns the type of this player context. The type determines if this context plays audio by default and
    /// which underlaying player slot will be used. A video player will preferably be located in the primary
    /// player slot.
    /// The type is also used to find conflicts (A-A, V-V).
    /// </summary>
    PlayerContextType MediaType { get; }

    /// <summary>
    /// Returns the playlist of this player context.
    /// </summary>
    IPlaylist Playlist { get; }

    /// <summary>
    /// Configures this player context to be automatically closed when the player is stopped or has ended.
    /// This will typically be used when playing videos, which run without a playlist.
    /// </summary>
    bool CloseWhenFinished { get; set; }

    /// <summary>
    /// Returns the player which is currently playing content for this player context. Can be <c>null</c> if
    /// the player slot is stopped.
    /// </summary>
    IPlayer CurrentPlayer { get; }

    /// <summary>
    /// Returns the underlaying player slot controller.
    /// </summary>
    IPlayerSlotController PlayerSlotController { get; }

    /// <summary>
    /// Returns the playback state of the <see cref="CurrentPlayer"/>.
    /// </summary>
    PlaybackState PlayerState { get; }

    /// <summary>
    /// Gets the name of this player context. This value might be a localized value.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns the id of the "currently playing" workflow state for the current player of this player context,
    /// if present.
    /// </summary>
    Guid? CurrentlyPlayingWorkflowStateId { get; }

    /// <summary>
    /// Plays the specified media item without putting it into the playlist.
    /// </summary>
    /// <param name="item">Media item to play.</param>
    /// <returns><c>true</c>, if the specified item could be played, else <c>false</c>.</returns>
    bool DoPlay(MediaItem item);

    /// <summary>
    /// Plays the specified media resource without putting it into the playlist.
    /// </summary>
    /// <param name="locator">Media item locator to the media resource.</param>
    /// <param name="mimeType">Mime type of the media resource, if known. If this parameter is given, the
    /// decision whether the media resource can be played might be faster. If this parameter is set to <c>null</c>,
    /// this method will potentially need more time to look into the given resource's content.</param>
    /// <param name="mediaItemTitle">The title of the media item. This value is necessary for some players,
    /// which don't extract metadata from the media file themselves.</param>
    /// <returns><c>true</c>, if the specified item could be played, else <c>false</c>.</returns>
    bool DoPlay(IMediaItemLocator locator, string mimeType, string mediaItemTitle);

    /// <summary>
    /// Returns all audio stream descriptors for this player context.
    /// </summary>
    /// <returns>Enumeration of audio stream descriptors.</returns>
    IEnumerable<AudioStreamDescriptor> GetAudioStreamDescriptors();

    /// <summary>
    /// Sets a user-defined context variable in this player context.
    /// </summary>
    /// <param name="key">The key to access the variable.</param>
    /// <param name="value">The value to set.</param>
    void SetContextVariable(string key, object value);

    /// <summary>
    /// Removes the context variable with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to access the variable.</param>
    void ResetContextVariable(string key);

    /// <summary>
    /// Returns the context variable with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to access the variable.</param>
    /// <returns>Context variable for the specified <paramref name="key"/> or <c>null</c>, if the variable was not
    /// set before.</returns>
    object GetContextVariable(string key);

    // Fullscreen content workflow state can only be shown by the PlayerContextManager, because the FSC screen can only
    // be shown for the primary player

    /// <summary>
    /// Stops playback of the current player.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses playback of the current player.
    /// </summary>
    void Pause();

    /// <summary>
    /// Starts or resumes playback of the current player.
    /// </summary>
    void Play();

    /// <summary>
    /// If playing, this method will pause the current player, else it will make it play or replay the current item.
    /// </summary>
    void TogglePlayPause();

    /// <summary>
    /// Restarts playback of the item in the current player.
    /// </summary>
    void Restart();

    /// <summary>
    /// Plays the previous item from the playlist.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the previous item could be started, else <c>false</c>.
    /// </returns>
    bool PreviousItem();

    /// <summary>
    /// Plays the next item from the playlist.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the next item could be started, else <c>false</c>.
    /// </returns>
    bool NextItem();
  }
}
