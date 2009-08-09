using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HttpServer.Exceptions;
using HttpServer.Sessions;

namespace HttpServer.HttpModules
{
  /// <summary>
  /// The purpose of this module is to serve files.
  /// </summary>
  public class FileModule : HttpModule
  {
    private readonly string _baseUri;
    private readonly string _basePath;
    private readonly bool _useLastModifiedHeader;
    private readonly IDictionary<string, string> _mimeTypes = new Dictionary<string, string>();
    private static readonly string[] DefaultForbiddenChars = new[] {"\\", "..", ":"};
    private string[] _forbiddenChars;
    private static readonly string PathSeparator = Path.DirectorySeparatorChar.ToString();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileModule"/> class.
    /// </summary>
    /// <param name="baseUri">Uri to serve, for instance "/files/"</param>
    /// <param name="basePath">Path on hard drive where we should start looking for files</param>
    /// <param name="useLastModifiedHeader">If true a Last-Modifed header will be sent upon requests urging web browser to cache files</param>
    public FileModule(string baseUri, string basePath, bool useLastModifiedHeader)
    {
      Check.Require(baseUri, "baseUri");
      Check.Require(basePath, "basePath");

      _useLastModifiedHeader = useLastModifiedHeader;
      _baseUri = baseUri;
      _basePath = basePath;
      if (!_basePath.EndsWith(PathSeparator))
        _basePath += PathSeparator;
      ForbiddenChars = DefaultForbiddenChars;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileModule"/> class.
    /// </summary>
    /// <param name="baseUri">Uri to serve, for instance "/files/"</param>
    /// <param name="basePath">Path on hard drive where we should start looking for files</param>
    public FileModule(string baseUri, string basePath)
        : this(baseUri, basePath, false)
    {
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
    /// characters that may not  exist in a path.
    /// </summary>
    /// <example>
    /// fileMod.ForbiddenChars = new string[]{ "\\", "..", ":" };
    /// </example>
    public string[] ForbiddenChars
    {
      get { return _forbiddenChars; }
      set { _forbiddenChars = value; }
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

      MimeTypes.Add("ico", "image/vnd.microsoft.icon");
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
    /// Determines if the request should be handled by this module.
    /// Invoked by the <see cref="HttpServer"/>
    /// </summary>
    /// <param name="uri"></param>
    /// <returns>true if this module should handle it.</returns>
    public bool CanHandle(Uri uri)
    {
      if (Contains(uri.AbsolutePath, _forbiddenChars))
        return false;

      return uri.AbsolutePath.StartsWith(_baseUri) && File.Exists(GetPath(uri));
    }

    /// <exception cref="BadRequestException">Illegal path</exception>
    private string GetPath(Uri uri)
    {
      if (Contains(uri.AbsolutePath, _forbiddenChars))
        throw new BadRequestException("Illegal path");

      string path = _basePath + uri.AbsolutePath.Substring(_baseUri.Length);
      return path.Replace('/', Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// check if source contains any of the chars.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="chars"></param>
    /// <returns></returns>
    private static bool Contains(string source, IEnumerable<string> chars)
    {
      foreach (string s in chars)
      {
        if (source.Contains(s))
          return true;
      }

      return false;
    }

    /// <summary>
    /// Method that process the Uri.
    /// </summary>
    /// <param name="request">Information sent by the browser about the request</param>
    /// <param name="response">Information that is being sent back to the client.</param>
    /// <param name="session">Session used to </param>
    /// <exception cref="InternalServerException">Failed to find file extension</exception>
    /// <exception cref="ForbiddenException">File type is forbidden.</exception>
    public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      if (!CanHandle(request.Uri))
        return false;

      try
      {
        string path = GetPath(request.Uri);
        string extension = GetFileExtension(path);
        if (extension == null)
          throw new InternalServerException("Failed to find file extension");

        if (MimeTypes.ContainsKey(extension))
          response.ContentType = MimeTypes[extension];
        else
          throw new ForbiddenException("Forbidden file type: " + extension);

        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          if (!string.IsNullOrEmpty(request.Headers["if-Modified-Since"]))
          {
            DateTime lastRequest = DateTime.Parse(request.Headers["if-Modified-Since"]);
            if (lastRequest.CompareTo(File.GetLastWriteTime(path)) <= 0)
              response.Status = HttpStatusCode.NotModified;
          }

          if (_useLastModifiedHeader)
            response.AddHeader("Last-modified", File.GetLastWriteTime(path).ToString("r"));
          response.ContentLength = stream.Length;
          response.SendHeaders();

          if (request.Method != "Headers" && response.Status != HttpStatusCode.NotModified)
          {
            byte[] buffer = new byte[8192];
            int bytesRead = stream.Read(buffer, 0, 8192);
            while (bytesRead > 0)
            {
              response.SendBody(buffer, 0, bytesRead);
              bytesRead = stream.Read(buffer, 0, 8192);
            }
          }
        }
      }
      catch (FileNotFoundException err)
      {
        throw new InternalServerException("Failed to proccess file.", err);
      }

      return true;
    }

    /// <summary>
    /// return a file extension from an absolute Uri path (or plain filename)
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    public static string GetFileExtension(string uri)
    {
      int pos = uri.LastIndexOf('.');
      return pos == -1 ? null : uri.Substring(pos + 1);
    }
  }
}