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

using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Presentation.Players
{
  /// <summary>
  /// State indicator for the playback state of a player slot.
  /// This state is a complement to the current player's state.
  /// </summary>
  public enum PlayerSlotState
  {
    /// <summary>
    /// The player context is inactive and doesn't contain any relevant information. The current player is <c>null</c>.
    /// </summary>
    Inactive,

    /// <summary>
    /// The player context is active. The playback state can be read from current player's <see cref="IPlayer.State"/>.
    /// </summary>
    Playing,

    /// <summary>
    /// The player context is active but stopped. There is no current player.
    /// </summary>
    Stopped
  }

  /// <summary>
  /// Player slot controller for a player in a player slot of the <see cref="IPlayerManager"/>.
  /// The player slot controller maintains the state of each player slot and exposes context variables, which can contain
  /// user defined data like a playlist, the player's aspect ratio or additional information about a currently
  /// viewed TV channel, for example.
  /// </summary>
  /// <remarks>
  /// This player slot can adopt similar play states as the player (see <see cref="PlayerSlotState"/>). Mostly, the
  /// states of player and its player slot controller correspond to each other, but the states can differ in case
  /// when the player is exchanged (for example because of a playlist advance).<br/>
  /// </remarks>
  public interface IPlayerSlotController
  {
    /// <summary>
    /// Returns the index of the slot which is controlled by this slot controller.
    /// </summary>
    int SlotIndex { get; }

    /// <summary>
    /// Returns the information if this player slot is the audio slot. In this case, will play the audio signal,
    /// if it is not muted.
    /// </summary>
    /// <remarks>
    /// This property is located here rather than in the player manager, because when exchanging the player, we need
    /// to configure each new player according to this property.
    /// </remarks>
    bool IsAudioSlot { get; }

    /// <summary>
    /// Returns the information if this player slot is muted. A player slot can be both the audio slot and muted.
    /// </summary>
    bool IsMuted { get; }

    /// <summary>
    /// Gets or sets the volume for the current player slot. This will set the volume for the current player
    /// (if available) and all future players of this slot until it is changed again.
    /// </summary>
    int Volume { get; set; }

    /// <summary>
    /// Returns the information if this player slot is activated.
    /// Only active slots can play media content.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the playback state of this player slot.
    /// </summary>
    PlayerSlotState PlayerSlotState { get; }

    /// <summary>
    /// Gets the player playing the current item of this player slot.
    /// The current player can chainge, for example when the playlist advances.
    /// </summary>
    IPlayer CurrentPlayer { get; }

    /// <summary>
    /// Returns a (key; value) mapping of all context variables in this player slot. Changing the returned dictionary will
    /// change the context variables.
    /// </summary>
    IDictionary<string, object> ContextVariables { get; }

    /// <summary>
    /// Plays a media resource. An appropriate player will be choosen to play the specified media resource.
    /// </summary>
    /// <param name="locator">Media item locator to the media resource.</param>
    /// <param name="mimeType">Mime type of the media resource, if known. If this parameter is given, the
    /// decision whether the media resource can be played might be faster. If this parameter is set to <c>null</c>,
    /// this method will potentially need more time to look into the given resource's content.</param>
    /// <returns><c>true</c>, if the specified media resource can be played, else <c>false</c>.</returns>
    bool Play(IMediaItemLocator locator, string mimeType);

    /// <summary>
    /// Stops the player of this player slot. This won't deactivate this slot, but the current player
    /// will be released, if it is active.
    /// Calling this method will set the <see cref="PlayerSlotState"/> to <see cref="Players.PlayerSlotState.Stopped"/>.
    /// </summary>
    void Stop();

    /// <summary>
    /// Resets this player slot controller. This will stop this player slot controller and clear all context variables.
    /// </summary>
    void Reset();
  }
}
