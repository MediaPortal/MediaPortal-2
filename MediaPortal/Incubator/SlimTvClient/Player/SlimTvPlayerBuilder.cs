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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  /// <summary>
  /// Player builder for Slim Tv player.
  /// </summary>
  public class SlimTvPlayerBuilder : IPlayerBuilder
  {
    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(MediaItem mediaItem)
    {
      string mimeType;
      string title;
      if (!mediaItem.GetPlayData(out mimeType, out title))
        return null;
      if (mimeType != "video/livetv")
        return null;
      IResourceLocator locator = mediaItem.GetResourceLocator();
      LiveTvPlayer player = new LiveTvPlayer();
      try
      {
        player.SetMediaItem(locator, title);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("LiveTvPlayer: Error playing media item '{0}'", e, locator);
        player.Dispose();
        return null;
      }
      return player;
    }

    #endregion
  }
}