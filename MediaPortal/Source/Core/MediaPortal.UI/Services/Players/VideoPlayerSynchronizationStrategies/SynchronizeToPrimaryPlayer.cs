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

using MediaPortal.Common;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Services.Players.VideoPlayerSynchronizationStrategies
{
  /// <summary>
  /// Strategy implementation which synchronizes the screen control to the primary video player of the <see cref="IPlayerContextManager"/>.
  /// </summary>
  public class SynchronizeToPrimaryPlayer : BaseVideoPlayerSynchronizationStrategy
  {
    protected override IVideoPlayer GetPlayerToSynchronize()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IVideoPlayer player = playerContextManager[PlayerContextIndex.PRIMARY] as IVideoPlayer;
      // Note: once the player "Ended", the PCM might still have a reference to this player (due to asynchronous message delivery),
      // so we check that the player is active. Otherwise we return null to disable synchronization with that player.
      return player == null || player.State != PlayerState.Active ? null : player;
    }
  }
}