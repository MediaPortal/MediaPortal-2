using System;
using System.Collections.Generic;
using System.Diagnostics;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Cache;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images.BaseClasses;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  // TODO: implement offset
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "maxWidth", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "maxHeight", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "type", Type = typeof(WebMediaType), Nullable = true)]
  internal class ExtractImageResized : BaseGetArtwork
  {
    // We just return a Thumbnail from MP
    public byte[] Process(WebMediaType type, string itemId, int maxWidth, int maxHeight, string borders = null)
    {
      // set borders to transparent
      borders = "transparent";

      if (itemId == null)
        throw new BadRequestException("ExtractImageResized: id is null");
      if (maxWidth == null)
        throw new BadRequestException("ExtractImageResized: maxWidth is null");
      if (maxHeight == null)
        throw new BadRequestException("ExtractImageResized: maxHeight is null");

      string fanartType;
      string fanArtMediaType;
      MapTypes(WebFileType.Content, WebMediaType.File, out fanartType, out fanArtMediaType);

      bool isTvRadio = fanArtMediaType == FanArtMediaTypes.ChannelTv || fanArtMediaType == FanArtMediaTypes.ChannelRadio;
      bool isRecording = (type == WebMediaType.Recording);

      Guid idGuid;
      int idInt;
      if (!Guid.TryParse(itemId, out idGuid) && !isTvRadio)
        throw new BadRequestException(String.Format("ExtractImageResized: Couldn't parse if '{0}' to Guid", itemId));
      else if (int.TryParse(itemId, out idInt) && (fanArtMediaType == FanArtMediaTypes.ChannelTv || fanArtMediaType == FanArtMediaTypes.ChannelRadio))
        idGuid = IntToGuid(idInt);

      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(idGuid, isTvRadio, maxWidth, maxHeight, borders, 0, FanArtTypes.Thumbnail, FanArtMediaTypes.Undefined);

      byte[] data;
      if (ImageCache.TryGetImageFromCache(identifier, out data))
      {
        Logger.Info("GetArtworkResized: got image from cache");
        return data;
      }

      IList<FanArtImage> fanart = GetFanArtImages(itemId, isTvRadio, isRecording, fanartType, fanArtMediaType);

      // get a random FanArt from the List
      Random rnd = new Random();
      int r = rnd.Next(fanart.Count);
      byte[] resizedImage;
      if (maxWidth != 0 && maxHeight != 0)
        resizedImage = Plugins.MP2Extended.WSS.Images.ResizeImage(fanart[r].BinaryData, maxWidth, maxHeight, borders);
      else
        resizedImage = fanart[r].BinaryData;

      // Add to cache, but only if it is no dummy image
      if (fanart[r].Name != NO_FANART_IMAGE_NAME)
        if (ImageCache.AddImageToCache(resizedImage, identifier))
          Logger.Info("GetArtworkResized: Added image to cache");

      return resizedImage;
    }

    internal new static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}