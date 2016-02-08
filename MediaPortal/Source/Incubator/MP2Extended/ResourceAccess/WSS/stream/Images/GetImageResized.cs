using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Cache;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using Microsoft.AspNet.Http;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  // TODO: implement offset
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "maxWidth", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "maxHeight", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "borders", Type = typeof(string), Nullable = true)]
  internal class GetImageResized : BaseSendData
  {
    public void Process(HttpContext httpContext, WebMediaType type, string id, int maxWidth, int maxHeight, string borders = null)
    {
      if (id == null)
        throw new BadRequestException("GetImageResized: id is null");
      if (maxWidth == null)
        throw new BadRequestException("GetImageResized: maxWidth is null");
      if (maxHeight == null)
        throw new BadRequestException("GetImageResized: maxHeight is null");

      Guid idGuid;
      if (!Guid.TryParse(id, out idGuid))
        throw new BadRequestException(String.Format("GetImageResized: Couldn't parse if '{0}' to Guid", id));

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);
      MediaItem item = GetMediaItems.GetMediaItemById(idGuid, necessaryMIATypes);

      var resourcePathStr = item[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());

      var ra = GetResourceAccessor(resourcePath);
      IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
      if (fsra == null)
        throw new InternalServerException("GetImage: failed to create IFileSystemResourceAccessor");

      // Resize
      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(idGuid, false, maxWidth, maxHeight, borders, 0, FanArtTypes.Undefined, FanArtMediaTypes.Image);
      byte[] resizedImage;

      if (ImageCache.TryGetImageFromCache(identifier, out resizedImage))
      {
        Logger.Info("GetImageResized: Got image from cache");
      }
      else
      {
        using (var resourceStream = fsra.OpenRead())
        {
          byte[] buffer = new byte[resourceStream.Length];
          resourceStream.Read(buffer, 0, Convert.ToInt32(resourceStream.Length));
          resizedImage = Plugins.MP2Extended.WSS.Images.ResizeImage(buffer, maxWidth, maxHeight, borders);
        }

        // Add to cache
        if (ImageCache.AddImageToCache(resizedImage, identifier))
          Logger.Info("GetImageResized: Added image to cache");
      }

      using (var resourceStream = new MemoryStream(resizedImage))
      {
        // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
        if (!string.IsNullOrEmpty(httpContext.Request.Headers["If-Modified-Since"]))
        {
          DateTime lastRequest = DateTime.Parse(httpContext.Request.Headers["If-Modified-Since"]);
          if (lastRequest.CompareTo(fsra.LastChanged) <= 0)
            httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
        }

        // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
        httpContext.Response.Headers.Add("Last-Modified", fsra.LastChanged.ToUniversalTime().ToString("r"));

        string byteRangesSpecifier = httpContext.Request.Headers["Range"];
        IList<Range> ranges = ParseByteRanges(byteRangesSpecifier, resourceStream.Length);
        bool onlyHeaders = httpContext.Request.Method == Method.Header || httpContext.Response.StatusCode == StatusCodes.Status304NotModified;
        if (ranges != null && ranges.Count > 0)
          // We only support last range
          SendRange(httpContext, resourceStream, ranges[ranges.Count - 1], onlyHeaders);
        else
          SendWholeFile(httpContext, resourceStream, onlyHeaders);
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
