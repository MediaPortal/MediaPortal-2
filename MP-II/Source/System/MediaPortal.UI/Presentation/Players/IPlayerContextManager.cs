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
  /// <summary>
  /// For each video player, there need to be present two special states to present its video content:
  /// The currently playing state and the fullscreen content state.
  /// </summary>
  public enum VideoStateType
  {
    /// <summary>
    /// No special video screen.
    /// </summary>
    None,

    /// <summary>
    /// Indicates the currently playing state. The screen to this state shows detailed information
    /// about the current played media item, the current playing state and presents additional actions
    /// for the current played media.
    /// </summary>
    /// <remarks>
    /// The currently playing screen can be shown for both the primary and the secondary player.
    /// </remarks>
    CurrentlyPlaying,

    /// <summary>
    /// Indicates the fullscreen content state. The screen to this state shows the video fullscreen
    /// with additional onscreen display, info and/or actions to be taken by the user.
    /// </summary>
    /// <remarks>
    /// The fullscreen content screen can only be shown for the "primary" player.
    /// </remarks>
    FullscreenContent,
  }

  /// <summary>
  /// Management service for active players and their integration into the UI.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Media modules (like Music, Video, Picture, TV, Radio) can initiate and access player contexts,
  /// which are specially created for them. The player context manager service manages all those player contexts,
  /// tracks their player state and manages UI workflow states relating to active players.
  /// The separation of player context initiator (= media plugin) and player context manager
  /// (= <see cref="IPlayerContextManager"/>) is necessary, because players also need to run while the
  /// media modules possibly don't have the active control over them.
  /// The typical separation of roles is like this:
  /// The media module initiates one or more player contexts. It doesn't need to track its player contexts, because
  /// it is always possible to find the player contexts of the media module again.
  /// The player context manager will track current media workflow states to switch between states, if necessary
  /// (for example when the primary and secondary players are switched, the fullscreen content workflow state needs
  /// to be exchanged). It tracks player context activity states, takes care of automatic playlist advance and closes
  /// player contexts automatically, if necessary.
  /// Media modules should use this service interface to manage their player contexts instead of using the
  /// basic <see cref="IPlayerManager"/> API itself.
  /// </para>
  /// <para>
  /// <b>Functionality:</b><br/>
  /// While the <see cref="IPlayerManager"/> deals with primary and secondary player slots, this service
  /// provides a more abstract view for the client, it deals with typed player contexts and playlists.
  /// The functionality of this component is comprehensive, it deals with the collectivity of all players, while the
  /// <see cref="IPlayerManager"/>'s functionality is mostly focused to single technical player slots.
  /// This service manages and solves player conflicts (like two audio players at the same time) automatically by
  /// simply closing an old player when a new conflicting player is opened. Non-conflicting players can be played
  /// concurrently.
  /// The technical target player slot (primary/secondary) of a given <see cref="IPlayerContext"/>
  /// is managed almost transparently for the client. There is a rare number of cases where the client needs to cope
  /// with the set-up of primary and secondary players, for example when two video players are running, one of them as
  /// PIP player. In that situation, it can be necessary to explicitly exchange the player slots.
  /// </para>
  /// <para>
  /// <b>Playlists</b><br/>
  /// This service also provides playlist management, i.e. it manages automatic playlist advance, and provides methods
  /// to control the current player like <see cref="Stop"/>, <see cref="Pause"/> etc.
  /// </para>
  /// <para>
  /// <b>Thread-Safety:</b><br/>
  /// This class can be called from multiple threads. It synchronizes thread access to its fields via the
  /// <see cref="IPlayerManager.SyncObj"/> instance, which is also exposed by the <see cref="SyncObj"/> property for
  /// convenience. Player context manager messages are sent asynchronously to clients via the
  /// message queue <see cref="PlayerContextManagerMessaging.QUEUE"/>.
  /// </para>
  /// </remarks>
  public interface IPlayerContextManager
  {
    /// <summary>
    /// Returns the player manager's synchronization object to synchronize thread access on this instance.
    /// This is a convenience property for getting the player manager's synchronization object.
    /// </summary>
    object SyncObj { get; }

    /// <summary>
    /// Returns the information if there is already an audio player active.
    /// </summary>
    bool IsAudioPlayerActive { get; }

    /// <summary>
    /// Returns the information if there is already a video (V or AV) player active.
    /// </summary>
    bool IsVideoPlayerActive { get; }

    /// <summary>
    /// Returns the information if a secondary player is running in PIP mode.
    /// </summary>
    bool IsPipActive { get; }

    /// <summary>
    /// Gets or sets the index of the current player slot. The current player is the player which has the
    /// "user focus", i.e. it receives all commands from the remote or from other play controls and it will be shown
    /// in the "currently playing" screen.
    /// </summary>
    int CurrentPlayerIndex { get; set; }

    /// <summary>
    /// Convenience property for calling <see cref="GetPlayerContext"/> with the <see cref="CurrentPlayerIndex"/>.
    /// </summary>
    IPlayerContext CurrentPlayerContext { get; }

    /// <summary>
    /// Returns the number of active player contexts.
    /// </summary>
    int NumActivePlayerContexts { get; }

    /// <summary>
    /// Shuts the function of this service down. This is necessary before the player manager gets closed.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Returns the player context object which is assigned to the player manager's slot with the specified
    /// <paramref name="slotIndex"/>.
    /// </summary>
    /// <param name="slotIndex">Index of the player manager's slot, whose corresponding player context should
    /// be retrieved.</param>
    /// <returns>Player context instance or <c>null</c>, if the specified slot isn't active at the moment or
    /// has no player context assigned.</returns>
    IPlayerContext GetPlayerContext(int slotIndex);

    /// <summary>
    /// Returns the number of player contexts playing the specified <paramref name="mediaType"/>.
    /// </summary>
    /// <param name="mediaType">Type of the player contexts to search.</param>
    /// <returns>Number of player contexts, will be in the range 0-2.</returns>
    int NumPlayerContextsOfMediaType(PlayerContextType mediaType);

    /// <summary>
    /// Opens an audio player context. This will replace a running audio player context, if present. If a video player is
    /// active, it depends on the parameter <paramref name="concurrent"/> whether the video player context will be
    /// deactivated or not.
    /// </summary>
    /// <remarks>
    /// The returned player context off course does not play yet; its playlist first has to be filled. For a streaming
    /// player, the playlist will typically be filled with one single URL entry, while for resource based players, the
    /// playlist typically will contain multiple entries.
    /// After the playlist was filled, the player context has to be started.
    /// </remarks>
    /// <param name="mediaModuleId">Id of the requesting media module. The new player context will be sticked to the specified
    /// module.</param>
    /// <param name="name">A name for the new player context. The name will be shown in each skin control which
    /// represents the player context.</param>
    /// <param name="concurrent">If set to <c>true</c>, an already active video player will continue to play muted.
    /// If set to <c>false</c>, an active video player context will be deactivated.</param>
    /// <param name="currentlyPlayingWorkflowStateId">The id of the workflow state to be used as currently playing
    /// workflow state for the new player context.</param>
    /// <param name="fullscreenContentWorkflowStateId">The id of the workflow state to be used as fullscreen content
    /// workflow state for the new player context.</param>
    /// <returns>Descriptor object for the new audio player context. The returned player context will be installed
    /// into the system but is not playing yet.</returns>
    IPlayerContext OpenAudioPlayerContext(Guid mediaModuleId, string name, bool concurrent,
        Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId);

    /// <summary>
    /// Opens a video player context. If there is already an active player, it depends on the parameter
    /// <paramref name="concurrent"/> whether the already active player context will be deactivated or not.
    /// If there is already a video player active, it depends on the <paramref name="subordinatedVideo"/> parameter
    /// whether the new video player will be run in picture-in-picture mode (PIP) or whether the active player will
    /// be replaced by the new player.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method has to handle many conflict situations with already running players. Almost all conflict situations
    /// can be controlled by setting the parameters <paramref name="concurrent"/> and <paramref name="subordinatedVideo"/>.
    /// The situation where there are already a primary and a secondary (PIP) video player is a very complicated situation,
    /// where not every possible combination can be achieved by this method; you cannot automatically preserve the PIP
    /// player, as always the PIP video player will be removed:
    /// If <paramref name="concurrent"/> is set to <c>false</c>, all active players will be deactivated first.
    /// If <paramref name="concurrent"/> is set to <c>true</c>, always the secondary active player (which is used for
    /// PIP) will be deactivated. So to exchange the primary player with a new player, while the secondary (PIP) player
    /// should be preserved, you need to do this manually (switching the players).
    /// </para>
    /// <para>
    /// The returned player context off course does not play yet; its playlist first has to be filled. For a streaming
    /// player, the playlist will typically be filled with one single URL entry, while for resource based players, the
    /// playlist typically will contain multiple entries.
    /// After the playlist was filled, the player context has to be started.
    /// </para>
    /// <para>
    /// If the audio signal should be taken from the new video player context, the audio slot index should to be changed
    /// subsequently to this method (see <see cref="IPlayerManager.AudioSlotIndex"/>).
    /// </para>
    /// </remarks>
    /// <param name="mediaModuleId">Id of the requesting media module. The new player context will be sticked to the specified
    /// module.</param>
    /// <param name="name">A name for the new player context. The name will be shown in each skin control which
    /// represents the player context.</param>
    /// <param name="concurrent">If set to <c>true</c>, an already active player will continue to play and the new
    /// video player context will be muted if the active player provides an audio signal.
    /// If set to <c>false</c>, an active player context will be deactivated.</param>
    /// <param name="subordinatedVideo">This parameter is only evaluated when the <paramref name="concurrent"/> parameter
    /// is set to <c>true</c>. If <paramref name="subordinatedVideo"/> is set to <c>true</c>, an already active primary
    /// video player will continue playing in the primary player slot, and the new player context will be created as
    /// secondary player/PIP.
    /// If set to <c>false</c>, an already active primary video player context will be replaced by the new player
    /// context.</param>
    /// <param name="currentlyPlayingWorkflowStateId">The id of the workflow state to be used as currently playing
    /// workflow state for the new player context.</param>
    /// <param name="fullscreenContentWorkflowStateId">The id of the workflow state to be used as fullscreen content
    /// workflow state for the new player context.</param>
    /// <returns>Descriptor object for the new video player context.</returns>
    IPlayerContext OpenVideoPlayerContext(Guid mediaModuleId, string name, bool concurrent, bool subordinatedVideo,
        Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId);

    /// <summary>
    /// Returns all player contexts of the media module with the specified <paramref name="mediaModuleId"/>.
    /// </summary>
    /// <param name="mediaModuleId">The id of the media module which is looking for its player contexts.</param>
    /// <returns>Enumeration of player contexts belonging the specified media module.</returns>
    IEnumerable<IPlayerContext> GetPlayerContextsByMediaModuleId(Guid mediaModuleId);

    /// <summary>
    /// Switches to the "currently playing" workflow state for the current player.
    /// </summary>
    void ShowCurrentlyPlaying();

    /// <summary>
    /// Switches to the "fullscreen content" workflow state for the primary player.
    /// </summary>
    void ShowFullscreenContent();

    /// <summary>
    /// Closes the player context with the specified player slot index.
    /// </summary>
    /// <param name="slotIndex">Index of the slot to be closed.</param>
    void ClosePlayerContext(int slotIndex);

    /// <summary>
    /// Returns the player context type of the specified media <paramref name="item"/>. The player context type of a media
    /// item determines if a player context of audio type (<see cref="PlayerContextType.Audio"/>) or of video type
    /// (<see cref="PlayerContextType.Video"/>) is needed to play the given <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The media item to examine.</param>
    /// <returns>Player context type of the given media <paramref name="item"/>.</returns>
    PlayerContextType GetTypeOfMediaItem(MediaItem item);

    /// <summary>
    /// Returns all audio streams which are available from the currently active players.
    /// </summary>
    ICollection<AudioStreamDescriptor> GetAvailableAudioStreams();

    /// <summary>
    /// Activates one of the available audio streams. This method might fail if the specified audio stream isn't available
    /// (any more).
    /// </summary>
    /// <param name="stream">One of the available audio streams, which were returned by <see cref="GetAvailableAudioStreams"/>.
    /// </param>
    /// <returns><c>true</c>, if the specified <paramref name="stream"/> could successfully be activated, else
    /// <c>false</c>.</returns>
    bool SetAudioStream(AudioStreamDescriptor stream);

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
    /// If playing, this method will pause the current player play, else it will make it play or replay the current item.
    /// </summary>
    void TogglePlayPause();

    /// <summary>
    /// Restarts playback of the item in the current player.
    /// </summary>
    void Restart();

    /// <summary>
    /// Plays the previous item from the current player's playlist.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the previous item could be started, else <c>false</c>.
    /// </returns>
    bool PreviousItem();

    /// <summary>
    /// Plays the next item from the current player's playlist.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the next item could be started, else <c>false</c>.
    /// </returns>
    bool NextItem();

    /// <summary>
    /// Changes the index of the "current" player to the other active player, if possible.
    /// </summary>
    void ToggleCurrentPlayer();
  }
}
