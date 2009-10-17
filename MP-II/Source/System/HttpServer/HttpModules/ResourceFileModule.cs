using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using HttpServer.Exceptions;
using HttpServer.Helpers;
using HttpServer.Sessions;

namespace HttpServer.HttpModules
{
  /// <summary>
  /// Serves files that are stored in embedded resources.
  /// </summary>
  public class ResourceFileModule : HttpModule
  {
    private readonly ResourceManager _resourceManager;

    private readonly IDictionary<string, string> _mimeTypes = new Dictionary<string, string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFileModule"/> class.
    /// Runs <see cref="AddDefaultMimeTypes"/> to make sure the basic mime types are available, they can be cleared later
    /// through the use of <see cref="MimeTypes"/> if desired.
    /// </summary>
    public ResourceFileModule()
    {
      AddDefaultMimeTypes();
      _resourceManager = new ResourceManager();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFileModule"/> class.
    /// Runs <see cref="AddDefaultMimeTypes"/> to make sure the basic mime types are available, they can be cleared later
    /// through the use of <see cref="MimeTypes"/> if desired.
    /// </summary>
    /// <param name="logWriter">The log writer to use when logging events</param>
    public ResourceFileModule(ILogWriter logWriter)
    {
      _resourceManager = new ResourceManager(logWriter);
    }

    /// <summary>
    /// List with all mime-type that are allowed. 
    /// </summary>
    /// <remarks>All other mime types will result in a Forbidden http status code.</remarks>
    public IDictionary<string, string> MimeTypes
    {
      get { return _mimeTypes; }
    }

    /// <summary>
    /// Mimtypes that this class can handle per default
    /// </summary>
    public void AddDefaultMimeTypes()
    {
      MimeTypes.Add("default", "application/octet-stream");
      MimeTypes.Add("txt", "text/plain");
      MimeTypes.Add("html", "text/html");
      MimeTypes.Add("htm", "text/html");
      MimeTypes.Add("jpg", "image/jpg");
      MimeTypes.Add("jpeg", "image/jpg");
      MimeTypes.Add("bmp", "image/bmp");
      MimeTypes.Add("gif", "image/gif");
      MimeTypes.Add("png", "image/png");

      MimeTypes.Add("css", "text/css");
      MimeTypes.Add("gzip", "application/x-gzip");
      MimeTypes.Add("zip", "multipart/x-zip");
      MimeTypes.Add("tar", "application/x-tar");
      MimeTypes.Add("pdf", "application/pdf");
      MimeTypes.Add("rtf", "application/rtf");
      MimeTypes.Add("xls", "application/vnd.ms-excel");
      MimeTypes.Add("ppt", "application/vnd.ms-powerpoint");
      MimeTypes.Add("doc", "application/application/msword");
      MimeTypes.Add("js", "application/javascript");
      MimeTypes.Add("au", "audio/basic");
      MimeTypes.Add("snd", "audio/basic");
      MimeTypes.Add("es", "audio/echospeech");
      MimeTypes.Add("mp3", "audio/mpeg");
      MimeTypes.Add("mp2", "audio/mpeg");
      MimeTypes.Add("mid", "audio/midi");
      MimeTypes.Add("wav", "audio/x-wav");
      MimeTypes.Add("swf", "application/x-shockwave-flash");
      MimeTypes.Add("avi", "video/avi");
      MimeTypes.Add("rm", "audio/x-pn-realaudio");
      MimeTypes.Add("ram", "audio/x-pn-realaudio");
      MimeTypes.Add("aif", "audio/x-aiff");
    }

    /// <summary>
    /// Loads resources from a namespace in the given assembly to an uri
    /// </summary>
    /// <param name="toUri">The uri to map the resources to</param>
    /// <param name="fromAssembly">The assembly in which the resources reside</param>
    /// <param name="fromNamespace">The namespace from which to load the resources</param>
    /// <usage>
    /// resourceLoader.LoadResources("/user/", typeof(User).Assembly, "MyLib.Models.User.Views");
    /// 
    /// will make ie the resource MyLib.Models.User.Views.stylesheet.css accessible via /user/stylesheet.css
    /// </usage>
    /// <returns>The amount of loaded files, giving you the possibility of making sure the resources needed gets loaded</returns>
    public int AddResources(string toUri, Assembly fromAssembly, string fromNamespace)
    {
      return _resourceManager.LoadResources(toUri, fromAssembly, fromNamespace);
    }

    /// <summary>
    /// Returns true if the module can handle the request
    /// </summary>
    private bool CanHandle(IHttpRequest request)
    {
      return !request.Uri.AbsolutePath.EndsWith("*") && _resourceManager.ContainsResource(request.Uri.AbsolutePath);
    }

    #region Overrides of HttpModule

    /// <summary>
    /// Method that process the url
    /// </summary>
    /// <param name="request">Information sent by the browser about the request</param>
    /// <param name="response">Information that is being sent back to the client.</param>
    /// <param name="session">Session used to </param>
    /// <returns>true if this module handled the request.</returns>
    public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      if (!CanHandle(request))
        return false;

      string path = request.Uri.AbsolutePath;
      string contentType;
      Stream resourceStream = GetResourceStream(path, out contentType);
      if (resourceStream == null)
        return false;

      response.ContentType = contentType;
      DateTime modifiedTime = DateTime.MinValue;
      if (!string.IsNullOrEmpty(request.Headers["if-Modified-Since"]))
      {
        DateTime lastRequest = DateTime.Parse(request.Headers["if-Modified-Since"]);
        if (lastRequest.CompareTo(modifiedTime) <= 0)
          response.Status = HttpStatusCode.NotModified;
      }

      // Albert, Team MediaPortal: No conversion from modifiedTime to UTC necessary because modifiedTime doesn't denote any
      // meaningful time here
      response.AddHeader("Last-modified", modifiedTime.ToString("r"));
      response.ContentLength = resourceStream.Length;
      response.SendHeaders();

      if (request.Method != "Headers" && response.Status != HttpStatusCode.NotModified)
      {
        byte[] buffer = new byte[8192];
        int bytesRead = resourceStream.Read(buffer, 0, 8192);
        while (bytesRead > 0)
        {
          response.SendBody(buffer, 0, bytesRead);
          bytesRead = resourceStream.Read(buffer, 0, 8192);
        }
      }

      return true;
    }

    #endregion

    private Stream GetResourceStream(string path, out string contentType)
    {
      int extensionPosition = path.LastIndexOf('.');
      string extension = extensionPosition == -1 ? null : path.Substring(extensionPosition + 1);
      if (extension == null)
        throw new InternalServerException("Failed to find file extension");

      if (MimeTypes.ContainsKey(extension))
        contentType = MimeTypes[extension];
      else
        throw new ForbiddenException("Forbidden file type: " + extension);

      return _resourceManager.GetResourceStream(path);
    }
  }
}