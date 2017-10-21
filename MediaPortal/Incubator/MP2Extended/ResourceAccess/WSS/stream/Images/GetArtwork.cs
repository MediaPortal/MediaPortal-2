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
      int offsetInt = 0;

      if (id == null)
        throw new BadRequestException("GetArtwork: id is null");
      if (artworktype == null)
        throw new BadRequestException("GetArtwork: artworktype is null");
      if (mediatype == null)
        throw new BadRequestException("GetArtwork: mediatype is null");


      string fanartType;
      string fanArtMediaType;
      MapTypes(artworktype, mediatype, out fanartType, out fanArtMediaType);

      bool isTvRadio = fanArtMediaType == FanArtMediaTypes.ChannelTv || fanArtMediaType == FanArtMediaTypes.ChannelRadio;
      bool isRecording = mediatype == WebMediaType.Recording;

      IList<FanArtImage> fanart = GetFanArtImages(id, isTvRadio, isRecording, fanartType, fanArtMediaType);

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