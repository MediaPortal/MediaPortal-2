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
using System.IO;
using System.Net;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.Objects.MediaLibrary;

namespace MediaPortal.Plugins.MediaServer.Protocols
{
  public class XBoxProtocolHandler : GenericAccessProtocol
  {
    private static string ALBUM_ART_TRUE = "?albumArt=true";

    public override Stream HandleResourceRequest(IHttpRequest request, IHttpResponse response, IHttpSession session, DlnaMediaItem item)
    {
      if (request.Uri.AbsolutePath.EndsWith(ALBUM_ART_TRUE, StringComparison.InvariantCultureIgnoreCase))
      {
        // Get album art from FanArt
        MediaLibraryAlbumArt albumArt = new MediaLibraryAlbumArt(item.MediaSource, item.Client);
        albumArt.Initialise();
        WebRequest imageRequest = WebRequest.Create(albumArt.Uri);
        WebResponse imageResponse = imageRequest.GetResponse();
        response.ContentType = albumArt.MimeType;
        return imageResponse.GetResponseStream();

        //if (item.MediaSource.Aspects.ContainsKey(ThumbnailLargeAspect.ASPECT_ID))
        //{
        //  //Routing the image request to cover image request
        //  var thumb = (byte[])item.MediaSource.Aspects[ThumbnailLargeAspect.ASPECT_ID].GetAttributeValue(ThumbnailLargeAspect.ATTR_THUMBNAIL);
        //  if (thumb != null && thumb.Length > 0)
        //  {
        //    response.ContentType = "image/jpeg";
        //    MemoryStream ms = new MemoryStream((byte[])thumb);
        //    return ms;
        //  }
        //}
      }
      return null;
    }
  }
}
