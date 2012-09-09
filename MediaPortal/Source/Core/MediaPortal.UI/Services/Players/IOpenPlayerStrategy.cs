#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Services.Players
{
  public interface IOpenPlayerStrategy
  {
    /// <summary>
    /// Prepares a new audio player. The arrangement of slots and the configuration of the audio slot and current player will be done according to this
    /// strategy.
    /// </summary>
    /// <param name="playerManager">The player manager.</param>
    /// <param name="playerContexts">List of active player contexts. The order of the player contexts will be from <see cref="PlayerManagerConsts.PRIMARY_SLOT"/>
    /// to <see cref="PlayerManagerConsts.SECONDARY_SLOT"/>.</param>
    /// <param name="concurrentVideo">If set to <c>true</c>, an already active video player will continue to play muted.
    /// If set to <c>false</c>, an active video player context will be deactivated.</param>
    /// <param name="mediaModuleId">Id of the requesting media module. The caller will stick the new player context to the specified module.</param>
    /// <param name="slotController">Returns the slot controller which was prepared.</param>
    /// <param name="audioSlotIndex">Returns the index of the audio slot to be set according to this strategy.</param>
    /// <param name="currentPlayerIndex">Returns the index of the current player to be set according to this strategy.</param>
    void PrepareAudioPlayer(IPlayerManager playerManager, IList<IPlayerContext> playerContexts, bool concurrentVideo, Guid mediaModuleId,
        out IPlayerSlotController slotController, ref int audioSlotIndex, ref int currentPlayerIndex);

    /// <summary>
    /// Prepares a new video player. The arrangement of slots and the configuration of the audio slot and current player will be done according to this
    /// strategy.
    /// </summary>
    /// <param name="playerManager">The player manager.</param>
    /// <param name="playerContexts">List of active player contexts. The order of the player contexts will be from <see cref="PlayerManagerConsts.PRIMARY_SLOT"/>
    /// to <see cref="PlayerManagerConsts.SECONDARY_SLOT"/>.</param>
    /// <param name="concurrencyMode">If set to <see cref="PlayerContextConcurrencyMode.ConcurrentAudio"/>, an already
    /// active audio player will continue to play and the new video player context will be muted.
    /// If set to <see cref="PlayerContextConcurrencyMode.ConcurrentVideo"/>, an already active audio player context will be
    /// deactivated while an already active video player context will continue to play. If a video player context was
    /// available, the video players will be arranged according to the configured open player strategy.</param>
    /// <param name="mediaModuleId">Id of the requesting media module. The caller will stick the new player context to the specified module.</param>
    /// <param name="slotController">Returns the slot controller which was prepared.</param>
    /// <param name="audioSlotIndex">Returns the index of the audio slot to be set according to this strategy.</param>
    /// <param name="currentPlayerIndex">Returns the index of the current player to be set according to this strategy.</param>
    void PrepareVideoPlayer(IPlayerManager playerManager, IList<IPlayerContext> playerContexts, PlayerContextConcurrencyMode concurrencyMode, Guid mediaModuleId,
        out IPlayerSlotController slotController, ref int audioSlotIndex, ref int currentPlayerIndex);
  }
}