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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAlbumArt : IDirectoryAlbumArt
  {
    private const int MAX_SIZE_THUMBS = 256;

    private MediaItem Item { get; set; }

    public MediaLibraryAlbumArt(MediaItem item)
    {
      Item = item;
    }

    public void Initialise()
    {
      string mediaType = Item.Aspects.ContainsKey(ImageAspect.ASPECT_ID) ? "Image" : "Undefined";

      // Using MP2's FanArtService provides access to all kind of resources, thumbnails from ML and also local fanart from filesystem
      var url = string.Format("{0}/FanartService?mediatype={1}&fanarttype=Thumbnail&name={2}&width={3}&height={4}",
        MediaLibraryResource.GetBaseResourceURL(), mediaType, Item.MediaItemId, MAX_SIZE_THUMBS, MAX_SIZE_THUMBS);

      Uri = url;
      ProfileId = "JPEG_TN";
    }

    public string Uri { get; set; }

    public string ProfileId { get; set; }
  }
}
