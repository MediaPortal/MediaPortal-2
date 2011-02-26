#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Players.Video
{
  /// <summary>
  /// Player builder for all video players of the VideoPlayers plugin.
  /// </summary>
  public class VideoPlayerBuilder : IPlayerBuilder
  {
    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(IResourceLocator locator, string mimeType)
    {
      Type playerType = PlayerRegistration.GetPlayerTypeForMediaItem(locator, mimeType);
      if (playerType == null)
        return null;
      IInitializablePlayer player = (IInitializablePlayer) Activator.CreateInstance(playerType);
      try
      {
        player.SetMediaItemLocator(locator);
      }
      catch (Exception e)
      { // The file might be broken, so the player wasn't able to play it
        ServiceRegistration.Get<ILogger>().Warn("{0}: Unable to play '{1}'", e, playerType, locator);
        if (player is IDisposable)
          ((IDisposable) player).Dispose();
        player = null;
      }
      return (IPlayer) player;
    }

    #endregion
  }
}