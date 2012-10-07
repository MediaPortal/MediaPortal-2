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
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.UI.Players.Video;
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
      if (mimeType != LiveTvMediaItem.MIME_TYPE_TV && mimeType != LiveTvMediaItem.MIME_TYPE_RADIO)
        return null;
      IResourceLocator locator = mediaItem.GetResourceLocator();
      BaseDXPlayer player = mimeType == LiveTvMediaItem.MIME_TYPE_TV ? (BaseDXPlayer) new LiveTvPlayer() : new LiveRadioPlayer(true);
      try
      {
        player.SetMediaItem(locator, title);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("SlimTvPlayerBuilder: Error playing media item '{0}'", e, locator);
        player.Dispose();
        return null;
      }
      return player;
    }

    #endregion
  }
}