#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core.MediaManagement;
using MediaPortal.UI.Presentation.Players;

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
      // TODO: Go on with extensions mapping
      // TODO: Go on with mime types mapping
    }

    protected static Type GetPlayerTypeForMediaItem(ILocalFsResourceAccessor accessor, string mimeType)
    {
      string path = accessor.ResourcePathName;
      string extension = Path.GetExtension(path).ToLowerInvariant();
      Type playerType;
      if (mimeType != null)
        if (MIMETYPES2PLAYER.TryGetValue(mimeType.ToLowerInvariant(), out playerType))
          return playerType;
        else
          return null;
      if (EXTENSIONS2PLAYER.TryGetValue(extension, out playerType))
        return playerType;
      return null;
    }

    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(IResourceLocator locator, string mimeType)
    {
      ILocalFsResourceAccessor accessor = locator.CreateLocalFsAccessor();
      Type playerType = GetPlayerTypeForMediaItem(accessor, mimeType);
      if (playerType == null)
        return null;
      IInitializablePlayer player = (IInitializablePlayer) Activator.CreateInstance(playerType);
      try
      {
        player.SetMediaItemLocator(locator);
      }
      catch (Exception)
      { // The file might be broken, so the player wasn't able to play it
        if (player is IDisposable)
          ((IDisposable) player).Dispose();
        player = null;
      }
      return (IPlayer) player;
    }

    #endregion
  }
}