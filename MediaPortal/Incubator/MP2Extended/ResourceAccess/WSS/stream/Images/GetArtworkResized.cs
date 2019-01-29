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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Cache;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images.BaseClasses;
using MediaPortal.Common.FanArt;
using System.Net.Http;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using System.Threading.Tasks;
using Microsoft.Owin;
using System.IO;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  // TODO: implement offset
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "artworktype", Type = typeof(WebFileType), Nullable = false)]
  [ApiFunctionParam(Name = "mediatype", Type = typeof(WebMediaType), Nullable = false)]
  [ApiFunctionParam(Name = "maxWidth", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "maxHeight", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "borders", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "offset", Type = typeof(string), Nullable = true)]
  internal class GetArtworkResized : BaseGetArtwork
  {
    public static async Task ProcessAsync(IOwinContext context, WebMediaType mediatype, string id, WebFileType artworktype, int offset, int maxWidth, int maxHeight, string borders = null)
    {
      int offsetInt = 0;

      if (id == null)
        throw new BadRequestException("GetArtworkResized: id is null");

      string fanartType;
      string fanArtMediaType;
      MapTypes(artworktype, mediatype, out fanartType, out fanArtMediaType);

      bool isTvRadio = fanArtMediaType == FanArtMediaTypes.ChannelTv || fanArtMediaType == FanArtMediaTypes.ChannelRadio;
      bool isRecording = mediatype == WebMediaType.Recording;

      Guid idGuid;
      int idInt;
      if (!Guid.TryParse(id, out idGuid) && !isTvRadio)
        throw new BadRequestException(String.Format("GetArtworkResized: Couldn't parse if '{0}' to Guid", id));
      if (int.TryParse(id, out idInt) && (fanArtMediaType == FanArtMediaTypes.ChannelTv || fanArtMediaType == FanArtMediaTypes.ChannelRadio))
        idGuid = IntToGuid(idInt);

      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(idGuid, isTvRadio, maxWidth, maxHeight, borders, offsetInt, fanartType, fanArtMediaType);

      Stream resourceStream;
      byte[] data;
      if (ImageCache.TryGetImageFromCache(context, identifier, out data))
      {
        Logger.Info("GetArtworkResized: got image from cache");
        resourceStream = ImageFile(data);
        context.Response.ContentType = "image/*";
        await SendWholeFileAsync(context, resourceStream, false);
        resourceStream.Dispose();
      }

      IList<FanArtImage> fanart = GetFanArtImages(context, id, isTvRadio, isRecording, fanartType, fanArtMediaType);

      // get offset
      if (offsetInt >= fanart.Count)
      {
        Logger.Warn("GetArtwork: offset is too big! FanArt: {0} Offset: {1}", fanart.Count, offsetInt);
        offsetInt = 0;
      }
      byte[] resizedImage = Plugins.MP2Extended.WSS.Images.ResizeImage(fanart[offsetInt].BinaryData, maxWidth, maxHeight, borders);

      // Add to cache, but only if it is no dummy image
      if (fanart[offsetInt].Name != NO_FANART_IMAGE_NAME)
        if (ImageCache.AddImageToCache(context, resizedImage, identifier))
          Logger.Info("GetArtworkResized: Added image to cache");

      resourceStream = ImageFile(resizedImage);
      context.Response.ContentType = "image/*";
      await SendWholeFileAsync(context, resourceStream, false);
      resourceStream.Dispose();
    }

    internal new static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
