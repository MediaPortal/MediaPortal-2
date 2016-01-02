using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  internal class GetMediaItem : BaseSendData
  {
    public void Process(HttpContext httpContext, Guid itemId)
    {
      // Grab the media item given in the request.
      try
      {
        Logger.Debug("DlnaResourceAccessModule: Attempting to load mediaitem {0}", itemId.ToString());

        // Attempt to grab the media item from the database.
        ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
        necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
        necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
        necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
        var item = GetMediaItems.GetMediaItemById(itemId, necessaryMIATypes);
        if (item == null)
          throw new BadRequestException(string.Format("Media item '{0}' not found.", itemId));

        // Grab the mimetype from the media item and set the Content Type header.
        // TODO: Fix
        string mimeType = "video/*";
        //httpContext.Response.ContentType = item[MediaAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE).ToString();

        // Grab the resource path for the media item.
        var resourcePathStr = item[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());

        var ra = GetResourceAccessor(resourcePath);
        IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
        if (fsra == null)
          throw new InternalServerException("GetMediaItem: failed to create IFileSystemResourceAccessor");


        using (var resourceStream = fsra.OpenRead())
        {
          // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
          if (!string.IsNullOrEmpty(httpContext.Response.Headers["If-Modified-Since"]))
          {
            DateTime lastRequest = DateTime.Parse(httpContext.Response.Headers["If-Modified-Since"]);
            if (lastRequest.CompareTo(fsra.LastChanged) <= 0)
              httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
          }

          // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
          httpContext.Response.Headers.Add("Last-Modified", fsra.LastChanged.ToUniversalTime().ToString("r"));

          string byteRangesSpecifier = httpContext.Request.Headers["Range"];
          IList<Range> ranges = ParseByteRanges(byteRangesSpecifier, resourceStream.Length);
          bool onlyHeaders = httpContext.Request.Method == Method.Header || httpContext.Response.StatusCode == StatusCodes.Status304NotModified;
          if (ranges != null)
            // We only support last range
            SendRange(httpContext, resourceStream, ranges[ranges.Count - 1], onlyHeaders);
          else
            SendWholeFile(httpContext, resourceStream, onlyHeaders);
        }
      }
      catch (FileNotFoundException ex)
      {
        throw new InternalServerException(string.Format("Failed to proccess media item '{0}'", itemId), ex);
      }
    }


    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
