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
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Extensions.MediaServer.ResourceAccess;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryVideoBroadcastItem : BasicItem, IDirectoryItemThumbnail, IDirectoryVideoBroadcast
  {
    public MediaLibraryVideoBroadcastItem(string title, int channelNr, EndPointSettings client)
      : base(DlnaResourceAccessUtils.TV_CHANNEL_RESOURCE + channelNr, client)
    {
      Id = Key;
      Title = title;
      ChannelName = title;
      ChannelNr = channelNr;
      Date = DateTime.Now.ToString("yyyy-MM-dd");
      AlbumArtUrls = new List<IDirectoryAlbumArt>();

      var albumArt = new MediaLibraryAlbumArt(Guid.Empty, client);
      albumArt.Initialise(title, true);
      AlbumArtUrls.Add(albumArt);

      if (client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.All || client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.Icon)
      {
        Icon = albumArt.Uri;
      }
      if (client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.All || client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.Resource)
      {
        var albumResource = new MediaLibraryAlbumArtResource(albumArt);
        albumResource.Initialise();
        Resources.Add(albumResource);
      }
      if (client.Profile.Settings.Thumbnails.Delivery != ThumbnailDelivery.All && client.Profile.Settings.Thumbnails.Delivery != ThumbnailDelivery.AlbumArt)
      {
        AlbumArtUrls.Clear();
      }

      var resource = new MediaLibraryLiveResource(Key, channelNr, client);
      resource.Initialise();
      Resources.Add(resource);
    }

    public override string Class
    {
      get { return "object.item.videoItem.videoBroadcast"; }
    }

    public string Date { get; set; }

    public string Region { get; set; }

    public int ChannelNr { get; set; }

    public string ChannelName { get; set; }

    public string Icon { get; set; }

    public IList<IDirectoryAlbumArt> AlbumArtUrls { get; set; }
  }
}
