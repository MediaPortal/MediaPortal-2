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
using System.Reflection;
using System.Threading;
using System.Drawing.Imaging;
using System.Drawing;
using System.Globalization;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.HttpModules;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.Objects.MediaLibrary;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.MediaServer.Protocols;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;
using MediaPortal.Plugins.Transcoding.Service.Objects;

namespace MediaPortal.Plugins.MediaServer.ResourceAccess
{
  public class DlnaResourceAccessModule : HttpModule, IDisposable
  {
    public const long TRANSCODED_VIDEO_STREAM_MAX = 50000000000L;
    public const long TRANSCODED_AUDIO_STREAM_MAX = 900000000L;
    public const long TRANSCODED_IMAGE_STREAM_MAX = 9000000L;
    public const long TRANSCODED_SUBTITLE_STREAM_MAX = 300000L;

    private string _serverOsVersion = null;
    private string _product = null;
    private Dictionary<string, Guid> _lastMediaItem = new Dictionary<string, Guid>();

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
      _serverOsVersion = WindowsAPI.GetOsVersionString();
      Assembly assembly = Assembly.GetExecutingAssembly();
      _product = "MediaPortal 2 DLNA Server/" + AssemblyName.GetAssemblyName(assembly.Location).Version.ToString(2);
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
        get 
        {
          if (_to <= _from) return 0;
          return _to - _from; 
        }
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

    private long GetStreamSize(DlnaMediaItem dlnaItem)
    {
      long length = dlnaItem.DlnaMetadata.Metadata.Size;
      if (dlnaItem.IsTranscoding == true || dlnaItem.IsLive == true || length <= 0)
      //if (length <= 0)
      {
        if (dlnaItem.IsAudio) return TRANSCODED_AUDIO_STREAM_MAX;
        else if (dlnaItem.IsImage) return TRANSCODED_IMAGE_STREAM_MAX;
        else if (dlnaItem.IsVideo) return TRANSCODED_VIDEO_STREAM_MAX;
        return TRANSCODED_VIDEO_STREAM_MAX;
      }
      return length;
    }

    protected IList<Range> ParseTimeRanges(string timeRangesSpecifier, double duration)
    {
      if (string.IsNullOrEmpty(timeRangesSpecifier))
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
            {
              if (!string.IsNullOrEmpty(tokens[1]))
                result.Add(new Range(Convert.ToInt64(TimeSpan.Parse(tokens[0], CultureInfo.InvariantCulture).TotalSeconds), Convert.ToInt64(TimeSpan.Parse(tokens[1], CultureInfo.InvariantCulture).TotalSeconds)));
              else
                result.Add(new Range(Convert.ToInt64(TimeSpan.Parse(tokens[0], CultureInfo.InvariantCulture).TotalSeconds), Convert.ToInt64(duration)));
            }
            else
            {
              result.Add(new Range(Math.Max(0, Convert.ToInt64(duration) - Convert.ToInt64(TimeSpan.Parse(tokens[1], CultureInfo.InvariantCulture).TotalSeconds)), Convert.ToInt64(duration)));
            }
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
              else if(start < size)
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
        Logger.Debug("ResourceAccessModule: Received illegal Range header", e);
        // As specified in RFC2616, section 14.35.1, ignore invalid range header
      }
      return result;
    }

    protected Range ConvertToByteRange(Range timeRange, DlnaMediaItem item)
    {
      if (timeRange.Length <= 0.0)
      {
        return new Range(0, item.DlnaMetadata.Metadata.Size);
      }
      long startByte = 0;
      long endByte = 0;
      if (item.IsTranscoding == true)
      {
        long length = GetStreamSize(item);
        double factor = Convert.ToDouble(length) / item.DlnaMetadata.Metadata.Duration;
        startByte = Convert.ToInt64(Convert.ToDouble(timeRange.From) * factor);
        endByte = Convert.ToInt64(Convert.ToDouble(timeRange.To) * factor);
      }
      else
      {
        double bitrate = 0;
        if (item.IsSegmented == false)
        {
          bitrate = Convert.ToDouble(item.DlnaMetadata.Metadata.Bitrate) * 1024; //Bitrate in bits/s
        }
        startByte = Convert.ToInt64((bitrate * timeRange.From) / 8.0);
        endByte = Convert.ToInt64((bitrate * timeRange.To) / 8.0);
      }
      return new Range(startByte, endByte);
    }

