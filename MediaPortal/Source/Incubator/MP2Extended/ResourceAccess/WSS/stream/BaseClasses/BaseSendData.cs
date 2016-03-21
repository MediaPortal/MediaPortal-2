#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Http;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Threading;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses
{
  class BaseSendData : IDisposable
  {
    protected readonly IDictionary<string, CachedResource> _resourceAccessorCache = new Dictionary<string, CachedResource>(10);
    protected IntervalWork _tidyUpCacheWork;
    protected readonly object _syncObj = new object();

    public static TimeSpan RESOURCE_CACHE_TIME = TimeSpan.FromMinutes(5);
    public static TimeSpan CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(1);
    public const long TRANSCODED_VIDEO_STREAM_MAX = 50000000000L;
    public const long TRANSCODED_AUDIO_STREAM_MAX = 900000000L;
    public const long TRANSCODED_IMAGE_STREAM_MAX = 9000000L;
    public const long TRANSCODED_SUBTITLE_STREAM_MAX = 300000L;

    private byte[] _chunkDelimiter = new byte[] { 13, 10 };
    private byte[] _chunkEnd = new byte[] { 48, 13, 10, 13, 10 };

    #region Enum

    protected enum StreamMode
    {
      Unknown,
      Normal,
      ByteRange
    }

    protected enum TransferMode
    {
      Unknown,
      Streaming,
      Interactive
    }

    #endregion Enum

    #region send

    protected void SendRange(HttpContext httpContext, Stream resourceStream, Range range, bool onlyHeaders)
    {
      if (range.From > resourceStream.Length)
      {
        httpContext.Response.StatusCode = StatusCodes.Status416RequestedRangeNotSatisfiable;
        return;
      }
      httpContext.Response.StatusCode = StatusCodes.Status206PartialContent;
      httpContext.Response.ContentLength = range.Length;
      httpContext.Response.Headers.Add("Content-Range", String.Format("bytes {0}-{1}/{2}", range.From, range.To, resourceStream.Length));

      if (onlyHeaders)
        return;

      resourceStream.Seek(range.From, SeekOrigin.Begin);
      Send(httpContext, resourceStream, range.Length);
    }

    protected void SendWholeFile(HttpContext httpContext, Stream resourceStream, bool onlyHeaders)
    {
      if (httpContext.Response.StatusCode != StatusCodes.Status304NotModified) // respect the If-Modified-Since Header
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
      httpContext.Response.ContentLength = resourceStream.Length;

      if (onlyHeaders)
        return;

      Send(httpContext, resourceStream, resourceStream.Length);
    }

    protected void SendWholeFile(HttpContext httpContext, Stream resourceStream, ProfileMediaItem item, EndPointSettings client, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      if (WaitForMinimumFileSize(resourceStream, 1) == false)
      {
        httpContext.Response.StatusCode = StatusCodes.Status416RequestedRangeNotSatisfiable;
        // TODO: fix?!
        //response.Chunked = false;
        httpContext.Response.ContentLength = 0;
        httpContext.Response.ContentType = null;
        Logger.Debug("BaseSendData: Sending headers: " + string.Join(";", httpContext.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));
        return;
      }

      long length = GetStreamSize(item);
      if (resourceStream.CanSeek == true && (item.IsTranscoding == false || item.IsSegmented == true))
      {
        length = resourceStream.Length;
      }

      bool chunked = false;
      if (resourceStream.CanSeek == false && httpContext.Request.Protocol == HttpHelper.HTTP11 && client.Profile.Settings.Communication.AllowChunckedTransfer)
      {
        httpContext.Response.StatusCode = StatusCodes.Status206PartialContent;
        httpContext.Response.ContentLength = 0;
        chunked = true;
      }
      else
      {
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentLength = length;
      }

      Range byteRange = new Range(0, httpContext.Response.ContentLength ?? 0);
      Send(httpContext, resourceStream, item, client, onlyHeaders, partialResource, byteRange, chunked);
    }

    protected void Send(HttpContext httpContext, Stream resourceStream, long length)
    {
      const int BUF_LEN = 8192;
      byte[] buffer = new byte[BUF_LEN];
      int bytesRead;
      while ((bytesRead = resourceStream.Read(buffer, 0, length > BUF_LEN ? BUF_LEN : (int)length)) > 0 && httpContext.Response.Body.CanWrite)
      // Don't use Math.Min since (int) length is negative for length > Int32.MaxValue
      {
        try
        {
          httpContext.Response.Body.Write(buffer, 0, bytesRead);
          length -= bytesRead;
        }
        catch (Exception)
        {
          // Client disconnected
          break;
        }

      }
    }

    protected void Send(HttpContext httpContext, Stream resourceStream, ProfileMediaItem item, EndPointSettings client, bool onlyHeaders, bool partialResource, Range byteRange, bool chunked)
    {
      if (chunked)
      {
        httpContext.Response.StatusCode = StatusCodes.Status206PartialContent;
        httpContext.Response.Headers[HeaderNames.TransferEncoding] = "chunked";
        httpContext.Response.ContentLength = null;
      }
      if(httpContext.Response.ContentLength == 0)
      {
        //Not allowed to have a content length of zero
        httpContext.Response.ContentLength = null;
      }

      Logger.Debug("BaseSendData: Sending headers: " + string.Join(";", httpContext.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));

      if (onlyHeaders)
        return;

      Guid streamID = item.StartStreaming();
      if (streamID == Guid.Empty)
      {
        Logger.Error("BaseSendData: Unable to start stream");
        return;
      }
      try
      {
        Logger.Debug("Sending chunked: {0}", chunked);
        string clientID = httpContext.Request.Headers["remote_addr"];
        int bufferSize = client.Profile.Settings.Communication.DefaultBufferSize;
        if (bufferSize <= 0)
        {
          bufferSize = 1500;
        }
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        long count = 0;
        bool isStream = false;
        bool clientDisconnected = false;
        long waitForSize = 0;
        if (byteRange.Length == 0 || (byteRange.Length > 0 && byteRange.Length >= client.Profile.Settings.Communication.InitialBufferSize))
        {
          waitForSize = client.Profile.Settings.Communication.InitialBufferSize;
        }
        if (partialResource == false)
        {
          if (waitForSize < byteRange.From) waitForSize = byteRange.From;
        }
        if (WaitForMinimumFileSize(resourceStream, waitForSize) == false)
        {
          Logger.Error("BaseSendData: Unable to send stream because of invalid length: {0} ({1} required)", resourceStream.Length, waitForSize);
          return;
        }

        long start = 0;
        if (partialResource == false)
        {
          start = byteRange.From;
        }
        if (resourceStream.CanSeek)
          resourceStream.Seek(start, SeekOrigin.Begin);
        long length = byteRange.Length;
        if (length <= 0 || item.IsLive || (item.IsSegmented == false && item.IsTranscoding == true))
        {
          isStream = true;
        }
        int emptyCount = 0;
        while (item.IsStreamActive(streamID))
        {
          if (isStream)
          {
            if (resourceStream.CanSeek)
              length = resourceStream.Length - count;
            else
              length = bufferSize; //Keep stream alive
          }

          bytesRead = resourceStream.Read(buffer, 0, length > bufferSize ? bufferSize : (int)length);
          count += bytesRead;

          if (httpContext.Response.Body.CanWrite == false)
          {
            //Can no longer write to client
            Logger.Debug("BaseSendData: Connection lost after {0} bytes", count);
            clientDisconnected = true;
            break;
          }

          if (bytesRead > 0)
          {
            emptyCount = 0;
            try
            {
              //Send fetched bytes
              if (chunked) SendChunk(httpContext, buffer, 0, bytesRead);
              else httpContext.Response.Body.Write(buffer, 0, bytesRead);
            }
            catch (Exception)
            {
              // Client disconnected
              Logger.Debug("BaseSendData: Connection lost after {0} bytes", count);
              clientDisconnected = true;
              break;
            }
            length -= bytesRead;

            if (isStream == false && length <= 0)
            {
              //All bytes in the requested range sent
              break;
            }
          }
          else
          {
            emptyCount++;
            if (emptyCount > 2)
            {
              Logger.Debug("BaseSendData: Buffer underrun delay");
              Thread.Sleep(100);
            }
            if (emptyCount > 10)
            {
              //Stream is not getting any bigger
              break;
            }
          }

          if (resourceStream.CanSeek)
          {
            if (item.IsTranscoding == false && resourceStream.Position == resourceStream.Length)
            {
              //No more data will be available
              break;
            }
          }
        }
        if (chunked && clientDisconnected == false)
        {
          SendChunk(httpContext, null, 0, 0);
        }
      }
      finally
      {
        item.StopStreaming(streamID);
        Logger.Debug("BaseSendData: Sending complete");
      }
    }

    private void SendChunk(HttpContext httpContext, byte[] data, int offset, int count)
    {
      if (data != null)
      {
        if (httpContext.Response.Body.CanWrite)
        {
          //Logger.Debug("BaseSendData: Chunk of size {0}", count);
          byte[] _dataSize = Encoding.ASCII.GetBytes(Convert.ToString(count - offset, 16));
          httpContext.Response.Body.Write(_dataSize, 0, _dataSize.Length);
          httpContext.Response.Body.Write(_chunkDelimiter, 0, _chunkDelimiter.Length);
          httpContext.Response.Body.Write(data, offset, count);
          httpContext.Response.Body.Write(_chunkDelimiter, 0, _chunkDelimiter.Length);
          httpContext.Response.Body.Flush(); // Bypass IIS write-behind buffering
        }
      }
      else
      {
        if (httpContext.Response.Body.CanWrite)
        {
          Logger.Debug("BaseSendData: Sending final chunck");
          httpContext.Response.Body.Write(_chunkEnd, 0, _chunkEnd.Length);
          httpContext.Response.Body.Flush(); // Bypass IIS write-behind buffering
        }
      }
    }

    protected void SendByteRange(HttpContext httpContext, Stream resourceStream, ProfileMediaItem item, EndPointSettings client, Range range, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      if (range.From > 0 && range.From == range.To)
      {
        httpContext.Response.StatusCode = StatusCodes.Status416RequestedRangeNotSatisfiable;
        httpContext.Response.ContentLength = 0;
        httpContext.Response.ContentType = null;
        Logger.Debug("BaseSendData: Sending headers: " + string.Join(";", httpContext.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));
        return;
      }
      long length = range.Length;
      if (item.IsSegmented == false && item.IsTranscoding == true)
      {
        length = GetStreamSize(item);
      }
      else
      {
        length = resourceStream.Length;
      }
      Range fileRange = ConvertToFileRange(range, item, length);
      if (fileRange.From < 0 || length <= fileRange.From)
      {
        httpContext.Response.StatusCode = StatusCodes.Status416RequestedRangeNotSatisfiable;
        httpContext.Response.ContentLength = 0;
        httpContext.Response.ContentType = null;
        Logger.Debug("BaseSendData: Sending headers: " + string.Join(";", httpContext.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));
        return;
      }
      if (partialResource == false && WaitForMinimumFileSize(resourceStream, fileRange.From) == false)
      {
        httpContext.Response.StatusCode = StatusCodes.Status416RequestedRangeNotSatisfiable;
        httpContext.Response.ContentLength = 0;
        httpContext.Response.ContentType = null;
        Logger.Debug("BaseSendData: Sending headers: " + string.Join(";", httpContext.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));
        return;
      }
      if (range.From > length || range.To > length)
      {
        range = fileRange;
      }

      httpContext.Response.StatusCode = StatusCodes.Status206PartialContent;
      httpContext.Response.ContentLength = range.Length;

      if (item.IsLive ||range.Length == 0)
      {
        httpContext.Response.Headers.Add("Content-Range", string.Format("bytes {0}-", range.From));
      }
      else if (length <= 0)
      {
        httpContext.Response.Headers.Add("Content-Range", string.Format("bytes {0}-{1}", range.From, range.To - 1));
      }
      else
      {
        httpContext.Response.Headers.Add("Content-Range", string.Format("bytes {0}-{1}/{2}", range.From, range.To - 1, length));
      }
      if (item.IsLive == false)
      {
        httpContext.Response.Headers.Add("X-Content-Duration", item.WebMetadata.Metadata.Duration.ToString("0.00", CultureInfo.InvariantCulture));
        httpContext.Response.Headers.Add("Content-Duration", item.WebMetadata.Metadata.Duration.ToString("0.00", CultureInfo.InvariantCulture));
      }

      bool chunked = false;
      if (mediaTransferMode == TransferMode.Streaming && httpContext.Request.Protocol == HttpHelper.HTTP11 && client.Profile.Settings.Communication.AllowChunckedTransfer)// && item.IsTranscoding == true)
      {
        chunked = true;
      }

      Send(httpContext, resourceStream, item, client, onlyHeaders, partialResource, fileRange, chunked);
    }

    protected Range ConvertToFileRange(Range requestedByteRange, ProfileMediaItem item, long length)
    {
      long toRange = requestedByteRange.To;
      long fromRange = requestedByteRange.From;
      if (toRange <= 0 || toRange > length)
      {
        toRange = length;
      }
      if (item.IsSegmented == false && item.IsTranscoding == true)
      {
        if (item.WebMetadata.Metadata.Size > 0 && (toRange > item.WebMetadata.Metadata.Size || fromRange > item.WebMetadata.Metadata.Size))
        {
          fromRange = Convert.ToInt64((Convert.ToDouble(fromRange) / Convert.ToDouble(length)) * Convert.ToDouble(item.WebMetadata.Metadata.Size));
          toRange = Convert.ToInt64((Convert.ToDouble(toRange) / Convert.ToDouble(length)) * Convert.ToDouble(item.WebMetadata.Metadata.Size));
        }
      }
      return new Range(fromRange, toRange);
    }

    protected Range ConvertToTimeRange(Range byteRange, ProfileMediaItem item)
    {
      if (byteRange.Length <= 0.0)
      {
        return new Range(0, Convert.ToInt64(item.WebMetadata.Metadata.Duration));
      }

      double startSeconds = 0;
      double endSeconds = 0;
      if (item.IsTranscoding == true)
      {
        long length = GetStreamSize(item);
        double factor = item.WebMetadata.Metadata.Duration / Convert.ToDouble(length);
        startSeconds = Convert.ToDouble(byteRange.From) * factor;
        endSeconds = Convert.ToDouble(byteRange.To) * factor;
      }
      else
      {
        double bitrate = 0;
        if (item.IsSegmented == false)
        {
          bitrate = Convert.ToDouble(item.WebMetadata.Metadata.Bitrate) * 1024; //Bitrate in bits/s
        }
        if (bitrate > 0)
        {
          startSeconds = Convert.ToDouble(byteRange.From) / (bitrate / 8.0);
          endSeconds = Convert.ToDouble(byteRange.To) / (bitrate / 8.0);
        }
      }
      return new Range(Convert.ToInt64(startSeconds), Convert.ToInt64(endSeconds));
    }

    #endregion send

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

    protected IResourceAccessor GetResourceAccessor(ResourcePath resourcePath)
    {
      lock (_syncObj)
      {
        CachedResource resource;
        string resourcePathStr = resourcePath.Serialize();
        if (_resourceAccessorCache.TryGetValue(resourcePathStr, out resource))
          return resource.ResourceAccessor;
        // TODO: Security check. Only deliver resources which are located inside local shares.
        ServiceRegistration.Get<ILogger>().Debug("BaseSendData: Access of resource '{0}'", resourcePathStr);
        IResourceAccessor result;
        if (resourcePath.TryCreateLocalResourceAccessor(out result))
        {
          _resourceAccessorCache[resourcePathStr] = new CachedResource(result);
        }
        return result;
      }
    }

    internal bool WaitForMinimumFileSize(Stream resourceStream, long minimumSize)
    {
      if (resourceStream.CanSeek == false)
        return resourceStream.CanRead;

      int iTry = 20;
      while (iTry > 0 && minimumSize > resourceStream.Length)
      {
        Thread.Sleep(100);
        iTry--;
      }
      if (iTry <= 0)
      {
        return false;
      }
      return true;
    }

    internal long GetStreamSize(ProfileMediaItem item)
    {
      long length = item.WebMetadata.Metadata.Size;
      if (item.IsTranscoding == true || item.IsLive == true || length <= 0)
      //if (length <= 0)
      {
        if (item.IsAudio) return TRANSCODED_AUDIO_STREAM_MAX;
        else if (item.IsImage) return TRANSCODED_IMAGE_STREAM_MAX;
        else if (item.IsVideo) return TRANSCODED_VIDEO_STREAM_MAX;
        return TRANSCODED_VIDEO_STREAM_MAX;
      }
      return length;
    }

    #region cache

    protected class CachedResource : IDisposable
    {
      private IResourceAccessor _resourceAccessor;

      public CachedResource(IResourceAccessor resourceAccessor)
      {
        LastTimeUsed = DateTime.Now;
        _resourceAccessor = resourceAccessor;
      }

      public void Dispose()
      {
        _resourceAccessor.Dispose();
        _resourceAccessor = null;
      }

      public DateTime LastTimeUsed { get; private set; }

      public IResourceAccessor ResourceAccessor
      {
        get
        {
          LastTimeUsed = DateTime.Now;
          return _resourceAccessor;
        }
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
            ServiceRegistration.Get<ILogger>().Warn("BaseSendData: Error disposing resource accessor '{0}'", e, entry.Key);
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
            ServiceRegistration.Get<ILogger>().Warn("BaseSendData: Error disposing resource accessor '{0}'", e, entry.Key);
          }
        }
        _resourceAccessorCache.Clear();
      }
    }

    #endregion cache

    #region parse ranges

    protected IList<Range> ParseByteRanges(string byteRangesSpecifier, long size)
    {
      if (string.IsNullOrEmpty(byteRangesSpecifier))
        return null;
      IList<Range> result = new List<Range>();
      try
      {
        string[] tokens = byteRangesSpecifier.Split(new char[] { '=', ':' });
        if (tokens.Length == 2 && tokens[0].Trim() == "bytes")
          foreach (string rangeSpec in tokens[1].Split(new char[] { ',' }))
          {
            tokens = rangeSpec.Split(new char[] { '-' });
            if (tokens.Length != 2)
              return new Range[] { };
            long start = 0;
            long end = 0;
            if (!string.IsNullOrEmpty(tokens[0]))
            {
              start = long.Parse(tokens[0]);
              if (!string.IsNullOrEmpty(tokens[1]))
              {
                end = long.Parse(tokens[1]);
              }
              else if (start < size)
              {
                end = size;
              }
            }
            else
            {
              start = Math.Max(0, size - long.Parse(tokens[1]));
              end = size;
            }
            result.Add(new Range(start, end));
          }
      }
      catch (Exception e)
      {
        Logger.Debug("BaseSendData: Received illegal Range header", e);
        // As specified in RFC2616, section 14.35.1, ignore invalid range header
      }
      return result;
    }

    internal class Range
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
        get
        {
          if (_to <= _from) return 0;
          return _to - _from;
        }
      }
    }

    #endregion parse ranges

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
