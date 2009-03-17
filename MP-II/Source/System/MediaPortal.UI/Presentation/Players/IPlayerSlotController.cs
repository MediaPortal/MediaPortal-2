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

using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Presentation.Players
{
  /// <summary>
  /// State indicator for the playback state of a player slot controller.
  /// </summary>
  public enum PlayerSlotState
  {
    Inactive,
    Playing,
    Paused,
    Switching,
    Stopped
  }

  /// <summary>
  /// Slot controller for a player slot of the <see cref="IPlayerManager"/>.
  /// The slot controller maintains the state of each player slot and exposes properties and methods to get and
  /// change the state, like <see cref="IsActive"/>, <see cref="IsAudioSlot"/>, ..., <see cref="NextItem"/>, ...
  /// </summary>
  /// <remarks>
  /// This player slot can adopt similar play states as the player (see <see cref="PlayerSlotState"/>). The states differ
  /// when the player is exchanged because of a playlist advance.<br/>
  /// </remarks>
  public interface IPlayerSlotController
  {
    /// <summary>
    /// Gets the playlist for the underlaying slot.
    /// </summary>
    IPlaylist PlayList { get; }

    /// <summary>
    /// Returns the information if this slot plays the audio signal.
    /// </summary>
    bool IsAudioSlot { get; }

    /// <summary>
    /// Returns the information if this player slot is activated.
    /// An active slot can play media content.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Returns the information if this slot is able to play media content, i.e. contains a
    /// playlist which has not ended yet.
    /// </summary>
    bool CanPlay { get; }

    /// <summary>
    /// Gets the playback state of this player slot.
    /// </summary>
    PlayerSlotState PlayerSlotState { get; }

    /// <summary>
    /// Gets the player playing the current item in the playlist.
    /// </summary>
    IPlayer CurrentPlayer { get; }

    /// <summary>
    /// Stops an active player, clears the playlist.
    /// </summary>
    void Reset();

    /// <summary>
    /// Stops playback.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses playback.
    /// </summary>
    void Pause();

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    void Play();

    /// <summary>
    /// Plays a media file directly, without having a <see cref="MediaItem"/> instance for it.
    /// The played media file won't be added to the playlist.
    /// </summary>
    /// <param name="locator">Media item locator to the media file.</param>
    /// <param name="mimeType">Mime type of the media item, if known. If this parameter is given, the
    /// decision if the media file can be played might be faster. If this parameter is set to <c>null</c>,
    /// this method will potentially need more time to look into the given resource.</param>
    /// <returns><c>true</c>, if the specified media resource can be played, else <c>false</c>.</returns>
    bool Play(IMediaItemLocator locator, string mimeType);

    /// <summary>
    /// Restarts playback of the current item.
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
