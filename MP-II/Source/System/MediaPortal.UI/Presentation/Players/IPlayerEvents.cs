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

namespace MediaPortal.Presentation.Players
{
  public delegate void PlayerEventDlgt(IPlayer player, int playerSlot);

  /// <summary>
  /// Interface for communicating the current player state to the player manager.
  /// </summary>
  public interface IPlayerEvents
  {
    /// <summary>
    /// Initializes the player with its communication handle (the <paramref name="playerSlot"/>). The given
    /// player slot must be used for the event communication of the state events.
    /// </summary>
    /// <param name="playerSlot">The player slot to be used in the future state events.</param>
    /// <param name="started">Event delegate to be called when the player was started.</param>
    /// <param name="stopped">Event delegate to be called when the player was stopped.</param>
    /// <param name="ended">Event delegate to be called when the player content has ended.</param>
    /// <param name="paused">Event delegate to be called when the player was paused.</param>
    /// <param name="resumed">Event delegate to be called when the player was resumed.</param>
    void InitializePlayerEvents(int playerSlot, PlayerEventDlgt started, PlayerEventDlgt stopped,
        PlayerEventDlgt ended, PlayerEventDlgt paused, PlayerEventDlgt resumed);

    /// <summary>
    /// Removes all player events and resets the player slot.
    /// </summary>
    void ResetPlayerEvents();
  }
}