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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  internal class MessageNowPlaying : MessageNowPlayingBase, IMessage
  {
    public String Type
    {
      get { return "nowplaying"; }
    }
    

    public IAdditionalNowPlayingInfo MediaInfo
    {
      get
      {
        if (Helper.IsNowPlaying())
        {
          MediaItem mediaItem = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentMediaItem;

          if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
            return new NowPlayingMusic(mediaItem);
          // TODO Add Radio
          // TODO Add Dvd
          if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
            return new NowPlayingMovingPictures(mediaItem);

          if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
            return new NowPlayingSeries(mediaItem);

            if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
          return new NowPlayingVideo(mediaItem);
          // TODO: Add Tv

        }

        return null;
      }
    }

    /// <summary>
    /// Constructor. 
    /// </summary>
    public MessageNowPlaying()
    {

    }
  }
}
