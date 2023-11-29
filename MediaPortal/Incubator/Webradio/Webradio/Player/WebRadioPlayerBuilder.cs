#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using System.IO;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Players.BassPlayer;
using MediaPortal.UI.Players.BassPlayer.PlayerComponents;
using MediaPortal.UI.Presentation.Players;
using Un4seen.Bass;

namespace Webradio.Player
{
  public class WebRadioPlayerBuilder : IPlayerBuilder
  {
    private readonly string _pluginDirectory;

    public WebRadioPlayerBuilder()
    {
      var bassDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BassPlayer");
      _pluginDirectory = bassDir; // Points to root of BassPlayer plugin!
    }

    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(MediaItem mediaItem)
    {
      string mimeType;
      string title;
      if (!mediaItem.GetPlayData(out mimeType, out title))
        return null;

      // Our special player is only used for our mimetype
      if (mimeType != WebRadioPlayerHelper.WEBRADIO_MIMETYPE)
        return null;

      // Set back to valid audio mimetype
      mimeType = "audio/stream";

      var locator = mediaItem.GetResourceLocator();
      if (InputSourceFactory.CanPlay(locator, mimeType))
      {
        BassPlayer player = new WebRadioBassPlayer(_pluginDirectory);

        // Config the BASSPlayer to play also .pls and .m3u
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_NET_PLAYLIST, 1);
        try
        {
          player.SetMediaItem(mediaItem);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("WebRadioBassPlayer: Error playing media item '{0}'", e, locator);
          player.Dispose();
          return null;
        }

        return player;
      }

      return null;
    }

    #endregion
  }
}
