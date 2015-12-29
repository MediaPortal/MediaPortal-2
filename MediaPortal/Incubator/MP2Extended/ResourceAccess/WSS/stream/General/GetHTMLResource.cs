using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "This function is inernally used by the MP2Ext webinterface.")]
  [ApiFunctionParam(Name = "path", Type = typeof(string), Nullable = false)]
  internal class GetHtmlResource : BaseSendData, IStreamRequestMicroModuleHandler2
  {
    /// <summary>
    /// The folder inside the MP2Ext folder where the files are stored
    /// </summary>
    private const string RESOURCE_DIR = "www";
    
    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string path = httpParam["path"].Value;

      string[] uriParts = request.Uri.AbsolutePath.Split('/');
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
      if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"]))
      {
        DateTime lastRequest = DateTime.Parse(request.Headers["If-Modified-Since"]);
        if (lastRequest.CompareTo(lastChanged) <= 0)
          response.Status = HttpStatusCode.NotModified;
      }

      // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
      response.AddHeader("Last-Modified", lastChanged.ToUniversalTime().ToString("r"));

      // Cache
      response.AddHeader("Cache-Control", "public; max-age=31536000");
      response.AddHeader("Expires", DateTime.Now.AddYears(1).ToString("r"));

      // Content
      bool onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
      Stream resourceStream = File.OpenRead(resourcePath);
      SendWholeFile(response, resourceStream, onlyHeaders);
      resourceStream.Close();

      return true;
    }


    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
