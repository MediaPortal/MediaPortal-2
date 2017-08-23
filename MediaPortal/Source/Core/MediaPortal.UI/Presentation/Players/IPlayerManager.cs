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

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// The player manager provides basic, technical player information for all currently opened player slots.
  /// It manages the audio signal, volume setting, muting and generic context variables.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A player slot is a logical unit which contains a player, several metadata and other properties. Clients
  /// like the <see cref="IPlayerContextManager"/> use a player slot controller to build their player contexts
  /// on it, storing data like a playlist in it.
  /// </para>
  /// <para>
  /// The player manager provides the very technical interface to players. It deals at the level of technical player
  /// management. The compontent to manage players at a more user-related level is
  /// <see cref="IPlayerContextManager"/>, which is based on this <see cref="IPlayerManager"/> service.
  /// </para>
  /// <para>
  /// Relation to the <see cref="IPlayerContextManager"/>:
  /// There might be more players managed by this service than player contexts known by the <see cref="IPlayerContextManager"/>.
  /// From the player manager's point of view, all players are handled the same, independently if they are coupled with
  /// player contexts or which slot number they are attached with in the player context manager.
  /// </para>
  /// <para>
  /// Audio is played by the player in the slot where <see cref="IPlayerSlotController.IsAudioSlot"/> is <c>true</c>,
  /// except when the <see cref="Muted"/> property is set to <c>true</c>.
  /// </para>
  /// <para>
  /// <b>Thread-Safety:</b><br/>
  /// Methods of this class can be called from multiple threads. It synchronizes thread access to its fields via its
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
    /// Returns the number of currently active player slots.
    /// </summary>
    int NumActiveSlots { get; }

    /// <summary>
    /// Gets or sets the muted state of the system.
    /// </summary>
    /// <remarks>
    /// The muted state is independent from the setting of the audio player
    /// slot controller, both states complete each other, i.e. even if <see cref="Muted"/> is set to <c>true</c>, one of the
    /// player slot controllers has the <see cref="IPlayerSlotController.IsAudioSlot"/> property set and the player of that slot
    /// controller only plays the audio signal if <see cref="Muted"/> is set to <c>false</c>.
    /// </remarks>
    bool Muted { get; set; }

    /// <summary>
    /// Gets or sets the main volume for all players. The value range is from 0 (muted) to 100 (loudest).
    /// </summary>
    int Volume { get; set; }

    /// <summary>
    /// Gets or sets the player slot controller which currently plays the audio signal. If there is no active slot
    /// at the moment, this property returns <c>null</c>. If the system is muted (see <see cref="Muted"/>),
    /// this property yet returns the value of the slot controller which will play the audio signal when
    /// <see cref="Muted"/> is set to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// This property returns the slot controller which has the <see cref="IPlayerSlotController.IsAudioSlot"/> property set.
    /// </remarks>
    IPlayerSlotController AudioSlotController { get; set; }

    /// <summary>
    /// Gets all active player slot controller instances.
    /// </summary>
    ICollection<IPlayerSlotController> PlayerSlotControllers { get; }

    /// <summary>
    /// Opens a player slot.
    /// </summary>
    /// <returns>New player slot controller or <c>null</c>, if no slot controller could be opened.</returns>
    IPlayerSlotController OpenSlot();

    /// <summary>
    /// Releases the player of the specified <paramref name="playerSlotController"/> and closes its slot.
    /// </summary>
    /// <remarks>
    /// If the specified <paramref name="playerSlotController"/> provides the audio signal, the audio flag will be moved to
    /// the next available slot, if present.
    /// </remarks>
    /// <param name="playerSlotController">The controller of the slot to close.</param>
    void CloseSlot(IPlayerSlotController playerSlotController);

    /// <summary>
    /// Stops and releases all active players and closes their slots.
    /// </summary>
    void CloseAllSlots();

    /// <summary>
    /// Executes the given <paramref name="action"/> for each player slot controller.
    /// </summary>
    /// <param name="action">Method to execute.</param>
    void ForEach(Action<IPlayerSlotController> action);

    /// <summary>
    /// Increments the volume (see <see cref="Volume"/>).
    /// </summary>
    void VolumeUp();

    /// <summary>
    /// Decrements the volume (see <see cref="Volume"/>).
    /// </summary>
    void VolumeDown();
  }
}
