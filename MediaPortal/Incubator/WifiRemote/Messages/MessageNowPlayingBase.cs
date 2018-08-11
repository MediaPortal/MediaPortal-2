#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  class MessageNowPlayingBase
  {
    /// <summary>
    /// Duration of the media in seconds
    /// </summary>
    public int Duration
    {
      get
      {
        if (!Helper.IsNowPlaying())
        {
          return 0;
        }

        IPlayer player = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer;
        if (player != null)
        {
          IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
          return mediaPlaybackControl == null ? 0 : Convert.ToInt32(mediaPlaybackControl.Duration.TotalSeconds);
        }
        return 0;
      }
    }

    /// <summary>
    /// The filename of the currently playing item
    /// </summary>
    public String File
    {
      get
      {
        if (!Helper.IsNowPlaying())
        {
          return String.Empty;
        }

        return ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.CurrentPlayer.MediaItemTitle;
      }
    }

    /// <summary>
    /// Current position in the file in seconds
    /// </summary>
    public int Position
    {
      get
      {
        if (!Helper.IsNowPlaying())
        {
          return 0;
        }

        IPlayer player = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer;
        if (player != null)
        {
          IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
          return mediaPlaybackControl == null ? 0 : Convert.ToInt32(mediaPlaybackControl.CurrentTime.TotalSeconds);
        }
        return 0;
      }
    }

    // TODO: reimplement
    /// <summary>
    /// Is the current playing item tv
    /// </summary>
    public bool IsTv
    {
      get
      {
        return false;
      }
    }

    /// <summary>
    /// Is the player in fullscreen mode
    /// </summary>
    public bool IsFullscreen
    {
      get { return ServiceRegistration.Get<IPlayerContextManager>().IsFullscreenContentWorkflowStateActive; }
    }
  }
}
