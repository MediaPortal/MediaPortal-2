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
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Geometries;

namespace MediaPortal.UI.Presentation.Players
{
  public enum AVType
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

  public enum PlaybackState
  {
    Playing,
    Paused,
    Seeking,
    Ended,
    Idle,
  }

  /// <summary>
  /// High-level facade to a <see cref="IPlayerSlotController"/>, describing a typed "place" where a player can run.
  /// A player context belongs to a specified media module, defined by its <see cref="MediaModuleId"/>.
  /// The player context can contain a typed playlist (A/V/AV) which will automatically be advanced.
  /// </summary>
  /// <remarks>
  /// This component is multithreading safe.
  /// </remarks>
  public interface IPlayerContext
  {
    /// <summary>
    /// Returns the information if this player context is still connected to a player slot. If <see cref="IsActive"/> is
    /// <c>false</c>, the player context was closed and cannot be used any more. Especially the underlaying data
    /// structure is not accessible any more in inactive player contexts, so the playlist and all context variables
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
    ///   [Evaluation of IsActive property]
    ///   ...
    /// }
    /// </code>
    /// </remarks>
    bool IsActive { get; }

    /// <summary>
    /// Returns the id of the module which belongs to this player context.
    /// </summary>
    Guid MediaModuleId { get; }

    /// <summary>
    /// Returns the type of this player context. The type determines if this context plays audio by default and
    /// which underlaying player slot will be used. A video player will preferably be located in the primary
    /// player slot.
    /// The type is also used to find conflicts (A-A, V-V).
    /// </summary>
    AVType AVType { get; }

    /// <summary>
    /// Returns the information if the player in this player context is the current player. The current player is the
    /// player which is controlled by the play controls on the remote.
    /// </summary>
    bool IsCurrentPlayerContext { get; }

    /// <summary>
    /// Returns the information if this player context is the primary player context. The primary player context contains the
    /// fullscreen player, the secondary player context contains the PiP player.
    /// </summary>
    bool IsPrimaryPlayerContext { get; }

    /// <summary>
    /// Returns the playlist of this player context. The playlist is always not-null.
    /// </summary>
    IPlaylist Playlist { get; }

    /// <summary>
    /// Returns the media item which is currently played.
    /// </summary>
    MediaItem CurrentMediaItem { get; }

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
    PlaybackState PlaybackState { get; }

    /// <summary>
    /// Gets the name of this player context. This value might be a localized value.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns the id of a workflow state which provides the "currently playing" functionality for this player context.
    /// </summary>
    Guid CurrentlyPlayingWorkflowStateId { get; }

    /// <summary>
    /// Returns the id of a workflow state which provides the "fullscreen content" functionality for this player context.
    /// </summary>
    Guid FullscreenContentWorkflowStateId { get; }

    /// <summary>
    /// Plays the specified media item without putting it into the playlist.
    /// </summary>
    /// <param name="item">Media item to play.</param>
    /// <returns><c>true</c>, if the specified item could be played, else <c>false</c>.</returns>
    bool DoPlay(MediaItem item);

    /// <summary>
    /// Returns all audio stream descriptors for this player context.
    /// </summary>
    /// <param name="currentAudioStream">Descriptor for the current audio stream. This will also be filled if
    /// we are in muted mode. If there is no current audio stream set, <c>null</c> will be returned.</param>
    /// <returns>Collection of audio stream descriptors.</returns>
    ICollection<AudioStreamDescriptor> GetAudioStreamDescriptors(out AudioStreamDescriptor currentAudioStream);

    /// <summary>
    /// Sets a special <paramref name="geometry"/> for the <see cref="CurrentPlayer"/>, if it is a video player.
    /// The geometry will only be applied to the current player. It will be lost when the current player is disposed.
    /// </summary>
    /// <remarks>
    /// This method takes care of notifying all video output modules of the geometry change.
    /// </remarks>
    /// <param name="geometry">The geometry to be used with the <see cref="CurrentPlayer"/>.</param>
    void OverrideGeometry(IGeometry geometry);

    /// <summary>
    /// Sets a special <paramref name="effect"/> for the <see cref="CurrentPlayer"/>, if it is a compatible player.
    /// The effect will only be applied to the current player. It will be lost when the current player is disposed.
    /// </summary>
    /// <param name="effect">The effect to be used with the <see cref="CurrentPlayer"/>.</param>
    void OverrideEffect(string effect);

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

    /// <summary>
    /// Closes this player context.
    /// </summary>
    void Close();

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
    /// Starts seeking forward or doubles the play rate, if we are already seeking forward. If we are already seeking
    /// in the other direction, the seek direction will be inverted. If the player is currently in paused state,
    /// the playback rate will be set to 0.5.
    /// </summary>
    void SeekForward();

    /// <summary>
    /// Starts seeking backward or doubles the play rate, if we are already seeking backward. If we are already seeking
    /// in the other direction, the seek direction will be inverted. If the player is currently in paused state,
    /// the playback rate will be set to -0.5.
    /// </summary>
    void SeekBackward();

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

    /// <summary>
    /// InstantSkipForward immediately skips the playback position forward/backward by a specific percentual value of the total playback duration.
    /// </summary>
    /// <param name="percent">Skip percent (+/- supported).</param>
    void InstantSkip(int percent);

    /// <summary>
    /// Checks the player if the requested skip can be done. 
    /// For positive TimeSpans it's checked if the current playback position plus the <paramref name="skipDuration"/> is less than the total duration.
    /// For negative TimeSpans it's checked if the current playback position plus the <paramref name="skipDuration"/> is greater than zero.
    /// </summary>
    /// <param name="skipDuration">Duration to skip starting at the current position.</param>
    /// <returns>True if supported.</returns>
    bool CanSkipRelative(TimeSpan skipDuration);

    /// <summary>
    /// Skips the playback for a given timespan. The <see cref="skipDuration"/> can be positive or negative related
    /// to the current playback position.
    /// </summary>
    /// <param name="skipDuration">Skips the given duration starting at the current position. The skip might be done asynchronous,
    /// so the given duration is not met exactly.</param>
    void SkipRelative(TimeSpan skipDuration);
    
    /// <summary>
    /// Skips to the start.
    /// </summary>
    void SkipToStart();

    /// <summary>
    /// Skips to the end.
    /// </summary>
    void SkipToEnd();

    /// <summary>
    /// Decouples this player context from the underlaying player slot controller.
    /// This can be necessary to be triggered manually when the underlaying player slot controller should be reused for another usage
    /// and the automatic player context close functions like <see cref="CloseWhenFinished"/> should be disabled.
    /// </summary>
    /// <returns>Underlaying player slot controller. That slot controller can be reused after this player context has rewoked the usage
    /// of that player slot controller.</returns>
    IPlayerSlotController Revoke();
  }
}
