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
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Services.Players.PCMOpenPlayerStrategy
{
  /// <summary>
  /// Default strategy for preparing new players. To understand the function, look at the code.
  /// </summary>
  public class Default : IOpenPlayerStrategy
  {
    public virtual IPlayerSlotController PrepareAudioPlayerSlotController(IPlayerManager playerManager, PlayerContextManager playerContextManager,
        bool concurrentVideo, out int currentPlayerSlotIndex)
    {
      int numActive;
      if (concurrentVideo)
      {
        numActive = playerContextManager.NumActivePlayerContexts;
        // Solve conflicts - close conflicting slots
        IPlayerContext secondaryPC = playerContextManager.GetPlayerContext(PlayerContextIndex.SECONDARY);
        if (secondaryPC != null)
          secondaryPC.Close();
        IPlayerContext pcPrimary;
        if (numActive > 0 && (pcPrimary = playerContextManager.GetPlayerContext(PlayerContextIndex.PRIMARY)) != null && pcPrimary.AVType == AVType.Audio)
          pcPrimary.Close();
      }
      else // !concurrentVideo
        // Don't enable concurrent controllers: Close all except the primary slot controller
        playerContextManager.CloseAllPlayerContexts();
      numActive = playerContextManager.NumActivePlayerContexts;
      currentPlayerSlotIndex = numActive;
      return playerManager.OpenSlot();
    }

    public virtual IPlayerSlotController PrepareVideoPlayerSlotController(IPlayerManager playerManager, PlayerContextManager playerContextManager,
        PlayerContextConcurrencyMode concurrencyMode, out bool makePrimaryPlayer, out int audioPlayerSlotIndex, out int currentPlayerSlotIndex)
    {
      IList<IPlayerContext> playerContexts = playerContextManager.PlayerContexts;
      int numActive = playerContexts.Count;
      IPlayerSlotController result;
      switch (concurrencyMode)
      {
        case PlayerContextConcurrencyMode.ConcurrentAudio:
          if (numActive > 1 && playerContexts[PlayerContextIndex.SECONDARY].AVType == AVType.Audio)
          { // The secondary slot is an audio player slot
            IPlayerContext playerContext = playerContexts[PlayerContextIndex.PRIMARY];
            result = playerContext.Revoke();
            makePrimaryPlayer = true;
            audioPlayerSlotIndex = PlayerContextIndex.SECONDARY;
          }
          else if (numActive == 1 && playerContexts[PlayerContextIndex.PRIMARY].AVType == AVType.Audio)
          { // The primary slot is an audio player slot
            result = playerManager.OpenSlot();
            makePrimaryPlayer = true;
            audioPlayerSlotIndex = PlayerContextIndex.SECONDARY;
          }
          else
          { // No audio slot available
            playerContextManager.CloseAllPlayerContexts();
            result = playerManager.OpenSlot();
            makePrimaryPlayer = true;
            audioPlayerSlotIndex = PlayerContextIndex.PRIMARY;
          }
          break;
        case PlayerContextConcurrencyMode.ConcurrentVideo:
          if (numActive >= 1 && playerContexts[PlayerContextIndex.PRIMARY].AVType == AVType.Video)
          { // The primary slot is a video player slot
            if (numActive > 1)
            {
              IPlayerContext pcSecondary = playerContextManager.GetPlayerContext(PlayerContextIndex.SECONDARY);
              pcSecondary.Close();
            }
            result = playerManager.OpenSlot();
            makePrimaryPlayer = false;
            audioPlayerSlotIndex = PlayerContextIndex.PRIMARY;
          }
          else
          {
            playerContextManager.CloseAllPlayerContexts();
            result = playerManager.OpenSlot();
            makePrimaryPlayer = true;
            audioPlayerSlotIndex = PlayerContextIndex.PRIMARY;
          }
          break;
        default:
          // Don't enable concurrent controllers: Close all except the primary slot controller
          playerContextManager.CloseAllPlayerContexts();
          result = playerManager.OpenSlot();
          makePrimaryPlayer = true;
          audioPlayerSlotIndex = PlayerContextIndex.PRIMARY;
          break;
      }
      currentPlayerSlotIndex = makePrimaryPlayer ? PlayerContextIndex.PRIMARY : PlayerContextIndex.SECONDARY;
      return result;
    }
  }
}