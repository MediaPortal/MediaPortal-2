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

namespace MediaPortal.UI.Services.Players.PCMOpenPlayerStrategy
{
  /// <summary>
  /// Default strategy for preparing new players. For an understanding of the function, see the code.
  /// </summary>
  public class Default : IOpenPlayerStrategy
  {
    public virtual void PrepareAudioPlayer(IPlayerManager playerManager, IList<IPlayerContext> playerContexts, bool concurrentVideo, Guid mediaModuleId,
        out IPlayerSlotController slotController, ref int audioSlotIndex, ref int currentPlayerIndex)
    {
      if (concurrentVideo)
      {
        int numActive = playerManager.NumActiveSlots;
        // Solve conflicts - close conflicting slots
        if (numActive > 1)
          playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
        IPlayerContext pc;
        if (numActive > 0 && (pc = playerContexts[PlayerManagerConsts.PRIMARY_SLOT]) != null && pc.AVType == AVType.Audio)
          playerManager.CloseSlot(PlayerManagerConsts.PRIMARY_SLOT);
      }
      else // !concurrentVideo
        // Don't enable concurrent controllers: Close all except the primary slot controller
        playerManager.CloseAllSlots(true);
      // Open new slot
      int slotIndex;
      playerManager.OpenSlot(out slotIndex, out slotController);
      audioSlotIndex = slotController.SlotIndex;
      currentPlayerIndex = slotIndex;
    }

    public virtual void PrepareVideoPlayer(IPlayerManager playerManager, IList<IPlayerContext> playerContexts, PlayerContextConcurrencyMode concurrencyMode, Guid mediaModuleId,
        out IPlayerSlotController slotController, ref int audioSlotIndex, ref int currentPlayerIndex)
    {
        int numActive = playerContexts.Count;
        int slotIndex;
        switch (concurrencyMode)
        {
          case PlayerContextConcurrencyMode.ConcurrentAudio:
            if (numActive > 1 && playerContexts[1].AVType == AVType.Audio)
            { // The secondary slot is an audio player slot
              slotIndex = PlayerManagerConsts.PRIMARY_SLOT;
              IPlayerContext pc = playerContexts[0];
              pc.Reset(); // Necessary to reset the player context to disable the auto close function (pc.CloseWhenFinished)
              playerManager.ResetSlot(slotIndex, out slotController);
              audioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
            }
            else if (numActive == 1 && playerContexts[0].AVType == AVType.Audio)
            { // The primary slot is an audio player slot
              playerManager.OpenSlot(out slotIndex, out slotController);
              // Make new video slot the primary slot
              playerManager.SwitchSlots();
              audioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
            }
            else
            { // No audio slot available
              playerManager.CloseAllSlots(true);
              playerManager.OpenSlot(out slotIndex, out slotController);
              audioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            }
            break;
          case PlayerContextConcurrencyMode.ConcurrentVideo:
            if (numActive >= 1 && playerContexts[0].AVType == AVType.Video)
            { // The primary slot is a video player slot
              if (numActive > 1)
                playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
              playerManager.OpenSlot(out slotIndex, out slotController);
              audioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            }
            else
            {
              playerManager.CloseAllSlots(true);
              playerManager.OpenSlot(out slotIndex, out slotController);
              audioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            }
            break;
          default:
            // Don't enable concurrent controllers: Close all except the primary slot controller
            playerManager.CloseAllSlots(true);
            playerManager.OpenSlot(out slotIndex, out slotController);
            audioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            break;
        }
      currentPlayerIndex = slotIndex;
    }
  }
}