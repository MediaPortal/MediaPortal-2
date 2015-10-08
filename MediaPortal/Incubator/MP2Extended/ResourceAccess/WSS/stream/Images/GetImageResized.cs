using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  // TODO: implement offset
  internal class GetImageResized : SendDataBase, IStreamRequestMicroModuleHandler2
  {
    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      string maxWidth = httpParam["maxWidth"].Value;
      string maxHeight = httpParam["maxHeight"].Value;
      string borders = httpParam["borders"].Value;

      if (id == null)
        throw new BadRequestException("GetImage: id is null");
      if (maxWidth == null)
        throw new BadRequestException("GetArtworkResized: maxWidth is null");
      if (maxHeight == null)
        throw new BadRequestException("GetArtworkResized: maxHeight is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      var resourcePathStr = item.Aspects[ProviderResourceAspect.ASPECT_ID].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());

      var ra = GetResourceAccessor(resourcePath);
      IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
      if (fsra == null)
        throw new InternalServerException("GetImage: failed to create IFileSystemResourceAccessor");

      // Resize
      int maxWidthInt;
      if (!Int32.TryParse(maxWidth, out maxWidthInt))
      {
        throw new BadRequestException(String.Format("GetArtworkResized: Couldn't convert maxWidth to int: {0}", maxWidth));
      }

      int maxHeightInt;
      if (!Int32.TryParse(maxHeight, out maxHeightInt))
      {
        throw new BadRequestException(String.Format("GetArtworkResized: Couldn't convert maxHeight to int: {0}", maxHeight));
      }

      byte[] resizedImage;
      using (var resourceStream = fsra.OpenRead())
      {
        byte[] buffer = new byte[resourceStream.Length];
        resourceStream.Read(buffer, 0, Convert.ToInt32(resourceStream.Length));
        resizedImage = Plugins.MP2Extended.WSS.Images.ResizeImage(buffer, maxWidthInt, maxHeightInt, borders);
      }



      using (var resourceStream = new MemoryStream(resizedImage))
      {
        // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
        if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"]))
        {
          DateTime lastRequest = DateTime.Parse(request.Headers["If-Modified-Since"]);
          if (lastRequest.CompareTo(fsra.LastChanged) <= 0)
            response.Status = HttpStatusCode.NotModified;
        }

        // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
        response.AddHeader("Last-Modified", fsra.LastChanged.ToUniversalTime().ToString("r"));

        string byteRangesSpecifier = request.Headers["Range"];
        IList<Range> ranges = ParseRanges(byteRangesSpecifier, resourceStream.Length);
        bool onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
        if (ranges != null && ranges.Count == 1)
          // We only support one range
          SendRange(response, resourceStream, ranges[0], onlyHeaders);
        else
          SendWholeFile(response, resourceStream, onlyHeaders);
      }

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}