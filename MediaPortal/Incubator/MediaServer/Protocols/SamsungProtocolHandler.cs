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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpServer;
using MediaPortal.Common.MediaManagement;
using HttpServer.Sessions;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.Objects.MediaLibrary;
using MediaPortal.Plugins.MediaServer.ResourceAccess;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.IO;
using MediaPortal.Common;

namespace MediaPortal.Plugins.MediaServer.Protocols
{
  public class SamsungProtocolHandler : GenericAccessProtocol
  {
    public override bool HandleRequest(IHttpRequest request, IHttpResponse response, IHttpSession session, DlnaMediaItem item)
    {
      bool bHandled = false;
      if (!string.IsNullOrEmpty(request.Headers["getCaptionInfo.sec"]))
      {
        if (request.Headers["getCaptionInfo.sec"] == "1")
        {
          if (request.Uri.ToString().ToUpperInvariant().Contains("LOCALHOST"))
          {
            bHandled = true;
          }
          string mime = "";
          string type = "";
          response.AddHeader("CaptionInfo.sec", DlnaResourceAccessUtils.GetSubtitleBaseURL(item.MediaSource, item.Client, out mime, out type));
        }
      }

      if (!string.IsNullOrEmpty(request.Headers["getMediaInfo.sec"]))
      {
        if (request.Headers["getMediaInfo.sec"] == "1")
        {
          object durationSeconds = item.MediaSource.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_DURATION);
          if (durationSeconds != null)
          {
            response.AddHeader("MediaInfo.sec", string.Format("SEC_Duration={0};", Convert.ToInt32(durationSeconds) * 1000));
          }
        }
      }
      return bHandled;
    }

    public override bool CanHandleRequest(IHttpRequest request)
    {
      if (!string.IsNullOrEmpty(request.Headers["getCaptionInfo.sec"]))
      {
        if (request.Headers["getCaptionInfo.sec"] == "1")
        {
          return true;
        }
      }
      if (!string.IsNullOrEmpty(request.Headers["getMediaInfo.sec"]))
      {
        if (request.Headers["getMediaInfo.sec"] == "1")
        {
          return true;
        }
      }
      return false;
    }

    public override Stream HandleResourceRequest(IHttpRequest request, IHttpResponse response, IHttpSession session, DlnaMediaItem item)
    {
      //if (item.DlnaProfile == "JPEG_SM")
      //{
      //  if (item.MediaSource.Aspects.ContainsKey(ThumbnailLargeAspect.ASPECT_ID))
      //  {
      //    //Routing the image request to cover image request
      //    var thumb = (byte[])item.MediaSource.Aspects[ThumbnailLargeAspect.ASPECT_ID].GetAttributeValue(ThumbnailLargeAspect.ATTR_THUMBNAIL);
      //    if (thumb != null && thumb.Length > 0)
      //    {
      //      response.ContentType = "image/jpeg";
      //      MemoryStream ms = new MemoryStream((byte[])thumb);
      //      return ms;
      //    }
      //  }
      //}
      return null;
    }
  }
}
