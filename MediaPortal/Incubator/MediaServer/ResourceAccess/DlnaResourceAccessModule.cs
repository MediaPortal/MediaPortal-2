#region Copyright (C) 2007-2015 Team MediaPortal

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
using System.IO;
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.HttpModules;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.Objects.MediaLibrary;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Extensions.MediaServer.Profiles;
using System.Globalization;
using MediaPortal.Utilities.SystemAPI;
using System.Reflection;
using MediaPortal.Plugins.Transcoding.Service;
using System.Threading;
using MediaPortal.Extensions.MediaServer.Protocols;
using System.Drawing.Imaging;
using System.Drawing;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;

namespace MediaPortal.Extensions.MediaServer.ResourceAccess
{
  public class DlnaResourceAccessModule : HttpModule, IDisposable
  {
    protected IntervalWork _tidyUpCacheWork;
    protected readonly object _syncObj = new object();

    public TimeSpan RESOURCE_CACHE_TIME = TimeSpan.FromMinutes(5);
    public TimeSpan CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(1);
    public const long TRANSCODED_VIDEO_STREAM_MAX = 50000000000L;
    public const long TRANSCODED_AUDIO_STREAM_MAX = 900000000L;
    public const long TRANSCODED_IMAGE_STREAM_MAX = 9000000L;
    public const long TRANSCODED_SUBTITLE_STREAM_MAX = 300000L;

    private string _serverOsVersion = null;
    private string _product = null;
    private Dictionary<string, Guid> _lastMediaItem = new Dictionary<string, Guid>();
    private Dictionary<string, TranscodeContext> _lastClientTranscode = new Dictionary<string, TranscodeContext>();
    private MediaConverter _transcoder = new MediaConverter();

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

