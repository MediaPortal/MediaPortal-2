#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Threading;
using MediaPortal.Utilities.SystemAPI;
using Microsoft.Owin;

namespace MediaPortal.Common.Services.ResourceAccess
{
  public class ResourceAccessModule : OwinMiddleware, IDisposable
  {
    public const string DEFAULT_MIME_TYPE = "application/octet-stream";

    public static TimeSpan RESOURCE_CACHE_TIME = TimeSpan.FromMinutes(5);
    public static TimeSpan CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(1);

    protected class CachedResource : IDisposable
    {
      protected DateTime _lastTimeUsed;
      protected IFileSystemResourceAccessor _resourceAccessor;

      public CachedResource(IFileSystemResourceAccessor resourceAccessor)
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

      public IFileSystemResourceAccessor ResourceAccessor
      {
        get
        {
          _lastTimeUsed = DateTime.Now;
          return _resourceAccessor;
        }
      }
    }

    protected readonly IDictionary<string, string> _defaultMimeTypes = new Dictionary<string, string>();
    protected readonly IDictionary<string, CachedResource> _resourceAccessorCache = new Dictionary<string, CachedResource>(10);
    protected IntervalWork _tidyUpCacheWork;
    protected readonly object _syncObj = new object();

    public ResourceAccessModule(OwinMiddleware next) : base(next)
    {
      AddDefaultMimeTypes();
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      _tidyUpCacheWork = new IntervalWork(TidyUpResourceAccessorCache, CACHE_CLEANUP_INTERVAL);
      threadPool.AddIntervalWork(_tidyUpCacheWork, false);
    }

    public void Dispose()
    {
      if (_tidyUpCacheWork != null)
      {
        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
        threadPool.RemoveIntervalWork(_tidyUpCacheWork);
        _tidyUpCacheWork = null;
      }
      ClearResourceAccessorCache();
    }

    /// <summary>
    /// List with all mime-type that are allowed. 
    /// </summary>
    /// <remarks>All other mime types will result in a Forbidden http status code.</remarks>
    public IDictionary<string, string> DefaultMimeTypes
    {
      get { return _defaultMimeTypes; }
    }

    /// <summary>
    /// Mimtypes that this class can handle per default.
    /// </summary>
    protected void AddDefaultMimeTypes()
    {
      _defaultMimeTypes.Add(".txt", "text/plain");
      _defaultMimeTypes.Add(".html", "text/html");
      _defaultMimeTypes.Add(".htm", "text/html");
      _defaultMimeTypes.Add(".jpg", "image/jpg");
      _defaultMimeTypes.Add(".jpeg", "image/jpg");
      _defaultMimeTypes.Add(".bmp", "image/bmp");
      _defaultMimeTypes.Add(".gif", "image/gif");
      _defaultMimeTypes.Add(".png", "image/png");

      _defaultMimeTypes.Add(".ico", "image/vnd.microsoft.icon");
      _defaultMimeTypes.Add(".css", "text/css");
      _defaultMimeTypes.Add(".gzip", "application/x-gzip");
      _defaultMimeTypes.Add(".zip", "multipart/x-zip");
      _defaultMimeTypes.Add(".tar", "application/x-tar");
      _defaultMimeTypes.Add(".pdf", "application/pdf");
      _defaultMimeTypes.Add(".rtf", "application/rtf");
      _defaultMimeTypes.Add(".xls", "application/vnd.ms-excel");
      _defaultMimeTypes.Add(".ppt", "application/vnd.ms-powerpoint");
      _defaultMimeTypes.Add(".doc", "application/application/msword");
      _defaultMimeTypes.Add(".js", "application/javascript");
      _defaultMimeTypes.Add(".au", "audio/basic");
      _defaultMimeTypes.Add(".snd", "audio/basic");
      _defaultMimeTypes.Add(".es", "audio/echospeech");
      _defaultMimeTypes.Add(".mp3", "audio/mpeg");
      _defaultMimeTypes.Add(".mp2", "audio/mpeg");
      _defaultMimeTypes.Add(".mid", "audio/midi");
      _defaultMimeTypes.Add(".wav", "audio/x-wav");
      _defaultMimeTypes.Add(".swf", "application/x-shockwave-flash");
      _defaultMimeTypes.Add(".avi", "video/avi");
      _defaultMimeTypes.Add(".rm", "audio/x-pn-realaudio");
      _defaultMimeTypes.Add(".ram", "audio/x-pn-realaudio");
      _defaultMimeTypes.Add(".aif", "audio/x-aiff");
    }