    protected Range ConvertToTimeRange(Range byteRange, DlnaMediaItem item)
    {
      if (byteRange.Length <= 0.0)
      {
        return new Range(0, Convert.ToInt64(item.DlnaMetadata.Metadata.Duration));
      }

      double startSeconds = 0;
      double endSeconds = 0;
      if (item.IsTranscoding == true)
      {
        long length = GetStreamSize(item);
        double factor = item.DlnaMetadata.Metadata.Duration / Convert.ToDouble(length);
        startSeconds = Convert.ToDouble(byteRange.From) * factor;
        endSeconds = Convert.ToDouble(byteRange.To) * factor;
      }
      else
      {
        double bitrate = 0;
        if (item.IsSegmented == false)
        {
          bitrate = Convert.ToDouble(item.DlnaMetadata.Metadata.Bitrate) * 1024; //Bitrate in bits/s
        }
        if (bitrate > 0)
        {
          startSeconds = Convert.ToDouble(byteRange.From) / (bitrate / 8.0);
          endSeconds = Convert.ToDouble(byteRange.To) / (bitrate / 8.0);
        }
      }
      return new Range(Convert.ToInt64(startSeconds), Convert.ToInt64(endSeconds));
    }

    protected Range ConvertToFileRange(Range requestedByteRange, DlnaMediaItem item, long length)
    {
      long toRange = requestedByteRange.To;
      long fromRange = requestedByteRange.From;
      if (toRange <= 0 || toRange > length)
      {
        toRange = length;
      }
      if (item.IsSegmented == false && item.IsTranscoding == true)
      {
        if (item.DlnaMetadata.Metadata.Size > 0 && (toRange > item.DlnaMetadata.Metadata.Size || fromRange > item.DlnaMetadata.Metadata.Size))
        {
          fromRange = Convert.ToInt64((Convert.ToDouble(fromRange) / Convert.ToDouble(length)) * Convert.ToDouble(item.DlnaMetadata.Metadata.Size));
          toRange = Convert.ToInt64((Convert.ToDouble(toRange) / Convert.ToDouble(length)) * Convert.ToDouble(item.DlnaMetadata.Metadata.Size));
        }
      }
      return new Range(fromRange, toRange);
    }

