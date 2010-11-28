#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.HttpModules;
using HttpServer.Sessions;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.Threading;

namespace MediaPortal.Core.Services.MediaManagement
{
  public class ResourceAccessModule : HttpModule
  {
    public const string DEFAULT_MIME_TYPE = "application/octet-stream";

    public static TimeSpan RESOURCE_CACHE_TIME = TimeSpan.FromMinutes(5);
    public static TimeSpan CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(1);

    protected class CachedResource : IDisposable
    {
      protected DateTime _lastTimeUsed;
      protected IResourceAccessor _resourceAccessor;

      public CachedResource(IResourceAccessor resourceAccessor)
      {
        _lastTimeUsed = DateTime.Now;
        _resourceAccessor = resourceAccessor;
      }

      public void Dispose()
      {
        _resourceAccessor.Dispose();
        _resourceAccessor = null;
      }

      public DateTime LastTimeUsed
      {
        get { return _lastTimeUsed; }
      }

      public IResourceAccessor ResourceAccessor
      {
         get
         {
           _lastTimeUsed = DateTime.Now;
           return _resourceAccessor;
         }
      }
    }

    protected readonly IDictionary<string, string> _mimeTypes = new Dictionary<string, string>();
    protected readonly IDictionary<string, CachedResource> _resourceAccessorCache = new Dictionary<string, CachedResource>(10);
    protected readonly object _syncObj = new object();

    public ResourceAccessModule()
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.AddIntervalWork(new IntervalWork(TidyUpResourceAccessorCache, CACHE_CLEANUP_INTERVAL), false);
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
    /// Mimtypes that this class can handle per default.
    /// </summary>
    protected void AddDefaultMimeTypes()
    {
      _mimeTypes.Add(".txt", "text/plain");
      _mimeTypes.Add(".html", "text/html");
      _mimeTypes.Add(".htm", "text/html");
      _mimeTypes.Add(".jpg", "image/jpg");
      _mimeTypes.Add(".jpeg", "image/jpg");
      _mimeTypes.Add(".bmp", "image/bmp");
      _mimeTypes.Add(".gif", "image/gif");
      _mimeTypes.Add(".png", "image/png");

      _mimeTypes.Add(".ico", "image/vnd.microsoft.icon");
      _mimeTypes.Add(".css", "text/css");
      _mimeTypes.Add(".gzip", "application/x-gzip");
      _mimeTypes.Add(".zip", "multipart/x-zip");
      _mimeTypes.Add(".tar", "application/x-tar");
      _mimeTypes.Add(".pdf", "application/pdf");
      _mimeTypes.Add(".rtf", "application/rtf");
      _mimeTypes.Add(".xls", "application/vnd.ms-excel");
      _mimeTypes.Add(".ppt", "application/vnd.ms-powerpoint");
      _mimeTypes.Add(".doc", "application/application/msword");
      _mimeTypes.Add(".js", "application/javascript");
      _mimeTypes.Add(".au", "audio/basic");
      _mimeTypes.Add(".snd", "audio/basic");
      _mimeTypes.Add(".es", "audio/echospeech");
      _mimeTypes.Add(".mp3", "audio/mpeg");
      _mimeTypes.Add(".mp2", "audio/mpeg");
      _mimeTypes.Add(".mid", "audio/midi");
      _mimeTypes.Add(".wav", "audio/x-wav");
      _mimeTypes.Add(".swf", "application/x-shockwave-flash");
      _mimeTypes.Add(".avi", "video/avi");
      _mimeTypes.Add(".rm", "audio/x-pn-realaudio");
      _mimeTypes.Add(".ram", "audio/x-pn-realaudio");
      _mimeTypes.Add(".aif", "audio/x-aiff");
    }

