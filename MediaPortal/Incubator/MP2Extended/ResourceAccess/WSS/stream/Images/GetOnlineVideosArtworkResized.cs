using System;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Cache;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "Returns the resized Thumbnail for Sites, GlobalSites, Categories, Subcategories and Videos. This function uses the MP2Ext Cache for the resized Thumbs.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "mediatype", Type = typeof(WebOnlineVideosMediaType), Nullable = false)]
  [ApiFunctionParam(Name = "maxWidth", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "maxHeight", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "borders", Type = typeof(string), Nullable = true)]
  internal class GetOnlineVideosArtworkResized : IStreamRequestMicroModuleHandler
  {
    public byte[] Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      string mediatype = httpParam["mediatype"].Value;
      string maxWidth = httpParam["maxWidth"].Value;
      string maxHeight = httpParam["maxHeight"].Value;
      string borders = httpParam["borders"].Value;

      if (id == null)
        throw new BadRequestException("GetOnlineVideosArtworkResized: id is null");
      if (mediatype == null)
        throw new BadRequestException("GetOnlineVideosArtworkResized: mediatype is null");
      if (maxWidth == null)
        throw new BadRequestException("GetOnlineVideosArtworkResized: maxWidth is null");
      if (maxHeight == null)
        throw new BadRequestException("GetOnlineVideosArtworkResized: maxHeight is null");

      int maxWidthInt;
      if (!Int32.TryParse(maxWidth, out maxWidthInt))
      {
        throw new BadRequestException(String.Format("GetOnlineVideosArtworkResized: Couldn't convert maxWidth to int: {0}", maxWidth));
      }

      int maxHeightInt;
      if (!Int32.TryParse(maxHeight, out maxHeightInt))
      {
        throw new BadRequestException(String.Format("GetOnlineVideosArtworkResized: Couldn't convert maxHeight to int: {0}", maxHeight));
      }

      WebOnlineVideosMediaType mediaTypeEnum;
      if (!Enum.TryParse(mediatype, out mediaTypeEnum))
        throw new BadRequestException("GetOnlineVideosArtworkResized: Error parsing mediatype");

      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(StringToGuid(id), false, maxWidthInt, maxHeightInt, borders, 0, FanArtConstants.FanArtType.Thumbnail, FanArtConstants.FanArtMediaType.Undefined);

      byte[] data;
      if (ImageCache.TryGetImageFromCache(identifier, out data))
      {
        Logger.Info("GetOnlineVideosArtworkResized: got image from cache");
        return data;
      }

      byte[] resizedImage = Plugins.MP2Extended.WSS.Images.ResizeImage(OnlineVideosThumbs.GetThumb(mediaTypeEnum, id), maxWidthInt, maxHeightInt, borders);

      return resizedImage;
    }

    private Guid StringToGuid(string value)
    {
      byte[] bytes = ResourceAccessUtils.GetBytes(value);
      return new Guid(bytes);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}