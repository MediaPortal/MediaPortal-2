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
  /// Player open strategy which works the same as the <see cref="Default"/> strategy except for the case when at least one video player is active and
  /// another video player is requested in concurrency mode <see cref="PlayerContextConcurrencyMode.ConcurrentVideo"/>. In this case,
  /// the new video will replace the primary slot, PiP remains untouched playing in background.
  /// If we have only one video playing, the old video is be moved to PiP while the new video gets the primary (fullscreen) video.
  /// </summary>
  public class PreservePiP : Default
  {
    public override IPlayerSlotController PrepareVideoPlayerSlotController(IPlayerManager playerManager, PlayerContextManager playerContextManager,
        PlayerContextConcurrencyMode concurrencyMode, out bool makePrimaryPlayer, out int audioPlayerSlotIndex, out int currentPlayerSlotIndex)
    {
        int numActive = playerContextManager.NumActivePlayerContexts;
        switch (concurrencyMode)
        {
          case PlayerContextConcurrencyMode.ConcurrentVideo:
            IList<IPlayerContext> playerContexts = playerContextManager.PlayerContexts;
            if (numActive >= 1 && playerContexts[PlayerContextIndex.PRIMARY].AVType == AVType.Video)
            { // The primary slot is a video player slot
              IPlayerSlotController result;
              if (numActive == 1)
              {
                result = playerManager.OpenSlot();
                makePrimaryPlayer = true;
              }
              else // numActive > 1
              {
                IPlayerContext pc = playerContexts[PlayerContextIndex.PRIMARY];
                result = pc.Revoke(); // Necessary to revoke the player context to disable the auto close function (pc.CloseWhenFinished)
                makePrimaryPlayer = true;
              }

              audioPlayerSlotIndex = PlayerContextIndex.PRIMARY;
              currentPlayerSlotIndex = PlayerContextIndex.PRIMARY;
              return result;
            }
            break;
        }
      // All other cases are handled the same as in the default player open strategy
      return base.PrepareVideoPlayerSlotController(playerManager, playerContextManager, concurrencyMode, out makePrimaryPlayer,
          out audioPlayerSlotIndex, out currentPlayerSlotIndex);
    }
  }
}