    protected IResourceAccessor GetResourceAccessor(ResourcePath resourcePath)
    {
      lock (_syncObj)
      {
        CachedResource resource;
        string resourcePathStr = resourcePath.Serialize();
        if (_resourceAccessorCache.TryGetValue(resourcePathStr, out resource))
          return resource.ResourceAccessor;
        // TODO: Security check. Only deliver resources which are located inside local shares.
        ServiceRegistration.Get<ILogger>().Debug("ResourceAccessModule: Access of resource '{0}'", resourcePathStr);
        IResourceAccessor result = resourcePath.CreateLocalResourceAccessor();
        _resourceAccessorCache[resourcePathStr] = new CachedResource(result);
        return result;
      }
    }

    protected void TidyUpResourceAccessorCache()
    {
      lock (_syncObj)
      {
        DateTime threshold = DateTime.Now - RESOURCE_CACHE_TIME;
        ICollection<string> removedResources = new List<string>(_resourceAccessorCache.Count);
        foreach (KeyValuePair<string, CachedResource> entry in _resourceAccessorCache)
        {
          CachedResource resource = entry.Value;
          if (resource.LastTimeUsed > threshold)
            continue;
          try
          {
            resource.Dispose();
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("ResourceAccessModule: Error disposing resource accessor '{0}'", e, entry.Key);
          }
          removedResources.Add(entry.Key);
        }
        foreach (string resourcePathStr in removedResources)
          _resourceAccessorCache.Remove(resourcePathStr);
      }
    }

    protected class Range
    {
      protected long _from;
      protected long _to;

      public Range(long from, long to)
      {
        _from = from;
        _to = to;
      }

      public long From
      {
        get { return _from; }
      }

      public long To
      {
        get { return _to; }
      }

      public long Length
      {
        get { return _to - _from + 1; }
      }
    }

    protected IList<Range> ParseRanges(string byteRangesSpecifier, long size)
    {
      IList<Range> result = new List<Range>();
      if (size == 0)
        return result;
      try
      {
        string[] tokens = byteRangesSpecifier.Split(new char[] {'='});
        if (tokens.Length == 2 && tokens[0].Trim() == "bytes")
          foreach (string rangeSpec in tokens[1].Split(new char[] {','}))
          {
            tokens = rangeSpec.Split(new char[] {'-'});
            if (tokens.Length != 2)
              return new Range[] {};
            if (!string.IsNullOrEmpty(tokens[0]))
              if (!string.IsNullOrEmpty(tokens[1]))
                result.Add(new Range(long.Parse(tokens[0]), long.Parse(tokens[1])));
              else
                result.Add(new Range(long.Parse(tokens[0]), size-1));
            else
              result.Add(new Range(Math.Max(0, size - long.Parse(tokens[1])), size-1));
          }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Debug("ResourceAccessModule: Received illegal Range header", e);
        // As specified in RFC2616, section 14.35.1, ignore invalid range header
      }
      return result;
    }

    protected void SendRange(IHttpResponse response, Stream resourceStream, Range range, bool onlyHeaders)
    {
      if (range.From > resourceStream.Length)
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
        response.SendHeaders();
        return;
      }
      response.Status = HttpStatusCode.PartialContent;
      response.ContentLength = range.Length;
      response.AddHeader("Content-Range", "bytes " + range.From + "-" + range.To + "/" + resourceStream.Length);
      response.SendHeaders();

      if (onlyHeaders)
        return;

      resourceStream.Seek(range.From, SeekOrigin.Begin);
      Send(response, resourceStream, (int) range.Length);
    }

    protected void SendWholeFile(IHttpResponse response, Stream resourceStream, bool onlyHeaders)
    {
      response.Status = HttpStatusCode.OK;
      response.ContentLength = resourceStream.Length;
      response.SendHeaders();

      if (onlyHeaders)
        return;

      Send(response, resourceStream, (int) resourceStream.Length);
    }

