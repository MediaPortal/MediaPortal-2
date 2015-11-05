using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.General;
using MediaPortal.Plugins.Transcoding.Service;

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

    #region Enum

    protected enum StreamMode
    {
      Unknown,
      Normal,
      ByteRange,
      TimeRange
    }

    protected enum TransferMode
    {
      Unknown,
      Streaming,
      Interactive,
      Background
    }

    #endregion Enum
    
    #region send

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
      response.AddHeader("Content-Range", String.Format("bytes {0}-{1}/{2}", range.From, range.To, resourceStream.Length));
      response.SendHeaders();

      if (onlyHeaders)
        return;

      resourceStream.Seek(range.From, SeekOrigin.Begin);
      Send(response, resourceStream, range.Length);
    }

    protected void SendWholeFile(IHttpResponse response, Stream resourceStream, bool onlyHeaders)
    {
      response.Status = HttpStatusCode.OK;
      response.ContentLength = resourceStream.Length;
      response.SendHeaders();

      if (onlyHeaders)
        return;

      Send(response, resourceStream, resourceStream.Length);
    }

    protected void SendWholeFile(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, EndPointSettings client, bool onlyHeaders, TransferMode mediaTransferMode)
    {
      if (WaitForMinimumFileSize(resourceStream, 1) == false)
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;

        response.SendHeaders();

        return;
      }

      long length = resourceStream.Length;
      long streamLength = resourceStream.Length;
      if (item.IsSegmented == false && item.IsTranscoding == true)
      {
        streamLength = 0;
        length = item.DlnaMetadata.Metadata.Size;
        if (length == 0)
        {
          length = GetStreamSize(item);
        }
      }

      response.Status = HttpStatusCode.OK;
      response.ContentLength = length;
      response.Chunked = false;

      Send(request, response, resourceStream, item, client, onlyHeaders, 0, streamLength);
    }

    protected void Send(IHttpResponse response, Stream resourceStream, long length)
    {
      const int BUF_LEN = 8192;
      byte[] buffer = new byte[BUF_LEN];
      int bytesRead;
      while ((bytesRead = resourceStream.Read(buffer, 0, length > BUF_LEN ? BUF_LEN : (int)length)) > 0)
      // Don't use Math.Min since (int) length is negative for length > Int32.MaxValue
      {
        length -= bytesRead;
        response.SendBody(buffer, 0, bytesRead);
      }
    }

    protected void Send(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, EndPointSettings client, bool onlyHeaders, long start, long length)
    {
      response.SendHeaders();

      if (onlyHeaders)
        return;

      Guid streamId = item.StartStreaming();
      if (streamId == Guid.Empty)
      {
        Logger.Error("DlnaResourceAccessModule: Unable to start stream");
        return;
      }
      try
      {
        Logger.Debug("Sending chunked: {0}", response.Chunked.ToString());
        int bufferSize = client.Profile.Settings.Communication.DefaultBufferSize;
        if (bufferSize <= 0)
        {
          bufferSize = 1500;
        }
        byte[] buffer = new byte[bufferSize];
        long count = 0;
        bool bIsStream = false;
        long waitForSize = 0;
        if (length == 0 || (length > 0 && length >= client.Profile.Settings.Communication.InitialBufferSize))
        {
          waitForSize = client.Profile.Settings.Communication.InitialBufferSize;
        }
        if (start > waitForSize)
        {
          waitForSize = start;
        }
        if (WaitForMinimumFileSize(resourceStream, waitForSize) == false)
        {
          Logger.Error("DlnaResourceAccessModule: Unable to send stream beacause of invalid length: {0} ({1} required)", resourceStream.Length, start);
          return;
        }
        resourceStream.Seek(start, SeekOrigin.Begin);
        if (length <= 0)
        {
          bIsStream = true;
          length = resourceStream.Length;
        }
        while (item.IsStreamActive(streamId) && length > 0)
        {
          var bytesRead = resourceStream.Read(buffer, 0, length > bufferSize ? bufferSize : (int)length);
          count += bytesRead;
          if (bIsStream)
          {
            length = resourceStream.Length - count;
          }
          else
          {
            length -= bytesRead;
          }
          if (bytesRead > 0)
          {
            if (response.SendBody(buffer, 0, bytesRead) == false)
            {
              Logger.Debug("Connection lost after {0} bytes", count);
              break;
            }
          }
          if (item.IsTranscoding == false && resourceStream.Position == resourceStream.Length)
          {
            //No more data will be available
            length = 0;
          }
          if (item.IsSegmented == false && item.IsTranscoding)
          {
            while (bIsStream && item.IsStreamActive(streamId) && item.IsTranscoding && length == 0)
            {
              Thread.Sleep(10);
              length = resourceStream.Length - start - count;
              Logger.Debug("Buffer underrun delay {0}/{1}", count, resourceStream.Length - start);
            }
          }
        }
        if (response.Chunked)
        {
          response.SendBody(null, 0, 0);
          Logger.Debug("Sending final chunck");
        }
      }
      finally
      {
        // closes the Stream so that FFMpeg can replace the playlist file in case of HLS
        resourceStream.Close();
        item.StopStreaming(streamId);
        Logger.Debug("Sending complete");
      }
    }

    protected void SendTimeRange(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, EndPointSettings client, Range range, bool onlyHeaders, TransferMode mediaTransferMode)
    {
      if (item.IsTranscoding)
      {
        //Transcoding delay
        Thread.Sleep(1000);
      }
      double duration = item.DlnaMetadata.Metadata.Duration;
      if (range.From > Convert.ToInt64(duration))
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;

        response.SendHeaders();
        return;
      }
      double bitrate = 0;
      if (item.IsSegmented == false)
      {
        bitrate = Convert.ToDouble(item.DlnaMetadata.Metadata.Bitrate) * 1024; //Bitrate in bits/s
      }

      long lengthByte = Convert.ToInt64((bitrate * duration) / 8.0);
      response.Status = HttpStatusCode.PartialContent;
      response.ContentLength = lengthByte;
      response.AddHeader("TimeSeekRange.dlna.org", duration == 0 ? String.Format("npt={0}-", range.From) : String.Format("npt={0}-{1}/{2}", range.From, range.To, Convert.ToInt64(duration)));

      if (mediaTransferMode == TransferMode.Streaming && request.HttpVersion == HttpHelper.HTTP11 && client.Profile.Settings.Communication.AllowChunckedTransfer)
      {
        response.Chunked = true;
      }
      else
      {
        response.Chunked = false;
      }

      long startByte = Convert.ToInt64((bitrate * range.From) / 8.0);
      long endByte = Convert.ToInt64((bitrate * range.To) / 8.0);
      Range byteRange = new Range(startByte, endByte);

      Send(request, response, resourceStream, item, client, onlyHeaders, byteRange.From, byteRange.Length);
    }

    protected void SendByteRange(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, EndPointSettings client, Range range, bool onlyHeaders, TransferMode mediaTransferMode)
    {
      if (WaitForMinimumFileSize(resourceStream, range.From) == false)
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;

        response.SendHeaders();
        return;
      }

      long length = range.Length;
      long toRange = range.To;
      long fromRange = range.From;
      if (item.IsSegmented == false && item.IsTranscoding == true)
      {
        length = item.DlnaMetadata.Metadata.Size;
        if (length == 0)
        {
          length = GetStreamSize(item);
        }
        if (range.To <= 0 || range.To > length)
        {
          toRange = length - 1;
        }
      }
      else
      {
        length = resourceStream.Length;
        if (range.From >= length)
        {
          fromRange = length;
        }
        if (range.To <= 0 || range.To > length)
        {
          toRange = length;
        }
      }

      Range byteRange = new Range(fromRange, toRange);

      response.Status = HttpStatusCode.PartialContent;
      response.ContentLength = length;

      response.AddHeader("Content-Range", length == 0 ? String.Format("bytes {0}-", range.From) : String.Format("bytes {0}-{1}/{2}", byteRange.From, byteRange.To, length));

      if (mediaTransferMode == TransferMode.Streaming && request.HttpVersion == HttpHelper.HTTP11 && client.Profile.Settings.Communication.AllowChunckedTransfer)// && item.IsTranscoding == true)
      {
        response.Chunked = true;
      }
      else
      {
        response.Chunked = false;
      }

      Send(request, response, resourceStream, item, client, onlyHeaders, byteRange.From, byteRange.Length);
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
        GetMediaItem.CachedResource resource;
        string resourcePathStr = resourcePath.Serialize();
        if (_resourceAccessorCache.TryGetValue(resourcePathStr, out resource))
          return resource.ResourceAccessor;
        // TODO: Security check. Only deliver resources which are located inside local shares.
        ServiceRegistration.Get<ILogger>().Debug("ResourceAccessModule: Access of resource '{0}'", resourcePathStr);
        IResourceAccessor result;
        if (resourcePath.TryCreateLocalResourceAccessor(out result))
        {
          _resourceAccessorCache[resourcePathStr] = new GetMediaItem.CachedResource(result);
        }
        return result;
      }
    }

    internal bool WaitForMinimumFileSize(Stream resourceStream, long minimumSize)
    {
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

    internal long GetStreamSize(DlnaMediaItem dlnaItem)
    {
      if (dlnaItem.IsAudio) return TRANSCODED_AUDIO_STREAM_MAX;
      else if (dlnaItem.IsImage) return TRANSCODED_IMAGE_STREAM_MAX;
      else if (dlnaItem.IsVideo) return TRANSCODED_VIDEO_STREAM_MAX;
      return TRANSCODED_VIDEO_STREAM_MAX;
    }

    #region subtitle

    public static bool FindSubtitle(EndPointSettings client, out SubtitleCodec targetCodec, out string targetMime)
    {
      targetCodec = SubtitleCodec.Unknown;
      targetMime = "text/plain";
      if (client.Profile.Settings.Subtitles.SubtitleMode == SubtitleSupport.SoftCoded)
      {
        targetCodec = client.Profile.Settings.Subtitles.SubtitlesSupported[0].Format;
        if (string.IsNullOrEmpty(client.Profile.Settings.Subtitles.SubtitlesSupported[0].Mime) == false)
          targetMime = client.Profile.Settings.Subtitles.SubtitlesSupported[0].Mime;
        else
          targetMime = GetSubtitleMime(targetCodec);
        return true;
      }
      return false;
    }

    private static string GetSubtitleMime(SubtitleCodec codec)
    {
      switch (codec)
      {
        case SubtitleCodec.Srt:
          return "text/srt";
        case SubtitleCodec.MicroDvd:
          return "text/microdvd";
        case SubtitleCodec.SubView:
          return "text/plain";
        case SubtitleCodec.Ass:
          return "text/x-ass";
        case SubtitleCodec.Ssa:
          return "text/x-ssa";
        case SubtitleCodec.Smi:
          return "smi/caption";
      }
      return "text/plain";
    }

    #endregion subtitle

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

    #endregion cache


    #region parse ranges

    protected IList<Range> ParseTimeRanges(string timeRangesSpecifier, double duration)
    {
      if (string.IsNullOrEmpty(timeRangesSpecifier) || duration == 0)
        return null;
      IList<Range> result = new List<Range>();
      try
      {
        string[] tokens = timeRangesSpecifier.Split(new char[] { '=', ':' });
        if (tokens.Length == 2 && tokens[0].Trim() == "npt")
          foreach (string rangeSpec in tokens[1].Split(new char[] { ',' }))
          {
            tokens = rangeSpec.Split(new char[] { '-' });
            if (tokens.Length != 2)
              return new Range[] { };
            if (!string.IsNullOrEmpty(tokens[0]))
              if (!string.IsNullOrEmpty(tokens[1]))
                result.Add(new Range(Convert.ToInt64(TimeSpan.Parse(tokens[0], CultureInfo.InvariantCulture).TotalSeconds), Convert.ToInt64(TimeSpan.Parse(tokens[1], CultureInfo.InvariantCulture).TotalSeconds)));
              else
                result.Add(new Range(Convert.ToInt64(TimeSpan.Parse(tokens[0], CultureInfo.InvariantCulture).TotalSeconds), Convert.ToInt64(duration) - 1));
            else
              result.Add(new Range(Math.Max(0, Convert.ToInt64(duration) - Convert.ToInt64(TimeSpan.Parse(tokens[1], CultureInfo.InvariantCulture).TotalSeconds)), Convert.ToInt64(duration) - 1));
          }
      }
      catch (Exception e)
      {
        Logger.Debug("ResourceAccessModule: Received illegal Range header", e);
        // As specified in RFC2616, section 14.35.1, ignore invalid range header
      }
      return result;
    }

    protected IList<Range> ParseByteRanges(string byteRangesSpecifier, long size)
    {
      if (string.IsNullOrEmpty(byteRangesSpecifier) || size == 0)
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
        Logger.Debug("ResourceAccessModule: Received illegal Range header", e);
        // As specified in RFC2616, section 14.35.1, ignore invalid range header
      }
      return result;
    }


    protected IList<Range> ParseRanges(string byteRangesSpecifier, long size)
    {
      if (String.IsNullOrEmpty(byteRangesSpecifier) || size == 0)
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
            if (!String.IsNullOrEmpty(tokens[0]))
              if (!String.IsNullOrEmpty(tokens[1]))
                result.Add(new Range(Int64.Parse(tokens[0]), Int64.Parse(tokens[1])));
              else
                result.Add(new Range(Int64.Parse(tokens[0]), size - 1));
            else
              result.Add(new Range(Math.Max(0, size - Int64.Parse(tokens[1])), size - 1));
          }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Debug("ResourceAccessModule: Received illegal Range header", e);
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
        get { return _to - _from + 1; }
      }
    }

    #endregion parse ranges

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
