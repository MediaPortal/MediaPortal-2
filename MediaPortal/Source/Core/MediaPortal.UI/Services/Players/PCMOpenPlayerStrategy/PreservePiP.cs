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
  public class PreservePiP : Default
  {
    public override void OpenVideoPlayer(IPlayerManager playerManager, IList<IPlayerContext> playerContexts, PlayerContextConcurrencyMode concurrencyMode, Guid mediaModuleId,
        out IPlayerSlotController slotController, ref int audioSlotIndex, ref int currentPlayerIndex)
    {
        int numActive = playerContexts.Count;
        switch (concurrencyMode)
        {
          case PlayerContextConcurrencyMode.ConcurrentVideo:
            // If we have 2 concurrent playing videos, a new video will replace the primary slot, PiP remains untouched playing in background.
            // If we have only one video playing, the new video will be added to secondary slot and the PiP players will be switched: The old
            // playing video will be moved to PiP, the new will get fullscreen video.
            if (numActive >= 1 && playerContexts[0].AVType == AVType.Video)
            { // The primary slot is a video player slot
              int newCurrentPlayerIndex = PlayerManagerConsts.SECONDARY_SLOT;
              if (numActive > 1)
              {
                playerManager.SwitchSlots();
                playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
                newCurrentPlayerIndex = PlayerManagerConsts.PRIMARY_SLOT;
              }

              int slotIndex;
              playerManager.OpenSlot(out slotIndex, out slotController);
              audioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
              currentPlayerIndex = newCurrentPlayerIndex;
              playerManager.SwitchSlots();
              return;
            }
            break;
        }
      base.OpenVideoPlayer(playerManager, playerContexts, concurrencyMode, mediaModuleId, out slotController, ref audioSlotIndex, ref currentPlayerIndex);
    }
  }
}