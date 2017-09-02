using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "This function is inernally used by the MP2Ext webinterface.")]
  [ApiFunctionParam(Name = "path", Type = typeof(string), Nullable = false)]
  internal class GetHtmlResource : BaseSendData
  {
    /// <summary>
    /// The folder inside the MP2Ext folder where the files are stored
    /// </summary>
    private const string RESOURCE_DIR = "www";
    
    public bool Process(string path, HttpContext httpContext)
    {
      string[] uriParts = httpContext.Request.Path.Value.Split('/');
      if (uriParts.Length >= 6)
        path = string.Join("/", uriParts.Skip(5));

      if (path == null)
        throw new BadRequestException("GetHtmlResource: path is null");

      string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      if (assemblyPath == null)
        throw new BadRequestException("GetHtmlResource: assemblyPath is null");

      string resourceBasePath = Path.Combine(assemblyPath, RESOURCE_DIR);

      string resourcePath = Path.GetFullPath(Path.Combine(resourceBasePath, path));

      if (!resourcePath.StartsWith(resourceBasePath))
        throw new BadRequestException(string.Format("GetHtmlResource: outside home dir! reguested Path: {0}", resourcePath));

      if (!File.Exists(resourcePath))
        throw new BadRequestException(string.Format("GetHtmlResource: File doesn't exist! reguested Path: {0}", resourcePath));

      // Headers

      DateTime lastChanged = File.GetLastWriteTime(resourcePath);

      // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
      if (!string.IsNullOrEmpty(httpContext.Request.Headers["If-Modified-Since"]))
      {
        DateTime lastRequest = DateTime.Parse(httpContext.Request.Headers["If-Modified-Since"]);
        if (lastRequest.CompareTo(lastChanged) <= 0)
          httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
      }

      // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
      httpContext.Response.Headers.Add("Last-Modified", lastChanged.ToUniversalTime().ToString("r"));

      // Cache
      httpContext.Response.Headers.Add("Cache-Control", "public; max-age=31536000");
      httpContext.Response.Headers.Add("Expires", DateTime.Now.AddYears(1).ToString("r"));

      // Content
      bool onlyHeaders = httpContext.Request.Method == Method.Header || httpContext.Response.StatusCode == StatusCodes.Status304NotModified;
      Stream resourceStream = File.OpenRead(resourcePath);
      SendWholeFile(httpContext, resourceStream, onlyHeaders);
      resourceStream.Close();

      return true;
    }


    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
