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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Cache;
using MediaPortal.Common.FanArt;
using System.Net.Http;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images.BaseClasses;
using System.Threading.Tasks;
using Microsoft.Owin;
using System.IO;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "Returns the resized Thumbnail for Sites, GlobalSites, Categories, Subcategories and Videos. This function uses the MP2Ext Cache for the resized Thumbs.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "mediatype", Type = typeof(WebOnlineVideosMediaType), Nullable = false)]
  [ApiFunctionParam(Name = "maxWidth", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "maxHeight", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "borders", Type = typeof(string), Nullable = true)]
  internal class GetOnlineVideosArtworkResized : BaseGetArtwork
  {
    public async Task ProcessAsync(IOwinContext context, WebOnlineVideosMediaType mediatype, string id, int maxWidth, int maxHeight, string borders = null)
    {
      if (id == null)
        throw new BadRequestException("GetOnlineVideosArtworkResized: id is null");

      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(StringToGuid(id), false, maxWidth, maxHeight, borders, 0, FanArtTypes.Thumbnail, FanArtMediaTypes.Undefined);

      Stream resourceStream;
      byte[] data;
      if (ImageCache.TryGetImageFromCache(context, identifier, out data))
      {
        Logger.Info("GetOnlineVideosArtworkResized: got image from cache");
        resourceStream = ImageFile(data);
        context.Response.ContentType = "image/*";
        await SendWholeFileAsync(context, resourceStream, false);
        resourceStream.Dispose();
      }

      byte[] resizedImage = Plugins.MP2Extended.WSS.Images.ResizeImage(OnlineVideosThumbs.GetThumb(mediatype, id), maxWidth, maxHeight, borders);
      resourceStream = ImageFile(resizedImage);
      context.Response.ContentType = "image/*";
      await SendWholeFileAsync(context, resourceStream, false);
      resourceStream.Dispose();
    }
  }
}
