#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.UI.Presentation.Players;
using Ui.Players.Video.Interfaces;

namespace Ui.Players.Video
{
  /// <summary>
  /// Player builder for all video players of the VideoPlayers plugin.
  /// </summary>
  public class VideoPlayerBuilder : IPlayerBuilder
  {
    public static IDictionary<string, Type> EXTENSIONS2PLAYER = new Dictionary<string, Type>();
    public static IDictionary<string, Type> MIMETYPES2PLAYER = new Dictionary<string, Type>();

    static VideoPlayerBuilder()
    {
      EXTENSIONS2PLAYER.Add(".avi", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".mpg", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".mpeg", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".ts", typeof(TsVideoPlayer));
      EXTENSIONS2PLAYER.Add(".mp4", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".mkv", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".flv", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".vob", typeof(DvdPlayer));
      EXTENSIONS2PLAYER.Add(".ifo", typeof(DvdPlayer));
      // TODO: Go on with extensions mapping

      MIMETYPES2PLAYER.Add("video/x-ms-wmv", typeof(VideoPlayer));
      MIMETYPES2PLAYER.Add("video/mp2t", typeof(TsVideoPlayer));
      MIMETYPES2PLAYER.Add("video/dvd", typeof(DvdPlayer));
      // TODO: Go on with mime types mapping
    }

    protected static Type GetPlayerTypeForMediaItem(IResourceLocator locator, string mimeType)
    {
      string path = locator.NativeResourcePath.LastPathSegment.Path;
      string extension = Path.GetExtension(path).ToLowerInvariant();

      Type playerType;
      if (mimeType != null && MIMETYPES2PLAYER.TryGetValue(mimeType.ToLowerInvariant(), out playerType))
        return playerType;
      // 2nd chance: if no mimetype match, try extensions
      if (EXTENSIONS2PLAYER.TryGetValue(extension, out playerType))
        return playerType;
      return null;
    }

    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(IResourceLocator locator, string mimeType)
    {
      Type playerType = GetPlayerTypeForMediaItem(locator, mimeType);
      if (playerType == null)
        return null;
      IInitializablePlayer player = (IInitializablePlayer) Activator.CreateInstance(playerType);
      try
      {
        player.SetMediaItemLocator(locator);
      }
      catch (Exception e)
      { // The file might be broken, so the player wasn't able to play it
        ServiceRegistration.Get<ILogger>().Warn("{0}: Unable to play file '{1}'", e, playerType, locator);
        if (player is IDisposable)
          ((IDisposable) player).Dispose();
        player = null;
      }
      return (IPlayer) player;
    }

    #endregion
  }
}