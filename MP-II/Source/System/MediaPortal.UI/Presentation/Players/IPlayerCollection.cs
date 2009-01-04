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
using MediaPortal.Presentation.DataObjects;

namespace MediaPortal.Presentation.Players
{
  /// <summary>
  /// abstract class for the player collection
  /// </summary>
  public delegate void OnPlaybackStoppedHandler(IPlayer player);

  public interface IPlayerCollection : IDisposable
  {

    /// <summary>
    /// release any gui dependent resources
    /// </summary>
    void ReleaseResources();

    /// <summary>
    /// realloc any gui dependent resources
    /// </summary>
    void ReallocResources();

    /// <summary>
    /// send a message to the players
    /// </summary>
    /// <param name="m">message </param>
    void OnMessage(object m);

    /// <summary>
    /// Gets the number of player.
    /// </summary>
    /// <value>The number of players.</value>
    int Count { get; }

    /// <summary>
    /// Adds the specified player.
    /// </summary>
    /// <param name="player">The player.</param>
    void Add(IPlayer player);

    /// <summary>
    /// Removes the specified player.
    /// </summary>
    /// <param name="player">The player.</param>
    void Remove(IPlayer player);

    /// <summary>
    /// Does the collection already contain the specified player.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    bool CollectionContainsPlayer(IPlayer player);

    /// <summary>
    /// Gets or sets the <see cref="MediaPortal.Presentation.Players.IPlayer"/> at the specified index.
    /// </summary>
    /// <value></value>
    IPlayer this[int index] { get; set; }

    /// <summary>
    /// Gets or sets the active players property.
    /// </summary>
    /// <value>The active players property.</value>
    Property ActivePlayersProperty { get; }

    /// <summary>
    /// Gets or sets the paused property.
    /// </summary>
    /// <value>The paused property.</value>
    Property PausedProperty { get; }

    /// <summary>
    /// Gets or sets the is muted property.
    /// </summary>
    /// <value>The is muted property.</value>
    Property IsMutedProperty { get; }
    /// <summary>
    /// Gets or sets a value indicating whether playback  is muted.
    /// </summary>
    /// <value><c>true</c> if playback is muted; otherwise, <c>false</c>.</value>
    bool IsMuted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="IPlayerCollection"/> is paused.
    /// </summary>
    /// <value><c>true</c> if paused; otherwise, <c>false</c>.</value>
    bool Paused { get; set; }

  }
}
