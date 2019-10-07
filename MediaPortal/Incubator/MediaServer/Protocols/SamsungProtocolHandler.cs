﻿#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.ResourceAccess;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;

namespace MediaPortal.Extensions.MediaServer.Protocols
{
  public class SamsungProtocolHandler : GenericAccessProtocol
  {
    public override bool HandleRequest(IOwinContext context, DlnaMediaItem item)
    {
      bool bHandled = false;
      if (!string.IsNullOrEmpty(context.Request.Headers["getCaptionInfo.sec"]))
      {
        if (context.Request.Headers["getCaptionInfo.sec"] == "1")
        {
          if (context.Request.Uri.ToString().ToUpperInvariant().Contains("LOCALHOST"))
          {
            bHandled = true;
          }
          string mime = "";
          string type = "";
          context.Response.Headers["CaptionInfo.sec"] = DlnaResourceAccessUtils.GetSubtitleBaseURL(item.MediaSource, item.Client, out mime, out type);
        }
      }

      if (!string.IsNullOrEmpty(context.Request.Headers["getMediaInfo.sec"]))
      {
        if (context.Request.Headers["getMediaInfo.sec"] == "1")
        {
          //TODO: How to handle multiple video streams?
          if (MediaItemAspect.TryGetAttribute(item.MediaSource.Aspects, VideoStreamAspect.ATTR_DURATION, out List<long> durations))
          {
            context.Response.Headers["MediaInfo.sec"] = $"SEC_Duration={Convert.ToInt32(durations.First()) * 1000};";
          }
        }
      }
      return bHandled;
    }

    public override bool CanHandleRequest(IOwinRequest request)
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

    public override Stream HandleResourceRequest(IOwinContext context, DlnaMediaItem item)
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
