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
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.Transcoding.Service.Objects;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "hls", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "file", Type = typeof(string), Nullable = true)]
  internal class RetrieveStream : BaseSendData, IStreamRequestMicroModuleHandler2
  {
    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      Stream resourceStream = null;
      bool onlyHeaders = false;
      HttpParam httpParam = request.Param;
      string identifier = httpParam["identifier"].Value;
      string hls = httpParam["hls"].Value;
      string file = httpParam["file"].Value;

      if (identifier == null)
        throw new BadRequestException("RetrieveStream: identifier is null");

      if (!StreamControl.ValidateIdentifie(identifier))
        throw new BadRequestException("RetrieveStream: identifier is not valid");

      StreamItem streamItem = StreamControl.GetStreamItem(identifier);
      long startPosition = streamItem.StartPosition;
      if (streamItem.IsActive && hls != null)
      {
        #region Handle segment/playlist request

        if (SendSegment(hls, request, response, streamItem) == true)
        {
          return true;
        }
        else if(streamItem.ItemType != Common.WebMediaType.TV && 
          streamItem.ItemType != Common.WebMediaType.Radio &&
          MediaConverter.GetSegmentSequence(hls) > 0)
        {
          long segmentRequest = MediaConverter.GetSegmentSequence(hls);
          if(streamItem.RequestSegment(segmentRequest) == false)
          {
            Logger.Error("RetrieveStream: Request for segment file {0} cancelled", hls);

            response.Status = HttpStatusCode.InternalServerError;
            response.Chunked = false;
            response.ContentLength = 0;
            response.ContentType = null;
            response.SendHeaders();

            return true;
          }
          startPosition = segmentRequest * MediaConverter.HLSSegmentTimeInSeconds;
        }
        else
        {
          Logger.Error("RetrieveStream: Unable to find segment file {0}", hls);

          response.Status = HttpStatusCode.InternalServerError;
          response.Chunked = false;
          response.ContentLength = 0;
          response.ContentType = null;
          response.SendHeaders();

          return true;
        }

        #endregion
      }

      if (streamItem.IsActive == false)
      {
        Logger.Debug("RetrieveStream: Stream for {0} is no longer active", identifier);

        response.Status = HttpStatusCode.InternalServerError;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;
        response.SendHeaders();

        return true;
      }

      #region Init response

      EndPointSettings endPointSettings = ProfileManager.GetEndPointSettings(streamItem.Profile.ID);

      // Grab the mimetype from the media item and set the Content Type header.
      if (streamItem.TranscoderObject.Mime == null)
        throw new InternalServerException("RetrieveStream: Media item has bad mime type, re-import media item");
      response.ContentType = streamItem.TranscoderObject.Mime;

      TransferMode mediaTransferMode = TransferMode.Interactive;
      if (streamItem.TranscoderObject.IsVideo || streamItem.TranscoderObject.IsAudio)
      {
        mediaTransferMode = TransferMode.Streaming;
      }

      StreamMode requestedStreamingMode = StreamMode.Normal;
      string byteRangesSpecifier = request.Headers["Range"];
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
          response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
          response.Chunked = false;
          response.ContentLength = 0;
          response.ContentType = null;
          Logger.Debug("RetrieveStream: Sending headers: " + response.SendHeaders());
          return true;
        }
      }

      if (streamItem.TranscoderObject.IsSegmented == false && streamItem.TranscoderObject.IsTranscoding == true && mediaTransferMode == TransferMode.Streaming)
      {
        if ((requestedStreamingMode == StreamMode.ByteRange) && (ranges == null || ranges.Count == 0))
        {
          //At least 1 range is needed
          response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
          response.Chunked = false;
          response.ContentLength = 0;
          response.ContentType = null;
          Logger.Debug("RetrieveStream: Sending headers: " + response.SendHeaders());
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
          resourceStream = MediaConverter.GetReadyFileBuffer((ILocalFsResourceAccessor)streamItem.TranscoderObject.WebMetadata.Metadata.Source);
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

      // Attempting to transcode
      Logger.Debug("RetrieveStream: Attempting transcoding for mediaitem {0} in mode {1}", streamItem.RequestedMediaItem.MediaItemId, requestedStreamingMode.ToString());
      if (streamItem.TranscoderObject.StartTrancoding() == false)
      {
        Logger.Debug("RetrieveStream: Transcoding busy for mediaitem {0}", streamItem.RequestedMediaItem.MediaItemId);
        response.Status = HttpStatusCode.InternalServerError;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;

        response.SendHeaders();
        return true;
      }

      bool partialResource = false;
      if (resourceStream == null)
      {
        StreamControl.StopStreaming(identifier);
        StreamControl.StartStreaming(identifier, timeRange.From);
        partialResource = streamItem.StreamContext.Partial;
        resourceStream = streamItem.StreamContext.TranscodedStream;

        if (hls != null)
        {
          //Send HLS file originally requested
          if(SendSegment(hls, request, response, streamItem) == true)
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
      if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"]))
      {
        DateTime lastRequest = DateTime.Parse(request.Headers["If-Modified-Since"]);
        if (lastRequest.CompareTo(streamItem.TranscoderObject.LastUpdated) <= 0)
          response.Status = HttpStatusCode.NotModified;
      }

      // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
      response.AddHeader("Last-Modified", streamItem.TranscoderObject.LastUpdated.ToUniversalTime().ToString("r"));

      if (resourceStream == null)
      {
        response.Status = HttpStatusCode.InternalServerError;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;

        response.SendHeaders();
        return true;
      }

      lock (streamItem.BusyLock)
      {
        onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
        if (requestedStreamingMode == StreamMode.ByteRange)
        {
          if (ranges != null && ranges.Count > 0)
          {
            // We only support last range
            SendByteRange(request, response, resourceStream, streamItem.TranscoderObject, endPointSettings, ranges[ranges.Count - 1], onlyHeaders, partialResource, mediaTransferMode);
            return true;
          }
        }
        Logger.Debug("RetrieveStream: Sending file header only: {0}", onlyHeaders.ToString());
        SendWholeFile(request, response, resourceStream, streamItem.TranscoderObject, endPointSettings, onlyHeaders, partialResource, mediaTransferMode);
      }

      #endregion

      return true;
    }

    private bool SendSegment(string fileName, IHttpRequest request, IHttpResponse response, StreamItem streamItem)
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
              response.ContentType = mime;
            }
            else if (containerEnum is SubtitleCodec)
            {
              response.ContentType = MediaConverter.GetSubtitleMime((SubtitleCodec)containerEnum);
            }
            bool onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
            Logger.Debug("RetrieveStream: Sending file header only: {0}", onlyHeaders.ToString());

            SendWholeFile(response, resourceStream, onlyHeaders, false);
            // Close the Stream so that FFMpeg can replace the playlist file
            resourceStream.Dispose();
            return true;
          }
        }
      }
      return false;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
