#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
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

        IPlayer player = ServiceRegistration.Get<IPlayerContextManager>(false)?.CurrentPlayerContext?.CurrentPlayer;
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

        return ServiceRegistration.Get<IPlayerContextManager>(false)?.PrimaryPlayerContext?.CurrentPlayer?.MediaItemTitle ?? String.Empty;
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

        IPlayer player = ServiceRegistration.Get<IPlayerContextManager>(false)?.CurrentPlayerContext?.CurrentPlayer;
        if (player != null)
        {
          IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
          return mediaPlaybackControl == null ? 0 : Convert.ToInt32(mediaPlaybackControl.CurrentTime.TotalSeconds);
        }
        return 0;
      }
    }

    /// <summary>
    /// Is the current playing item tv
    /// </summary>
    public bool IsTv
    {
      get
      {
        MediaItem mediaItem = ServiceRegistration.Get<IPlayerContextManager>(false)?.CurrentPlayerContext?.CurrentMediaItem;
        if (mediaItem == null)
          return false;

        IList<MultipleMediaItemAspect> providerAspects;
        if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out providerAspects) &&
          providerAspects.Any(pra => LiveTvMediaItem.MIME_TYPE_TV.Equals(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE), StringComparison.InvariantCultureIgnoreCase)))
          return true;

        if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out providerAspects) &&
          providerAspects.Any(pra => LiveTvMediaItem.MIME_TYPE_RADIO.Equals(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE), StringComparison.InvariantCultureIgnoreCase)))
          return true;

        return false;
      }
    }

    /// <summary>
    /// Is the player in fullscreen mode
    /// </summary>
    public bool IsFullscreen
    {
      get { return ServiceRegistration.Get<IPlayerContextManager>(false)?.IsFullscreenContentWorkflowStateActive ?? false; }
    }
  }
}
