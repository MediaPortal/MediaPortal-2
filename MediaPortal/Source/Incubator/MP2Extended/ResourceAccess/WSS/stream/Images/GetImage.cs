using System;
using System.Collections.Generic;
using System.Net;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using Microsoft.AspNet.Http;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  // TODO: implement offset
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetImage : BaseSendData
  {
    public bool Process(HttpContext httpContext, WebMediaType type, string id)
    {
      if (id == null)
        throw new BadRequestException("GetImage: id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      var resourcePathStr = item[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());

      var ra = GetResourceAccessor(resourcePath);
      IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
      if (fsra == null)
        throw new InternalServerException("GetImage: failed to create IFileSystemResourceAccessor");

      using (var resourceStream = fsra.OpenRead())
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

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