    public DlnaResourceAccessModule()
    {
      _tidyUpCacheWork = new IntervalWork(TidyUpCache, CACHE_CLEANUP_INTERVAL);
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.AddIntervalWork(_tidyUpCacheWork, false);
      _serverOsVersion = WindowsAPI.GetOsVersionString();
      Assembly assembly = Assembly.GetExecutingAssembly();
      _product = "MediaPortal 2 DLNA Server/" + AssemblyName.GetAssemblyName(assembly.Location).Version.ToString(2);

      _transcoder.Logger = Logger;

      ClearCache();
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

    public void TidyUpCache()
    {
      lock (_syncObj)
      {
        _transcoder.CleanUpTranscodeCache();
      }
    }

    public static void Shutdown()
    {
      foreach (EndPointSettings clients in ProfileManager.ProfileLinks.Values)
      {
        foreach (DlnaMediaItem item in clients.DlnaMediaItems.Values)
        {
          try
          {
            if (item.IsStreaming)
            {
              item.StopStreaming();
              Logger.Debug("ResourceAccessModule: Stopping stream of mediaitem ", item.MediaSource.MediaItemId);
            }
          }
          catch (Exception e)
          {
            Logger.Warn("ResourceAccessModule: Error stopping stream", e);
          }
          try
          {
            if (item.IsTranscoding)
            {
              item.StopTranscoding();
              Logger.Debug("ResourceAccessModule: Aborting transcoding of mediaitem ", item.MediaSource.MediaItemId);
            }
          }
          catch (Exception e)
          {
            Logger.Warn("ResourceAccessModule: Error stopping transcoding", e);
          }
        }
      }
    }

    public void ClearCache()
    {
      Shutdown();
      TidyUpCache();
    }

    private long GetStreamSize(DlnaMediaItem dlnaItem)
    {
      if (dlnaItem.IsAudio) return TRANSCODED_AUDIO_STREAM_MAX;
      else if (dlnaItem.IsImage) return TRANSCODED_IMAGE_STREAM_MAX;
      else if (dlnaItem.IsVideo) return TRANSCODED_VIDEO_STREAM_MAX;
      return TRANSCODED_VIDEO_STREAM_MAX;
    }

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

    public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      var uri = request.Uri;
      Guid mediaItemGuid = Guid.Empty;
      bool bHandled = false;
      Logger.Debug("DlnaResourceAccessModule: Received request {0}", request.Uri);
#if DEBUG
      for (int i = 0; i < request.Headers.Count; i++)
        Logger.Debug(string.Format("DlnaResourceAccessModule: Header {0}: {1}={2}", i, request.Headers.GetKey(i), request.Headers.Get(i)));
#endif
      try
      {
        EndPointSettings deviceClient = null;
        string clientId = request.Headers["remote_addr"];
        if (clientId == null)
        {
          clientId = "noip";
        }


        deviceClient = ProfileManager.DetectProfile(request.Headers);

        if (deviceClient == null || deviceClient.Profile == null)
        {
          Logger.Warn("DlnaResourceAccessModule: Client {0} has no valid link or profile", clientId);
          return false;
        }
        Logger.Debug("DlnaResourceAccessModule: Using profile {0} for client {1}", deviceClient.Profile.Name, clientId);

        GenericAccessProtocol protocolResource = GenericAccessProtocol.GetProtocolResourceHandler(deviceClient.Profile.ResourceAccessHandler);
        response.AddHeader("Server", _serverOsVersion + " UPnP/1.1 DLNADOC/1.50, " + _product);
        response.AddHeader("Cache-control", "no-cache");
        response.Connection = ConnectionType.Close;

        // Check the request path to see if it's for us.
        if (!uri.AbsolutePath.StartsWith(DlnaResourceAccessUtils.RESOURCE_ACCESS_PATH))
        {
          if (protocolResource.CanHandleRequest(request) == false)
            return false;
        }

        if (request.QueryString["aspect"].Value == "ICON")
        {
          bHandled = true;
          Logger.Debug("DlnaResourceAccessModule: Attempting to load Icon");
          using (var fs = new FileStream(FileUtils.BuildAssemblyRelativePath(string.Format("MP2_DLNA_Server_{0}.{1}", request.QueryString["size"].Value, request.QueryString["type"].Value)), FileMode.Open, FileAccess.Read))
          {
            response.ContentType = "image/" + request.QueryString["type"].Value;
            using (MemoryStream ms = new MemoryStream())
            {
              Image img = Image.FromStream(fs);
              img.Save(ms, ImageFormat.Png);
              SendResourceFile(request, response, ms, false);
            }
          }
        }

        if (bHandled == false)
        {
          // Grab the media item given in the request.
          lock (_lastMediaItem)
          {
            if (_lastMediaItem.ContainsKey(clientId) == false)
            {
              _lastMediaItem.Add(clientId, Guid.Empty);
            }

            if (!DlnaResourceAccessUtils.ParseMediaItem(uri, out mediaItemGuid))
            {
              if (_lastMediaItem[clientId] == Guid.Empty)
              {
                throw new BadRequestException(string.Format("Illegal request syntax. Correct syntax is '{0}'", DlnaResourceAccessUtils.SYNTAX));
              }
              mediaItemGuid = _lastMediaItem[clientId];
              Logger.Debug("DlnaResourceAccessModule: Attempting to reload last mediaitem {0}", mediaItemGuid.ToString());
            }
            else
            {
              Logger.Debug("DlnaResourceAccessModule: Attempting to load mediaitem {0}", mediaItemGuid.ToString());
            }
          }

          DlnaMediaItem dlnaItem = null;
          if (deviceClient.DlnaMediaItems.ContainsKey(mediaItemGuid) == false)
          {
            // Attempt to grab the media item from the database.
            MediaItem item = MediaLibraryHelper.GetMediaItem(mediaItemGuid);
            if (item == null)
              throw new BadRequestException(string.Format("Media item '{0}' not found.", mediaItemGuid));

            dlnaItem = deviceClient.GetDlnaItem(item);
          }
          else
          {
            dlnaItem = deviceClient.DlnaMediaItems[mediaItemGuid];
          }
          if (dlnaItem == null)
            throw new BadRequestException(string.Format("DLNA media item '{0}' not found.", mediaItemGuid));
          lock (_lastMediaItem)
          {
            _lastMediaItem[clientId] = mediaItemGuid;
          }

          SubtitleStream subSource = null;
          SubtitleCodec subTargetCodec = SubtitleCodec.Unknown;
          string subTargetMime = "";
          bool subUseLocal = false;
          if (dlnaItem.IsSubtitled)
          {
            subUseLocal = DlnaResourceAccessUtils.FindSubtitle(deviceClient, out subTargetCodec, out subTargetMime);
            if (dlnaItem.IsTranscoded && dlnaItem.IsVideo)
            {
              VideoTranscoding video = (VideoTranscoding)dlnaItem.TranscodingParameter;
              video.TargetSubtitleCodec = subTargetCodec;
              video.TargetSubtitleLanguages = deviceClient.PreferredSubtitleLanguages;
            }
            else if (dlnaItem.IsVideo)
            {
              VideoTranscoding subtitle = (VideoTranscoding)dlnaItem.SubtitleTranscodingParameter;
              subtitle.TargetSubtitleCodec = subTargetCodec;
              subtitle.TargetSubtitleLanguages = deviceClient.PreferredSubtitleLanguages;
            }
          }

          using (Stream resource = protocolResource.HandleResourceRequest(request, response, session, dlnaItem))
          {
            if (resource != null)
            {
              bHandled = true;
              Logger.Debug("DlnaResourceAccessModule: Resource protocol sending request for {0}", uri.ToString());
              SendResourceFile(request, response, resource, false);
            }
          }

          if (protocolResource.HandleRequest(request, response, session, dlnaItem) == true)
          {
            bHandled = true;
          }

          if (request.QueryString["aspect"].Value == "THUMBNAIL")
          {
            bHandled = true;
            Logger.Debug("DlnaResourceAccessModule: Attempting to load media item thumbnail");

            byte[] thumb = (byte[])dlnaItem.MediaSource.Aspects[ThumbnailLargeAspect.ASPECT_ID].GetAttributeValue(ThumbnailLargeAspect.ATTR_THUMBNAIL);
            if (thumb != null && thumb.Length > 0)
            {
              response.ContentType = "image/jpeg";
              using (MemoryStream ms = new MemoryStream((byte[])thumb))
              {
                SendResourceFile(request, response, ms, false);
              }
            }
            else
            {
              Logger.Debug("DlnaResourceAccessModule: Thumbnail was empty for mediaitem {0}", mediaItemGuid.ToString());
            }
          }

          if (request.QueryString["aspect"].Value == "SUBTITLE")
          {
            bHandled = true;
            if (subSource != null)
            {
              Logger.Debug("DlnaResourceAccessModule: Attempting to load subtitle");
              response.ContentType = subTargetMime;
              if (subUseLocal)
              {
                using (var subStream = _transcoder.GetReadyFileBuffer(subSource.Source))
                {
                  Logger.Debug("DlnaResourceAccessModule: Sending subtitle file for {0}", uri.ToString());
                  SendResourceFile(request, response, subStream, false);
                }
              }
              else
              {
                if (dlnaItem.IsTranscoded)
                {
                  using (var subStream = _transcoder.GetSubtitleStream((VideoTranscoding)dlnaItem.TranscodingParameter))
                  {
                    Logger.Debug("DlnaResourceAccessModule: Sending transcoded subtitle file for {0}", uri.ToString());
                    SendResourceFile(request, response, subStream, false);
                  }
                }
                else
                {
                  using (var subStream = _transcoder.GetSubtitleStream((VideoTranscoding)dlnaItem.SubtitleTranscodingParameter))
                  {
                    Logger.Debug("DlnaResourceAccessModule: Sending transcoded subtitle file for {0}", uri.ToString());
                    SendResourceFile(request, response, subStream, false);
                  }
                }
              }
            }
          }

          if (bHandled == false)
          {
            // Grab the mimetype from the media item and set the Content Type header.
            if (dlnaItem.DlnaMime == null)
              throw new InternalServerException("Media item has bad mime type, re-import media item");
            response.ContentType = dlnaItem.DlnaMime;

            TransferMode mediaTransferMode = TransferMode.Interactive;
            if (dlnaItem.IsVideo || dlnaItem.IsAudio)
            {
              mediaTransferMode = TransferMode.Streaming;
            }
            if (!string.IsNullOrEmpty(request.Headers["transferMode.dlna.org"]))
            {
              string transferMode = request.Headers["transferMode.dlna.org"];
              Logger.Debug("DlnaResourceAccessModule: Requested transfer of type " + transferMode);
              if (transferMode == "Streaming")
              {
                mediaTransferMode = TransferMode.Streaming;
              }
              else if (transferMode == "Interactive")
              {
                mediaTransferMode = TransferMode.Interactive;
              }
              else if (transferMode == "Background")
              {
                mediaTransferMode = TransferMode.Background;
              }
            }

            StreamMode requestedStreamingMode = StreamMode.Normal;
            string byteRangesSpecifier = request.Headers["Range"];
            if (byteRangesSpecifier != null)
            {
              Logger.Debug("DlnaResourceAccessModule: Requesting range {1} for mediaitem {0}", mediaItemGuid.ToString(), byteRangesSpecifier);
              if (byteRangesSpecifier.Contains("npt=") == true)
              {
                requestedStreamingMode = StreamMode.TimeRange;
              }
              else
              {
                requestedStreamingMode = StreamMode.ByteRange;
              }
            }

            Logger.Debug("DlnaResourceAccessModule: Attempting transcoding for mediaitem {0} in mode {1}", mediaItemGuid.ToString(), requestedStreamingMode.ToString());
            if (dlnaItem.StartTrancoding() == false)
            {
              Logger.Debug("DlnaResourceAccessModule: Transcoding busy for mediaitem {0}", mediaItemGuid.ToString());
              response.Status = HttpStatusCode.InternalServerError;
              response.Chunked = false;
              response.ContentLength = 0;
              response.ContentType = null;
#if DEBUG
              Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
              response.SendHeaders();
#endif
              return true;
            }

            Stream resourceStream = null;
            if (resourceStream == null && dlnaItem.IsSegmented)
            {
              int startIndex = request.Uri.AbsoluteUri.LastIndexOf("/") + 1;
              string fileName = request.Uri.AbsoluteUri.Substring(startIndex);
              if (Path.GetExtension(_transcoder.HLSSegmentFileTemplate) == Path.GetExtension(fileName))
              {
                string segmentFile = Path.Combine(dlnaItem.SegmentDir, fileName);
                if (File.Exists(segmentFile) == true)
                {
                  resourceStream = _transcoder.GetReadyFileBuffer(segmentFile);
                }
                else
                {
                  Logger.Error("DlnaResourceAccessModule: Unable to find segment file {0}", fileName);

                  response.Status = HttpStatusCode.InternalServerError;
                  response.Chunked = false;
                  response.ContentLength = 0;
                  response.ContentType = null;
#if DEBUG
                  Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
                  response.SendHeaders();
#endif
                  return true;
                }
              }
            }
            if (resourceStream == null && dlnaItem.IsTranscoded == false)
            {
              resourceStream = _transcoder.GetReadyFileBuffer((ILocalFsResourceAccessor)dlnaItem.DlnaMetadata.Metadata.Source);
            }
            if (resourceStream == null)
            {
              TranscodeContext context = _transcoder.GetMediaStream(dlnaItem.TranscodingParameter, true);
              dlnaItem.SegmentDir = context.SegmentDir;
              resourceStream = context.TranscodedStream;
              lock (_lastClientTranscode)
              {
                if (_lastClientTranscode.ContainsKey(clientId) == false)
                {
                  _lastClientTranscode.Add(clientId, context);
                }
                else
                {
                  if (_lastClientTranscode[clientId].Running == true && _lastClientTranscode[clientId] != context)
                  {
                    //Don't waste resources on transcoding if the client wants different media item
                    _lastClientTranscode[clientId].Stop();
                  }
                  _lastClientTranscode[clientId] = context;
                }
              }
            }

            if (resourceStream == null)
            {
              response.Status = HttpStatusCode.InternalServerError;
              response.Chunked = false;
              response.ContentLength = 0;
              response.ContentType = null;
#if DEBUG
              Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
                response.SendHeaders();
#endif
              return true;
            }

            if (dlnaItem.IsStreamable == false)
            {
              Logger.Debug("DlnaResourceAccessModule: Live transcoding of mediaitem {0} is not possible because of media container", mediaItemGuid.ToString());
            }

            IList<Range> ranges = null;
            if (requestedStreamingMode == StreamMode.TimeRange)
            {
              double duration = dlnaItem.DlnaMetadata.Metadata.Duration;
              if (dlnaItem.IsSegmented)
              {
                //Is this possible?
                duration = _transcoder.HLSSegmentTimeInSeconds;
              }
              ranges = ParseTimeRanges(byteRangesSpecifier, duration);
              if (ranges == null || ranges.Count != 1)
              {
                //Only support 1 range
                response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
                response.Chunked = false;
                response.ContentLength = 0;
                response.ContentType = null;
#if DEBUG
                Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
                  response.SendHeaders();
#endif
                return true;
              }
            }
            else if (requestedStreamingMode == StreamMode.ByteRange)
            {
              long lSize = dlnaItem.IsTranscoding ? GetStreamSize(dlnaItem) : resourceStream.Length;
              if (dlnaItem.IsSegmented)
              {
                lSize = resourceStream.Length;
              }
              ranges = ParseByteRanges(byteRangesSpecifier, lSize);
              if (ranges == null || ranges.Count != 1)
              {
                //Only support 1 range
                response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
                response.Chunked = false;
                response.ContentLength = 0;
                response.ContentType = null;
#if DEBUG
                Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
                  response.SendHeaders();
#endif
                return true;
              }
            }

            if (dlnaItem.IsSegmented == false && dlnaItem.IsTranscoding && mediaTransferMode == TransferMode.Streaming)
            {
              if ((requestedStreamingMode == StreamMode.ByteRange || requestedStreamingMode == StreamMode.TimeRange) && ranges == null)
              {
                //Only support 1 range
                response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
                response.Chunked = false;
                response.ContentLength = 0;
                response.ContentType = null;
#if DEBUG
                Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
                  response.SendHeaders();
#endif
                return true;
              }
            }

            // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
            if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"]))
            {
              DateTime lastRequest = DateTime.Parse(request.Headers["If-Modified-Since"]);
              if (lastRequest.CompareTo(dlnaItem.LastUpdated) <= 0)
                response.Status = HttpStatusCode.NotModified;
            }

            // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
            response.AddHeader("Last-Modified", dlnaItem.LastUpdated.ToUniversalTime().ToString("r"));

            // DLNA Requirement: [7.4.26.1-6]
            // Since the DLNA spec allows contentFeatures.dlna.org with any request, we'll put it in.
            if (!string.IsNullOrEmpty(request.Headers["getcontentFeatures.dlna.org"]))
            {
              if (request.Headers["getcontentFeatures.dlna.org"] != "1")
              {
                // DLNA Requirement [7.4.26.5]
                throw new BadRequestException("Illegal value for getcontentFeatures.dlna.org");
              }

              var dlnaString = DlnaProtocolInfoFactory.GetProfileInfo(dlnaItem, deviceClient.Profile.ProtocolInfo).ToString();
              response.AddHeader("contentFeatures.dlna.org", dlnaString);
              Logger.Debug("DlnaResourceAccessModule: Returning contentFeatures {0}", dlnaString);
            }

            // DLNA Requirement: [7.4.55-57]
            // TODO: Bad implementation of requirement
            if (mediaTransferMode == TransferMode.Streaming)
            {
              response.AddHeader("transferMode.dlna.org", "Streaming");
            }
            else if (mediaTransferMode == TransferMode.Interactive)
            {
              response.AddHeader("transferMode.dlna.org", "Interactive");
            }
            else if (mediaTransferMode == TransferMode.Background)
            {
              response.AddHeader("transferMode.dlna.org", "Background");
            }
            response.AddHeader("realTimeInfo.dlna.org", "DLNA.ORG_TLAG=*");

            bool onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
            if (requestedStreamingMode == StreamMode.TimeRange)
            {
              Logger.Debug("DlnaResourceAccessModule: Sending time range header only: {0}", onlyHeaders.ToString());
              if (ranges != null && ranges.Count == 1)
              {
                // We only support one range
                SendTimeRange(request, response, resourceStream, dlnaItem, deviceClient, ranges[0], onlyHeaders, mediaTransferMode);
                return true;
              }
            }
            else if (requestedStreamingMode == StreamMode.ByteRange)
            {
              Logger.Debug("DlnaResourceAccessModule: Sending byte range header only: {0}", onlyHeaders.ToString());
              if (ranges != null && ranges.Count == 1)
              {
                // We only support one range
                SendByteRange(request, response, resourceStream, dlnaItem, deviceClient, ranges[0], onlyHeaders, mediaTransferMode);
                return true;
              }
            }
            Logger.Debug("DlnaResourceAccessModule: Sending file header only: {0}", onlyHeaders.ToString());
            SendWholeFile(request, response, resourceStream, dlnaItem, deviceClient, onlyHeaders, mediaTransferMode);
          }
        }
      }
      catch (FileNotFoundException ex)
      {
        throw new InternalServerException(string.Format("Failed to proccess media item '{0}'", mediaItemGuid), ex);
      }

      return true;
    }

