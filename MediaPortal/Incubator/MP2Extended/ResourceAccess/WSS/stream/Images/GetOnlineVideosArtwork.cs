using System;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "Returns the Thumbnail for Sites, GlobalSites, Categories, Subcategories and Videos. This function uses the OnlineVideos Cache, not the MP2Ext Cache.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "mediatype", Type = typeof(WebOnlineVideosMediaType), Nullable = false)]
  internal class GetOnlineVideosArtwork : IStreamRequestMicroModuleHandler
  {
    public byte[] Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      string mediatype = httpParam["mediatype"].Value;


      if (id == null)
        throw new BadRequestException("GetOnlineVideosArtwork: id is null");
      if (mediatype == null)
        throw new BadRequestException("GetOnlineVideosArtwork: mediatype is null");

      WebOnlineVideosMediaType mediaTypeEnum;
      if (!Enum.TryParse(mediatype, out mediaTypeEnum))
        throw new BadRequestException("GetOnlineVideosArtwork: Error parsing mediatype");

      return OnlineVideosThumbs.GetThumb(mediaTypeEnum, id);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}