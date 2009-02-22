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
using MediaPortal.Core.MediaManagement.MediaProviders;

namespace MediaPortal.Presentation.Players
{
  /// <summary>
  /// Manages all active player instances in the application.
  /// </summary>
  /// <remarks>
  /// There are N slots available for players. A slot holding an active player will produce
  /// a stream. One of the active players is the primary player. The slot number of the
  /// primary player will change over time, if multiple players are active.
  /// A player playing a media item will remain in its slot during the time when it playes
  /// the media item.
  /// </remarks>
  public interface IPlayerManager : IDisposable
  {
    /// <summary>
    /// Tries to prepare a player for the media resource with the specified access information.
    /// The player will be allocated in an empty player slot, whose slot number will be returned
    /// if the preparation was successful.
    /// </summary>
    /// <param name="provider">Media provider which delivers the content to be played.</param>
    /// <param name="path">Path of the content to be played.</param>
    /// <param name="mimeType">MimeType of the content to be played, if available. Else, this
    /// parameter should be set to <c>null</c>.</param>
    /// <param name="playerSlot">Returns the slot number of the new player, if the preparation was
    /// successful.</param>
    /// <returns>Instance of the new player, or <c>null</c>, if the preparation was not successful,
    /// i.e. the content cannot be played with the currently available players and/or resources.</returns>
    IPlayer PreparePlayer(IMediaProvider provider, string path, string mimeType, out int playerSlot);

    /// <summary>
    /// Stops the player in the specified <paramref name="playerSlot"/>, releases it and removes
    /// it from the collection of active players.
    /// </summary>
    void ReleasePlayer(int playerSlot);

    /// <summary>
    /// Stops and releases all active players.
    /// </summary>
    void ReleaseAllPlayers();

    /// <summary>
    /// Releases any GUI dependent resources in all players.
    /// </summary>
    /// <summary>
    /// This method can be called to temporary prevent all visual output from all players.
    /// Call <see cref="ReallocGUIResources"/> to resume visual output.
    /// </summary>
    void ReleaseGUIResources();

    /// <summary>
    /// (Re)allocs any GUI dependent resources in all active players.
    /// </summary>
    void ReallocGUIResources();

    /// <summary>
    /// Returns the slot number of the currently primary player.
    /// </summary>
    int PrimaryPlayer { get; }

    /// <summary>
    /// Returns the number of active players.
    /// </summary>
    /// <remarks>
    /// A player is active from its preparation (see <see cref="PreparePlayer"/>) until its release
    /// (see <see cref="ReleasePlayer"/>).
    /// </remarks>
    int NumActivePlayers { get; }

    /// <summary>
    /// Gets the player at the specified player slot, or <c>null</c>, if there is no player in the
    /// specified slot.
    /// </summary>
    IPlayer this[int slot] { get; }
  }
}