    public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      var uri = request.Uri;
      Guid mediaItemGuid = Guid.Empty;
      bool bHandled = false;
      Logger.Debug("DlnaResourceAccessModule: Received request {0}", request.Uri);
      for (int i = 0; i < request.Headers.Count; i++)
        Logger.Debug(string.Format("DlnaResourceAccessModule: Header {0}: {1}={2}", i, request.Headers.GetKey(i), request.Headers.Get(i)));
      try
      {
        #region Determine profile

        EndPointSettings deviceClient = null;
        string clientIp = request.Headers["remote_addr"];
        if (clientIp == null)
        {
          clientIp = "noip";
        }

        deviceClient = ProfileManager.DetectProfile(request.Headers);
        if (deviceClient == null || deviceClient.Profile == null)
        {
          Logger.Warn("DlnaResourceAccessModule: Client {0} has no valid link or profile", clientIp);
          return false;
        }
        Logger.Debug("DlnaResourceAccessModule: Using profile {0} for client {1}", deviceClient.Profile.Name, clientIp);
        GenericAccessProtocol protocolResource = GenericAccessProtocol.GetProtocolResourceHandler(deviceClient.Profile.ResourceAccessHandler);

        #endregion

        response.AddHeader("Server", _serverOsVersion + " UPnP/1.1 DLNADOC/1.50, " + _product);
        response.AddHeader("Cache-control", "no-cache");
        response.Connection = ConnectionType.Close;

        // Check the request path to see if it's for us.
        if (!uri.AbsolutePath.StartsWith(DlnaResourceAccessUtils.RESOURCE_ACCESS_PATH))
        {
          if (protocolResource.CanHandleRequest(request) == false)
            return false;
        }

        #region Handle icon request

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

        #endregion

        if (bHandled == false)
        {
          #region Determine media item

          lock (_lastMediaItem)
          {
            if (_lastMediaItem.ContainsKey(deviceClient.ClientId) == false)
            {
              _lastMediaItem.Add(deviceClient.ClientId, Guid.Empty);
            }

            if (!DlnaResourceAccessUtils.ParseMediaItem(uri, out mediaItemGuid))
            {
              if (_lastMediaItem[deviceClient.ClientId] == Guid.Empty)
              {
                throw new BadRequestException(string.Format("Illegal request syntax. Correct syntax is '{0}'", DlnaResourceAccessUtils.SYNTAX));
              }
              mediaItemGuid = _lastMediaItem[deviceClient.ClientId];
              Logger.Debug("DlnaResourceAccessModule: Attempting to reload last mediaitem {0}", mediaItemGuid.ToString());
            }
            else
            {
              Logger.Debug("DlnaResourceAccessModule: Attempting to load mediaitem {0}", mediaItemGuid.ToString());
            }
          }

          #endregion

          #region Determine DLNA media item

          DlnaMediaItem dlnaItem = null;
          if (deviceClient.DlnaMediaItems.ContainsKey(mediaItemGuid) == false)
          {
            // Attempt to grab the media item from the database.
            MediaItem item = MediaLibraryHelper.GetMediaItem(mediaItemGuid);
            if (item == null)
              throw new BadRequestException(string.Format("Media item '{0}' not found.", mediaItemGuid));

            dlnaItem = deviceClient.GetDlnaItem(item, false);
          }
          else
          {
            dlnaItem = deviceClient.DlnaMediaItems[mediaItemGuid];
          }
          if (dlnaItem == null)
            throw new BadRequestException(string.Format("DLNA media item '{0}' not found.", mediaItemGuid));
          lock (_lastMediaItem)
          {
            _lastMediaItem[deviceClient.ClientId] = mediaItemGuid;
          }

          #endregion

          #region Determine subtitle mode

          SubtitleCodec subTargetCodec = SubtitleCodec.Unknown;
          if (dlnaItem.IsVideo)
          {
            string subTargetMime = "";
            if (DlnaResourceAccessUtils.UseSoftCodedSubtitle(deviceClient, out subTargetCodec, out subTargetMime))
            {
              if (dlnaItem.IsTranscoded)
              {
                VideoTranscoding video = (VideoTranscoding)dlnaItem.TranscodingParameter;
                video.TargetSubtitleCodec = subTargetCodec;
                video.TargetSubtitleLanguages = deviceClient.PreferredSubtitleLanguages;
                video.TargetSubtitleMime = subTargetMime;
              }
              else
              {
                VideoTranscoding subtitle = (VideoTranscoding)dlnaItem.SubtitleTranscodingParameter;
                subtitle.TargetSubtitleCodec = subTargetCodec;
                subtitle.TargetSubtitleLanguages = deviceClient.PreferredSubtitleLanguages;
                subtitle.TargetSubtitleMime = subTargetMime;
              }
            }
          }

          #endregion

          #region Check if protocol can handle request

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

          #endregion

          #region Handle thumbnail request

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

          #endregion

          #region Handle subtitle request

          if (request.QueryString["aspect"].Value == "SUBTITLE")
          {
            bHandled = true;
            if (dlnaItem.IsTranscoded)
            {
              using (var subStream = MediaConverter.GetSubtitleStream(deviceClient.ClientId, (VideoTranscoding)dlnaItem.TranscodingParameter))
              {
                response.ContentType = ((VideoTranscoding)dlnaItem.TranscodingParameter).TargetSubtitleMime;
                if (subStream != null)
                {
                  Logger.Debug("DlnaResourceAccessModule: Sending transcoded subtitle file for {0}", uri.ToString());
                  SendResourceFile(request, response, subStream, false);
                }
              }
            }
            else
            {
              using (var subStream = MediaConverter.GetSubtitleStream(deviceClient.ClientId, (VideoTranscoding)dlnaItem.SubtitleTranscodingParameter))
              {
                response.ContentType = ((VideoTranscoding)dlnaItem.SubtitleTranscodingParameter).TargetSubtitleMime;
                if (subStream != null)
                {
                  Logger.Debug("DlnaResourceAccessModule: Sending transcoded subtitle file for {0}", uri.ToString());
                  SendResourceFile(request, response, subStream, false);
                }
              }
            }
          }

          #endregion

          if (bHandled == false)
          {
            // Grab the mimetype from the media item and set the Content Type header.
            if (dlnaItem.DlnaMime == null)
              throw new InternalServerException("Media item has bad mime type, re-import media item");
            response.ContentType = dlnaItem.DlnaMime;

            #region Determine transfer mode

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

            #endregion

            #region Determine streaming mode

            StreamMode requestedStreamingMode = StreamMode.Normal;
            string rangeSpecifier = null;
            string byteRangesSpecifier = request.Headers["Range"];
            if (byteRangesSpecifier != null)
            {
              rangeSpecifier = byteRangesSpecifier;
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

            string timeRangesSpecifier = request.Headers["TimeSeekRange.dlna.org"];
            if (timeRangesSpecifier != null)
            {
              rangeSpecifier = timeRangesSpecifier;
              Logger.Debug("DlnaResourceAccessModule: Requesting range {1} for mediaitem {0}", mediaItemGuid.ToString(), timeRangesSpecifier);
              if (timeRangesSpecifier.Contains("npt=") == true)
              {
                requestedStreamingMode = StreamMode.TimeRange;
              }
            }

            #endregion

            Logger.Debug("DlnaResourceAccessModule: Attempting transcoding for mediaitem {0} in mode {1}", mediaItemGuid.ToString(), requestedStreamingMode.ToString());
            if (dlnaItem.StartTrancoding() == false)
            {
              Logger.Debug("DlnaResourceAccessModule: Transcoding busy for mediaitem {0}", mediaItemGuid.ToString());
              response.Status = HttpStatusCode.InternalServerError;
              response.Chunked = false;
              response.ContentLength = 0;
              response.ContentType = null;
              Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
              return true;
            }

            Stream resourceStream = null;
            double hlsStartRequest = 0;
            string hlsFileRequest = null;

            #region Check for HLS segment

            long segmentNumber = -1;
            if (resourceStream == null && dlnaItem.IsSegmented)
            {
              int startIndex = request.Uri.AbsoluteUri.LastIndexOf("/") + 1;
              hlsFileRequest = request.Uri.AbsoluteUri.Substring(startIndex);
              if (GetHlsSegment(hlsFileRequest, dlnaItem, response, ref resourceStream) == true)
              {
              }
              else if (MediaConverter.GetSegmentSequence(hlsFileRequest) > 0)
              {
                hlsStartRequest = MediaConverter.GetSegmentSequence(hlsFileRequest) * MediaConverter.HLSSegmentTimeInSeconds;
              }
            }

            #endregion

            #region Check for original file usage

            if (resourceStream == null && dlnaItem.IsTranscoded == false)
            {
              resourceStream = MediaConverter.GetReadyFileBuffer((ILocalFsResourceAccessor)dlnaItem.DlnaMetadata.Metadata.Source);
            }

            #endregion

            #region Process range request

            IList<Range> ranges = null;
            Range timeRange = null;
            Range byteRange = null;
            if (requestedStreamingMode == StreamMode.TimeRange)
            {
              double duration = dlnaItem.DlnaMetadata.Metadata.Duration;
              //if (dlnaItem.IsSegmented)
              //{
              //  //Is this possible?
              //  duration = MediaConverter.HLSSegmentTimeInSeconds;
              //}
              ranges = ParseTimeRanges(rangeSpecifier, duration);
              if (ranges == null || ranges.Count == 0)
              {
                //At least 1 range is needed
                response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
                response.Chunked = false;
                response.ContentLength = 0;
                response.ContentType = null;
                Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
                return true;
              }
            }
            else if (requestedStreamingMode == StreamMode.ByteRange)
            {
              long lSize = GetStreamSize(dlnaItem);
              //if (dlnaItem.IsSegmented)
              //{
              //  //TODO: Check if this is works
              //  lSize = resourceStream.Length;
              //}
              ranges = ParseByteRanges(rangeSpecifier, lSize);
              if (ranges == null || ranges.Count == 0)
              {
                //At least 1 range is needed
                response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
                response.Chunked = false;
                response.ContentLength = 0;
                response.ContentType = null;
                Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
                return true;
              }
            }

            if (dlnaItem.IsSegmented == false && dlnaItem.IsTranscoding == true && mediaTransferMode == TransferMode.Streaming)
            {
              if ((requestedStreamingMode == StreamMode.ByteRange || requestedStreamingMode == StreamMode.TimeRange) && (ranges == null || ranges.Count == 0))
              {
                //At least 1 range is needed
                response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
                response.Chunked = false;
                response.ContentLength = 0;
                response.ContentType = null;
                Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
                return true;
              }
            }
            if (ranges != null && ranges.Count > 0)
            {
              //Use only last range
              if (requestedStreamingMode == StreamMode.ByteRange)
              {
                byteRange = ranges[ranges.Count - 1];
                timeRange = ConvertToTimeRange(byteRange, dlnaItem);
              }
              else if (requestedStreamingMode == StreamMode.TimeRange)
              {
                timeRange = ranges[ranges.Count - 1];
                byteRange = ConvertToByteRange(timeRange, dlnaItem);
              }
            }
            if (timeRange == null)
            {
              if (hlsStartRequest > 0)
              {
                timeRange = new Range(Convert.ToInt64(hlsStartRequest), 0);
              }
              else
              {
                timeRange = new Range(0, 0);
              }
            }
            if (byteRange == null)
            {
              byteRange = new Range(0, 0);
            }

            #endregion

            #region Handle transcoding

            bool partialResource = false;
            TranscodeContext context = null;
            if (resourceStream == null)
            {
              context = MediaConverter.GetMediaStream(deviceClient.ClientId, dlnaItem.TranscodingParameter, timeRange.From, timeRange.Length, true);
              context.InUse = true;
              partialResource = context.Partial;
              dlnaItem.TranscodingContext = context;

              if (hlsFileRequest != null)
              {
                if(GetHlsSegment(hlsFileRequest, dlnaItem, response, ref resourceStream) == false)
                {
                  Logger.Error("DlnaResourceAccessModule: Unable to find segment file {0}", hlsFileRequest);

                  response.Status = HttpStatusCode.InternalServerError;
                  response.Chunked = false;
                  response.ContentLength = 0;
                  response.ContentType = null;
                  Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
                  return true;
                }
              }
              else
              {
                resourceStream = context.TranscodedStream;
              }
              if (dlnaItem.IsTranscoding == false || (context.Partial == false && context.TargetFileSize > 0 && context.TargetFileSize > dlnaItem.DlnaMetadata.Metadata.Size))
              {
                dlnaItem.DlnaMetadata.Metadata.Size = context.TargetFileSize;
              }
            }

            #endregion

            #region Create and send response

            if (resourceStream == null)
            {
              response.Status = HttpStatusCode.InternalServerError;
              response.Chunked = false;
              response.ContentLength = 0;
              response.ContentType = null;
              Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
              return true;
            }

            if (dlnaItem.IsStreamable == false)
            {
              Logger.Debug("DlnaResourceAccessModule: Live transcoding of mediaitem {0} is not possible because of media container", mediaItemGuid.ToString());
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

            try
            {
              bool onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
              if (requestedStreamingMode == StreamMode.TimeRange)
              {
                Logger.Debug("DlnaResourceAccessModule: Sending time range header only: {0}", onlyHeaders.ToString());
                if (timeRange != null && byteRange != null)
                {
                  // We only support one range
                  SendTimeRange(request, response, resourceStream, dlnaItem, deviceClient, timeRange, byteRange, onlyHeaders, partialResource, mediaTransferMode);
                  return true;
                }
              }
              else if (requestedStreamingMode == StreamMode.ByteRange)
              {
                Logger.Debug("DlnaResourceAccessModule: Sending byte range header only: {0}", onlyHeaders.ToString());
                if (byteRange != null)
                {
                  // We only support one range
                  SendByteRange(request, response, resourceStream, dlnaItem, deviceClient, byteRange, onlyHeaders, partialResource, mediaTransferMode);
                  return true;
                }
              }
              Logger.Debug("DlnaResourceAccessModule: Sending file header only: {0}", onlyHeaders.ToString());
              SendWholeFile(request, response, resourceStream, dlnaItem, deviceClient, onlyHeaders, partialResource, mediaTransferMode);
            }
            finally
            {
              if (context != null)
              {
                //Update current segment
                if (segmentNumber >= 0 && context.Segmented) context.CurrentSegment = segmentNumber;
                context.InUse = false;
              }
            }

            #endregion
          }
        }
      }
      catch (FileNotFoundException ex)
      {
        throw new InternalServerException(string.Format("Failed to proccess media item '{0}'", mediaItemGuid), ex);
      }

      return true;
    }

    private bool GetHlsSegment(string fileName, DlnaMediaItem dlnaItem, IHttpResponse response, ref Stream resourceStream)
    {
      if (fileName != null)
      {
        string hlsFile = Path.Combine(dlnaItem.TranscodingContext.SegmentDir, fileName);
        if (File.Exists(hlsFile) == true)
        {
          object containerEnum = null;
          if (MediaConverter.GetSegmentFile((VideoTranscoding)dlnaItem.TranscodingParameter, dlnaItem.TranscodingContext, fileName, out resourceStream, out containerEnum) == true)
          {
            if (containerEnum is VideoContainer)
            {
              VideoTranscoding video = (VideoTranscoding)dlnaItem.TranscodingParameter;
              List<string> profiles = DlnaProfiles.ResolveVideoProfile((VideoContainer)containerEnum, dlnaItem.DlnaMetadata.Video.Codec, video.TargetAudioCodec, dlnaItem.DlnaMetadata.Video.ProfileType, 
                dlnaItem.DlnaMetadata.Video.HeaderLevel, dlnaItem.DlnaMetadata.Video.Framerate, dlnaItem.DlnaMetadata.Video.Width, dlnaItem.DlnaMetadata.Video.Height, dlnaItem.DlnaMetadata.Video.Bitrate, 
                video.TargetAudioBitrate, dlnaItem.DlnaMetadata.Video.TimestampType);
              string mime = "video/unknown";
              string profile = null;
              DlnaProfiles.FindCompatibleProfile(dlnaItem.Client, profiles, ref profile, ref mime);
              response.ContentType = mime;
            }
            else if (containerEnum is SubtitleCodec)
            {
              response.ContentType = MediaConverter.GetSubtitleMime((SubtitleCodec)containerEnum);
            }
            return true;
          }
        }
      }
      return false;
    }

    protected void SendTimeRange(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, Profiles.EndPointSettings client, Range timeRange, Range byteRange, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      if (item.IsTranscoding)
      {
        //Transcoding delay
        Thread.Sleep(1000);
      }
      double duration = item.DlnaMetadata.Metadata.Duration;
      if (timeRange.From > Convert.ToInt64(duration))
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;
        Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
        return;
      }

      long length = byteRange.Length;
      if (item.IsSegmented == false && item.IsTranscoding == true)
      {
        length = GetStreamSize(item);
      }
      else
      {
        length = resourceStream.Length;
      }
      Range fileRange = ConvertToFileRange(byteRange, item, length);

      response.Status = HttpStatusCode.PartialContent;
      response.ContentLength = byteRange.Length;
      if (timeRange.Length == 0)
      {
        response.AddHeader("TimeSeekRange.dlna.org", string.Format("npt={0}-", timeRange.From));
      }
      else if (duration == 0)
      {
        response.AddHeader("TimeSeekRange.dlna.org", string.Format("npt={0}-{1}", timeRange.From, timeRange.To - 1));
      }
      else
      {
        response.AddHeader("TimeSeekRange.dlna.org", string.Format("npt={0}-{1}/{2}", timeRange.From, timeRange.To - 1, Convert.ToInt64(duration)));
      }
      response.AddHeader("X-Content-Duration", item.DlnaMetadata.Metadata.Duration.ToString("0.00", CultureInfo.InvariantCulture));
      response.AddHeader("Content-Duration", item.DlnaMetadata.Metadata.Duration.ToString("0.00", CultureInfo.InvariantCulture));

      if (mediaTransferMode == TransferMode.Streaming && request.HttpVersion == HttpHelper.HTTP11 && client.Profile.Settings.Communication.AllowChunckedTransfer)
      {
        response.Chunked = true;
      }
      else
      {
        response.Chunked = false;
      }

      Send(request, response, resourceStream, item, client, onlyHeaders, partialResource, fileRange);
    }

