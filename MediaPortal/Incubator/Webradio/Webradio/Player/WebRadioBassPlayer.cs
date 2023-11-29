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
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Players.BassPlayer;
using MediaPortal.UI.Presentation.Players;

namespace Webradio.Player
{
  /// <summary>
  /// Acts as a wrapper around <see cref="BassPlayer"/> to implement <see cref="IUIContributorPlayer"/>.
  /// </summary>
  public class WebRadioBassPlayer : BassPlayer, IUIContributorPlayer
  {
    public WebRadioBassPlayer(string playerMainDirectory)
      : base(playerMainDirectory)
    {
    }

    public Type UIContributorType => typeof(WebRadioUIContributor);

    protected override bool GetMediaItemPlayData(MediaItem mediaItem, out string mimeType, out string title)
    {
      // Change the mimeType back to "audio/..." to allow input source factory building a valid source.
      // While we could use and "audio/..." mimeType from beginning, we could not control if the player builder prefers
      // the WebRadioBassPlayer over the default BassPlayer.
      var result = base.GetMediaItemPlayData(mediaItem, out mimeType, out title);
      if (mimeType == WebRadioPlayerHelper.WEBRADIO_MIMETYPE)
        mimeType = "audio/stream";
      return result;
    }
  }
}
