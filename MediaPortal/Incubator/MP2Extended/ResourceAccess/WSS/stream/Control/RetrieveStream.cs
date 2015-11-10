using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "hls", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "folder", Type = typeof(string), Nullable = true)]
  internal class RetrieveStream : BaseSendData, IStreamRequestMicroModuleHandler2
  {
    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      Stream resourceStream = null;
      HttpParam httpParam = request.Param;
      string identifier = httpParam["identifier"].Value;
      string hls = httpParam["hls"].Value;
      string folder = httpParam["folder"].Value;

      if (identifier == null)
        throw new BadRequestException("RetrieveStream: identifier is null");

      if (folder != null && hls != null)
      {
        #region Handle segment request

        string segmentDir = MediaConverter.GetFolderFromFolderId(folder);
        string fileName = hls;
        if (!fileName.Contains("identifier"))
        {
          string segmentFile = Path.Combine(segmentDir, fileName);
          if (File.Exists(segmentFile) == true)
          {
            resourceStream = MediaConverter.GetReadyFileBuffer(segmentFile);
            response.ContentType = MediaConverter.GetHlsFileMime(segmentFile);

            bool onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
            Logger.Debug("RetrieveStream: Sending file header only: {0}", onlyHeaders.ToString());
            SendWholeFile(response, resourceStream, onlyHeaders);

            return true;
          }
          else
          {
            Logger.Error("RetrieveStream: Unable to find segment file {0}", fileName);

            response.Status = HttpStatusCode.InternalServerError;
            response.Chunked = false;
            response.ContentLength = 0;
            response.ContentType = null;
            response.SendHeaders();

            return true;
          }
        }

        #endregion
      }

      if (!StreamControl.ValidateIdentifie(identifier))
        throw new BadRequestException("RetrieveStream: identifier is not valid");

      #region Init stream item

      StreamItem streamItem = StreamControl.GetStreamItem(identifier);
      EndPointSettings endPointSettings = ProfileManager.GetEndPointSettings(streamItem.Profile.ID);

      if (hls == null)
      {
        ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
        necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
        necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);

        ISet<Guid> optionalMIATypes = new HashSet<Guid>();
        optionalMIATypes.Add(VideoAspect.ASPECT_ID);
        optionalMIATypes.Add(AudioAspect.ASPECT_ID);
        optionalMIATypes.Add(ImageAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemAudioAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemImageAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemVideoAspect.ASPECT_ID);

        MediaItem item = GetMediaItems.GetMediaItemById(streamItem.ItemId, necessaryMIATypes, optionalMIATypes);

        if (item == null)
        {
          Logger.Info("RetrieveStream: Couldn't start stream! No Mediaitem found with id: {0}", streamItem.ItemId.ToString());
        }

        streamItem.TranscoderObject = new ProfileMediaItem(item, endPointSettings);

        // set HLS Base URL
        if ((streamItem.TranscoderObject.TranscodingParameter is VideoTranscoding))
        {
          ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).HlsBaseUrl = string.Format("RetrieveStream?identifier={0}&folder={1}&hls=", identifier, MediaConverter.SEGMENT_FOLDER_TOKEN);
          if (streamItem.AudioStream >= 0)
            ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceAudioStreamIndex = streamItem.AudioStream;
          if (streamItem.SubtitleStream >= 0)
            ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceSubtitleStreamIndex = streamItem.SubtitleStream;
          else
            ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceSubtitleStreamIndex = MediaConverter.NO_SUBTITLE;
        }
      }

      #endregion

      #region Init response

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
        Logger.Debug("RetrieveStream: Requesting range {1} for mediaitem {0}", streamItem.ItemId.ToString(), byteRangesSpecifier);
        requestedStreamingMode = StreamMode.ByteRange;
      }

      #endregion

      #region Process range request

      IList<Range> ranges = null;
      Range timeRange = new Range(streamItem.StartPosition, 0);
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
        resourceStream = MediaConverter.GetReadyFileBuffer((ILocalFsResourceAccessor)streamItem.TranscoderObject.WebMetadata.Metadata.Source);
      }

      #endregion

      #region Handle transcode

      // Attempting to transcode
      Logger.Debug("RetrieveStream: Attempting transcoding for mediaitem {0} in mode {1}", streamItem.ItemId.ToString(), requestedStreamingMode.ToString());
      if (streamItem.TranscoderObject.StartTrancoding() == false)
      {
        Logger.Debug("RetrieveStream: Transcoding busy for mediaitem {0}", streamItem.ItemId.ToString());
        response.Status = HttpStatusCode.InternalServerError;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;

        response.SendHeaders();
        return true;
      }

      bool partialResource = false;
      TranscodeContext context = null;
      if (resourceStream == null)
      {
        context = MediaConverter.GetMediaStream(streamItem.TranscoderObject.TranscodingParameter, timeRange.From, timeRange.Length, true);
        context.InUse = true;
        partialResource = context.Partial;
        streamItem.TranscoderObject.SegmentDir = context.SegmentDir;
        resourceStream = context.TranscodedStream;
        if (streamItem.TranscoderObject.IsTranscoding == false || (context.Partial == false && context.TargetFileSize > 0 && context.TargetFileSize > streamItem.TranscoderObject.WebMetadata.Metadata.Size))
        {
          streamItem.TranscoderObject.WebMetadata.Metadata.Size = context.TargetFileSize;
        }

        lock (StreamControl.CurrentClientTranscodes)
        {
          if (StreamControl.CurrentClientTranscodes.ContainsKey(streamItem.ClientIp) == false)
          {
            StreamControl.CurrentClientTranscodes.Add(streamItem.ClientIp, new Dictionary<string, List<TranscodeContext>>());
          }
          if (StreamControl.CurrentClientTranscodes[streamItem.ClientIp].Count > 0 && StreamControl.CurrentClientTranscodes[streamItem.ClientIp].ContainsKey(streamItem.TranscoderObject.TranscodingParameter.TranscodeId) == false)
          {
            //Don't waste resources on transcoding if the client wants different media item
            Logger.Debug("RetrieveStream: Ending {0} transcodes for client {1}", StreamControl.CurrentClientTranscodes[streamItem.ClientIp].Count, streamItem.ClientIp);
            foreach (var transcodeContexts in StreamControl.CurrentClientTranscodes[streamItem.ClientIp].Values)
            {
              foreach (var transcodeContext in transcodeContexts)
              {
                if (transcodeContext.Running) transcodeContext.Stop();
                transcodeContext.InUse = false;
              }
            }
            StreamControl.CurrentClientTranscodes[streamItem.ClientIp].Clear();
          }
          if (StreamControl.CurrentClientTranscodes[streamItem.ClientIp].ContainsKey(streamItem.TranscoderObject.TranscodingParameter.TranscodeId) == false)
          {
            StreamControl.CurrentClientTranscodes[streamItem.ClientIp].Add(streamItem.TranscoderObject.TranscodingParameter.TranscodeId, new List<TranscodeContext>());
          }
          StreamControl.CurrentClientTranscodes[streamItem.ClientIp][streamItem.TranscoderObject.TranscodingParameter.TranscodeId].Add(context);
        }
      }

      if (!streamItem.TranscoderObject.IsStreamable)
      {
        Logger.Debug("RetrieveStream: Live transcoding of mediaitem {0} is not possible because of media container", streamItem.ItemId.ToString());
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

      try
      {
        bool onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
        if (requestedStreamingMode == StreamMode.ByteRange)
        {
          //Logger.Debug("DlnaResourceAccessModule: Sending byte range header only: {0}", onlyHeaders.ToString());
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
      finally
      {
        if (context != null) context.InUse = false;
      }

      #endregion

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