    protected void SendByteRange(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, Profiles.EndPointSettings client, Range range, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      if (range.From > 0 && range.From == range.To)
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;
        Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
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
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;
        Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
        return;
      }
      if (partialResource == false && WaitForMinimumFileSize(resourceStream, fileRange.From) == false)
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;
        Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
        return;
      }
      if(range.From > length || range.To > length)
      {
        range = fileRange;
      }

      response.Status = HttpStatusCode.PartialContent;
      response.ContentLength = range.Length;

      if (range.Length == 0)
      {
        response.AddHeader("Content-Range", string.Format("bytes {0}-", range.From));
      }
      else if(length <= 0)
      {
        response.AddHeader("Content-Range", string.Format("bytes {0}-{1}", range.From, range.To - 1));
      }
      else
      {
        response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", range.From, range.To - 1, length));
      }
      response.AddHeader("X-Content-Duration", item.DlnaMetadata.Metadata.Duration.ToString("0.00", CultureInfo.InvariantCulture));
      response.AddHeader("Content-Duration", item.DlnaMetadata.Metadata.Duration.ToString("0.00", CultureInfo.InvariantCulture));

      if (mediaTransferMode == TransferMode.Streaming && request.HttpVersion == HttpHelper.HTTP11 && client.Profile.Settings.Communication.AllowChunckedTransfer)// && item.IsTranscoding == true)
      {
        response.Chunked = true;
      }
      else
      {
        response.Chunked = false;
      }

      Send(request, response, resourceStream, item, client, onlyHeaders, partialResource, fileRange);
    }

    protected void SendWholeFile(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, Profiles.EndPointSettings client, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      if (WaitForMinimumFileSize(resourceStream, 1) == false)
      {
        response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;
        Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());
        return;
      }

      long length = GetStreamSize(item);
      if (resourceStream.CanSeek == true && (item.IsTranscoding == false || item.IsSegmented == true))
      {
        length = resourceStream.Length;
      }

      if (resourceStream.CanSeek == false && request.HttpVersion == HttpHelper.HTTP11 && client.Profile.Settings.Communication.AllowChunckedTransfer)
      {
        response.Status = HttpStatusCode.PartialContent;
        response.ContentLength = 0;
        response.Chunked = true;
      }
      else
      {
        response.Status = HttpStatusCode.OK;
        response.ContentLength = length;
        response.Chunked = false;
      }

      Range byteRange = new Range(0, response.ContentLength);
      Send(request, response, resourceStream, item, client, onlyHeaders, partialResource, byteRange);
    }

    protected void SendResourceFile(IHttpRequest request, IHttpResponse response, Stream resourceStream, bool onlyHeaders)
    {
      response.Status = HttpStatusCode.OK;
      response.Chunked = false;
      response.ContentLength = resourceStream.Length;
      Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());

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

    protected void Send(IHttpRequest request, IHttpResponse response, Stream resourceStream, DlnaMediaItem item, Profiles.EndPointSettings client, bool onlyHeaders, bool partialResource, Range byteRange)
    {
      Logger.Debug("DlnaResourceAccessModule: Sending headers: " + response.SendHeaders());

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
        bool isStream = false;
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
          Logger.Error("DlnaResourceAccessModule: Unable to send stream beacause of invalid length: {0} ({1} required)", resourceStream.Length, waitForSize);
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
          length = bufferSize;
        }
        int emptyCount = 0;
        while (item.IsStreamActive(streamID) && length > 0)
        {
          bytesRead = resourceStream.Read(buffer, 0, length > bufferSize ? bufferSize : (int)length);
          count += bytesRead;
          if (isStream)
          {
            if (resourceStream.CanSeek)
              length = resourceStream.Length - count;
            else if (bytesRead > 0)
              length = bufferSize;
            else
              length = 0;
          }
          else
          {
            length -= bytesRead;
          }
          if (bytesRead > 0)
          {
            emptyCount = 0;
            if (response.SendBody(buffer, 0, bytesRead) == false)
            {
              Logger.Debug("DlnaResourceAccessModule: Connection lost after {0} bytes", count);
              break;
            }
          }
          else
          {
            emptyCount++;
          }
          if (resourceStream.CanSeek)
          {
            if (item.IsTranscoding == false && resourceStream.Position == resourceStream.Length)
            {
              //No more data will be available
              length = 0;
            }
            if (item.IsSegmented == false && item.IsTranscoding)
            {
              long startWaitStreamLength = resourceStream.Length;
              int iWaits = 10;
              while (isStream && item.IsStreamActive(streamID) && item.IsTranscoding && length == 0)
              {
                Thread.Sleep(10);
                length = resourceStream.Length - start - count;
                Logger.Debug("DlnaResourceAccessModule: Buffer underrun delay {0}/{1}", count, resourceStream.Length - start);
                if (startWaitStreamLength == resourceStream.Length && iWaits <= 0) break; //Stream is not getting any bigger
                iWaits--;
              }
            }
          }
          else
          {
            if (emptyCount > 2) Thread.Sleep(10);
            if (emptyCount > 10) break; //Stream is not getting any bigger
          }
        }
        if (response.Chunked)
        {
          response.SendBody(null, 0, 0);
          Logger.Debug("DlnaResourceAccessModule: Sending final chunck");
        }
      }
      finally
      {
        // closes the Stream so that FFMpeg can replace the playlist file in case of HLS
        resourceStream.Close();
        item.StopStreaming(streamID);
        Logger.Debug("DlnaResourceAccessModule: Sending complete");
      }
    }

    public void Dispose()
    {
      Shutdown();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
