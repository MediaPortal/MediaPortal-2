#region Copyright (C) 2007-2015 Team MediaPortal

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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.MediaServer.ResourceAccess;
using System;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAudioBroadcastItem : MediaLibraryItem, IDirectoryAudioBroadcast
  {
    public MediaLibraryAudioBroadcastItem(MediaItem item, string title, int channelNr, EndPointSettings client)
      : base(item, client)
    {
      Title = title;
      ChannelName = title;
      ChannelNr = channelNr;

      if (AlbumArtUrls.Count > 0)
      {
        AlbumArtUrls[0].Uri = DlnaResourceAccessUtils.GetChannelLogoBaseURL(title, client, false);
        if (client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.All || client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.Icon)
        {
          Icon = AlbumArtUrls[0].Uri;
        }
        if (client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.All || client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.Resource)
        {
          var albumResource = new MediaLibraryAlbumArtResource((MediaLibraryAlbumArt)AlbumArtUrls[0]);
          albumResource.Initialise();
          Resources.Add(albumResource);
        }
        if (client.Profile.Settings.Thumbnails.Delivery != ThumbnailDelivery.All && client.Profile.Settings.Thumbnails.Delivery != ThumbnailDelivery.AlbumArt)
        {
          AlbumArtUrls.Clear();
        }
      }

      var resource = new MediaLibraryLiveResource(item, client);
      resource.Initialise();
      Resources.Add(resource);
    }

    public override string Class
    {
      get { return "object.item.audioItem.audioBroadcast"; }
    }

    public string Date { get; set; }

    public string Region { get; set; }

    public string RadioCallSign { get; set; }

    public string RadioStationId { get; set; }

    public string RadioBand { get; set; }

    public int ChannelNr { get; set; }

    public string ChannelName { get; set; }

    public string Icon { get; set; }
  }
}
