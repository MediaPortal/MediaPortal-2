#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.UI.Presentation.Players
{
  public delegate void PlayerEventDlgt(IPlayer player);

  /// <summary>
  /// Interface for communicating the current player state to the player manager.
  /// </summary>
  /// <remarks>
  /// Each player which should be run by the <see cref="IPlayerManager"/> must implement this interface.
  /// </remarks>
  public interface IPlayerEvents
  {
    /// <summary>
    /// Initializes the player event delegate methods.
    /// </summary>
    /// <param name="started">Event delegate to be called when the player was started.</param>
    /// <param name="stateReady">Event delegate to be called when the player state was initialized. This event
    /// is necessary to be a dedicated event which will be fired after the <paramref name="started"/> event because
    /// some players are not done with their state initialization when the <paramref name="started"/> event is raised.
    /// The semantics is that when this event was fired, the player is ready and all properties have sensible values.
    /// This will be used for example by the default video player which doesn't get the video size before the first
    /// picture was displayed.
    /// For a simple player, the <paramref name="started"/> and the <paramref name="stateReady"/> events can be fired
    /// directly in sequence.</param>
    /// <param name="stopped">Event delegate to be called when the player was stopped.</param>
    /// <param name="ended">Event delegate to be called when the player content has ended.</param>
    /// <param name="playbackStateChanged">Event delegate to be called when the playback state of this player
    /// changed, i.e. when it was paused, resumed or the seeking state changed.</param>
    /// <param name="playbackError">Event delegate to be called when the player was an unrecoverable
    /// error during playback.</param>
    void InitializePlayerEvents(PlayerEventDlgt started, PlayerEventDlgt stateReady, PlayerEventDlgt stopped,
        PlayerEventDlgt ended, PlayerEventDlgt playbackStateChanged, PlayerEventDlgt playbackError);

    /// <summary>
    /// Removes all player event delegates.
    /// </summary>
    void ResetPlayerEvents();
  }
}
