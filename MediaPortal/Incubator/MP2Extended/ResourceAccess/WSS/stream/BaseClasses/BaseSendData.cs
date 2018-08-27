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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Threading;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using Microsoft.Owin;
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses
{
  class BaseSendData : IDisposable
  {
    protected readonly IDictionary<string, CachedResource> _resourceAccessorCache = new Dictionary<string, CachedResource>(10);
    protected IntervalWork _tidyUpCacheWork;
    protected readonly object _syncObj = new object();

    public static TimeSpan RESOURCE_CACHE_TIME = TimeSpan.FromMinutes(5);
    public static TimeSpan CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(1);
    public static CancellationTokenSource SendDataCancellation = new CancellationTokenSource();
    public const long TRANSCODED_VIDEO_STREAM_MAX = 50000000000L;
    public const long TRANSCODED_AUDIO_STREAM_MAX = 900000000L;
    public const long TRANSCODED_IMAGE_STREAM_MAX = 9000000L;
    public const long TRANSCODED_SUBTITLE_STREAM_MAX = 300000L;

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

    protected async Task SendRangeAsync(IOwinContext context, Stream resourceStream, Range range, bool onlyHeaders)
    {
      if (range.From > resourceStream.Length)
      {
        context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;
        return;
      }
      context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
      long length = resourceStream.Length;
      if (length <= 0)
      {
        context.Response.Headers["Content-Range"] = $"bytes {range.From}-{range.To - 1}";
        context.Response.ContentLength = range.Length;
      }
      else
      {
        context.Response.Headers["Content-Range"] = $"bytes {range.From}-{range.To - 1}/{length}";
        context.Response.ContentLength = range.Length;
      }

      if (onlyHeaders)
        return;

      resourceStream.Seek(range.From, SeekOrigin.Begin);
      await SendAsync(context, resourceStream, range.Length);
    }

    protected async Task SendWholeFileAsync(IOwinContext context, Stream resourceStream, bool onlyHeaders)
    {
      if (context.Response.StatusCode != (int)HttpStatusCode.NotModified) // respect the If-Modified-Since Header
        context.Response.StatusCode = (int)HttpStatusCode.OK;
      context.Response.ContentLength = resourceStream.Length;

      if (onlyHeaders)
        return;

      await SendAsync(context, resourceStream, resourceStream.Length);
    }

    protected async Task SendWholeFileAsync(IOwinContext context, Stream resourceStream, ProfileMediaItem item, EndPointProfile profile, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      if (await WaitForMinimumFileSizeAsync(resourceStream, 1) == false)
      {
        context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;
        Logger.Debug("BaseSendData: Sending headers: " + string.Join(";", context.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));
        return;
      }

      long length = GetStreamSize(item);
      if (resourceStream.CanSeek == true && (item.IsTranscoding == false || item.IsSegmented == true))
      {
        length = resourceStream.Length;
      }

      if (resourceStream.CanSeek == false && context.Request.Protocol == "HTTP/1.1" && profile.Settings.Communication.AllowChunckedTransfer)
      {
        context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
        context.Response.ContentLength = null;
      }
      else
      {
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.ContentLength = length;
      }

      Range byteRange = new Range(0, context.Response.ContentLength ?? 0);
      await SendAsync(context, resourceStream, item, profile, onlyHeaders, partialResource, byteRange);
    }

    protected async Task SendAsync(IOwinContext context, Stream resourceStream, long length)
    {
      context.Response.StatusCode = (int)HttpStatusCode.OK;
      context.Response.ContentLength = resourceStream.Length;

      Logger.Debug("Sending data");
      resourceStream.Seek(0, SeekOrigin.Begin);
      const int BUF_LEN = 8192;
      byte[] buffer = new byte[BUF_LEN];
      int bytesRead;
      long count = 0;

      while ((bytesRead = resourceStream.Read(buffer, 0, length > BUF_LEN ? BUF_LEN : (int)length)) > 0)
      {
        length -= bytesRead;
        count += bytesRead;
        await context.Response.WriteAsync(buffer, 0, bytesRead, SendDataCancellation.Token);
      }
      Logger.Debug("Sending data complete");
    }

    protected async Task SendAsync(IOwinContext context, Stream resourceStream, ProfileMediaItem item, EndPointProfile profile, bool onlyHeaders, bool partialResource, Range byteRange)
    {
      if (onlyHeaders)
        return;

      bool clientDisconnected = false;
      Guid streamID = item.StartStreaming();
      if (streamID == Guid.Empty)
      {
        Logger.Error("BaseSendData: Unable to start stream");
        return;
      }
      try
      {
        if (context.Response.ContentLength == 0)
        {
          //Not allowed to have a content length of zero
          context.Response.ContentLength = null;
        }
        Logger.Debug("BaseSendData: Sending chunked: {0}", context.Response.ContentLength == null);
        string clientID = context.Request.RemoteIpAddress;
        int bufferSize = profile.Settings.Communication.DefaultBufferSize;
        if (bufferSize <= 0)
        {
          bufferSize = 1500;
        }
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        long count = 0;
        bool isStream = false;
        long waitForSize = 0;
        if (byteRange.Length == 0 || (byteRange.Length > 0 && byteRange.Length >= profile.Settings.Communication.InitialBufferSize))
        {
          waitForSize = profile.Settings.Communication.InitialBufferSize;
        }
        if (partialResource == false)
        {
          if (waitForSize < byteRange.From) waitForSize = byteRange.From;
        }
        if (await WaitForMinimumFileSizeAsync(resourceStream, waitForSize) == false)
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
          bytesRead = await resourceStream.ReadAsync(buffer, 0, length > bufferSize ? bufferSize : (int)length);
          count += bytesRead;

          if (bytesRead > 0)
          {
            emptyCount = 0;
            try
            {
              //Send fetched bytes
              await context.Response.WriteAsync(buffer, 0, bytesRead, SendDataCancellation.Token);
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
              await Task.Delay(100);
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
      }
      finally
      {
        item.StopStreaming(streamID);

        if (clientDisconnected || item.IsSegmented == false)
        {
          if (clientDisconnected == false)
          {
            //Everything sent to client so presume watched
            if (item.IsLive == false)
            {
              Guid? userId = ResourceAccessUtils.GetUser(context);
              IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
              if(library != null && userId.HasValue)
                library.NotifyUserPlayback(userId.Value, item.MediaSource.MediaItemId, 100, true);
            }
          }
        }
        Logger.Debug("BaseSendData: Sending complete");
      }
    }

    protected async Task SendByteRangeAsync(IOwinContext context, Stream resourceStream, ProfileMediaItem item, EndPointProfile profile, Range range, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      if (range.From > 0 && range.From == range.To)
      {
        context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;
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
        context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;
        return;
      }
      if (partialResource == false && await WaitForMinimumFileSizeAsync(resourceStream, fileRange.From) == false)
      {
        context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;
        return;
      }
      if (range.From > length || range.To > length)
      {
        range = fileRange;
      }

      context.Response.StatusCode = (int)HttpStatusCode.PartialContent;

      if (item.IsLive || range.Length == 0 ||
        (mediaTransferMode == TransferMode.Streaming && context.Request.Protocol == "HTTP/1.1" && profile.Settings.Communication.AllowChunckedTransfer))
      {
        context.Response.Headers["Content-Range"] = $"bytes {range.From}-";
        context.Response.ContentLength = null;
      }
      else if (length <= 0)
      {
        context.Response.Headers["Content-Range"] = $"bytes {range.From}-{range.To - 1}";
        context.Response.ContentLength = range.Length;
      }
      else
      {
        context.Response.Headers["Content-Range"] = $"bytes {range.From}-{range.To - 1}/{length}";
        context.Response.ContentLength = range.Length;
      }
      if (item.IsLive == false)
      {
        context.Response.Headers["X-Content-Duration"] = Convert.ToDouble(item.WebMetadata.Metadata.Duration).ToString("0.00", CultureInfo.InvariantCulture);
        context.Response.Headers["Content-Duration"] = Convert.ToDouble(item.WebMetadata.Metadata.Duration).ToString("0.00", CultureInfo.InvariantCulture);
      }

      await SendAsync(context, resourceStream, item, profile, onlyHeaders, partialResource, fileRange);
    }

    protected Range ConvertToByteRange(Range timeRange, ProfileMediaItem item)
    {
      if (timeRange.Length <= 0.0)
      {
        return new Range(0, item.WebMetadata.Metadata.Size ?? 0);
      }
      long startByte = 0;
      long endByte = 0;
      if (item.IsTranscoding == true)
      {
        long length = GetStreamSize(item);
        double factor = Convert.ToDouble(length) / Convert.ToDouble(item.WebMetadata.Metadata.Duration);
        startByte = Convert.ToInt64(Convert.ToDouble(timeRange.From) * factor);
        endByte = Convert.ToInt64(Convert.ToDouble(timeRange.To) * factor);
      }
      else
      {
        double bitrate = 0;
        if (item.IsSegmented == false)
        {
          bitrate = Convert.ToDouble(item.WebMetadata.Metadata.Bitrate) * 1024; //Bitrate in bits/s
        }
        startByte = Convert.ToInt64((bitrate * timeRange.From) / 8.0);
        endByte = Convert.ToInt64((bitrate * timeRange.To) / 8.0);
      }
      return new Range(startByte, endByte);
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
        double factor = Convert.ToDouble(item.WebMetadata.Metadata.Duration) / Convert.ToDouble(length);
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

    internal async Task<bool> WaitForMinimumFileSizeAsync(Stream resourceStream, long minimumSize)
    {
      if (resourceStream.CanSeek == false)
        return resourceStream.CanRead;

      int iTry = 20;
      while (iTry > 0 && minimumSize > resourceStream.Length)
      {
        await Task.Delay(100);
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
      long length = item?.WebMetadata?.Metadata?.Size ?? 0;
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
