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

using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UI.Presentation.Players
{
  public delegate void ClosedDlgt(IPlayerSlotController slotController);

  /// <summary>
  /// Manages a single player slot of the <see cref="IPlayerManager"/>.
  /// The player slot controller maintains the state of each player slot and exposes context variables, which can contain
  /// user defined data like a playlist, the player's aspect ratio or additional information about a currently
  /// viewed TV channel, for example. During its lifetime, a player slot controller can host multiple players.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This component is multithreading safe. To synchronize multithread access, this instance uses the
  /// <see cref="IPlayerManager.SyncObj"/> instance.
  /// </para>
  /// </remarks>
  public interface IPlayerSlotController
  {
    /// <summary>
    /// Synchronous event which gets fired when this player slot controller is closed. In the event handler,
    /// no other locks than the player manager's <see cref="IPlayerManager.SyncObj"/> may be acquired!
    /// </summary>
    event ClosedDlgt Closed;

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
    /// Gets or sets the volume coefficient which will be applied to the main <see cref="IPlayerManager.Volume"/> for
    /// this current player slot. This offset will be applied to the current player (if available) and all future
    /// players of this slot until this slot controller is closed or until the offset is changed again.
    /// </summary>
    float VolumeCoefficient { get; set; }

    /// <summary>
    /// Returns the information if this player slot is closed.
    /// A closed slot cannot play content nor can it be reactivated.
    /// </summary>
    bool IsClosed { get; }

    /// <summary>
    /// Gets the player playing the current item of this player slot.
    /// The current player can change, for example when the playlist advances.
    /// </summary>
    IPlayer CurrentPlayer { get; }

    /// <summary>
    /// Returns a (key; value) mapping of all context variables in this player slot. Changing the returned dictionary will
    /// change the context variables.
    /// </summary>
    /// <remarks>
    /// The access to the returned dictionary should be synchronized using the <see cref="IPlayerManager.SyncObj"/>.
    /// </remarks>
    IDictionary<string, object> ContextVariables { get; }

    /// <summary>
    /// Plays a media item. An appropriate player will be choosen.
    /// </summary>
    /// <param name="mediaItem">Media item to play.</param>
    /// <param name="startTime">Tells the player slot controller when it should play the next item.
    /// This parameter is necessary for the potential already playing player if it is about to be reused to play the
    /// new media resource.</param>
    /// <returns><c>true</c>, if the specified media resource can be played, else <c>false</c>.</returns>
    bool Play(MediaItem mediaItem, StartTime startTime);

    /// <summary>
    /// Stops the player of this player slot, i.e. releases it.
    /// </summary>
    void Stop();

    /// <summary>
    /// Resets this player slot controller. This will stop this player slot controller and clear all context variables.
    /// </summary>
    void Reset();
  }
}
