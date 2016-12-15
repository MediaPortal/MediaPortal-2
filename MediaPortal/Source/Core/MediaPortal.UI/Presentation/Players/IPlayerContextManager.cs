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

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// For each player, there need to be two special states to present its player contents:
  /// The currently playing state and the fullscreen content state.
  /// </summary>
  public enum MediaWorkflowStateType
  {
    /// <summary>
    /// No special screen.
    /// </summary>
    None,

    /// <summary>
    /// Indicates the currently playing state. The screen to this state shows detailed information
    /// about the current played media item, the current playing state and presents additional actions
    /// for the current played media.
    /// </summary>
    /// <remarks>
    /// The currently playing screen can be shown for both the primary and the secondary player of the
    /// <see cref="IPlayerContextManager"/>.
    /// </remarks>
    CurrentlyPlaying,

    /// <summary>
    /// Indicates the fullscreen content state. For video and image players, the screen to this state shows the
    /// video or image in fullscreen mode with additional onscreen display, info and/or controls for actions to
    /// be taken by the user. For audio players, this could be a visualization screen.
    /// </summary>
    /// <remarks>
    /// The fullscreen content screen can only be shown for the primary player of the <see cref="IPlayerContextManager"/>.
    /// </remarks>
    FullscreenContent,
  }

  /// <summary>
  /// Tells the called method, which player type should be played concurrently.
  /// </summary>
  public enum PlayerContextConcurrencyMode
  {
    /// <summary>
    /// No concurrent playing - all open player slots will be closed.
    /// </summary>
    None,

    /// <summary>
    /// An already playing audio player will be left open and played concurrently.
    /// </summary>
    ConcurrentAudio,

    /// <summary>
    /// An already playing video player will be left open (as primary or secondary player - depending on the open player strategy)
    /// and played concurrently.
    /// </summary>
    ConcurrentVideo,
  }

  /// <summary>
  /// Used as a parameter for methods which can work on either player to describe which player is meant.
  /// </summary>
  public enum PlayerChoice
  {
    /// <summary>
    /// The primary player.
    /// </summary>
    PrimaryPlayer,

    /// <summary>
    /// The secondary player.
    /// </summary>
    SecondaryPlayer,

    /// <summary>
    /// The player which is marked as "current player".
    /// </summary>
    CurrentPlayer,

    /// <summary>
    /// The player which is not marked as "current player".
    /// </summary>
    NotCurrentPlayer,
  }

  /// <summary>
  /// Management service for active players and their integration into the UI. This service handles player contexts.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Media modules (like Audio, Video, Image, TV, Radio) can initiate and access player contexts,
  /// which are specially connected to them. The player context manager service manages all those player contexts,
  /// tracks their player state and manages UI workflow states relating to active players.
  /// The transfer of the responsibility for the UI workflow state tracking from the actual media module to the
  /// player context manager is necessary, because players also need to run while the
  /// media modules possibly don't have the active control over them, maybe when players are running while the
  /// user navigates in the main menu.<br/>
  /// The typical separation of roles is like this:<br/>
  /// The media module initiates one or more player contexts. It doesn't need to track its player contexts;
  /// it is always possible to find the player contexts of the media module by its media module id (by calling
  /// <see cref="GetPlayerContextsByMediaModuleId"/>).
  /// The player context manager will track two current media workflow states for each player context: The workflow
  /// state for a "fullscreen content" state and the workflow state for a "currently playing" workflow state.
  /// The meaning of those two states should be complied to but the media module is responsible to provide appropriate
  /// workflow states.
  /// The player context manager will automatically switch workflow states, if necessary
  /// (for example when the primary and secondary players are switched, the fullscreen content workflow state needs
  /// to be exchanged if different fullscreen content workflow states are used for primary and secondary players).
  /// It will also automatically pop those workflow states from the workflow navigation stack, if the corresponding
  /// player context gets closed.
  /// The player context manager tracks player context activity states (like <see cref="IsAudioContextActive"/> and
  /// <see cref="IsPipActive"/>), takes care of automatic playlist advance and closes player contexts automatically,
  /// if necessary.
  /// Media modules should use this service interface to manage players which are integrated into the main UI instead
  /// of using the basic <see cref="IPlayerManager"/> API itself. If there are special needs, for example a video wall
  /// with more than two players or if players are needed with a different concurrentcy concept, the player manager's
  /// API can be used directly; in that case, all management beyond the basic player manager's functionality must be
  /// done by the module which directly uses the player manager's API.
  /// </para>
  /// <para>
  /// <b>Functionality:</b><br/>
  /// Built on the functionality of the <see cref="IPlayerManager"/>, the player context manager presents a more
  /// high view for the client, it deals with two (primary and secondary) typed player contexts and their playlists.
  /// The functionality of this component is comprehensive, it deals with the collectivity of all players, in contrast
  /// to the <see cref="IPlayerManager"/>'s functionality which is mostly focused to single technical player slot controllers.
  /// This service manages and solves player conflicts (like two audio players at the same time) automatically by
  /// simply closing an old player when a new conflicting player is opened. Non-conflicting players (e.g. video
  /// players) can be played concurrently.
  /// The technical target player slot (primary/secondary) of a given <see cref="IPlayerContext"/>
  /// is managed almost transparently for the client. There is a rare number of cases where the client needs to cope
  /// with the set-up of primary and secondary players directly, for example when two video players are running,
  /// one of them as PiP player. In that situation, it can be necessary to explicitly exchange the player slots.
  /// </para>
  /// <para>
  /// <b>Playlists:</b><br/>
  /// The player context manager also provides playlist management, i.e. it manages automatic playlist advance and
  /// provides methods to control the current player like <see cref="Stop"/>, <see cref="Pause"/> etc.
  /// </para>
  /// <para>
  /// <b>Thread-Safety:</b><br/>
  /// Methods of this class can be called from multiple threads. It synchronizes thread access to its fields via the
  /// <see cref="IPlayerManager.SyncObj"/> instance, which is also exposed by the <see cref="SyncObj"/> property of the
  /// player context manager service for convenience. Player context manager messages are sent asynchronously to
  /// clients via the message channel of name <see cref="PlayerContextManagerMessaging.CHANNEL"/>.
  /// </para>
  /// </remarks>
  public interface IPlayerContextManager
  {
    /// <summary>
    /// Returns the player manager's synchronization object to synchronize thread access on this instance.
    /// This is a convenience property for getting the player manager's <see cref="IPlayerManager.SyncObj"/>.
    /// </summary>
    object SyncObj { get; }

    /// <summary>
    /// Gets a list of all available player contexts in ascending slot index order (<see cref="PlayerContextIndex.PRIMARY"/>,
    /// <see cref="PlayerContextIndex.SECONDARY"/>).
    /// </summary>
    /// <remarks>
    /// The number of active player slot controllers managed by the <see cref="IPlayerManager"/> might be bigger than
    /// the size of the returned list because the player context manager only returns those players which are used for
    /// primery and secondary UI players.
    /// </remarks>
    IList<IPlayerContext> PlayerContexts { get; }

    /// <summary>
    /// Returns the information if there is already an active player context of type <see cref="AVType.Audio"/>.
    /// </summary>
    bool IsAudioContextActive { get; }

    /// <summary>
    /// Returns the information if there is already an active player context of type <see cref="AVType.Video"/>.
    /// </summary>
    bool IsVideoContextActive { get; }

    /// <summary>
    /// Returns the information if there's a secondary video player is running in PiP mode.
    /// </summary>
    bool IsPipActive { get; }

    /// <summary>
    /// Gets the information a "currently playing" workflow state is available on the workflow navigation stack.
    /// </summary>
    bool IsCurrentlyPlayingWorkflowStateActive { get; }

    /// <summary>
    /// Gets the information a "fullscreen content" workflow state is available on the workflow navigation stack.
    /// </summary>
    bool IsFullscreenContentWorkflowStateActive { get; }

    /// <summary>
    /// Gets or sets the index of the current player slot. The current player is the player which has the
    /// "user focus", i.e. it receives all commands from the remote or from other play controls and it will be shown
    /// in the "currently playing" screen.
    /// </summary>
    /// <remarks>
    /// If there is one player active, that player is the current player and this property will return the index
    /// of that player slot (<see cref="PlayerContextIndex.PRIMARY"/>). If no player is active at the moment,
    /// this property returns <c>-1</c>.
    /// </remarks>
    int CurrentPlayerIndex { get; set; }

    /// <summary>
    /// Gets or sets the player context of the current player, if present. The current player is the player which is controlled
    /// by the remote control.
    /// </summary>
    IPlayerContext CurrentPlayerContext { get; set; }

    /// <summary>
    /// Gets the primary player context, if present.
    /// </summary>
    IPlayerContext PrimaryPlayerContext { get; }

    /// <summary>
    /// Gets the secondary player context, if present.
    /// </summary>
    IPlayerContext SecondaryPlayerContext { get; }

    /// <summary>
    /// Returns the number of active player contexts (0, 1 or 2).
    /// </summary>
    int NumActivePlayerContexts { get; }

    /// <summary>
    /// Gets the player at the specified player context index, or <c>null</c>, if there is no player in the slot of the given <paramref name="index"/>.
    /// </summary>
    /// <remarks>
    /// The index to use must be one of <see cref="PlayerContextIndex.PRIMARY"/> or
    /// <see cref="PlayerContextIndex.SECONDARY"/>.
    /// </remarks>
    IPlayer this[int index] { get; }

    /// <summary>
    /// Shuts this service down. This must be done before the player manager gets closed.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Returns the player context object which is determined by the given <paramref name="player"/> parameter.
    /// </summary>
    /// <param name="player">Tells for which player the player context should be returned.</param>
    /// <returns>Player context instance or <c>null</c>, if the specified slot isn't active at the moment or
    /// has no player context assigned.</returns>
    IPlayerContext GetPlayerContext(PlayerChoice player);

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
    /// Opens an audio player context. This will replace a running audio player context, if present. If a video player is
    /// active, it depends on the parameter <paramref name="concurrentVideo"/> whether the video player context will be
    /// deactivated or not.
    /// </summary>
    /// <remarks>
    /// The returned player context off course does not play yet; its playlist first has to be filled. For a streaming
    /// player, the playlist will typically be filled with one single URL entry, while for resource based players, the
    /// playlist typically will contain multiple entries.
    /// After the playlist was filled, the player context can be started.
    /// </remarks>
    /// <param name="mediaModuleId">Id of the requesting media module. The new player context will be sticked to the specified
    /// module.</param>
    /// <param name="name">A name for the new player context. The name will be shown in each skin control which
    /// represents the player context.</param>
    /// <param name="concurrentVideo">If set to <c>true</c>, an already active video player will continue to play muted.
    /// If set to <c>false</c>, an active video player context will be deactivated.</param>
    /// <param name="currentlyPlayingWorkflowStateId">The id of the workflow state to be used as currently playing
    /// workflow state for the new player context.</param>
    /// <param name="fullscreenContentWorkflowStateId">The id of the workflow state to be used as fullscreen content
    /// workflow state for the new player context.</param>
    /// <returns>Descriptor object for the new audio player context. The returned player context will be installed
    /// into the system but is not playing yet.</returns>
    IPlayerContext OpenAudioPlayerContext(Guid mediaModuleId, string name, bool concurrentVideo,
        Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId);

    /// <summary>
    /// Opens a video player context. If there are already active players, the parameter <paramref name="concurrencyMode"/>
    /// determines if and which of the open player contexts will be left open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method has to handle many conflict situations with already running players. Almost all conflict situations
    /// can be controlled by setting the parameter <paramref name="concurrencyMode"/>.
    /// The situation where there are already a primary as well as a secondary (PiP) video player cannot be controlled
    /// by that parameter completely; In that case, the secondary (PiP) player will be closed in every case while the
    /// primary player will be left open when parameter <paramref name="concurrencyMode"/> is set to
    /// <see cref="PlayerContextConcurrencyMode.ConcurrentVideo"/>.
    /// So to exchange the primary player with a new player, while the secondary (PiP) player
    /// should be preserved, you need to do this manually (switching the players first).
    /// </para>
    /// <para>
    /// The returned player context off course does not play yet; its playlist first has to be filled. For a streaming
    /// player, the playlist will typically be filled with one single URL entry, while for resource based players, the
    /// playlist typically will contain multiple entries.
    /// After the playlist was filled, the player context can be started.
    /// </para>
    /// <para>
    /// If the audio signal should be taken from the new video player context, that should be switched by the means
    /// of the returned <see cref="IPlayerContext"/> subsequently to the call of this method.
    /// </para>
    /// </remarks>
    /// <param name="mediaModuleId">Id of the requesting media module. The new player context will be sticked to the specified
    /// module.</param>
    /// <param name="name">A name for the new player context. The name will be shown in each skin control which
    /// represents the player context.</param>
    /// <param name="concurrencyMode">If set to <see cref="PlayerContextConcurrencyMode.ConcurrentAudio"/>, an already
    /// active audio player will continue to play and the new video player context will be muted.
    /// If set to <see cref="PlayerContextConcurrencyMode.ConcurrentVideo"/>, an already active audio player context will be
    /// deactivated while an already active video player context will continue to play. If a video player context was
    /// available, the video players will be arranged according to the configured open player strategy.</param>
    /// <param name="currentlyPlayingWorkflowStateId">The id of the workflow state to be used as currently playing
    /// workflow state for the new player context.</param>
    /// <param name="fullscreenContentWorkflowStateId">The id of the workflow state to be used as fullscreen content
    /// workflow state for the new player context.</param>
    /// <returns>Descriptor object for the new video player context.</returns>
    IPlayerContext OpenVideoPlayerContext(Guid mediaModuleId, string name, PlayerContextConcurrencyMode concurrencyMode,
        Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId);

    /// <summary>
    /// Returns all player contexts of the media module with the specified <paramref name="mediaModuleId"/>.
    /// </summary>
    /// <param name="mediaModuleId">The id of the media module which is looking for its player contexts.</param>
    /// <returns>Enumeration of player contexts belonging the specified media module.</returns>
    IEnumerable<IPlayerContext> GetPlayerContextsByMediaModuleId(Guid mediaModuleId);

    /// <summary>
    /// Returns all player contexts with the specified <paramref name="avType"/>.
    /// </summary>
    /// <param name="avType">The type of media which is playing in the to-be-returned player contexts.</param>
    /// <returns>Enumeration of player contexts playing the specified <paramref name="avType"/>.</returns>
    IEnumerable<IPlayerContext> GetPlayerContextsByAVType(AVType avType);

    /// <summary>
    /// Closes all active player contexts.
    /// </summary>
    void CloseAllPlayerContexts();

    /// <summary>
    /// Switches to the "currently playing" workflow state for the current player.
    /// </summary>
    /// <remarks>
    /// The "currently playing" workflow state can only be shown for the current player. As long as the user remains
    /// in the CP state, the player context manager will automatically track changes of the current player and adapt the
    /// "currently playing" state to match the new current player.
    /// </remarks>
    /// <param name="asynchronously">If set to <c>true</c>, the workflow change will happen asynchronously.</param>
    void ShowCurrentlyPlaying(bool asynchronously);

    /// <summary>
    /// Switches to the "fullscreen content" workflow state for the primary player.
    /// </summary>
    /// <remarks>
    /// The "fullscreen content" workflow state can only be shown for the primary player. As long as the user remains
    /// in the FSC state, the player context manager will automatically track changes of the primary player and adapt the
    /// "fullscreen content" state to match the new primary player.
    /// </remarks>
    /// <param name="asynchronously">If set to <c>true</c>, the workflow change will happen asynchronously.</param>
    void ShowFullscreenContent(bool asynchronously);

    /// <summary>
    /// Returns the audio/video type of the specified media <paramref name="item"/>. The audio/video type of a media
    /// item determines if a player context of audio type (<see cref="AVType.Audio"/>) or of video type
    /// (<see cref="AVType.Video"/>) is needed to play the given <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The media item to examine.</param>
    /// <returns>Audio/video type of the given media <paramref name="item"/>.</returns>
    AVType GetTypeOfMediaItem(MediaItem item);

    /// <summary>
    /// Returns all audio streams which are available from the currently active players.
    /// </summary>
    /// <param name="currentAudioStream">Descriptor for the current audio stream. This will also be filled if
    /// we are in muted mode. If there is no current audio stream set, <c>null</c> will be returned.</param>
    /// <returns>Collection of audio stream descriptors.</returns>
    ICollection<AudioStreamDescriptor> GetAvailableAudioStreams(out AudioStreamDescriptor currentAudioStream);

    /// <summary>
    /// Activates one of the available audio streams. This method might fail if the specified audio stream isn't available
    /// (any more) or if the underlaying player refuses to play the audio signal.
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
    /// Triggers seeking forward at the current player (see <see cref="IPlayerContext.SeekForward"/>).
    /// </summary>
    void SeekForward();

    /// <summary>
    /// Triggers seeking backward at the current player (see <see cref="IPlayerContext.SeekBackward"/>).
    /// </summary>
    void SeekBackward();

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

    /// <summary>
    /// Switches the primary and secondary players, if they are both video players.
    /// </summary>
    void SwitchPipPlayers();
  }
}
