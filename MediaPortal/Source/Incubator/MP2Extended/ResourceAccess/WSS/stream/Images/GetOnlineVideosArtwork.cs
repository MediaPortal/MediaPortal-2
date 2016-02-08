using System;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "Returns the Thumbnail for Sites, GlobalSites, Categories, Subcategories and Videos. This function uses the OnlineVideos Cache, not the MP2Ext Cache.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "mediatype", Type = typeof(WebOnlineVideosMediaType), Nullable = false)]
  internal class GetOnlineVideosArtwork
  {
    public byte[] Process(WebOnlineVideosMediaType mediatype, string id)
    {
      if (id == null)
        throw new BadRequestException("GetOnlineVideosArtwork: id is null");

      return OnlineVideosThumbs.GetThumb(mediatype, id);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}