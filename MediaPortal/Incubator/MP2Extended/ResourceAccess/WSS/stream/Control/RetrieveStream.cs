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
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Control
{
  internal class RetrieveStream : BaseSendData, IStreamRequestMicroModuleHandler2
  {
    private MediaConverter _transcoder;

    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      _transcoder = new MediaConverter();
      
      HttpParam httpParam = request.Param;
      string identifier = httpParam["identifier"].Value;
      string hls = httpParam["hls"].Value;

      if (identifier == null)
        throw new BadRequestException("RetrieveStream: identifier is null");

      if (!StreamControl.ValidateIdentifie(identifier))
        throw new BadRequestException("RetrieveStream: identifier is not valid");

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

        streamItem.TranscoderObject = new DlnaMediaItem(item, endPointSettings);

        // set HLS Base URL
        if ((streamItem.TranscoderObject.TranscodingParameter is VideoTranscoding))
        {
          ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).HlsBaseUrl = string.Format("RetrieveStream?identifier={0}&hls=", identifier);
        }
      }

      // FROM DLNA MEDIA SERVER \\

      SubtitleStream subSource = null;
      SubtitleCodec subTargetCodec = SubtitleCodec.Unknown;
      string subTargetMime = "";

      #region handle Subs
      bool subUseLocal = false;
      if (streamItem.TranscoderObject.IsSubtitled)
      {
        subUseLocal = FindSubtitle(endPointSettings, out subTargetCodec, out subTargetMime);
        if (streamItem.TranscoderObject.IsTranscoded && streamItem.TranscoderObject.IsVideo)
        {
          VideoTranscoding video = (VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter;
          video.TargetSubtitleCodec = subTargetCodec;
          video.TargetSubtitleLanguages = endPointSettings.PreferredSubtitleLanguages;
        }
        else if (streamItem.TranscoderObject.IsVideo)
        {
          VideoTranscoding subtitle = (VideoTranscoding)streamItem.TranscoderObject.SubtitleTranscodingParameter;
          subtitle.TargetSubtitleCodec = subTargetCodec;
          subtitle.TargetSubtitleLanguages = endPointSettings.PreferredSubtitleLanguages;
        }
      }

      #endregion handle Subs

      // Grab the mimetype from the media item and set the Content Type header.
      if (streamItem.TranscoderObject.DlnaMime == null)
        throw new InternalServerException("RetrieveStream: Media item has bad mime type, re-import media item");
      response.ContentType = streamItem.TranscoderObject.DlnaMime;


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
        requestedStreamingMode = byteRangesSpecifier.Contains("npt=") == true ? StreamMode.TimeRange : StreamMode.ByteRange;
      }

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

      Stream resourceStream = null;
      if (resourceStream == null && streamItem.TranscoderObject.IsSegmented)
      {
        //int startIndex = request.Uri.AbsoluteUri.LastIndexOf("/") + 1;
        //string fileName = request.Uri.AbsoluteUri.Substring(startIndex);
        string fileName = hls;
        if (Path.GetExtension(_transcoder.HLSSegmentFileTemplate) == Path.GetExtension(fileName) && !fileName.Contains("identifier"))
        {
          string segmentFile = Path.Combine(streamItem.TranscoderObject.SegmentDir, fileName);
          if (File.Exists(segmentFile) == true)
          {
            resourceStream = _transcoder.GetReadyFileBuffer(segmentFile);
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
      }
      if (resourceStream == null && streamItem.TranscoderObject.IsTranscoded == false)
      {
        resourceStream = _transcoder.GetReadyFileBuffer((ILocalFsResourceAccessor)streamItem.TranscoderObject.DlnaMetadata.Metadata.Source);
      }
      if (resourceStream == null)
      {
        TranscodeContext context = _transcoder.GetMediaStream(streamItem.TranscoderObject.TranscodingParameter, true);
        streamItem.TranscoderObject.SegmentDir = context.SegmentDir;
        StreamControl.UpdateStreamItem(identifier, streamItem); // save the changes to the SegmentDir
        resourceStream = context.TranscodedStream;
        
        lock (StreamControl.LastClientTranscode)
        {
          if (StreamControl.LastClientTranscode.ContainsKey(identifier) == false)
          {
            StreamControl.LastClientTranscode.Add(identifier, context);
          }
          else
          {
            if (StreamControl.LastClientTranscode[identifier].Running == true && StreamControl.LastClientTranscode[identifier] != context)
            {
              //Don't waste resources on transcoding if the client wants different media item
              StreamControl.LastClientTranscode[identifier].Stop();
            }
            StreamControl.LastClientTranscode[identifier] = context;
          }
        }
      }

      if (resourceStream == null)
      {
        response.Status = HttpStatusCode.InternalServerError;
        response.Chunked = false;
        response.ContentLength = 0;
        response.ContentType = null;
          
        response.SendHeaders();
        return true;
      }

      if (!streamItem.TranscoderObject.IsStreamable)
      {
        Logger.Debug("RetrieveStream: Live transcoding of mediaitem {0} is not possible because of media container", streamItem.ItemId.ToString());
      }

      #region handle range requests

      IList<Range> ranges = null;
      if (requestedStreamingMode == StreamMode.TimeRange)
      {
        double duration = streamItem.TranscoderObject.DlnaMetadata.Metadata.Duration;
        if (streamItem.TranscoderObject.IsSegmented)
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

          response.SendHeaders();
          return true;
        }
      }
      else if (requestedStreamingMode == StreamMode.ByteRange)
      {
        long lSize = streamItem.TranscoderObject.IsTranscoding ? GetStreamSize(streamItem.TranscoderObject) : resourceStream.Length;
        if (streamItem.TranscoderObject.IsSegmented)
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

          response.SendHeaders();
          return true;
        }
      }

      if (streamItem.TranscoderObject.IsSegmented == false && streamItem.TranscoderObject.IsTranscoding && mediaTransferMode == TransferMode.Streaming)
      {
        if ((requestedStreamingMode == StreamMode.ByteRange || requestedStreamingMode == StreamMode.TimeRange) && ranges == null)
        {
          //Only support 1 range
          response.Status = HttpStatusCode.RequestedRangeNotSatisfiable;
          response.Chunked = false;
          response.ContentLength = 0;
          response.ContentType = null;

          response.SendHeaders();
          return true;
        }
      }

      #endregion handle range requests

      // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
      if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"]))
      {
        DateTime lastRequest = DateTime.Parse(request.Headers["If-Modified-Since"]);
        if (lastRequest.CompareTo(streamItem.TranscoderObject.LastUpdated) <= 0)
          response.Status = HttpStatusCode.NotModified;
      }

      // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
      response.AddHeader("Last-Modified", streamItem.TranscoderObject.LastUpdated.ToUniversalTime().ToString("r"));


      bool onlyHeaders = request.Method == Method.Header || response.Status == HttpStatusCode.NotModified;
      if (requestedStreamingMode == StreamMode.TimeRange)
      {
        Logger.Debug("DlnaResourceAccessModule: Sending time range header only: {0}", onlyHeaders.ToString());
        if (ranges != null && ranges.Count == 1)
        {
          // We only support one range
          SendTimeRange(request, response, resourceStream, streamItem.TranscoderObject, endPointSettings, ranges[0], onlyHeaders, mediaTransferMode);
          return true;
        }
      }
      else if (requestedStreamingMode == StreamMode.ByteRange)
      {
        //Logger.Debug("DlnaResourceAccessModule: Sending byte range header only: {0}", onlyHeaders.ToString());
        if (ranges != null && ranges.Count == 1)
        {
          // We only support one range
          SendByteRange(request, response, resourceStream, streamItem.TranscoderObject, endPointSettings, ranges[0], onlyHeaders, mediaTransferMode);
          return true;
        }
      }
      Logger.Debug("DlnaResourceAccessModule: Sending file header only: {0}", onlyHeaders.ToString());
      SendWholeFile(request, response, resourceStream, streamItem.TranscoderObject, endPointSettings, onlyHeaders, mediaTransferMode);

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}