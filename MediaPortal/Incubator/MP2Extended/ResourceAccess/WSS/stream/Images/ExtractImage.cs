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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "type", Type = typeof(WebMediaType), Nullable = true)]
  internal class ExtractImage : BaseGetArtwork
  {
    // We just return a Thumbnail from MP
    public Task<HttpResponseMessage> ProcessAsync(IOwinContext context, WebMediaType type, string itemId)
    {
      bool isSeason = false;
      string showId = string.Empty;

      if (itemId == null)
        throw new BadRequestException("ExtractImage: id is null");

      string fanartType;
      string fanArtMediaType;
      MapTypes(WebFileType.Content, WebMediaType.File, out fanartType, out fanArtMediaType);

      bool isTvRadio = fanArtMediaType == FanArtMediaTypes.ChannelTv || fanArtMediaType == FanArtMediaTypes.ChannelRadio;
      bool isRecording = (type == WebMediaType.Recording);

      Guid idGuid;
      int idInt;
      if (!Guid.TryParse(itemId, out idGuid) && !isTvRadio)
        throw new BadRequestException(String.Format("ExtractImage: Couldn't parse if '{0}' to Guid", isSeason ? showId : itemId));
      else if (int.TryParse(itemId, out idInt) && (fanArtMediaType == FanArtMediaTypes.ChannelTv || fanArtMediaType == FanArtMediaTypes.ChannelRadio))
        idGuid = IntToGuid(idInt);

      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(idGuid, isTvRadio, 0, 0, "undefined", 0, FanArtTypes.Thumbnail, FanArtMediaTypes.Undefined);

      IList<FanArtImage> fanart = GetFanArtImages(context, itemId, isTvRadio, isRecording, fanartType, fanArtMediaType);

      // get a random FanArt from the List
      Random rnd = new Random();
      int r = rnd.Next(fanart.Count);

      var resizedImage = fanart[r].BinaryData;

      return Task.FromResult(ImageFile(resizedImage));
    }

    internal new static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
