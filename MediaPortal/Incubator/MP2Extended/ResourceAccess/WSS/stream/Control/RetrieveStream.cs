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
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "file", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "hls", Type = typeof(string), Nullable = true)]
  internal class RetrieveStream : BaseSendData
  {
    public static async Task<bool> ProcessAsync(IOwinContext context, string identifier, string file, string hls)
    {
      Stream resourceStream = null;
      bool onlyHeaders = false;

      if (identifier == null)
        throw new BadRequestException("RetrieveStream: Identifier is null");

      StreamItem streamItem = await StreamControl.GetStreamItemAsync(identifier);
      if (streamItem == null)
        throw new BadRequestException("RetrieveStream: Identifier is not valid");

      long startPosition = streamItem.StartPosition;
      if (streamItem.IsActive && hls != null)
      {
        #region Handle segment/playlist request

        if (await SendSegmentAsync(hls, null, streamItem) == true)
        {
          return true;
        }
        else if (streamItem.ItemType != Common.WebMediaType.TV && streamItem.ItemType != Common.WebMediaType.Radio &&
          MediaConverter.GetSegmentSequence(hls) > 0)
        {
          long segmentRequest = MediaConverter.GetSegmentSequence(hls);
          if (streamItem.RequestSegment(segmentRequest) == false)
          {
            Logger.Error("RetrieveStream: Request for segment file {0} canceled", hls);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ReasonPhrase = "Request for segment file canceled";
            context.Response.ContentLength = 0;
            context.Response.ContentType = null;

            return true;
          }
          startPosition = segmentRequest * MediaConverter.HLSSegmentTimeInSeconds;
        }
        else
        {
          Logger.Error("RetrieveStream: Unable to find segment file {0}", hls);

          context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
          context.Response.ReasonPhrase = "Unable to find segment file";
          context.Response.ContentLength = 0;
          context.Response.ContentType = null;

          return true;
        }

        #endregion
      }

      if (streamItem.IsActive == false)
      {
        Logger.Debug("RetrieveStream: Stream for {0} is no longer active", identifier);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ReasonPhrase = "Stream is no longer active";
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;

        return true;
      }

      #region Init response

      // Grab the mimetype from the media item and set the Content Type header.
      if (streamItem.TranscoderObject.Mime == null)
        throw new InternalServerException("RetrieveStream: Media item has bad mime type, re-import media item");
      context.Response.ContentType = streamItem.TranscoderObject.Mime;

      TransferMode mediaTransferMode = TransferMode.Interactive;
      if (streamItem.TranscoderObject.IsVideo || streamItem.TranscoderObject.IsAudio)
      {
        mediaTransferMode = TransferMode.Streaming;
      }

      StreamMode requestedStreamingMode = StreamMode.Normal;
      string byteRangesSpecifier = context.Request.Headers["Range"];
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
          context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
          context.Response.ContentLength = 0;
          context.Response.ContentType = null;
          Logger.Debug("RetrieveStream: Sending headers: " + string.Join(";", context.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));
          return true;
        }
      }

      if (streamItem.TranscoderObject.IsSegmented == false && streamItem.TranscoderObject.IsTranscoding == true && mediaTransferMode == TransferMode.Streaming)
      {
        if ((requestedStreamingMode == StreamMode.ByteRange) && (ranges == null || ranges.Count == 0))
        {
          //At least 1 range is needed
          context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
          context.Response.ContentLength = 0;
          context.Response.ContentType = null;
          Logger.Debug("RetrieveStream: Sending headers: " + string.Join(";", context.Response.Headers.Select(x => x.Key + "=" + x.Value).ToArray()));
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
          resourceStream = await MediaConverter.GetFileStreamAsync((ILocalFsResourceAccessor)streamItem.TranscoderObject.WebMetadata.Metadata.Source);
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
        await StreamControl.StopStreamingAsync(identifier);
        await StreamControl.StartStreamingAsync(identifier, timeRange.From);
        partialResource = streamItem.StreamContext.Partial;
        resourceStream = streamItem.StreamContext.TranscodedStream;

        if (hls != null)
        {
          //Send HLS file originally requested
          if (await SendSegmentAsync(hls, context, streamItem) == true)
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
      if (!string.IsNullOrEmpty(context.Request.Headers["If-Modified-Since"]))
      {
        DateTime lastRequest = DateTime.Parse(context.Request.Headers["If-Modified-Since"]);
        if (lastRequest.CompareTo(streamItem.TranscoderObject.LastUpdated) <= 0)
          context.Response.StatusCode = (int)HttpStatusCode.NotModified;
      }

      // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
      context.Response.Headers["Last-Modified"] = streamItem.TranscoderObject.LastUpdated.ToUniversalTime().ToString("r");

      if (resourceStream == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ReasonPhrase = "No resource stream found";
        context.Response.ContentLength = 0;
        context.Response.ContentType = null;

        return true;
      }

      await streamItem.BusyLock.WaitAsync(SendDataCancellation.Token);
      try
      {
        // TODO: fix method
        onlyHeaders = context.Request.Method == "HEAD" || context.Response.StatusCode == (int)HttpStatusCode.NotModified;
        if (requestedStreamingMode == StreamMode.ByteRange)
        {
          if (ranges != null && ranges.Count > 0)
          {
            // We only support last range
            await SendByteRangeAsync(context, resourceStream, streamItem.TranscoderObject, streamItem.Profile, ranges[ranges.Count - 1], onlyHeaders, partialResource, mediaTransferMode);
            return true;
          }
        }
        Logger.Debug("RetrieveStream: Sending file header only: {0}", onlyHeaders.ToString());
        await SendWholeFileAsync(context, resourceStream, streamItem.TranscoderObject, streamItem.Profile, onlyHeaders, partialResource, mediaTransferMode);
      }
      finally
      {
        streamItem.BusyLock.Release();
      }

      #endregion

        return true;
    }

    private static async Task<bool> SendSegmentAsync(string fileName, IOwinContext context, StreamItem streamItem)
    {
      if (fileName != null)
      {
        await streamItem.BusyLock.WaitAsync(SendDataCancellation.Token);
        try
        {
          var segment = await MediaConverter.GetSegmentFileAsync((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter, streamItem.StreamContext, fileName);
          if (segment != null)
          {
            if (segment.Value.ContainerEnum is VideoContainer)
            {
              VideoTranscoding video = (VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter;
              List<string> profiles = ProfileMime.ResolveVideoProfile((VideoContainer)segment.Value.ContainerEnum, video.TargetVideoCodec, video.TargetAudioCodec, EncodingProfile.Unknown, 0, 0, 0, 0, 0, 0, Timestamp.None);
              string mime = "video/unknown";
              ProfileMime.FindCompatibleMime(streamItem.Profile, profiles, ref mime);
              context.Response.ContentType = mime;
            }
            else if (segment.Value.ContainerEnum is SubtitleCodec)
            {
              context.Response.ContentType = SubtitleHelper.GetSubtitleMime((SubtitleCodec)segment.Value.ContainerEnum);
            }
            bool onlyHeaders = context.Request.Method == "HEAD" || context.Response.StatusCode == (int)HttpStatusCode.NotModified;
            Logger.Debug("RetrieveStream: Sending file header only: {0}", onlyHeaders.ToString());

            await SendWholeFileAsync(context, segment.Value.FileData, onlyHeaders);
            // Close the Stream so that FFMpeg can replace the playlist file
            segment.Value.FileData.Dispose();
            return true;
          }
        }
        finally
        {
          streamItem.BusyLock.Release();
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
