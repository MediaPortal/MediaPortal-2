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

namespace MediaPortal.UI.Presentation.Players
{
  public delegate void PlayerSlotWorkerDelegate(IPlayerSlotController slotController);

  /// <summary>
  /// The player manager provides basic, technical player information for a maximum set of two players
  /// (primary and secondary player). It deals with primary and secondary player slots, player volume and muting.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The player manager provides the very technical interface to players. It deals with primary/secondary player
  /// (players 0 and 1), player slots and slot activity states. The compontent to manage players at a more user-related
  /// level is <see cref="IPlayerContextManager"/>, which is based on this <see cref="IPlayerManager"/> component.
  /// </para>
  /// <para>
  /// From the player manager's point of view, both the primary and the secondary player slot behave the same, except
  /// that the secondary slot can only be active if the primary slot is active.
  /// But the application (i.e. the presentation module) will typically use the primary player slot for the fullscreen
  /// video display, while a video in the secondary player slot will be displayed as a PIP video.
  /// </para>
  /// <para>
  /// Audio comes from the player in the slot denoted by <see cref="AudioSlotIndex"/>, except when the
  /// <see cref="Muted"/> property is set to <c>true</c>.
  /// Slots are switched active and inactive EXPLICITLY (no implicit CloseSlot(N)!).
  /// <para>
  /// <b>Thread-Safety:</b><br/>
  /// This class can be called from multiple threads. It synchronizes thread access to its fields via its
  /// <see cref="SyncObj"/> instance. Accesses to its contained <see cref="IPlayerSlotController"/>s are
  /// also synchronized via the same <see cref="SyncObj"/> instance.
  /// </para>
  /// <para>
  /// Important player manager notification messages are sent via the message channel of name
  /// <see cref="PlayerManagerMessaging.CHANNEL"/>.
  /// </para>
  /// </remarks>
  public interface IPlayerManager : IDisposable
  {
    /// <summary>
    /// Returns the synchronization object to lock this instance.
    /// </summary>
    object SyncObj { get; }

    /// <summary>
    /// Returns the number of active player slots (0, 1 or 2).
    /// </summary>
    int NumActiveSlots { get; }

    /// <summary>
    /// Gets the player at the specified player slot, or <c>null</c>, if there is no player in the slot.
    /// </summary>
    IPlayer this[int slotIndex] { get; }

    /// <summary>
    /// Gets or sets the index of the slot which provides the audio signal. If there is no active slot
    /// at the moment, or if the system is muted, then <see cref="AudioSlotIndex"/> will be <c>-1</c>.
    /// </summary>
    int AudioSlotIndex { get; set; }

    /// <summary>
    /// Gets or sets the muted state of the system. The muted state is independent from the audio slot index,
    /// both states complete each other, i.e. a player only plays the audio signal if it is the audio slot and not muted.
    /// </summary>
    bool Muted { get; set; }

    /// <summary>
    /// Gets or sets the volume which will be used in all players.
    /// </summary>
    int Volume { get; set; }

    /// <summary>
    /// Opens a player slot.
    /// This method succeeds if <see cref="NumActiveSlots"/> is less than <c>2</c>.
    /// </summary>
    /// <param name="slotIndex">Returns the index of the new slot, if the preparation was successful.</param>
    /// <param name="slotController">Returns the player slot controller of the new slot.</param>
    /// <returns><c>true</c>, if a new slot could be opened, else <c>false</c>.</returns>
    bool OpenSlot(out int slotIndex, out IPlayerSlotController slotController);

    /// <summary>
    /// Releases the player of the specified <paramref name="slotIndex"/> and closes the slot.
    /// </summary>
    /// <remarks>
    /// See <see cref="CloseSlot(MediaPortal.Presentation.Players.IPlayerSlotController)"/>.
    /// </remarks>
    /// <param name="slotIndex">Index of the slot to close.</param>
    void CloseSlot(int slotIndex);

    /// <summary>
    /// Releases the player of the specified <paramref name="playerSlotController"/> and closes the slot.
    /// </summary>
    /// <remarks>
    /// If the specified <paramref name="playerSlotController"/> provides the audio signal, the audio flag will go to
    /// the remaining slot, if present. If the specified slot is the first/primary player slot, then after closing it
    /// the secondary slot will become the primary slot.
    /// </remarks>
    /// <param name="playerSlotController">The controller of the slot to close.</param>
    void CloseSlot(IPlayerSlotController playerSlotController);

    /// <summary>
    /// Stops and releases all active players and closes their slots.
    /// </summary>
    void CloseAllSlots();

    /// <summary>
    /// Gets the player slot instance for the slot of the specified <paramref name="slotIndex"/> index.
    /// </summary>
    /// <param name="slotIndex">Index of the slot to return the controller instance for.</param>
    /// <returns>The controller instance for the specified slot.</returns>
    IPlayerSlotController GetPlayerSlotController(int slotIndex);

    /// <summary>
    /// Switches the primary and secondary player slots. The slot controller, which was located in slot <c>0</c>,
    /// will be moved to slot <c>1</c> and vice-versa. This method only succeeds if there are exactly two open slots.
    /// </summary>
    void SwitchSlots();

    /// <summary>
    /// Executes the given method on each active slot.
    /// </summary>
    /// <param name="execute">Method to execute.</param>
    void ForEach(PlayerSlotWorkerDelegate execute);

    /// <summary>
    /// Increments the volume.
    /// </summary>
    void VolumeUp();

    /// <summary>
    /// Decrements the volume.
    /// </summary>
    void VolumeDown();
  }
}
