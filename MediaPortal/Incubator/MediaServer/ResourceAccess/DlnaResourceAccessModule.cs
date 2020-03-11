#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Extensions.MediaServer.Protocols;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;
using MediaPortal.Backend.MediaLibrary;
using Microsoft.Owin;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MediaServer.ResourceAccess
{
  public class DlnaResourceAccessModule : OwinMiddleware, IDisposable
  {
    public const long TRANSCODED_VIDEO_STREAM_MAX = 50000000000L;
    public const long TRANSCODED_AUDIO_STREAM_MAX = 900000000L;
    public const long TRANSCODED_IMAGE_STREAM_MAX = 9000000L;
    public const long TRANSCODED_SUBTITLE_STREAM_MAX = 300000L;

    private string _serverOsVersion = null;
    private string _product = null;
    private MediaServerClientManager _clientManager = null;
    private static CancellationTokenSource _serverCancellation = new CancellationTokenSource();

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

    public DlnaResourceAccessModule(OwinMiddleware next) : base(next)
    {
      _clientManager = new MediaServerClientManager();
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
      _serverCancellation.Cancel();
      foreach (EndPointSettings clients in ProfileManager.ProfileLinks.Values)
      {
        foreach (DlnaMediaItem item in clients.DlnaMediaItems.Values)
        {
          try
          {
            if (item.IsStreaming)
            {
              item.StopStreaming();
              Logger.Debug("ResourceAccessModule: Stopping stream of mediaitem ", item.MediaItemId);
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
              Logger.Debug("ResourceAccessModule: Aborting transcoding of mediaitem ", item.MediaItemId);
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
      long length = dlnaItem?.Metadata?.Size ?? 0;
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
        Logger.Debug("ResourceAccessModule: Received illegal Range header", e);
        // As specified in RFC2616, section 14.35.1, ignore invalid range header
      }
      return result;
    }

    protected Range ConvertToByteRange(Range timeRange, DlnaMediaItem dlnaItem)
    {
      if (timeRange.Length <= 0.0)
      {
        return new Range(0, dlnaItem.Metadata?.Size ?? 0);
      }
      long startByte = 0;
      long endByte = 0;
      if (dlnaItem.IsTranscoding == true)
      {
        long length = GetStreamSize(dlnaItem);
        double factor = Convert.ToDouble(length) / Convert.ToDouble(dlnaItem.Metadata.Duration);
        startByte = Convert.ToInt64(Convert.ToDouble(timeRange.From) * factor);
        endByte = Convert.ToInt64(Convert.ToDouble(timeRange.To) * factor);
      }
      else
      {
        double bitrate = 0;
        if (dlnaItem.IsSegmented == false)
        {
          bitrate = Convert.ToDouble(dlnaItem.Metadata.Bitrate) * 1024; //Bitrate in bits/s
        }
        startByte = Convert.ToInt64((bitrate * timeRange.From) / 8.0);
        endByte = Convert.ToInt64((bitrate * timeRange.To) / 8.0);
      }
      return new Range(startByte, endByte);
    }

    protected Range ConvertToTimeRange(Range byteRange, DlnaMediaItem dlnaItem)
    {
      if (byteRange.Length <= 0.0)
      {
        return new Range(0, Convert.ToInt64(dlnaItem.Metadata.Duration));
      }

      double startSeconds = 0;
      double endSeconds = 0;
      if (dlnaItem.IsTranscoding == true)
      {
        long length = GetStreamSize(dlnaItem);
        double factor = Convert.ToDouble(dlnaItem.Metadata.Duration) / Convert.ToDouble(length);
        startSeconds = Convert.ToDouble(byteRange.From) * factor;
        endSeconds = Convert.ToDouble(byteRange.To) * factor;
      }
      else
      {
        double bitrate = 0;
        if (dlnaItem.IsSegmented == false)
        {
          bitrate = Convert.ToDouble(dlnaItem.Metadata.Bitrate) * 1024; //Bitrate in bits/s
        }
        if (bitrate > 0)
        {
          startSeconds = Convert.ToDouble(byteRange.From) / (bitrate / 8.0);
          endSeconds = Convert.ToDouble(byteRange.To) / (bitrate / 8.0);
        }
      }
      return new Range(Convert.ToInt64(startSeconds), Convert.ToInt64(endSeconds));
    }

    protected Range ConvertToFileRange(Range requestedByteRange, DlnaMediaItem dlnaItem, long length)
    {
      long toRange = requestedByteRange.To;
      long fromRange = requestedByteRange.From;
      if (toRange <= 0 || toRange > length)
      {
        toRange = length;
      }
      if (dlnaItem.IsSegmented == false && dlnaItem.IsTranscoding == true)
      {
        if (dlnaItem.Metadata.Size > 0 && (toRange > dlnaItem.Metadata.Size || fromRange > dlnaItem.Metadata.Size))
        {
          fromRange = Convert.ToInt64((Convert.ToDouble(fromRange) / Convert.ToDouble(length)) * Convert.ToDouble(dlnaItem.Metadata.Size));
          toRange = Convert.ToInt64((Convert.ToDouble(toRange) / Convert.ToDouble(length)) * Convert.ToDouble(dlnaItem.Metadata.Size));
        }
      }
      return new Range(fromRange, toRange);
    }

    /// <summary>
    /// Method that process the url
    /// </summary>
    public override async Task Invoke(IOwinContext context)
    {
      var uri = context.Request.Uri;
      if (!uri.ToString().Contains(DlnaResourceAccessUtils.RESOURCE_ACCESS_PATH))
      {
        await Next.Invoke(context);
        return;
      }

      bool bHandled = false;
      Logger.Debug($"DlnaResourceAccessModule: Received request {uri}");
#if DEBUG
      foreach (var header in context.Request.Headers)
        Logger.Debug($"DlnaResourceAccessModule: Header: {header.Key}={string.Join(";", header.Value)}");
#endif
      try
      {
        context.Response.Headers["Server"] = _serverOsVersion + " UPnP/1.1 DLNADOC/1.50, " + _product;
        context.Response.Headers["Cache-control"] = "no-cache";
        context.Response.Headers["Connection"] = "close";

        #region Handle icon request

        if (context.Request.Query["aspect"]?.Equals("ICON", StringComparison.InvariantCultureIgnoreCase) ?? false)
        {
          bHandled = true;
          Logger.Debug("DlnaResourceAccessModule: Attempting to load Icon");
          using (var fs = new FileStream(FileUtils.BuildAssemblyRelativePath(string.Format("MP2_DLNA_Server_{0}.{1}", context.Request.Query["size"], context.Request.Query["type"])), FileMode.Open, FileAccess.Read))
          {
            context.Response.ContentType = "image/" + context.Request.Query["type"];
            using (MemoryStream ms = new MemoryStream())
            {
              Image img = Image.FromStream(fs);
              img.Save(ms, ImageFormat.Png);
              await SendResourceFileAsync(context, ms, false);
            }
          }
        }

        #endregion

        #region Determine profile

        EndPointSettings deviceClient = null;
        string clientIp = context.Request.RemoteIpAddress;
        if (clientIp == null)
        {
          clientIp = "noip";
        }

        deviceClient = await ProfileManager.DetectProfileAsync(context.Request);
        if (deviceClient == null || deviceClient.Profile == null)
        {
          Logger.Warn("DlnaResourceAccessModule: Client {0} has no valid link or profile", clientIp);
          return;
        }

        Logger.Debug("DlnaResourceAccessModule: Using profile {0} for client {1}", deviceClient.Profile.Name, clientIp);
        GenericAccessProtocol protocolResource = GenericAccessProtocol.GetProtocolResourceHandler(deviceClient.Profile.ResourceAccessHandler);

        #endregion

        // Check the request path to see if it's for us.
        if (!context.Request.Path.Value.StartsWith(DlnaResourceAccessUtils.RESOURCE_ACCESS_PATH))
        {
          if (protocolResource.CanHandleRequest(context.Request) == false)
            return;
        }

        if (bHandled == false)
        {
          await deviceClient.InitializeAsync(clientIp);

          #region Determine media item and DLNA media item

          var potentialStreamItem = StreamControl.GetNewStreamItem(deviceClient, uri);
          var dlnaItem = potentialStreamItem.TranscoderObject;

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

          using (Stream resource = protocolResource.HandleResourceRequest(context, dlnaItem))
          {
            if (resource != null)
            {
              bHandled = true;
              Logger.Debug("DlnaResourceAccessModule: Resource protocol sending request for {0}", uri.ToString());
              await SendResourceFileAsync(context, resource, false);
            }
          }

          if (protocolResource.HandleRequest(context, dlnaItem) == true)
          {
            bHandled = true;
          }

          #endregion

          #region Handle subtitle request

          if (context.Request.Query["aspect"]?.Equals("SUBTITLE", StringComparison.InvariantCultureIgnoreCase) ?? false)
          {
            bHandled = true;
            if (dlnaItem.IsTranscoded)
            {
              using (var subStream = await MediaConverter.GetSubtitleStreamAsync(deviceClient.ClientId.ToString(), (VideoTranscoding)dlnaItem.TranscodingParameter))
              {
                context.Response.ContentType = ((VideoTranscoding)dlnaItem.TranscodingParameter).TargetSubtitleMime;
                if (subStream != null)
                {
                  Logger.Debug("DlnaResourceAccessModule: Sending transcoded subtitle file for {0}", uri.ToString());
                  await SendResourceFileAsync(context, subStream.Stream, false);
                }
              }
            }
            else
            {
              using (var subStream = await MediaConverter.GetSubtitleStreamAsync(deviceClient.ClientId.ToString(), (VideoTranscoding)dlnaItem.SubtitleTranscodingParameter))
              {
                context.Response.ContentType = ((VideoTranscoding)dlnaItem.SubtitleTranscodingParameter).TargetSubtitleMime;
                if (subStream != null)
                {
                  Logger.Debug("DlnaResourceAccessModule: Sending transcoded subtitle file for {0}", uri.ToString());
                  await SendResourceFileAsync(context, subStream.Stream, false);
                }
              }
            }
          }

          #endregion

          if (bHandled == false)
          {
            // Grab the mimetype from the media item and set the Content Type header.
            if (dlnaItem.DlnaMime == null)
            {
              Logger.Error("DlnaResourceAccessModule: Media item has bad mime type, re-import media item");

              context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
              context.Response.ReasonPhrase = "Media item has bad mime type, re-import media item";
              context.Response.ContentLength = 0;
              context.Response.ContentType = null;
              return;
            }
            context.Response.ContentType = dlnaItem.DlnaMime;

            #region Determine transfer mode

            TransferMode mediaTransferMode = TransferMode.Interactive;
            if (dlnaItem.IsVideo || dlnaItem.IsAudio)
            {
              mediaTransferMode = TransferMode.Streaming;
            }
            if (!string.IsNullOrEmpty(context.Request.Headers["transferMode.dlna.org"]))
            {
              string transferMode = context.Request.Headers["transferMode.dlna.org"];
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
            string byteRangesSpecifier = context.Request.Headers["Range"];
            if (byteRangesSpecifier != null)
            {
              rangeSpecifier = byteRangesSpecifier;
              Logger.Debug("DlnaResourceAccessModule: Requesting range {1} for mediaitem {0}", dlnaItem.MediaItemId, byteRangesSpecifier);
              if (byteRangesSpecifier.Contains("npt=") == true)
              {
                requestedStreamingMode = StreamMode.TimeRange;
              }
              else
              {
                requestedStreamingMode = StreamMode.ByteRange;
              }
            }

            string timeRangesSpecifier = context.Request.Headers["TimeSeekRange.dlna.org"];
            if (timeRangesSpecifier != null)
            {
              rangeSpecifier = timeRangesSpecifier;
              Logger.Debug("DlnaResourceAccessModule: Requesting range {1} for mediaitem {0}", dlnaItem.MediaItemId, timeRangesSpecifier);
              if (timeRangesSpecifier.Contains("npt=") == true)
              {
                requestedStreamingMode = StreamMode.TimeRange;
              }
            }

            #endregion

            Stream resourceStream = null;
            double hlsStartRequest = 0;
            string hlsFileRequest = null;
            StreamItem stream = StreamControl.GetExistingStreamItem(deviceClient);

            #region Check for HLS segment

            long segmentNumber = MediaConverter.GetSegmentSequence(hlsFileRequest);
            if (stream != null)
            {
              if (stream.IsActive == false && segmentNumber > 0)
              {
                Logger.Error("DlnaResourceAccessModule: Stream no longer active for segment file {0}", hlsFileRequest);

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ReasonPhrase = "Stream no longer active for segment file";
                context.Response.ContentLength = 0;
                context.Response.ContentType = null;
                return;
              }

              if (resourceStream == null && dlnaItem.IsSegmented)
              {
                int startIndex = context.Request.Uri.AbsoluteUri.LastIndexOf("/") + 1;
                hlsFileRequest = context.Request.Uri.AbsoluteUri.Substring(startIndex);
                resourceStream = await GetSegmentAsync(context, hlsFileRequest, stream);
                if (resourceStream != null && segmentNumber > 0)
                {
                  hlsStartRequest = segmentNumber * MediaConverter.HLSSegmentTimeInSeconds;
                }
              }
            }

            #endregion

            #region Check for original file usage

            if (resourceStream == null && dlnaItem.IsTranscoded == false)
            {
              await StreamControl.StopStreamingAsync(deviceClient);
              var streamContext = await StreamControl.StartOriginalFileStreamingAsync(deviceClient, potentialStreamItem);
              resourceStream = streamContext?.Stream;
              stream = potentialStreamItem;
            }

            #endregion

            #region Process range request

            IList<Range> ranges = null;
            Range timeRange = null;
            Range byteRange = null;
            if (requestedStreamingMode == StreamMode.TimeRange)
            {
              double duration = Convert.ToDouble(dlnaItem.Metadata.Duration);
              //if (dlnaItem.IsSegmented)
              //{
              //  //TODO: Check if this is works
              //  duration = MediaConverter.HLSSegmentTimeInSeconds;
              //}
              ranges = ParseTimeRanges(rangeSpecifier, duration);
              if (ranges == null || ranges.Count == 0)
              {
                //At least 1 range is needed
                context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
                context.Response.ContentLength = 0;
                context.Response.ContentType = null;
                return;
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
                context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
                context.Response.ContentLength = 0;
                context.Response.ContentType = null;
                return;
              }
            }

            if (dlnaItem.IsSegmented == false && dlnaItem.IsTranscoding == true && mediaTransferMode == TransferMode.Streaming)
            {
              if ((requestedStreamingMode == StreamMode.ByteRange || requestedStreamingMode == StreamMode.TimeRange) && (ranges == null || ranges.Count == 0))
              {
                //At least 1 range is needed
                context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
                context.Response.ContentLength = 0;
                context.Response.ContentType = null;
                return;
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
            if (resourceStream == null)
            {
              await StreamControl.StopStreamingAsync(deviceClient);
              var transcodeContext = await StreamControl.StartTranscodeStreamingAsync(deviceClient, timeRange.From, timeRange.Length, potentialStreamItem);
              partialResource = transcodeContext?.Partial ?? false;
              stream = potentialStreamItem;

              if (hlsFileRequest != null)
              {
                resourceStream = await GetSegmentAsync(context, hlsFileRequest, stream);
                if (resourceStream == null)
                {
                  Logger.Error("DlnaResourceAccessModule: Unable to find segment file {0}", hlsFileRequest);

                  context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                  context.Response.ReasonPhrase = "DlnaResourceAccessModule: Unable to find segment file";
                  context.Response.ContentLength = 0;
                  context.Response.ContentType = null;
                  return;
                }
              }
              else
              {
                resourceStream = transcodeContext?.Stream;
              }
              if (dlnaItem.IsTranscoding == false || (transcodeContext?.Partial == false && transcodeContext?.TargetFileSize > 0 && transcodeContext?.TargetFileSize > dlnaItem.Metadata.Size))
              {
                dlnaItem.Metadata.Size = transcodeContext?.TargetFileSize ?? 0;
              }
            }

            #endregion

            #region Create and send response

            if (resourceStream == null)
            {
              Logger.Error("DlnaResourceAccessModule: Resource stream was null");

              context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
              context.Response.ReasonPhrase = "Resource stream was null";
              context.Response.ContentLength = 0;
              context.Response.ContentType = null;
              return;
            }

            if (dlnaItem.IsStreamable == false)
            {
              Logger.Debug("DlnaResourceAccessModule: Live transcoding of mediaitem {0} is not possible because of media container", dlnaItem.MediaItemId);
            }

            // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
            if (!string.IsNullOrEmpty(context.Request.Headers["If-Modified-Since"]))
            {
              DateTime lastRequest = DateTime.Parse(context.Request.Headers["If-Modified-Since"]);
              if (lastRequest.CompareTo(dlnaItem.LastUpdated) <= 0)
                context.Response.StatusCode = (int)HttpStatusCode.NotModified;
            }

            // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
            context.Response.Headers["Last-Modified"] = dlnaItem.LastUpdated.ToUniversalTime().ToString("r");

            // DLNA Requirement: [7.4.26.1-6]
            // Since the DLNA spec allows contentFeatures.dlna.org with any request, we'll put it in.
            if (!string.IsNullOrEmpty(context.Request.Headers["getcontentFeatures.dlna.org"]))
            {
              if (context.Request.Headers["getcontentFeatures.dlna.org"] != "1")
              {
                Logger.Error("DlnaResourceAccessModule: Illegal value for getcontentFeatures.dlna.org");

                // DLNA Requirement [7.4.26.5]
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ReasonPhrase = "Illegal value for getcontentFeatures.dlna.org";
                context.Response.ContentLength = 0;
                context.Response.ContentType = null;
                return;
              }

              var dlnaString = DlnaProtocolInfoFactory.GetProfileInfo(dlnaItem, deviceClient.Profile.ProtocolInfo).ToString();
              context.Response.Headers["contentFeatures.dlna.org"] = dlnaString;
              Logger.Debug("DlnaResourceAccessModule: Returning contentFeatures {0}", dlnaString);
            }

            // DLNA Requirement: [7.4.55-57]
            // TODO: Bad implementation of requirement
            if (mediaTransferMode == TransferMode.Streaming)
            {
              context.Response.Headers["transferMode.dlna.org"] = "Streaming";
            }
            else if (mediaTransferMode == TransferMode.Interactive)
            {
              context.Response.Headers["transferMode.dlna.org"] = "Interactive";
            }
            else if (mediaTransferMode == TransferMode.Background)
            {
              context.Response.Headers["transferMode.dlna.org"] = "Background";
            }
            context.Response.Headers["realTimeInfo.dlna.org"] = "DLNA.ORG_TLAG=*";

            bool onlyHeaders = context.Request.Method == "HEAD" || context.Response.StatusCode == (int)HttpStatusCode.NotModified;
            if (requestedStreamingMode == StreamMode.TimeRange)
            {
              Logger.Debug("DlnaResourceAccessModule: Sending time range header only: {0}", onlyHeaders.ToString());
              if (timeRange != null && byteRange != null)
              {
                // We only support one range
                await SendTimeRangeAsync(context, resourceStream, stream, deviceClient, timeRange, byteRange, onlyHeaders, partialResource, mediaTransferMode);
                return;
              }
            }
            else if (requestedStreamingMode == StreamMode.ByteRange)
            {
              Logger.Debug("DlnaResourceAccessModule: Sending byte range header only: {0}", onlyHeaders.ToString());
              if (byteRange != null)
              {
                // We only support one range
                await SendByteRangeAsync(context, resourceStream, stream, deviceClient, byteRange, onlyHeaders, partialResource, mediaTransferMode);
                return;
              }
            }
            Logger.Debug("DlnaResourceAccessModule: Sending file header only: {0}", onlyHeaders.ToString());
            await SendWholeFileAsync(context, resourceStream, stream, deviceClient, onlyHeaders, partialResource, mediaTransferMode);

            #endregion
          }
        }
      }
      catch (FileNotFoundException ex)
      {
        Logger.Error("DlnaResourceAccessModule: Failed to process '{0}'", ex, uri);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ReasonPhrase = "Failed to process request";
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;
      }
    }

    private async Task<Stream> GetSegmentAsync(IOwinContext context, string fileName, StreamItem streamContext)
    {
      if (!string.IsNullOrEmpty(fileName) && streamContext.StreamContext is TranscodeContext tc)
      {
        string hlsFile = Path.Combine(tc.SegmentDir, fileName);
        if (File.Exists(hlsFile) == true)
        {
          var dlnaItem = streamContext.TranscoderObject;
          var file = await MediaConverter.GetSegmentFileAsync((VideoTranscoding)dlnaItem.TranscodingParameter, tc, fileName);
          if (file.HasValue)
          {
            if (file.Value.ContainerEnum is VideoContainer)
            {
              VideoTranscoding video = (VideoTranscoding)dlnaItem.TranscodingParameter;
              List<string> profiles = DlnaProfiles.ResolveVideoProfile((VideoContainer)file.Value.ContainerEnum, dlnaItem.Video.Codec, video.TargetAudioCodec, 
                dlnaItem.Video.ProfileType, dlnaItem.Video.HeaderLevel, dlnaItem.Video.Framerate, dlnaItem.Video.Width, dlnaItem.Video.Height, dlnaItem.Video.Bitrate,
                video.TargetAudioBitrate, dlnaItem.Video.TimestampType);
              string mime = "video/unknown";
              string profile = null;
              if (!DlnaProfiles.TryFindCompatibleProfile(dlnaItem.Client, profiles, ref profile, ref mime))
              {
                file.Value.FileData?.Dispose();
                return null;
              }
              context.Response.ContentType = mime;
            }
            else if (file.Value.ContainerEnum is SubtitleCodec)
            {
              context.Response.ContentType = SubtitleHelper.GetSubtitleMime((SubtitleCodec)file.Value.ContainerEnum);
            }
            return file.Value.FileData;
          }
        }
      }
      return null;
    }

    protected async Task SendTimeRangeAsync(IOwinContext context, Stream resourceStream, StreamItem streamContext, Profiles.EndPointSettings client, Range timeRange, Range byteRange, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      var dlnaItem = streamContext.TranscoderObject;
      if (dlnaItem.IsTranscoding)
      {
        //Transcoding delay
        await Task.Delay(1000);
      }
      double duration = Convert.ToDouble(dlnaItem.Metadata.Duration);
      if (timeRange.From > Convert.ToInt64(duration))
      {
        context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;
        return;
      }

      long length = byteRange.Length;
      if (dlnaItem.IsSegmented == false && dlnaItem.IsTranscoding == true)
      {
        length = GetStreamSize(dlnaItem);
      }
      else
      {
        length = resourceStream.Length;
      }
      Range fileRange = ConvertToFileRange(byteRange, dlnaItem, length);

      context.Response.StatusCode = (int)HttpStatusCode.PartialContent;

      if (dlnaItem.IsLive || timeRange.Length == 0 || 
        (mediaTransferMode == TransferMode.Streaming && context.Request.Protocol == "HTTP/1.1" && client.Profile.Settings.Communication.AllowChunckedTransfer))
      {
        context.Response.Headers["TimeSeekRange.dlna.org"] = $"npt={timeRange.From}-";
        context.Response.ContentLength = null;
      }
      else if (duration == 0)
      {
        context.Response.Headers["TimeSeekRange.dlna.org"] = $"npt={timeRange.From}-{timeRange.To - 1}";
        context.Response.ContentLength = byteRange.Length;
      }
      else
      {
        context.Response.Headers["TimeSeekRange.dlna.org"] = $"npt={timeRange.From}-{timeRange.To - 1}/{Convert.ToInt64(duration)}";
        context.Response.ContentLength = byteRange.Length;
      }
      if (dlnaItem.IsLive == false)
      {
        context.Response.Headers["X-Content-Duration"] = Convert.ToDouble(dlnaItem.Metadata.Duration).ToString("0.00", CultureInfo.InvariantCulture);
        context.Response.Headers["Content-Duration"] = Convert.ToDouble(dlnaItem.Metadata.Duration).ToString("0.00", CultureInfo.InvariantCulture);
      }

      await SendAsync(context, resourceStream, streamContext, client, onlyHeaders, partialResource, fileRange);
    }

    protected async Task SendByteRangeAsync(IOwinContext context, Stream resourceStream, StreamItem streamContext, EndPointSettings client, Range range, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      var dlnaItem = streamContext.TranscoderObject;
      if (range.From > 0 && range.From == range.To)
      {
        context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;
        return;
      }
      long length = range.Length;
      if (dlnaItem.IsSegmented == false && dlnaItem.IsTranscoding == true)
      {
        length = GetStreamSize(dlnaItem);
      }
      else
      {
        length = resourceStream.Length;
      }
      Range fileRange = ConvertToFileRange(range, dlnaItem, length);
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

      if (dlnaItem.IsLive || range.Length == 0 || 
        (mediaTransferMode == TransferMode.Streaming && context.Request.Protocol == "HTTP/1.1" && client.Profile.Settings.Communication.AllowChunckedTransfer))
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
      if (dlnaItem.IsLive == false)
      {
        context.Response.Headers["X-Content-Duration"] = Convert.ToDouble(dlnaItem.Metadata.Duration).ToString("0.00", CultureInfo.InvariantCulture);
        context.Response.Headers["Content-Duration"] = Convert.ToDouble(dlnaItem.Metadata.Duration).ToString("0.00", CultureInfo.InvariantCulture);
      }

      await SendAsync(context, resourceStream, streamContext, client, onlyHeaders, partialResource, fileRange);
    }

    protected async Task SendWholeFileAsync(IOwinContext context, Stream resourceStream, StreamItem streamContext, Profiles.EndPointSettings client, bool onlyHeaders, bool partialResource, TransferMode mediaTransferMode)
    {
      var item = streamContext.TranscoderObject;
      if (await WaitForMinimumFileSizeAsync(resourceStream, 1) == false)
      {
        context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;
        return;
      }

      long length = GetStreamSize(item);
      if (resourceStream.CanSeek == true && (item.IsTranscoding == false || item.IsSegmented == true))
      {
        length = resourceStream.Length;
      }

      if (resourceStream.CanSeek == false && context.Request.Protocol == "HTTP/1.1" && client.Profile.Settings.Communication.AllowChunckedTransfer)
      {
        context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
        context.Response.ContentLength = null;
      }
      else
      {
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.ContentLength = length;
      }

      Range byteRange = new Range(0, Convert.ToInt64(context.Response.ContentLength));
      await SendAsync(context, resourceStream, streamContext, client, onlyHeaders, partialResource, byteRange);
    }

    protected async Task SendResourceFileAsync(IOwinContext context, Stream resourceStream, bool onlyHeaders)
    {
      context.Response.StatusCode = (int)HttpStatusCode.OK;
      context.Response.ContentLength = resourceStream.Length;

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
        await context.Response.WriteAsync(buffer, 0, bytesRead, _serverCancellation.Token);
      }
      Logger.Debug("Sending resource file complete");
    }

    private async Task<bool> WaitForMinimumFileSizeAsync(Stream resourceStream, long minimumSize)
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

    protected async Task SendAsync(IOwinContext context, Stream resourceStream, StreamItem streamContext, EndPointSettings client, bool onlyHeaders, bool partialResource, Range byteRange)
    {
      if (onlyHeaders)
        return;

      var item = streamContext.TranscoderObject;
      bool clientDisconnected = false;
      Guid streamID = item.StartStreaming();
      if (streamID == Guid.Empty)
      {
        Logger.Error("DlnaResourceAccessModule: Unable to start stream");
        return;
      }
      try
      {
        Logger.Debug("DlnaResourceAccessModule: Sending chunked: {0}", context.Response.ContentLength == null);
        string clientID = context.Request.RemoteIpAddress;
        int bufferSize = client.Profile.Settings.Communication.DefaultBufferSize;
        if (bufferSize <= 0)
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
        if (await WaitForMinimumFileSizeAsync(resourceStream, waitForSize) == false)
        {
          Logger.Error("DlnaResourceAccessModule: Unable to send stream because of invalid length: {0} ({1} required)", resourceStream.Length, waitForSize);
          return;
        }

        _clientManager.AttachClient(client.ClientId);

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
              await context.Response.WriteAsync(buffer, 0, bytesRead, _serverCancellation.Token);
            }
            catch (Exception)
            {
              // Client disconnected
              Logger.Debug("DlnaResourceAccessModule: Connection lost after {0} bytes", count);
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
              Logger.Debug("DlnaResourceAccessModule: Buffer underrun delay");
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

        if (clientDisconnected || item.IsSegmented == false ||
          (streamContext.StreamContext is TranscodeContext tc && tc.Segmented && tc.CurrentSegment >= tc.LastSegment))
        {
          //If end of media or client disconnected
          Logger.Debug("DlnaResourceAccessModule: Ending stream");
          await StreamControl.StopStreamingAsync(client);
          _clientManager.DetachClient(client.ClientId);

          if (clientDisconnected == false)
          {
            //Everything sent to client so presume watched
            if (item.IsLive == false)
            {
              IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>(false);
              library?.NotifyUserPlayback(client.UserId.HasValue ? client.UserId.Value : client.ClientId, item.MediaItemId, 100, true);
            }
          }
        }
        Logger.Debug("DlnaResourceAccessModule: Sending complete");
      }
    }

    public void Dispose()
    {
      Shutdown();
    }

    private static IMediaConverter MediaConverter
    {
      get { return ServiceRegistration.Get<IMediaConverter>(); }
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
