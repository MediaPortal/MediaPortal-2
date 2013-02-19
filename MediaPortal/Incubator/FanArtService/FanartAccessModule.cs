#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.IO;
using System.Net;
using System.Web;
using HttpServer;
using HttpServer.HttpModules;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  public class FanartAccessModule : HttpModule
  {
    /// <summary>
    /// Method that process the url
    /// </summary>
    /// <param name="request">Information sent by the browser about the request</param>
    /// <param name="response">Information that is being sent back to the client.</param>
    /// <param name="session">Session used to </param>
    /// <returns>true if this module handled the request.</returns>
    public override bool Process (IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      Uri uri = request.Uri;
      if (!uri.AbsolutePath.StartsWith("/FanartService"))
        return false;

      IFanArtService fanart = ServiceRegistration.Get<IFanArtService>(false);
      if (fanart == null)
        return false;

      FanArtConstants.FanArtMediaType mediaType;
      FanArtConstants.FanArtType fanArtType;
      int maxWidth;
      int maxHeight;
      if (uri.Segments.Length < 4)
        return false;
      if (!Enum.TryParse(GetSegmentWithoutSlash(uri,2), out mediaType))
        return false;
      if (!Enum.TryParse(GetSegmentWithoutSlash(uri, 3), out fanArtType))
        return false;
      string name = GetSegmentWithoutSlash(uri, 4);

      // Both values are optional
      int.TryParse(GetSegmentWithoutSlash(uri, 5), out maxWidth);
      int.TryParse(GetSegmentWithoutSlash(uri, 6), out maxHeight);

      IList<FanArtImage> files = fanart.GetFanArt(mediaType, fanArtType, name, maxWidth, maxHeight, true);
      if (files == null || files.Count == 0)
        return false;

      using (MemoryStream memoryStream = new MemoryStream(files[0].BinaryData))
        SendWholeStream(response, memoryStream, false);
      return true;
    }

    protected static string GetSegmentWithoutSlash(Uri uri, int index)
    {
      if (uri.Segments.Length < index)
        return null;
      return HttpUtility.UrlDecode(uri.Segments[index].Replace("/", string.Empty));
    }

    protected void SendWholeStream(IHttpResponse response, Stream resourceStream, bool onlyHeaders)
    {
      response.Status = HttpStatusCode.OK;
      response.ContentLength = resourceStream.Length;
      response.SendHeaders();

      if (onlyHeaders)
        return;

      Send(response, resourceStream, resourceStream.Length);
    }

    protected void Send(IHttpResponse response, Stream resourceStream, long length)
    {
      const int BUF_LEN = 8192;
      byte[] buffer = new byte[BUF_LEN];
      int bytesRead;
      while ((bytesRead = resourceStream.Read(buffer, 0, length > BUF_LEN ? BUF_LEN : (int) length)) > 0) // Don't use Math.Min since (int) length is negative for length > Int32.MaxValue
      {
        length -= bytesRead;
        response.SendBody(buffer, 0, bytesRead);
      }
    }
  }
}
