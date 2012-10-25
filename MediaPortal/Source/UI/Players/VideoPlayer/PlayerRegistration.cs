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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Players.Video
{
  public class PlayerRegistration
  {
    /// <summary>
    /// List of (lower-case!) extensions which are played with one of our players.
    /// TODO: Move to settings like in ImagePlayer and BassPlayer.
    /// </summary>
    protected static IDictionary<string, Type> EXTENSIONS2PLAYER = new Dictionary<string, Type>();
    protected static IDictionary<string, Type> MIMETYPES2PLAYER = new Dictionary<string, Type>();

    static PlayerRegistration()
    {
      EXTENSIONS2PLAYER.Add(".avi", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".mpg", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".mpeg", typeof(VideoPlayer));

      // TsVideoPlayer depends on the MP1 TsReader.ax component, we distribute our own copy of this file and load it without registration.
      EXTENSIONS2PLAYER.Add(".ts", typeof (TsVideoPlayer));

      EXTENSIONS2PLAYER.Add(".mts", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".m2ts", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".dvr-ms", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".wtv", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".mp4", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".mkv", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".mov", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".flv", typeof(VideoPlayer));
      EXTENSIONS2PLAYER.Add(".vob", typeof(DvdPlayer));
      EXTENSIONS2PLAYER.Add(".ifo", typeof(DvdPlayer));
      // TODO: Go on with extensions mapping

      // mimetypes are mapped in plugin.xml
    }

    internal static void AddMimeTypeMapping(string mimeType, Type playerType)
    {
      MIMETYPES2PLAYER.Add(mimeType, playerType);
    }

    public static Type GetPlayerTypeForMediaItem(IResourceLocator locator, string mimeType)
    {
      Type playerType;
	    // When a mimetype is set, try to get the player type for it
      if (mimeType != null && MIMETYPES2PLAYER.TryGetValue(mimeType.ToLowerInvariant(), out playerType))
        return playerType;

	    // 2nd chance: If no mimetype matches, try extension
	    string path = locator.NativeResourcePath.LastPathSegment.Path;
	    string extension = StringUtils.TrimToEmpty(ProviderPathHelper.GetExtension(path)).ToLowerInvariant();

      if (EXTENSIONS2PLAYER.TryGetValue(extension, out playerType))
        return playerType;
      return null;
    }
  }
}