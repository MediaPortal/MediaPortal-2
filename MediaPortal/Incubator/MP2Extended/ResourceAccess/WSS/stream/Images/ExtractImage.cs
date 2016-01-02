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
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "type", Type = typeof(WebMediaType), Nullable = true)]
  internal class ExtractImage : BaseGetArtwork
  {
    // We just return a Thumbnail from MP
    public byte[] Process(WebMediaType type, string itemId)
    {
      bool isSeason = false;
      string showId = string.Empty;
      string seasonId = string.Empty;

      if (itemId == null)
        throw new BadRequestException("ExtractImage: id is null");

      FanArtConstants.FanArtType fanartType;
      FanArtConstants.FanArtMediaType fanArtMediaType;
      MapTypes(WebFileType.Content, WebMediaType.File, out fanartType, out fanArtMediaType);

      // if teh Id contains a ':' it is a season
      if (itemId.Contains(":"))
        isSeason = true;

      bool isTvRadio = fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelTv || fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelRadio;
      bool isRecording = (type == WebMediaType.Recording);

      Guid idGuid;
      int idInt;
      if (!Guid.TryParse(isSeason ? showId : itemId, out idGuid) && !isTvRadio)
        throw new BadRequestException(String.Format("ExtractImage: Couldn't parse if '{0}' to Guid", isSeason ? showId : itemId));
      else if (int.TryParse(itemId, out idInt) && (fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelTv || fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelRadio))
        idGuid = IntToGuid(idInt);

      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(idGuid, isTvRadio, 0, 0, "undefined", 0, FanArtConstants.FanArtType.Thumbnail, FanArtConstants.FanArtMediaType.Undefined);

      IList<FanArtImage> fanart = GetFanArtImages(itemId, showId, seasonId, isSeason, isTvRadio, isRecording, fanartType, fanArtMediaType);

      // get a random FanArt from the List
      Random rnd = new Random();
      int r = rnd.Next(fanart.Count);

      var resizedImage = fanart[r].BinaryData;

      return resizedImage;
    }

    internal new static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}