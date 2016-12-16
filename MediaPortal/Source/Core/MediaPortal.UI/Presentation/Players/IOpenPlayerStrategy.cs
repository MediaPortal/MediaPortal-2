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

using MediaPortal.UI.Services.Players;

namespace MediaPortal.UI.Presentation.Players
{
  public interface IOpenPlayerStrategy
  {
    /// <summary>
    /// Prepares a new audio player. The arrangement of slots and the configuration of the audio slot and current player will be done according to this
    /// strategy.
    /// </summary>
    /// <param name="playerManager">Player manager instance.</param>
    /// <param name="playerContextManager">Player context manager instance.</param>
    /// <param name="concurrentVideo">If set to <c>true</c>, an already active video player will continue to play muted.
    ///   If set to <c>false</c>, an active video player context will be deactivated.</param>
    /// <param name="currentPlayerSlotIndex"></param>
    IPlayerSlotController PrepareAudioPlayerSlotController(IPlayerManager playerManager, PlayerContextManager playerContextManager,
        bool concurrentVideo, out int currentPlayerSlotIndex);

    /// <summary>
    /// Prepares a new video player. The arrangement of slots and the configuration of the audio slot and current player will be done according to this
    /// strategy.
    /// </summary>
    /// <param name="playerManager">Player manager instance.</param>
    /// <param name="playerContextManager">Player context manager instance.</param>
    /// <param name="concurrencyMode">If set to <see cref="PlayerContextConcurrencyMode.ConcurrentAudio"/>, an already
    ///   active audio player will continue to play and the new video player context will be muted.
    ///   If set to <see cref="PlayerContextConcurrencyMode.ConcurrentVideo"/>, an already active audio player context will be
    ///   deactivated while an already active video player context will continue to play. If a video player context was
    ///   available, the video players will be arranged according to the configured open player strategy.</param>
    /// <param name="makePrimaryPlayer"></param>
    /// <param name="audioPlayerSlotIndex"></param>
    /// <param name="currentPlayerSlotIndex"></param>
    IPlayerSlotController PrepareVideoPlayerSlotController(IPlayerManager playerManager, PlayerContextManager playerContextManager,
        PlayerContextConcurrencyMode concurrencyMode, out bool makePrimaryPlayer, out int audioPlayerSlotIndex, out int currentPlayerSlotIndex);
  }
}