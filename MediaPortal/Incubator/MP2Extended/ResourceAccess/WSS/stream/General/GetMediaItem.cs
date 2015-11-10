using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  internal class GetMediaItem : BaseSendData, IStreamRequestMicroModuleHandler2
  {
    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string itemId = httpParam["itemId"].Value;
      if (itemId == null)
        throw new BadRequestException("GetMediaItem: itemId is null");

      var uri = request.Uri;

      // Grab the media item given in the request.
      Guid mediaItemGuid;
      if (!Guid.TryParse(itemId, out mediaItemGuid))
        throw new BadRequestException(string.Format("GetMediaItem: couldn't convert itemId '{0}' to guid", itemId));

      try
      {
        Logger.Debug("DlnaResourceAccessModule: Attempting to load mediaitem {0}", mediaItemGuid.ToString());

        // Attempt to grab the media item from the database.
        ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
        necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
        necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
        necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
        var item = GetMediaItems.GetMediaItemById(mediaItemGuid, necessaryMIATypes);
        if (item == null)
          throw new BadRequestException(string.Format("Media item '{0}' not found.", mediaItemGuid));

        // Grab the mimetype from the media item and set the Content Type header.
        response.ContentType = item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_MIME_TYPE).ToString();

        // Grab the resource path for the media item.
        var resourcePathStr = item.Aspects[ProviderResourceAspect.ASPECT_ID].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());

        var ra = GetResourceAccessor(resourcePath);
        IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
        if (fsra == null)
          throw new InternalServerException("GetMediaItem: failed to create IFileSystemResourceAccessor");

        using (var resourceStream = fsra.OpenRead())
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
          IList<Range> ranges = ParseByteRanges(byteRangesSpecifier, resourceStream.Length);
          bool onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
          if (ranges != null)
            // We only support last range
            SendRange(response, resourceStream, ranges[ranges.Count - 1], onlyHeaders);
          else
            SendWholeFile(response, resourceStream, onlyHeaders);
        }
      }
      catch (FileNotFoundException ex)
      {
        throw new InternalServerException(string.Format("Failed to proccess media item '{0}'", mediaItemGuid), ex);
      }

      return true;
    }


    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
