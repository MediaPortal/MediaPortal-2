using System;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
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
  internal class GetOnlineVideosArtworkResized
  {
    public byte[] Process(WebOnlineVideosMediaType mediatype, string id, int maxWidth, int maxHeight, string borders = null)
    {
      if (id == null)
        throw new BadRequestException("GetOnlineVideosArtworkResized: id is null");

      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(StringToGuid(id), false, maxWidth, maxHeight, borders, 0, FanArtTypes.Thumbnail, FanArtMediaTypes.Undefined);

      byte[] data;
      if (ImageCache.TryGetImageFromCache(identifier, out data))
      {
        Logger.Info("GetOnlineVideosArtworkResized: got image from cache");
        return data;
      }

      byte[] resizedImage = Plugins.MP2Extended.WSS.Images.ResizeImage(OnlineVideosThumbs.GetThumb(mediatype, id), maxWidth, maxHeight, borders);

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