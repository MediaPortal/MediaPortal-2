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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Extensions.MediaServer.ResourceAccess;
using MediaPortal.Plugins.Transcoding.Service;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAlbumArt : IDirectoryAlbumArt
  {
    public MediaItem Item { get; private set; }
    public EndPointSettings Client { get; private set; }

    public MediaLibraryAlbumArt(MediaItem item, EndPointSettings client)
    {
      Item = item;
      Client = client;
    }

    public void Initialise()
    {
      //bool useFanart = false;
      //if (Item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      //{
      //  useFanart = true;
      //}
      //else if (Item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      //{
      //  useFanart = true;
      //}

      bool useFanart = true;
      if (useFanart)
      {
        string mediaType = Item.Aspects.ContainsKey(ImageAspect.ASPECT_ID) ? "Image" : "Undefined";

        Console.WriteLine("Item {0}", Item != null ? Item.MediaItemId.ToString() : "NULL");
        Console.WriteLine("Client {0}", Client != null ? Client.GetHashCode().ToString() : "NULL");
        Console.WriteLine("Client profile {0}", Client != null && Client.Profile != null ? Client.Profile.GetHashCode().ToString() : "NULL");
        Console.WriteLine("Client profile settings {0}", Client != null && Client.Profile != null && Client.Profile.Settings != null ? Client.Profile.Settings.GetHashCode().ToString() : "NULL");
        Console.WriteLine("Client profile settings thumbnails {0}", Client != null && Client.Profile != null && Client.Profile.Settings != null && Client.Profile.Settings.Thumbnails != null ? Client.Profile.Settings.Thumbnails.GetHashCode().ToString() : "NULL");

        // Using MP2's FanArtService provides access to all kind of resources, thumbnails from ML and also local fanart from filesystem
        var url = string.Format("{0}/FanartService?mediatype={1}&fanarttype=Thumbnail&name={2}&width={3}&height={4}",
          DlnaResourceAccessUtils.GetBaseResourceURL(), mediaType, Item.MediaItemId, Client.Profile.Settings.Thumbnails.MaxWidth, Client.Profile.Settings.Thumbnails.MaxHeight);
        Uri = url;
      }
      else
      {
        // Using MP2's thumbnails
        var url = string.Format("{0}{1}?aspect={2}&width={3}&height={4}",
          DlnaResourceAccessUtils.GetBaseResourceURL(), DlnaResourceAccessUtils.GetResourceUrl(Item.MediaItemId), "THUMBNAIL", Client.Profile.Settings.Thumbnails.MaxWidth, Client.Profile.Settings.Thumbnails.MaxHeight);
        Uri = url;
      }

      string profileId = "";
      string mimeType = "image/jpeg";
      DlnaProfiles.FindCompatibleProfile(Client, DlnaProfiles.ResolveImageProfile(ImageContainer.Jpeg, Client.Profile.Settings.Thumbnails.MaxWidth, Client.Profile.Settings.Thumbnails.MaxHeight),
        ref profileId, ref mimeType);

      ProfileId = profileId;
      MimeType = mimeType;
    }

    public string Uri { get; set; }

    public string ProfileId { get; set; }

    public string MimeType { get; set; }

  }
}
