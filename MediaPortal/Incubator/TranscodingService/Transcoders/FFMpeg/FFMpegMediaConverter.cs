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
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Drawing;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters;
using MediaPortal.Utilities.Process;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Encoders;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg
{
  public class FFMpegMediaConverter : BaseMediaConverter
  {
    private FFMpegEncoderHandler _ffMpegEncoderHandler;
    private FFMpegCommandline _ffMpegCommandline;
    private string _transcoderBinPath;

    public FFMpegMediaConverter()
    {
      _transcoderBinPath = FFMpegBinary.FFMpegPath;
      string result;
      using (Process process = new Process { StartInfo = new ProcessStartInfo(_transcoderBinPath, "") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true } })
      {
        process.Start();
        using (process.StandardError)
        {
          result = process.StandardError.ReadToEnd();
        }
        if (!process.HasExited)
          process.Kill();
        process.Close();
      }

      if (result.IndexOf("--enable-libass") == -1)
      {
        _logger.Warn("MediaConverter: FFMPEG is not compiled with libass support, hardcoded subtitles will not work.");
        _supportHardcodedSubs = false;
      }

      _ffMpegEncoderHandler = new FFMpegEncoderHandler();
      _ffMpegCommandline = new FFMpegCommandline(_transcoderMaximumThreads, _transcoderTimeout, _cachePath, _hlsSegmentTimeInSeconds, HLS_SEGMENT_FILE_TEMPLATE, _supportHardcodedSubs);
    }

    #region Subtitles

    protected override bool ExtractSubtitleFile(VideoTranscoding video, SubtitleStream subtitle, string subtitleEncoding, string targetFilePath, double timeStart)
    {
      return _ffMpegCommandline.ExtractSubtitleFile(video, subtitle, subtitleEncoding, targetFilePath, timeStart);
    }

    protected override bool ConvertSubtitleFile(string clientId, VideoTranscoding video, double timeStart, string transcodingFile, SubtitleStream sourceSubtitle, ref SubtitleStream res)
    {
      SubtitleCodec targetCodec = video.TargetSubtitleCodec;
      if (targetCodec == SubtitleCodec.Unknown)
      {
        targetCodec = sourceSubtitle.Codec;
      }

      string tempFile = null;
      FFMpegTranscodeData data = new FFMpegTranscodeData(_cachePath) { TranscodeId = video.TranscodeId + "_sub", ClientId = clientId };
      if (string.IsNullOrEmpty(video.TranscoderBinPath) == false)
      {
        data.TranscoderBinPath = video.TranscoderBinPath;
      }
      if (string.IsNullOrEmpty(video.TranscoderArguments) == false)
      {
        // TODO: not sure if this is working
        data.TranscoderArguments = video.TranscoderArguments;
        LocalFsResourceProvider localFsResourceProvider = new LocalFsResourceProvider();
        IResourceAccessor resourceAccessor = new LocalFsResourceAccessor(localFsResourceProvider, res.Source);
        data.InputResourceAccessor = resourceAccessor;
      }
      else
      {
        tempFile = transcodingFile + ".tmp";
        res = ConvertSubtitleToUtf8(res, tempFile);

        // TODO: not sure if this is working
        LocalFsResourceProvider localFsResourceProvider = new LocalFsResourceProvider();
        IResourceAccessor resourceAccessor = new LocalFsResourceAccessor(localFsResourceProvider, res.Source);
        _ffMpegCommandline.InitTranscodingParameters(resourceAccessor, ref data);
        data.InputArguments.Add(string.Format("-f {0}", FFMpegGetSubtitleContainer.GetSubtitleContainer(sourceSubtitle.Codec)));
        if (timeStart > 0)
        {
          data.OutputArguments.Add(string.Format(CultureInfo.InvariantCulture, "-ss {0:0.0}", timeStart));
        }

        res.Codec = targetCodec;
        string subtitleEncoder = "copy";
        if (res.Codec == SubtitleCodec.Unknown)
        {
          res.Codec = SubtitleCodec.Ass;
        }
        if (sourceSubtitle.Codec != res.Codec)
        {
          subtitleEncoder = FFMpegGetSubtitleContainer.GetSubtitleContainer(res.Codec);
        }
        string subtitleFormat = FFMpegGetSubtitleContainer.GetSubtitleContainer(res.Codec);
        data.OutputArguments.Add("-vn");
        data.OutputArguments.Add("-an");
        data.OutputArguments.Add(string.Format("-c:s {0}", subtitleEncoder));
        data.OutputArguments.Add(string.Format("-f {0}", subtitleFormat));
      }
      data.OutputFilePath = transcodingFile;

      _logger.Debug("FFMpegMediaConverter: Invoking transcoder to transcode subtitle file '{0}' for transcode '{1}'", res.Source, data.TranscodeId);
      bool success = FFMpegBinary.FFMpegExecuteWithResourceAccessAsync((ILocalFsResourceAccessor)data.InputResourceAccessor, data.TranscoderArguments, ProcessPriorityClass.Normal, _transcoderTimeout).Result.Success;
      if (success && File.Exists(transcodingFile) == true)
      {
        if (tempFile != null && File.Exists(tempFile)) File.Delete(tempFile);
        res.Source = transcodingFile;
        return true;
      }
      return false;
    }

    #endregion

    #region Metadata

    protected override void GetVideoDimensions(VideoTranscoding video, out Size newSize, out Size newContentSize, out float newPixelAspectRatio, out bool pixelARChanged, out bool videoARChanged, out bool videoHeightChanged)
    {
      _ffMpegCommandline.GetVideoDimensions(video, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);
    }

    #endregion

    #region Transcoding

    protected override TranscodeContext TranscodeVideo(string clientId, VideoTranscoding video, double timeStart, double timeDuration, bool waitForBuffer)
    {
      FFMpegTranscodeContext context = new FFMpegTranscodeContext(_cacheEnabled, _cachePath) { Failed = false };
      context.TargetDuration = video.SourceDuration;
      if (timeStart == 0 && video.TargetIsLive == false && _cacheEnabled)
      {
        timeDuration = 0;
        context.Partial = false;
      }
      else if (video.TargetVideoContainer == VideoContainer.Hls)
      {
        context.Partial = true;
      }
      else
      {
        video.TargetIsLive = true;
        context.Partial = true;
      }
      if (video.TargetVideoContainer == VideoContainer.Unknown)
      {
        video.TargetVideoContainer = video.SourceVideoContainer;
      }
      string transcodingFile = video.TranscodeId;
      transcodingFile += ".A" + video.SourceAudioStreamIndex;

      bool embeddedSupported = false;
      SubtitleCodec embeddedSubCodec = SubtitleCodec.Unknown;
      if (video.TargetSubtitleSupport == SubtitleSupport.Embedded)
      {
        if (video.TargetVideoContainer == VideoContainer.Matroska)
        {
          embeddedSupported = true;
          embeddedSubCodec = SubtitleCodec.Ass;
          video.TargetSubtitleCodec = SubtitleCodec.Ass;
        }
        else if (video.TargetVideoContainer == VideoContainer.Mp4)
        {
          embeddedSupported = true;
          embeddedSubCodec = SubtitleCodec.MovTxt;
          video.TargetSubtitleCodec = SubtitleCodec.MovTxt;
        }
        else if (video.TargetVideoContainer == VideoContainer.Hls)
        {
          embeddedSupported = true;
          embeddedSubCodec = SubtitleCodec.WebVtt;
          video.TargetSubtitleCodec = SubtitleCodec.WebVtt;
        }
        else if (video.TargetVideoContainer == VideoContainer.Avi)
        {
          embeddedSupported = true;
          embeddedSubCodec = SubtitleCodec.Srt;
          video.TargetSubtitleCodec = SubtitleCodec.Srt;
        }
        //else if (video.TargetVideoContainer == VideoContainer.Mpeg2Ts)
        //{
        //  embeddedSupported = true;
        //  embeddedSubCodec = SubtitleCodec.DvbSub;
        //  video.TargetSubtitleCodec = SubtitleCodec.VobSub;
        //}
      }
      video.TargetSubtitleMime = Subtitles.GetSubtitleMime(video.TargetSubtitleCodec);

      SubtitleStream currentSub = GetSubtitle(clientId, video, timeStart);
      if (currentSub != null) video.SourceSubtitleAvailable = true;
      else video.SourceSubtitleAvailable = false;
      if (currentSub != null && _supportHardcodedSubs == true && (embeddedSupported || video.TargetSubtitleSupport == SubtitleSupport.HardCoded))
      {
        if (string.IsNullOrEmpty(currentSub.Language) == false)
        {
          transcodingFile += ".S" + currentSub.Language;
        }
      }

      if (context.Partial)
      {
        transcodingFile = DateTime.Now.Ticks.ToString();
      }
      transcodingFile = Path.Combine(_cachePath, transcodingFile + ".mptv");

      if (File.Exists(transcodingFile))
      {
        //Use non-partial transcode if possible
        TranscodeContext existingContext = null;
        if (AssignExistingTranscodeContext(clientId, video.TranscodeId, ref existingContext) == true)
        {
          existingContext.TargetFile = transcodingFile;
          if (existingContext.TranscodedStream == null)
            existingContext.AssignStream(GetFileStream(transcodingFile));
          if (existingContext.CurrentDuration.TotalSeconds == 0)
          {
            double bitrate = 0;
            if (video.TargetVideoBitrate > 0 && video.TargetAudioBitrate > 0)
            {
              bitrate = video.TargetVideoBitrate + video.TargetAudioBitrate;
            }
            else if (video.SourceVideoBitrate > 0 && video.SourceAudioBitrate > 0)
            {
              bitrate = video.SourceVideoBitrate + video.SourceAudioBitrate;
            }
            bitrate *= 1024; //Bitrate in bits/s
            if (bitrate > 0)
            {
              long startByte = Convert.ToInt64((bitrate * timeStart) / 8.0);
              if (existingContext.TranscodedStream.Length > startByte)
              {
                return existingContext;
              }
            }
          }
          else
          {
            if (existingContext.CurrentDuration.TotalSeconds > timeStart)
            {
              return existingContext;
            }
          }
        }
        else
        {
          //Presume that it is a cached file
          TouchFile(transcodingFile);
          context.Partial = false;
          context.TargetFile = transcodingFile;
          context.AssignStream(GetFileStream(transcodingFile));
          return context;
        }
      }

      if (video.TargetVideoContainer == VideoContainer.Hls)
      {
        long requestedSegmentSequence = requestedSegmentSequence = Convert.ToInt64(timeStart / HLSSegmentTimeInSeconds);
        if (requestedSegmentSequence > 0)
        {
          requestedSegmentSequence--; //1 segment file margin
        }
        string pathName = FFMpegPlaylistManifest.GetPlaylistFolderFromTranscodeFile(_cachePath, transcodingFile);
        string playlist = Path.Combine(pathName, PlaylistManifest.PLAYLIST_MANIFEST_FILE_NAME);
        string segmentFile = Path.Combine(pathName, requestedSegmentSequence.ToString("00000") + ".ts");
        if (File.Exists(playlist) == true && File.Exists(segmentFile) == true)
        {
          //Use exisitng context if possible
          TranscodeContext existingContext = null;
          if (AssignExistingTranscodeContext(clientId, video.TranscodeId, ref existingContext) == true)
          {
            if (existingContext.LastSegment > requestedSegmentSequence)
            {
              existingContext.TargetFile = playlist;
              existingContext.SegmentDir = pathName;
              if (existingContext.TranscodedStream == null)
                existingContext.AssignStream(GetFileStream(playlist));
              existingContext.HlsBaseUrl = video.HlsBaseUrl;
              return existingContext;
            }
          }
          else
          {
            //Presume that it is a cached file
            TouchDirectory(pathName);
            context.Partial = false;
            context.TargetFile = playlist;
            context.SegmentDir = pathName;
            context.HlsBaseUrl = video.HlsBaseUrl;
            context.AssignStream(GetFileStream(playlist));
            return context;
          }
        }
      }

      FFMpegTranscodeData data = new FFMpegTranscodeData(_cachePath) { TranscodeId = video.TranscodeId, ClientId = clientId };
      if (string.IsNullOrEmpty(video.TranscoderBinPath) == false)
      {
        data.TranscoderBinPath = video.TranscoderBinPath;
      }
      if (string.IsNullOrEmpty(video.TranscoderArguments) == false)
      {
        data.TranscoderArguments = video.TranscoderArguments;
        data.InputResourceAccessor = video.SourceMedia;
        if (currentSub != null && string.IsNullOrEmpty(currentSub.Source) == false)
        {
          data.InputSubtitleFilePath = currentSub.Source;
          context.TargetSubtitle = currentSub.Source;
        }
        data.OutputFilePath = transcodingFile;
        context.TargetFile = transcodingFile;
      }
      else
      {
        data.Encoder = _ffMpegEncoderHandler.StartEncoding(video.TranscodeId, video.TargetVideoCodec);
        _ffMpegCommandline.InitTranscodingParameters(video.SourceMedia, ref data);

        bool useX26XLib = video.TargetVideoCodec == VideoCodec.H264 || video.TargetVideoCodec == VideoCodec.H265;
        _ffMpegCommandline.AddTranscodingThreadsParameters(!useX26XLib, ref data);

        int subCopyStream = -1;
        if (currentSub != null)
        {
          if (currentSub.StreamIndex >= 0)
          {
            subCopyStream = currentSub.StreamIndex;
            _ffMpegCommandline.AddSubtitleCopyParameters(currentSub, ref data);
          }
          else if (embeddedSupported)
          {
            _ffMpegCommandline.AddSubtitleEmbeddingParameters(currentSub, embeddedSubCodec, timeStart, ref data);
          }
          else if(video.TargetSubtitleSupport != SubtitleSupport.SoftCoded)
          {
            video.TargetSubtitleSupport = SubtitleSupport.HardCoded; //Fallback to hardcoded subtitles
          }
        }
        else
        {
          embeddedSupported = false;
          data.OutputArguments.Add("-sn");
        }

        _ffMpegCommandline.AddTimeParameters(timeStart, timeDuration, video.SourceDuration.TotalSeconds, ref data);

        FFMpegEncoderConfig encoderConfig = _ffMpegEncoderHandler.GetEncoderConfig(data.Encoder);
        _ffMpegCommandline.AddVideoParameters(video, data.TranscodeId, currentSub, encoderConfig, ref data);

        string fileName = transcodingFile;
        long startSegment = 0;
        _ffMpegCommandline.AddTargetVideoFormatAndOutputFileParameters(video, currentSub, ref fileName, out startSegment, timeStart, ref data);
        context.TargetFile = fileName;
        context.CurrentSegment = startSegment;
        if (currentSub != null && string.IsNullOrEmpty(currentSub.Source) == false)
        {
          context.TargetSubtitle = currentSub.Source;
        }
        _ffMpegCommandline.AddVideoAudioParameters(video, ref data);
        
        _ffMpegCommandline.AddStreamMapParameters(video.SourceVideoStreamIndex, video.SourceAudioStreamIndex, subCopyStream, embeddedSupported, ref data);
      }

      _logger.Info("FFMpegMediaConverter: Invoking transcoder to transcode video file '{0}' for transcode '{1}' with arguments '{2}'", video.SourceMedia.Path, video.TranscodeId, String.Join(", ", data.OutputArguments.ToArray()));
      context.Start();
      context.AssignStream(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    protected override TranscodeContext TranscodeAudio(string clientId, AudioTranscoding audio, double timeStart, double timeDuration, bool waitForBuffer)
    {
      FFMpegTranscodeContext context = new FFMpegTranscodeContext(_cacheEnabled, _cachePath) { Failed = false };
      context.TargetDuration = audio.SourceDuration;
      if (timeStart == 0 && audio.TargetIsLive == false && _cacheEnabled)
      {
        timeDuration = 0;
        context.Partial = false;
      }
      else
      {
        audio.TargetIsLive = true;
        context.Partial = true;
      }
      if (audio.TargetAudioContainer == AudioContainer.Unknown)
      {
        audio.TargetAudioContainer = audio.SourceAudioContainer;
      }

      string transcodingFile = audio.TranscodeId;
      if (context.Partial)
      {
        transcodingFile = DateTime.Now.Ticks.ToString();
      }
      transcodingFile += ".mpta";
      transcodingFile = Path.Combine(_cachePath, transcodingFile);

      if (File.Exists(transcodingFile) == true)
      {
        //Use non-partial context if possible
        TranscodeContext existingContext = null;
        if (AssignExistingTranscodeContext(clientId, audio.TranscodeId, ref existingContext) == true)
        {
          existingContext.TargetFile = transcodingFile;
          if (existingContext.TranscodedStream == null)
            existingContext.AssignStream(GetFileStream(transcodingFile));

          if (existingContext.CurrentDuration.TotalSeconds == 0)
          {
            double bitrate = 0;
            if (audio.TargetAudioBitrate > 0)
            {
              bitrate = audio.TargetAudioBitrate;
            }
            else if (audio.SourceAudioBitrate > 0)
            {
              bitrate = audio.SourceAudioBitrate;
            }
            bitrate *= 1024; //Bitrate in bits/s
            if (bitrate > 0)
            {
              long startByte = Convert.ToInt64((bitrate * timeStart) / 8.0);
              if (existingContext.TranscodedStream.Length > startByte)
              {
                return existingContext;
              }
            }
          }
          else
          {
            if (existingContext.CurrentDuration.TotalSeconds > timeStart)
            {
              return existingContext;
            }
          }
        }
        else
        {
          //Presume that it is a cached file
          TouchFile(transcodingFile);
          context.Partial = false;
          context.TargetFile = transcodingFile;
          context.AssignStream(GetFileStream(transcodingFile));
          return context;
        }
      }

      FFMpegTranscodeData data = new FFMpegTranscodeData(_cachePath) { TranscodeId = audio.TranscodeId, ClientId = clientId };
      if (string.IsNullOrEmpty(audio.TranscoderBinPath) == false)
      {
        data.TranscoderBinPath = audio.TranscoderBinPath;
      }
      if (string.IsNullOrEmpty(audio.TranscoderArguments) == false)
      {
        data.TranscoderArguments = audio.TranscoderArguments;
        data.InputResourceAccessor = audio.SourceMedia;
        data.OutputFilePath = transcodingFile;
        context.TargetFile = transcodingFile;
      }
      else
      {
        _ffMpegCommandline.InitTranscodingParameters(audio.SourceMedia, ref data);
        _ffMpegCommandline.AddTranscodingThreadsParameters(true, ref data);

        _ffMpegCommandline.AddTimeParameters(timeStart, timeDuration, audio.SourceDuration.TotalSeconds, ref data);

        _ffMpegCommandline.AddAudioParameters(audio, ref data);

        string fileName = transcodingFile;
        _ffMpegCommandline.AddTargetAudioFormatAndOutputFileParameters(audio, ref fileName, ref data);
        context.TargetFile = fileName;

        data.OutputArguments.Add("-vn");
      }

      _logger.Debug("FFMpegMediaConverter: Invoking transcoder to transcode audio file '{0}' for transcode '{1}'", audio.SourceMedia, audio.TranscodeId);
      context.Start();
      context.AssignStream(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    protected override TranscodeContext TranscodeImage(string clientId, ImageTranscoding image, bool waitForBuffer)
    {
      FFMpegTranscodeContext context = new FFMpegTranscodeContext(_cacheEnabled, _cachePath) { Failed = false };
      context.Partial = false;
      string transcodingFile = Path.Combine(_cachePath, image.TranscodeId + ".mpti");

      if (File.Exists(transcodingFile) == true)
      {
        //Use exisitng contaxt if possible
        TranscodeContext existingContext = null;
        if (AssignExistingTranscodeContext(clientId, image.TranscodeId, ref existingContext) == true)
        {
          existingContext.TargetFile = transcodingFile;
          if (existingContext.TranscodedStream == null)
            existingContext.AssignStream(GetFileStream(transcodingFile));
          return existingContext;
        }
        else
        {
          //Presume that it is a cached file
          TouchFile(transcodingFile);
          context.Partial = false;
          context.TargetFile = transcodingFile;
          context.AssignStream(GetFileStream(transcodingFile));
          return context;
        }
      }

      FFMpegTranscodeData data = new FFMpegTranscodeData(_cachePath) { TranscodeId = image.TranscodeId, ClientId = clientId };
      if (string.IsNullOrEmpty(image.TranscoderBinPath) == false)
      {
        data.TranscoderBinPath = image.TranscoderBinPath;
      }
      if (string.IsNullOrEmpty(image.TranscoderArguments) == false)
      {
        data.TranscoderArguments = image.TranscoderArguments;
        data.InputResourceAccessor = image.SourceMedia;
      }
      else
      {
        _ffMpegCommandline.InitTranscodingParameters(image.SourceMedia, ref data);
        _ffMpegCommandline.AddTranscodingThreadsParameters(true, ref data);

        _ffMpegCommandline.AddImageParameters(image, ref data);

        data.InputArguments.Add("-f image2pipe"); //pipe works with network drives
        data.OutputArguments.Add("-f image2");
      }
      data.OutputFilePath = transcodingFile;
      context.TargetFile = transcodingFile;

      _logger.Debug("FFMpegMediaConverter: Invoking transcoder to transcode image file '{0}' for transcode '{1}'", image.SourceMedia, image.TranscodeId);
      context.Start();
      context.AssignStream(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    private Stream GetFileStream(string filePath)
    {
      int iTry = 60;
      while (iTry > 0)
      {
        if (File.Exists(filePath) == true)
        {
          long length = 0;
          try
          {
            length = new FileInfo(filePath).Length;
          }
          catch { }
          if (length > 0)
          {
            _logger.Debug(string.Format("FFMpegMediaConverter: Serving ready file '{0}'", filePath));
            BufferedStream stream = new BufferedStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            return stream;
          }
        }
        iTry--;
        Thread.Sleep(500);
      }
      _logger.Error("FFMpegMediaConverter: Timed out waiting for ready file '{0}'", filePath);
      return null;
    }

    #endregion

    #region Transcoder

    private Stream GetTranscodedFileBuffer(FFMpegTranscodeData data, FFMpegTranscodeContext context)
    {
      try
      {
        if (data.IsLive == true && data.SegmentPlaylist == null)
        {
          int iTry = 60;
          while (iTry > 0 && context.Failed == false && context.Aborted == false)
          {
            bool streamReady = false;
            try
            {
              if (data.LiveStream != null)
                streamReady = data.LiveStream.CanRead;
            }
            catch { }
            if (streamReady)
            {
              _logger.Debug(string.Format("FFMpegMediaConverter: Serving transcoded stream '{0}'", data.TranscodeId));
              return new BufferedStream(data.LiveStream);
            }
            iTry--;
            Thread.Sleep(500);
          }
          _logger.Error("FFMpegMediaConverter: Timed out waiting for transcoded stream '{0}'", data.TranscodeId);
        }
        else
        {
          string fileReturnPath = "";
          if (data.SegmentPlaylist != null)
          {
            fileReturnPath = Path.Combine(data.WorkPath, data.SegmentPlaylist);
          }
          else
          {
            fileReturnPath = Path.Combine(data.WorkPath, data.OutputFilePath);
          }

          int iTry = 60;
          while (iTry > 0 && context.Failed == false && context.Aborted == false)
          {
            if (File.Exists(fileReturnPath))
            {
              long length = 0;
              try
              {
                length = new FileInfo(fileReturnPath).Length;
              }
              catch { }
              if (length > 0)
              {
                _logger.Debug(string.Format("FFMpegMediaConverter: Serving transcoded file '{0}'", fileReturnPath));
                if (data.SegmentPlaylist != null)
                {
                  MemoryStream memStream = new MemoryStream(PlaylistManifest.CorrectPlaylistUrls(data.SegmentBaseUrl, fileReturnPath));
                  memStream.Position = 0;
                  return memStream;
                }
                else
                {
                  Stream stream = new FileStream(fileReturnPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                  return stream;
                }
              }
            }
            iTry--;
            Thread.Sleep(500);
          }
          _logger.Error("FFMpegMediaConverter: Timed out waiting for transcoded file '{0}'", fileReturnPath);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("FFMpegMediaConverter: Error waiting for transcoding '{0}'", ex, data.TranscodeId);
      }
      return null;
    }

    private Stream ExecuteTranscodingProcess(FFMpegTranscodeData data, FFMpegTranscodeContext context, bool waitForBuffer)
    {
      if (context.Partial == true || IsTranscodeRunning(data.ClientId, data.TranscodeId) == false)
      {
        try
        {
          AddTranscodeContext(data.ClientId, data.TranscodeId, context);
          //context.TargetFile = Path.Combine(data.WorkPath, data.SegmentPlaylist != null ? data.SegmentPlaylist : data.OutputFilePath);
          //if(context.TargetFile.EndsWith("pipe:"))
          //{
          //  context.TargetFile = "";
          //}
          context.Live = data.IsLive;
          context.SegmentDir = null;
          if (data.SegmentPlaylist != null)
          {
            context.SegmentDir = data.WorkPath;
          }
          string name = "MP Transcode - " + data.TranscodeId;
          if (context.Partial)
          {
            name += " - Partial: " + Thread.CurrentThread.ManagedThreadId;
          }
          Thread transcodeThread = new Thread(TranscodeProcessor)
          {
            //IsBackground = true, //Can cause invalid cache files
            Name = name,
            Priority = ThreadPriority.Normal
          };
          FFMpegTranscodeThreadData threadData = new FFMpegTranscodeThreadData()
          {
            TranscodeData = data,
            Context = context
          };
          transcodeThread.Start(threadData);
        }
        catch
        {
          _ffMpegEncoderHandler.EndEncoding(data.Encoder, data.TranscodeId);
          context.Stop();
          context.Failed = true;
          RemoveTranscodeContext(data.ClientId, data.TranscodeId, context);
          throw;
        }
      }

      if (waitForBuffer == false) return null;
      return GetTranscodedFileBuffer(data, context);
    }

    private void TranscodeProcessor(object args)
    {
      FFMpegTranscodeThreadData data = (FFMpegTranscodeThreadData)args;
      bool isStream = false;
      if (data.Context.Live == true && data.Context.Segmented == false)
      {
        isStream = true;
      }

      if (data.Context.Segmented == true)
      {
        FFMpegPlaylistManifest.CreatePlaylistFiles(data.TranscodeData);
      }

      bool isFile = true;
      bool isSlimTv = false;
      int liveChannelId = 0;
      bool runProcess = true;
      MediaItem liveItem;
      if (data.TranscodeData.InputResourceAccessor is ITranscodeLiveAccessor)
      {
        isSlimTv = true;
        liveChannelId = ((ITranscodeLiveAccessor)data.TranscodeData.InputResourceAccessor).ChannelId;
      }

      data.Context.CompleteEvent.Reset();
      data.Context.Start();
      data.Context.Failed = false;
      int exitCode = -1;

      IResourceAccessor mediaAccessor = data.TranscodeData.InputResourceAccessor;
      string identifier = "Transcode_" + data.TranscodeData.ClientId;
      if (isSlimTv)
      {
        if (_slimtTvHandler.StartTuning(identifier, liveChannelId, out liveItem) == false)
        {
          _logger.Error("FFMpegMediaConverter: Transcoder unable to start timeshifting for channel {0}", liveChannelId);
          runProcess = false;
          exitCode = 5000;
        }
        else
        {
          mediaAccessor = _slimtTvHandler.GetDefaultAccessor(liveChannelId);
          if (mediaAccessor is INetworkResourceAccessor)
          {
            data.TranscodeData.InputResourceAccessor = mediaAccessor;
          }
        }
      }
      if (data.TranscodeData.InputResourceAccessor is INetworkResourceAccessor)
      {
        isFile = false;
      }

      ProcessStartInfo startInfo = new ProcessStartInfo()
      {
        FileName = data.TranscodeData.TranscoderBinPath,
        WorkingDirectory = data.TranscodeData.WorkPath,
        Arguments = data.TranscodeData.TranscoderArguments,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        RedirectStandardInput = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8
      };

      _logger.Debug("FFMpegMediaConverter: Transcoder '{0}' invoked with command line arguments '{1}'", data.TranscodeData.TranscoderBinPath, data.TranscodeData.TranscoderArguments);
      //Task<ProcessExecutionResult> executionResult = ServiceRegistration.Get<IFFMpegLib>().FFMpegExecuteWithResourceAccessAsync((ILocalFsResourceAccessor)data.Data.InputResourceAccessor, data.Data.TranscoderArguments, ProcessPriorityClass.Normal, ProcessUtils.INFINITE);

      if (runProcess)
      {
        try
        {
          //TODO: Fix usages of obsolete and deprecated methods when alternative is available
          using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor((mediaAccessor).CanonicalLocalResourcePath))
          {
            using (ImpersonationProcess ffmpeg = new ImpersonationProcess { StartInfo = startInfo })
            {
              IntPtr userToken = IntPtr.Zero;
              if (isFile && !ImpersonationHelper.GetTokenByProcess(out userToken, true)) return;

              ffmpeg.EnableRaisingEvents = true; //Enable raising events because Process does not raise events by default.
              if (isStream == false)
              {
                ffmpeg.OutputDataReceived += data.Context.OutputDataReceived;
              }
              ffmpeg.ErrorDataReceived += data.Context.ErrorDataReceived;
              if (isFile) ffmpeg.StartAsUser(userToken);
              else ffmpeg.Start();
              ffmpeg.BeginErrorReadLine();
              if (isStream == false)
              {
                ffmpeg.BeginOutputReadLine();
              }
              else
              {
                data.TranscodeData.LiveStream = ffmpeg.StandardOutput.BaseStream;
              }
              if(isSlimTv && isFile)
              {
                _slimtTvHandler.AttachConverterStreamHook(identifier, ffmpeg.StandardInput.BaseStream);
              }

              while (ffmpeg.HasExited == false)
              {
                if (data.Context.Running == false)
                {
                  data.Context.Aborted = true;
                  if (isStream == false)
                  {
                    ffmpeg.CancelOutputRead();
                  }
                  ffmpeg.CancelErrorRead();
                  if (!(data.TranscodeData.InputResourceAccessor is TranscodeLiveAccessor))
                  {
                    ffmpeg.StandardInput.WriteLine("q"); //Soft exit
                    ffmpeg.StandardInput.Close();
                  }
                  if (ffmpeg.WaitForExit(2000) == false)
                  {
                    ffmpeg.Kill(); //Hard exit
                  }
                  break;
                }
                if (data.Context.Segmented == true)
                {
                  long lastSequence = 0;
                  if (Directory.Exists(data.Context.SegmentDir))
                  {
                    string[] segmentFiles = Directory.GetFiles(data.Context.SegmentDir, "*.ts");
                    foreach (string file in segmentFiles)
                    {
                      long sequenceNumber = GetSegmentSequence(file);
                      if (sequenceNumber > lastSequence) lastSequence = sequenceNumber;
                    }
                    data.Context.LastSegment = lastSequence;
                  }
                }
                Thread.Sleep(5);
              }
              ffmpeg.WaitForExit();
              exitCode = ffmpeg.ExitCode;
              //iExitCode = executionResult.Result.ExitCode;
              //if (data.TranscodeData.InputResourceAccessor is FFMpegLiveAccessor)
              //{
              //  ffmpeg.StandardInput.Close();
              //}
              ffmpeg.Close();
              if (isStream == true && data.TranscodeData.LiveStream != null)
              {
                data.TranscodeData.LiveStream.Dispose();
              }
              if (isFile) NativeMethods.CloseHandle(userToken);
            }
          }
        }
        catch (Exception ex)
        {
          if (isStream || data.TranscodeData.OutputFilePath == null)
          {
            _logger.Error("FFMpegMediaConverter: Transcoder command failed for stream '{0}'", ex, data.TranscodeData.TranscodeId);
          }
          else
          {
            _logger.Error("FFMpegMediaConverter: Transcoder command failed for file '{0}'", ex, data.TranscodeData.OutputFilePath);
          }
          data.Context.Failed = true;
        }
      }
      if (exitCode > 0)
      {
        data.Context.Failed = true;
      }
      data.Context.Stop();
      data.Context.CompleteEvent.Set();
      _ffMpegEncoderHandler.EndEncoding(data.TranscodeData.Encoder, data.TranscodeData.TranscodeId);

      if (isSlimTv)
      {
        if (_slimtTvHandler.EndTuning(identifier) == false)
        {
          _logger.Error("FFMpegMediaConverter: Transcoder unable to stop timeshifting for channel {0}", liveChannelId);
        }
      }

      string filePath = data.Context.TargetFile;
      bool isFolder = false;
      if (string.IsNullOrEmpty(data.Context.SegmentDir) == false)
      {
        filePath = data.Context.SegmentDir;
        isFolder = true;
      }
      if (exitCode > 0 || data.Context.Aborted == true)
      {
        if (exitCode > 0)
        {
          if (isStream || data.TranscodeData.OutputFilePath == null)
          {
            _logger.Debug("FFMpegMediaConverter: Transcoder command failed with error {1} for stream '{0}'", data.TranscodeData.TranscodeId, exitCode);
          }
          else
          {
            _logger.Debug("FFMpegMediaConverter: Transcoder command failed with error {1} for file '{0}'", data.TranscodeData.OutputFilePath, exitCode);
          }
        }
        if (data.Context.Aborted == true)
        {
          if (isStream || data.TranscodeData.OutputFilePath == null)
          {
            _logger.Debug("FFMpegMediaConverter: Transcoder command aborted for stream '{0}'", data.TranscodeData.TranscodeId);
          }
          else
          {
            _logger.Debug("FFMpegMediaConverter: Transcoder command aborted for file '{0}'", data.TranscodeData.OutputFilePath);
          }
        }
        else
        {
          _logger.Debug("FFMpegMediaConverter: FFMpeg error \n {0}", data.Context.ConsoleErrorOutput);
        }
        data.Context.DeleteFiles();
      }
      else
      {
        if (isFolder == false)
        {
          TouchFile(filePath);
        }
        else
        {
          TouchDirectory(filePath);
        }
      }
      RemoveTranscodeContext(data.TranscodeData.ClientId, data.TranscodeData.TranscodeId, data.Context);
    }

    #endregion
  }
}
