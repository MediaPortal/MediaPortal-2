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
using System.IO;
using System.Linq;
using Microsoft.AspNet.Http;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "file", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "hls", Type = typeof(string), Nullable = true)]
  internal class RetrieveStream : BaseSendData
  {
    public bool Process(HttpContext httpContext, string identifier, string file, string hls)
    {
      Stream resourceStream = null;
      bool onlyHeaders = false;

      if (identifier == null)
        throw new BadRequestException("RetrieveStream: identifier is null");

      if (!StreamControl.ValidateIdentifier(identifier))
        throw new BadRequestException("RetrieveStream: identifier is not valid");

      StreamItem streamItem = StreamControl.GetStreamItem(identifier);
      long startPosition = streamItem.StartPosition;
      if (streamItem.IsActive && hls != null)
      {
        #region Handle segment/playlist request

        if (SendSegment(hls, httpContext, streamItem) == true)
        {
          return true;
        }
        else if (streamItem.ItemType != Common.WebMediaType.TV &&
          streamItem.ItemType != Common.WebMediaType.Radio &&
          MediaConverter.GetSegmentSequence(hls) > 0)
        {
          long segmentRequest = MediaConverter.GetSegmentSequence(hls);
          if (streamItem.RequestSegment(segmentRequest) == false)

          {
            Logger.Error("RetrieveStream: Request for segment file {0} canceled", hls);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentLength = null;
            httpContext.Response.ContentType = null;

            return true;
          }
          startPosition = segmentRequest * MediaConverter.HLSSegmentTimeInSeconds;
        }
        else
        {
          Logger.Error("RetrieveStream: Unable to find segment file {0}", hls);

          httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
          httpContext.Response.ContentLength = null;
          httpContext.Response.ContentType = null;

          return true;
        }

        #endregion
      }

      if (streamItem.IsActive == false)
      {
        Logger.Debug("RetrieveStream: Stream for {0} is no longer active", identifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentLength = null;
        httpContext.Response.ContentType = null;

        return true;
      }

      #region Init response

      EndPointSettings endPointSettings = ProfileManager.GetEndPointSettings(streamItem.Profile.ID);

      // Grab the mimetype from the media item and set the Content Type header.
      if (streamItem.TranscoderObject.Mime == null)
        throw new InternalServerException("RetrieveStream: Media item has bad mime type, re-import media item");
      httpContext.Response.ContentType = streamItem.TranscoderObject.Mime;

      TransferMode mediaTransferMode = TransferMode.Interactive;
      if (streamItem.TranscoderObject.IsVideo || streamItem.TranscoderObject.IsAudio)
      {
        mediaTransferMode = TransferMode.Streaming;
      }

      StreamMode requestedStreamingMode = StreamMode.Normal;
      string byteRangesSpecifier = httpContext.Request.Headers["Range"];
      if (byteRangesSpecifier != null)
      {
        Logger.Debug("RetrieveStream: Requesting range {1} for mediaitem {0}", streamItem.RequestedMediaItem.MediaItemId, byteRangesSpecifier);
        requestedStreamingMode = StreamMode.ByteRange;
      }

      #endregion

      #region Process range request

      if (streamItem.TranscoderObject.IsTranscoding == false ||
        (streamItem.StreamContext.Partial == false &&
        streamItem.StreamContext.TargetFileSize > 0 &&
        streamItem.StreamContext.TargetFileSize > streamItem.TranscoderObject.WebMetadata.Metadata.Size))
      {
        streamItem.TranscoderObject.WebMetadata.Metadata.Size = streamItem.StreamContext.TargetFileSize;
      }

      IList<Range> ranges = null;
      Range timeRange = new Range(startPosition, 0);
      Range byteRange = null;
      if (requestedStreamingMode == StreamMode.ByteRange)
      {
        long lSize = GetStreamSize(streamItem.TranscoderObject);
        ranges = ParseByteRanges(byteRangesSpecifier, lSize);
        if (ranges == null || ranges.Count == 0)
        {
          //At least 1 range is needed
          httpContext.Response.StatusCode = StatusCodes.Status416RequestedRangeNotSatisfiable;
          httpContext.Response.ContentLength = null;
          httpContext.Response.ContentType = null;
          Logger.Debug("RetrieveStream: Sending headers: " + string.Join(";", httpContext.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));
          return true;
        }
      }

      if (streamItem.TranscoderObject.IsSegmented == false && streamItem.TranscoderObject.IsTranscoding == true && mediaTransferMode == TransferMode.Streaming)
      {
        if ((requestedStreamingMode == StreamMode.ByteRange) && (ranges == null || ranges.Count == 0))
        {
          //At least 1 range is needed
          httpContext.Response.StatusCode = StatusCodes.Status416RequestedRangeNotSatisfiable;
          httpContext.Response.ContentLength = null;
          httpContext.Response.ContentType = null;
          Logger.Debug("RetrieveStream: Sending headers: " + string.Join(";", httpContext.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));
          return true;
        }
      }

      if (ranges != null && ranges.Count > 0)
      {
        //Use only last range
        if (requestedStreamingMode == StreamMode.ByteRange)
        {
          byteRange = ranges[ranges.Count - 1];
          timeRange = ConvertToTimeRange(byteRange, streamItem.TranscoderObject);
        }
      }

      #endregion

      #region Handle ready file request

      if (resourceStream == null && streamItem.TranscoderObject.IsTranscoded == false)
      {
        if (streamItem.TranscoderObject.WebMetadata.Metadata.Source is ILocalFsResourceAccessor)
        {
          resourceStream = MediaConverter.GetFileStream((ILocalFsResourceAccessor)streamItem.TranscoderObject.WebMetadata.Metadata.Source);
        }
      }

      if (resourceStream == null && (streamItem.StartPosition == timeRange.From || file != null))
      {
        //The initial request
        if (streamItem.StreamContext != null)
        {
          resourceStream = streamItem.StreamContext.TranscodedStream;
        }
      }

      #endregion

      #region Handle transcode

      bool partialResource = false;
      if (resourceStream == null)
      {
        Logger.Debug("RetrieveStream: Attempting to start streaming for mediaitem {0} in mode {1}", streamItem.RequestedMediaItem.MediaItemId, requestedStreamingMode.ToString());
        StreamControl.StopStreaming(identifier);
        StreamControl.StartStreaming(identifier, timeRange.From);
        partialResource = streamItem.StreamContext.Partial;
        resourceStream = streamItem.StreamContext.TranscodedStream;

        if (hls != null)
        {
          //Send HLS file originally requested
          if (SendSegment(hls, httpContext, streamItem) == true)
          {
            return true;
          }
        }
      }

      if (!streamItem.TranscoderObject.IsStreamable)
      {
        Logger.Debug("RetrieveStream: Live transcoding of mediaitem {0} is not possible because of media container", streamItem.RequestedMediaItem.MediaItemId);
      }

      #endregion

      #region Finish and send response

      // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
      if (!string.IsNullOrEmpty(httpContext.Request.Headers["If-Modified-Since"]))
      {
        DateTime lastRequest = DateTime.Parse(httpContext.Request.Headers["If-Modified-Since"]);
        if (lastRequest.CompareTo(streamItem.TranscoderObject.LastUpdated) <= 0)
          httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
      }

      // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
      httpContext.Response.Headers.Add("Last-Modified", streamItem.TranscoderObject.LastUpdated.ToUniversalTime().ToString("r"));

      if (resourceStream == null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentLength = null;
        httpContext.Response.ContentType = null;

        return true;
      }

      lock (streamItem.BusyLock)
      {
        // TODO: fix method
        onlyHeaders = /*request.Method == Method.Header ||*/ httpContext.Response.StatusCode == StatusCodes.Status304NotModified;
        if (requestedStreamingMode == StreamMode.ByteRange)
        {
          if (ranges != null && ranges.Count > 0)
          {
            // We only support last range
            SendByteRange(httpContext, resourceStream, streamItem.TranscoderObject, endPointSettings, ranges[ranges.Count - 1], onlyHeaders, partialResource, mediaTransferMode);
            return true;
          }
        }
        Logger.Debug("RetrieveStream: Sending file header only: {0}", onlyHeaders.ToString());
        SendWholeFile(httpContext, resourceStream, streamItem.TranscoderObject, endPointSettings, onlyHeaders, partialResource, mediaTransferMode);
      }

      #endregion

      return true;
    }

    private bool SendSegment(string fileName, HttpContext httpContext, StreamItem streamItem)
    {
      if (fileName != null)
      {
        lock (streamItem.BusyLock)
        {
          object containerEnum = null;
          Stream resourceStream = null;
          if (MediaConverter.GetSegmentFile((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter, streamItem.StreamContext, fileName, out resourceStream, out containerEnum) == true)
          {
            if (containerEnum is VideoContainer)
            {
              VideoTranscoding video = (VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter;
              List<string> profiles = ProfileMime.ResolveVideoProfile((VideoContainer)containerEnum, video.TargetVideoCodec, video.TargetAudioCodec, EncodingProfile.Unknown, 0, 0, 0, 0, 0, 0, Timestamp.None);
              string mime = "video/unknown";
              ProfileMime.FindCompatibleMime(streamItem.Profile, profiles, ref mime);
              httpContext.Response.ContentType = mime;
            }
            else if (containerEnum is SubtitleCodec)
            {
              httpContext.Response.ContentType = Subtitles.GetSubtitleMime((SubtitleCodec)containerEnum);
            }
            bool onlyHeaders = httpContext.Request.Method == Method.Header || httpContext.Response.StatusCode == StatusCodes.Status304NotModified;
            Logger.Debug("RetrieveStream: Sending file header only: {0}", onlyHeaders.ToString());

            SendWholeFile(httpContext, resourceStream, onlyHeaders);
            // Close the Stream so that FFMpeg can replace the playlist file
            resourceStream.Dispose();
            return true;
          }
        }
      }
      return false;
    }

    internal static IMediaConverter MediaConverter
    {
      get { return ServiceRegistration.Get<IMediaConverter>(); }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
