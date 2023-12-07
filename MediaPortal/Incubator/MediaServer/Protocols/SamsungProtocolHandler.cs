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

using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.ResourceAccess;
using System;
using System.IO;
#if NET5_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
#else
using Microsoft.Owin;
#endif

namespace MediaPortal.Extensions.MediaServer.Protocols
{
  public class SamsungProtocolHandler : GenericAccessProtocol
  {
#if NET5_0_OR_GREATER
    public override bool HandleRequest(HttpContext context, DlnaMediaItem item)
#else
    public override bool HandleRequest(IOwinContext context, DlnaMediaItem item)
#endif
    {
      bool bHandled = false;
      if (!string.IsNullOrEmpty(context.Request.Headers["getCaptionInfo.sec"]))
      {
        if (context.Request.Headers["getCaptionInfo.sec"] == "1")
        {
          Uri uri = context.Request.GetUri();
          if (uri.ToString().ToUpperInvariant().Contains("LOCALHOST"))
          {
            bHandled = true;
          }
          string mime = "";
          string type = "";
          context.Response.Headers["CaptionInfo.sec"] = DlnaResourceAccessUtils.GetSubtitleBaseURL(item.MediaItemId, item.Client, out mime, out type);
        }
      }

      if (!string.IsNullOrEmpty(context.Request.Headers["getMediaInfo.sec"]))
      {
        if (context.Request.Headers["getMediaInfo.sec"] == "1")
        {
          //TODO: How to handle multiple video streams?
          if (item.Metadata.Duration.HasValue)
          {
            context.Response.Headers["MediaInfo.sec"] = $"SEC_Duration={Convert.ToInt32(item.Metadata.Duration.Value * 1000.0)};";
          }
        }
      }
      return bHandled;
    }

#if NET5_0_OR_GREATER
    public override bool CanHandleRequest(HttpRequest request)
#else
    public override bool CanHandleRequest(IOwinRequest request)
#endif
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

#if NET5_0_OR_GREATER
    public override Stream HandleResourceRequest(HttpContext context, DlnaMediaItem item)
#else
    public override Stream HandleResourceRequest(IOwinContext context, DlnaMediaItem item)
#endif
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
