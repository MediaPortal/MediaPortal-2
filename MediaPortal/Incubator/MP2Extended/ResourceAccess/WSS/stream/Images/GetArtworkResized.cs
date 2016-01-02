using System;
using System.Collections.Generic;
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
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "artworktype", Type = typeof(WebFileType), Nullable = false)]
  [ApiFunctionParam(Name = "mediatype", Type = typeof(WebMediaType), Nullable = false)]
  [ApiFunctionParam(Name = "maxWidth", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "maxHeight", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "borders", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "offset", Type = typeof(string), Nullable = true)]
  internal class GetArtworkResized : BaseGetArtwork
  {
    public byte[] Process(WebMediaType mediatype, string id, WebFileType artworktype, int offset, int maxWidth, int maxHeight, string borders = null)
    {
      bool isSeason = false;
      string showId = string.Empty;
      string seasonId = string.Empty;
      int offsetInt = 0;

      if (id == null)
        throw new BadRequestException("GetArtworkResized: id is null");

      FanArtConstants.FanArtType fanartType;
      FanArtConstants.FanArtMediaType fanArtMediaType;
      MapTypes(artworktype, mediatype, out fanartType, out fanArtMediaType);

      // if teh Id contains a ':' it is a season
      if (id.Contains(":"))
      {
        isSeason = true;
        showId = id.Split(':')[0];
      }

      bool isTvRadio = fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelTv || fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelRadio;
      bool isRecording = mediatype == WebMediaType.Recording;

      Guid idGuid;
      int idInt;
      if (!Guid.TryParse(isSeason ? showId : id, out idGuid) && !isTvRadio)
        throw new BadRequestException(String.Format("GetArtworkResized: Couldn't parse if '{0}' to Guid", isSeason ? showId : id));
      if (int.TryParse(id, out idInt) && (fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelTv || fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelRadio))
        idGuid = IntToGuid(idInt);

      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(isSeason ? StringToGuid(id) : idGuid, isTvRadio, maxWidth, maxHeight, borders, offsetInt, fanartType, fanArtMediaType);

      byte[] data;
      if (ImageCache.TryGetImageFromCache(identifier, out data))
      {
        Logger.Info("GetArtworkResized: got image from cache");
        return data;
      }

      IList<FanArtImage> fanart = GetFanArtImages(id, showId, seasonId, isSeason, isTvRadio, isRecording, fanartType, fanArtMediaType);

      // get offset
      if (offsetInt >= fanart.Count)
      {
        Logger.Warn("GetArtwork: offset is too big! FanArt: {0} Offset: {1}", fanart.Count, offsetInt);
        offsetInt = 0;
      }
      byte[] resizedImage = Plugins.MP2Extended.WSS.Images.ResizeImage(fanart[offsetInt].BinaryData, maxWidth, maxHeight, borders);

      // Add to cache, but only if it is no dummy image
      if (fanart[offsetInt].Name != NO_FANART_IMAGE_NAME)
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