    protected void Send(IHttpResponse response, Stream resourceStream, int length)
    {
      const int BUF_LEN = 8192;
      byte[] buffer = new byte[BUF_LEN];
      int bytesRead = resourceStream.Read(buffer, 0, Math.Min(length, BUF_LEN));
      while (bytesRead > 0)
      {
        length -= bytesRead;
        response.SendBody(buffer, 0, bytesRead);
        bytesRead = resourceStream.Read(buffer, 0, Math.Min(length, BUF_LEN));
      }
    }

    protected bool IsAllowedToAccess(ResourcePath resourcePath)
    {
      // TODO: How to check safety? We don't have access to our shares store here... See also method IsAllowedToAccess
      // in UPnPResourceInformationServiceImpl
      return true;
    }

    /// <summary>
    /// Method that processes the Uri.
    /// </summary>
    /// <param name="request">Information sent by the browser about the request.</param>
    /// <param name="response">Information that is being sent back to the client.</param>
    /// <param name="session">Session object used during the client connection.</param>
    /// <returns><c>true</c> if this module is able to handle the request, else <c>false</c>.</returns>
    /// <exception cref="InternalServerException">If an exception occured in the server.</exception>
    /// <exception cref="ForbiddenException">If the file path is forbidden.</exception>
    public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      ResourcePath resourcePath;
      Uri uri = request.Uri;
      if (!uri.AbsolutePath.StartsWith(ResourceHttpAccessUrlUtils.RESOURCE_ACCESS_PATH))
        return false;
      if (!ResourceHttpAccessUrlUtils.ParseResourceURI(uri, out resourcePath))
        throw new BadRequestException(string.Format("Illegal request syntax. Correct syntax is '{0}'", ResourceHttpAccessUrlUtils.SYNTAX));
      if (!IsAllowedToAccess(resourcePath))
      {
        ServiceRegistration.Get<ILogger>().Warn("ResourceAccessModule: Client tries to access forbidden resource '{0}'", resourcePath);
        throw new ForbiddenException(string.Format("Access of resource '{0}' not allowed", resourcePath));
      }

      string extension = GetFileExtension(resourcePath.FileName);
      try
      {
        string contentType;
        if (extension != null && _mimeTypes.TryGetValue(extension, out contentType))
          response.ContentType = contentType;
        else
          response.ContentType = DEFAULT_MIME_TYPE;

        IResourceAccessor ra = GetResourceAccessor(resourcePath);
        using (Stream resourceStream = ra.OpenRead())
        {
          if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"]))
          {
            DateTime lastRequest = DateTime.Parse(request.Headers["If-Modified-Since"]);
            if (lastRequest.CompareTo(ra.LastChanged) <= 0)
              response.Status = HttpStatusCode.NotModified;
          }

          response.AddHeader("Last-Modified", ra.LastChanged.ToUniversalTime().ToString("r"));

          string byteRangesSpecifier = request.Headers["Range"];
          IList<Range> ranges = ParseRanges(byteRangesSpecifier, resourceStream.Length);
          bool onlyHeaders = request.Method == "Headers" || response.Status == HttpStatusCode.NotModified;
          if (ranges.Count == 1)
            // We only support one range
            SendRange(response, resourceStream, ranges[0], onlyHeaders);
          else
            SendWholeFile(response, resourceStream, onlyHeaders);
        }
      }
      catch (FileNotFoundException ex)
      {
        throw new InternalServerException(string.Format("Failed to proccess resource '{0}'", resourcePath), ex);
      }

      return true;
    }

    /// <summary>
    /// Return a file extension from an absolute Uri path (or plain filename).
    /// </summary>
    /// <param name="uri">The URI to check.</param>
    /// <returns>File extension including the '.' or <c>null</c>, if the <paramref name="uri"/> doesn't contain an
    /// extension.</returns>
    public static string GetFileExtension(string uri)
    {
      int pos = uri.LastIndexOf('.');
      return pos == -1 ? null : uri.Substring(pos);
    }
  }
}