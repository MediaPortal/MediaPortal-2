#region Copyright (C) 2007-2017 Team MediaPortal

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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Network;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using Microsoft.Owin;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  public class FanartAccessModule : OwinMiddleware
  {

    public FanartAccessModule(OwinMiddleware next) : base(next)
    {
    }

    /// <summary>
    /// Method that process the url
    /// </summary>
    public override async Task Invoke(IOwinContext context)
    {
      var request = context.Request;
      var response = context.Response;
      Uri uri = request.Uri;
      if (!uri.AbsolutePath.StartsWith(ResourceHttpAccessUrlUtils.RESOURCE_SERVER_BASE_PATH) || !uri.AbsolutePath.Contains("/FanartService"))
      {
        await Next.Invoke(context);
        return;
      }

      IFanArtService fanart = ServiceRegistration.Get<IFanArtService>(false);
      if (fanart == null)
        return;

      string mediaType = request.Query["mediatype"];
      string fanArtType = request.Query["fanarttype"];
      int maxWidth;
      int maxHeight;
      string name = request.Query["name"];
      if (string.IsNullOrWhiteSpace(name))
        return;

      name = name.Decode(); // For safe handling of "&" character in name

      // Both values are optional
      int.TryParse(request.Query["width"], out maxWidth);
      int.TryParse(request.Query["height"], out maxHeight);

      IList<FanArtImage> files = fanart.GetFanArt(mediaType, fanArtType, name, maxWidth, maxHeight, true);
      if (files == null || files.Count == 0)
      {
#if DEBUG
        ServiceRegistration.Get<ILogger>().Debug("No FanArt for {0} '{1}' of type '{2}'", name, fanArtType, mediaType);
#endif
        return;
      }

      using (MemoryStream memoryStream = new MemoryStream(files[0].BinaryData))
        await SendWholeStream(response, memoryStream, false);
    }

    protected async Task SendWholeStream(IOwinResponse response, Stream resourceStream, bool onlyHeaders)
    {
      var length = resourceStream.Length;
      response.StatusCode = (int)HttpStatusCode.OK;
      response.ContentLength = length;

      CancellationTokenSource cts = new CancellationTokenSource();

      const int BUF_LEN = 8192;
      byte[] buffer = new byte[BUF_LEN];
      int bytesRead;
      while ((bytesRead = resourceStream.Read(buffer, 0, length > BUF_LEN ? BUF_LEN : (int)length)) > 0) // Don't use Math.Min since (int) length is negative for length > Int32.MaxValue
      {
        length -= bytesRead;
        await response.WriteAsync(buffer, 0, bytesRead, cts.Token);
      }
    }
  }
}