    protected void SendTimeRange(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, Profiles.EndPointSettings client, Range range, bool onlyHeaders, TransferMode mediaTransferMode)
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
#if DEBUG
        Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
        response.SendHeaders();
#endif
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
      if (duration == 0)
      {
        response.AddHeader("TimeSeekRange.dlna.org", string.Format("npt={0}-", range.From));
      }
      else
      {
        response.AddHeader("TimeSeekRange.dlna.org", string.Format("npt={0}-{1}/{2}", range.From, range.To, Convert.ToInt64(duration)));
      }

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

    protected void SendByteRange(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, Profiles.EndPointSettings client, Range range, bool onlyHeaders, TransferMode mediaTransferMode)
    {
      if (WaitForMinimumFileSize(resourceStream, range.From) == false)
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;
#if DEBUG
        Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
        response.SendHeaders();
#endif
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

//      if(length <= range.From)
//      {
//        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
//        response.Chunked = false;
//        response.ContentLength = 0;
//        response.ContentType = null;
//#if DEBUG
//        Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
//#else
//        response.SendHeaders();
//#endif
//        return;
//      }
      Range byteRange = new Range(fromRange, toRange);

      response.Status = HttpStatusCode.PartialContent;
      response.ContentLength = length;

      if (length == 0)
      {
        response.AddHeader("Content-Range", string.Format("bytes {0}-", range.From));
      }
      else
      {
        response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", byteRange.From, byteRange.To, length));
      }

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

    protected void SendWholeFile(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, Profiles.EndPointSettings client, bool onlyHeaders, TransferMode mediaTransferMode)
    {
      if (WaitForMinimumFileSize(resourceStream, 1) == false)
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
#if DEBUG
        Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
        response.SendHeaders();
#endif
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

    protected void SendResourceFile(IHttpRequest request, IHttpResponse response, Stream resourceStream, bool onlyHeaders)
    {
      response.Status = HttpStatusCode.OK;
      response.Chunked = false;
      response.ContentLength = resourceStream.Length;
#if DEBUG
      Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
      response.SendHeaders();
#endif

      if (onlyHeaders)
        return;

      Logger.Debug("Sending resource file");
      resourceStream.Seek(0, SeekOrigin.Begin);
      const int BUF_LEN = 8192;
      byte[] buffer = new byte[BUF_LEN];
      int bytesRead;
      long count = 0;
      long length = resourceStream.Length;

      while ((bytesRead = resourceStream.Read(buffer, 0, length > BUF_LEN ? BUF_LEN : (int)length)) > 0)
      {
        length -= bytesRead;
        count += bytesRead;
        if (response.SendBody(buffer, 0, bytesRead) == false)
        {
          Logger.Debug("Connection lost after {0} bytes", count);
          break;
        }
      }
      Logger.Debug("Sending resource file complete");
    }

    private bool WaitForMinimumFileSize(Stream resourceStream, long minimumSize)
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

    protected void Send(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, Profiles.EndPointSettings client, bool onlyHeaders, long start, long length)
    {
#if DEBUG
      Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
#else
      response.SendHeaders();
#endif

      if (onlyHeaders)
        return;

      Guid streamID = item.StartStreaming();
      if (streamID == Guid.Empty)
      {
        Logger.Error("DlnaResourceAccessModule: Unable to start stream");
        return;
      }
      try
      {
        Logger.Debug("Sending chunked: {0}", response.Chunked.ToString());
        string clientID = request.Headers["remote_addr"];
        int bufferSize = client.Profile.Settings.Communication.DefaultBufferSize;
        if(bufferSize <= 0)
        {
          bufferSize = 1500;
        }
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
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
        while (item.IsStreamActive(streamID) && length > 0)
        {
          bytesRead = resourceStream.Read(buffer, 0, length > bufferSize ? bufferSize : (int)length);
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
            while (bIsStream && item.IsStreamActive(streamID) && item.IsTranscoding && length == 0)
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
        item.StopStreaming(streamID);
        Logger.Debug("Sending complete");
      }
    }

    public void Dispose()
    {
      if (_tidyUpCacheWork != null)
      {
        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
        threadPool.RemoveIntervalWork(_tidyUpCacheWork);
        _tidyUpCacheWork = null;
      }
      ClearCache();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
