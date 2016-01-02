using System.Collections.Generic;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images.BaseClasses;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  // TODO: implement offset
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "artworktype", Type = typeof(WebFileType), Nullable = false)]
  [ApiFunctionParam(Name = "mediatype", Type = typeof(WebMediaType), Nullable = false)]
  [ApiFunctionParam(Name = "offset", Type = typeof(string), Nullable = true)]
  internal class GetArtwork : BaseGetArtwork
  {
    public byte[] Process(WebMediaType mediatype, string id, WebFileType artworktype, int offset)
    {
      bool isSeason = false;
      string showId = string.Empty;
      string seasonId = string.Empty;
      int offsetInt = 0;

      if (id == null)
        throw new BadRequestException("GetArtwork: id is null");
      if (artworktype == null)
        throw new BadRequestException("GetArtwork: artworktype is null");
      if (mediatype == null)
        throw new BadRequestException("GetArtwork: mediatype is null");


      FanArtConstants.FanArtType fanartType;
      FanArtConstants.FanArtMediaType fanArtMediaType;
      MapTypes(artworktype, mediatype, out fanartType, out fanArtMediaType);

      // if teh Id contains a ':' it is a season
      if (id.Contains(":"))
        isSeason = true;

      bool isTvRadio = fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelTv || fanArtMediaType == FanArtConstants.FanArtMediaType.ChannelRadio;
      bool isRecording = mediatype == WebMediaType.Recording;

      IList<FanArtImage> fanart = GetFanArtImages(id, showId, seasonId, isSeason, isTvRadio, isRecording, fanartType, fanArtMediaType);

      // get offset
      if (offsetInt >= fanart.Count)
      {
        Logger.Warn("GetArtwork: offset is too big! FanArt: {0} Offset: {1}", fanart.Count, offsetInt);
        offsetInt = 0;
      }

      return fanart[offsetInt].BinaryData;
    }

    internal new static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}