    protected IFileSystemResourceAccessor GetResourceAccessor(ResourcePath resourcePath)
    {
      lock (_syncObj)
      {
        CachedResource resource;
        string resourcePathStr = resourcePath.Serialize();
        if (_resourceAccessorCache.TryGetValue(resourcePathStr, out resource))
          return resource.ResourceAccessor;
        // TODO: Security check. Only deliver resources which are located inside local shares.
        ServiceRegistration.Get<ILogger>().Debug("ResourceAccessModule: Access of resource '{0}'", resourcePathStr);
        IResourceAccessor ra;
        if (!resourcePath.TryCreateLocalResourceAccessor(out ra))
          throw new ArgumentException("Unable to access resource path '{0}'", resourcePathStr);
        IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
        if (fsra == null)
        {
          ra.Dispose();
          throw new ArgumentException("The given resource path '{0}' doesn't denote a file system resource", resourcePathStr);
        }
        _resourceAccessorCache[resourcePathStr] = new CachedResource(fsra);
        return fsra;
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

    protected void ClearResourceAccessorCache()
    {
      lock (_syncObj)
      {
        foreach (KeyValuePair<string, CachedResource> entry in _resourceAccessorCache)
        {
          CachedResource resource = entry.Value;
          try
          {
            resource.Dispose();
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("ResourceAccessModule: Error disposing resource accessor '{0}'", e, entry.Key);
          }
        }
        _resourceAccessorCache.Clear();
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
      if (string.IsNullOrEmpty(byteRangesSpecifier) || size == 0)
        return null;
      IList<Range> result = new List<Range>();
      try
      {
        string[] tokens = byteRangesSpecifier.Split(new char[] { '=' });
        if (tokens.Length == 2 && tokens[0].Trim() == "bytes")
          foreach (string rangeSpec in tokens[1].Split(new char[] { ',' }))
          {
            tokens = rangeSpec.Split(new char[] { '-' });
            if (tokens.Length != 2)
              return new Range[] { };
            if (!string.IsNullOrEmpty(tokens[0]))
              if (!string.IsNullOrEmpty(tokens[1]))
                result.Add(new Range(long.Parse(tokens[0]), long.Parse(tokens[1])));
              else
                result.Add(new Range(long.Parse(tokens[0]), size - 1));
            else
              result.Add(new Range(Math.Max(0, size - long.Parse(tokens[1])), size - 1));
          }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Debug("ResourceAccessModule: Received illegal Range header", e);
        // As specified in RFC2616, section 14.35.1, ignore invalid range header
      }
      return result;
    }

    protected async Task SendRange(IOwinResponse response, Stream resourceStream, Range range, bool onlyHeaders, CancellationToken ct)
    {
      if (range.From > resourceStream.Length)
      {
        response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
        return;
      }
      response.StatusCode = (int)HttpStatusCode.PartialContent;
      response.ContentLength = range.Length;
      response.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", range.From, range.To, resourceStream.Length);
      if (onlyHeaders)
        return;

      resourceStream.Seek(range.From, SeekOrigin.Begin);
      await Send(response, resourceStream, range.Length, ct);
    }

    protected async Task SendWholeFile(IOwinResponse response, Stream resourceStream, bool onlyHeaders, CancellationToken ct)
    {
      response.StatusCode = (int)HttpStatusCode.OK;
      response.ContentLength = resourceStream.Length;

      if (onlyHeaders)
        return;

      await Send(response, resourceStream, resourceStream.Length, ct);
    }

    protected async Task Send(IOwinResponse response, Stream resourceStream, long length, CancellationToken ct)
    {
      const int BUF_LEN = 8192;
      byte[] buffer = new byte[BUF_LEN];
      int bytesRead;
      while ((bytesRead = await resourceStream.ReadAsync(buffer, 0, length > BUF_LEN ? BUF_LEN : (int)length)) > 0) // Don't use Math.Min since (int) length is negative for length > Int32.MaxValue
      {
        length -= bytesRead;
        await response.WriteAsync(buffer, 0, bytesRead, ct);
      }
    }

    protected bool IsAllowedToAccess(ResourcePath resourcePath)
    {
      // TODO: How to check safety? We don't have access to our shares store here... See also method IsAllowedToAccess
      // in UPnPResourceInformationServiceImpl
      return true;
    }

    protected string GuessMimeType(Stream resourceStream, string fileName)
    {
      string mimeType = MimeTypeDetector.GetMimeType(resourceStream);
      resourceStream.Seek(0, SeekOrigin.Begin);
      if (mimeType != null)
        return mimeType;
      string extension = GetFileExtension(fileName);
      string contentType;
      if (extension != null && _defaultMimeTypes.TryGetValue(extension, out contentType))
        return contentType;
      return DEFAULT_MIME_TYPE;
    }

    /// <summary>
    /// Method that processes the Uri.
    /// </summary>
    public override async Task Invoke(IOwinContext context)
    {
      var request = context.Request;
      var response = context.Response;
      ResourcePath resourcePath;
      Uri uri = request.Uri;
      if (!uri.AbsolutePath.StartsWith(ResourceHttpAccessUrlUtils.RESOURCE_SERVER_BASE_PATH) || !uri.AbsolutePath.Contains(ResourceHttpAccessUrlUtils.RESOURCE_ACCESS_PATH))
      {
        await Next.Invoke(context);
        return;
      }
      if (!ResourceHttpAccessUrlUtils.ParseResourceURI(uri, out resourcePath))
      {
        response.StatusCode = (int)HttpStatusCode.BadRequest;
        response.ReasonPhrase = string.Format("Illegal request syntax. Correct syntax is '{0}'", ResourceHttpAccessUrlUtils.SYNTAX);
        return;
      }
      if (!IsAllowedToAccess(resourcePath))
      {
        ServiceRegistration.Get<ILogger>().Warn("ResourceAccessModule: Client tries to access forbidden resource '{0}'", resourcePath);
        response.StatusCode = (int)HttpStatusCode.Forbidden;
        response.ReasonPhrase = string.Format("Access of resource '{0}' not allowed", resourcePath);
        return;
      }

      try
      {
        IFileSystemResourceAccessor fsra = GetResourceAccessor(resourcePath);
        using (Stream resourceStream = fsra.OpenRead())
        {
          response.ContentType = GuessMimeType(resourceStream, resourcePath.FileName);

          if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"]))
          {
            DateTime lastRequest = DateTime.Parse(request.Headers["If-Modified-Since"], System.Globalization.CultureInfo.InvariantCulture);
            if (lastRequest.CompareTo(fsra.LastChanged) <= 0)
              response.StatusCode = (int)HttpStatusCode.NotModified;
          }

          response.Headers["Last-Modified"] = fsra.LastChanged.ToUniversalTime().ToString("r");

          string byteRangesSpecifier = request.Headers["Range"];
          IList<Range> ranges = ParseRanges(byteRangesSpecifier, resourceStream.Length);
          bool onlyHeaders = request.Method == "Headers" || response.StatusCode == (int)HttpStatusCode.NotModified;

          CancellationTokenSource cts = new CancellationTokenSource();
          if (ranges != null && ranges.Count == 1)
            // We only support one range
            await SendRange(response, resourceStream, ranges[0], onlyHeaders, cts.Token);
          else
            await SendWholeFile(response, resourceStream, onlyHeaders, cts.Token);
        }
      }
      catch (FileNotFoundException ex)
      {
        response.StatusCode = (int)HttpStatusCode.InternalServerError;
        response.ReasonPhrase = string.Format("Failed to proccess resource '{0}': {1}", resourcePath, ex.Message);
      }
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
