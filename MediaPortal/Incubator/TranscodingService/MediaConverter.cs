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
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Threading;
using System.Linq;
using System.Globalization;
using System.Drawing;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters;
using MediaPortal.Utilities.Process;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Encoders;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Plugins.Transcoding.Service.Metadata;
using MediaPortal.Plugins.Transcoding.Service.Metadata.Streams;
using MediaPortal.Plugins.Transcoding.Service.Objects;
using MediaPortal.Plugins.Transcoding.Service.Analyzers;

namespace MediaPortal.Plugins.Transcoding.Service
{
  public static class MediaConverter
  {
    public const int NO_SUBTITLE = -2;
    public const int AUTO_SUBTITLE = -1;

    public const string INPUT_FILE_TOKEN = "{input}";
    public const string OUTPUT_FILE_TOKEN = "{output}";
    public const string SUBTITLE_FILE_TOKEN = "{subtitle}";

    public const string PLAYLIST_FILE_NAME = "playlist.m3u8";
    public const string PLAYLIST_SUBTITLE_FILE_NAME = PLAYLIST_FILE_NAME + "_vtt.m3u8";
    public const string PLAYLIST_MANIFEST_FILE_NAME = "manifest.m3u8";
    public const string PLAYLIST_TEMP_FILE_NAME = "temp_playlist.m3u8";
    public const string PLAYLIST_TEMP_SUBTITLE_FILE_NAME = PLAYLIST_TEMP_FILE_NAME + "_vtt.m3u8";

    private const string HLS_SEGMENT_FILE_TEMPLATE = "%05d.ts";
    private const string HLS_SEGMENT_SUB_TEMPLATE = "sub.vtt";
    private const int HLS_PLAYLIST_TIMEOUT = 25000; //Can take long time to start for RTSP

    public static bool SupportHardcodedSubs
    {
      get
      {
        return _supportHardcodedSubs;
      }
    }

    public static bool SupportIntelHW
    {
      get
      {
        return _supportIntelHW;
      }
    }

    public static bool SupportNvidiaHW
    {
      get
      {
        return _supportNvidiaHW;
      }
    }

    public static int HLSSegmentTimeInSeconds
    {
      get
      {
        return _hlsSegmentTimeInSeconds;
      }
    }

    private static Dictionary<string, Dictionary<string, List<TranscodeContext>>> _runningClientTranscodes = new Dictionary<string, Dictionary<string, List<TranscodeContext>>>();
    private static FFMpegEncoderHandler _ffMpegEncoderHandler;
    private static FFMpegCommandline _ffMpegCommandline;
    private static string _cachePath;
    private static long _cacheMaximumSize;
    private static long _cacheMaximumAge;
    private static bool _cacheEnabled;
    private static string _transcoderBinPath;
    private static int _transcoderMaximumThreads;
    private static int _transcoderTimeout;
    private static int _hlsSegmentTimeInSeconds;
    private static string _subtitleDefaultEncoding;
    private static string _subtitleDefaultLanguage;
    private static ILogger _logger;
    private static bool _supportHardcodedSubs = true;
    private static bool _supportNvidiaHW = true;
    private static bool _supportIntelHW = true;

    static MediaConverter()
    {
      _logger = ServiceRegistration.Get<ILogger>();
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
      if (result.IndexOf("--enable-nvenc") == -1)
      {
        _logger.Warn("MediaConverter: FFMPEG is not compiled with nvenc support, Nvidia hardware acceleration will not work.");
        _supportNvidiaHW = false;
      }
      if (result.IndexOf("--enable-libmfx") == -1)
      {
        _logger.Warn("MediaConverter: FFMPEG is not compiled with libmfx support, Intel hardware acceleration will not work.");
        _supportIntelHW = false;
      }

      if (TranscodingServicePlugin.Settings.IntelHWAccelerationAllowed && _supportIntelHW)
      {
        if (RegisterHardwareEncoder(EncoderHandler.HardwareIntel, TranscodingServicePlugin.Settings.IntelHWMaximumStreams,
          new List<VideoCodec>(TranscodingServicePlugin.Settings.IntelHWSupportedCodecs)) == false)
        {
          _logger.Warn("MediaConverter: Failed to register Intel hardware acceleration");
        }
      }
      if (TranscodingServicePlugin.Settings.NvidiaHWAccelerationAllowed && _supportNvidiaHW)
      {
        if (RegisterHardwareEncoder(EncoderHandler.HardwareNvidia, TranscodingServicePlugin.Settings.NvidiaHWMaximumStreams,
          new List<VideoCodec>(TranscodingServicePlugin.Settings.NvidiaHWSupportedCodecs)) == false)
        {
          _logger.Warn("MediaConverter: Failed to register Nvidia hardware acceleration");
        }
      }

      _ffMpegEncoderHandler = new FFMpegEncoderHandler();
      LoadSettings();
    }

    public static void LoadSettings()
    {
      _cacheEnabled = TranscodingServicePlugin.Settings.CacheEnabled;
      _cachePath = TranscodingServicePlugin.Settings.CachePath;
      _cacheMaximumSize = TranscodingServicePlugin.Settings.CacheMaximumSizeInGB; //GB
      _cacheMaximumAge = TranscodingServicePlugin.Settings.CacheMaximumAgeInDays; //Days
      _transcoderMaximumThreads = TranscodingServicePlugin.Settings.TranscoderMaximumThreads;
      _transcoderTimeout = TranscodingServicePlugin.Settings.TranscoderTimeout;
      _hlsSegmentTimeInSeconds = TranscodingServicePlugin.Settings.HLSSegmentTimeInSeconds;
      _subtitleDefaultLanguage = TranscodingServicePlugin.Settings.SubtitleDefaultLanguage;
      _subtitleDefaultEncoding = TranscodingServicePlugin.Settings.SubtitleDefaultEncoding;

      if (TranscodingServicePlugin.Settings.IntelHWAccelerationAllowed && _supportIntelHW)
      {
        if (RegisterHardwareEncoder(EncoderHandler.HardwareIntel, TranscodingServicePlugin.Settings.IntelHWMaximumStreams,
          new List<VideoCodec>(TranscodingServicePlugin.Settings.IntelHWSupportedCodecs)) == false)
        {
          _logger.Warn("MediaConverter: Failed to register Intel hardware acceleration");
        }
      }
      else
      {
        UnregisterHardwareEncoder(EncoderHandler.HardwareIntel);
      }
      if (TranscodingServicePlugin.Settings.NvidiaHWAccelerationAllowed && _supportNvidiaHW)
      {
        if (RegisterHardwareEncoder(EncoderHandler.HardwareNvidia, TranscodingServicePlugin.Settings.NvidiaHWMaximumStreams,
          new List<VideoCodec>(TranscodingServicePlugin.Settings.NvidiaHWSupportedCodecs)) == false)
        {
          _logger.Warn("MediaConverter: Failed to register Nvidia hardware acceleration");
        }
      }
      else
      {
        UnregisterHardwareEncoder(EncoderHandler.HardwareNvidia);
      }

      _ffMpegCommandline = new FFMpegCommandline(_transcoderMaximumThreads, _transcoderTimeout, _cachePath, _hlsSegmentTimeInSeconds, HLS_SEGMENT_FILE_TEMPLATE, _supportHardcodedSubs);
    }

    #region HLS

    public static long GetSegmentSequence(string fileName)
    {
      long sequenceNumber = -1;
      long.TryParse(Path.GetFileNameWithoutExtension(fileName), out sequenceNumber);
      return sequenceNumber;
    }

    public static bool GetSegmentFile(VideoTranscoding video, TranscodeContext context, string fileName, out Stream fileData, out object containerEnum)
    {
      fileData = null;
      containerEnum = null;
      string completePath = Path.Combine(context.SegmentDir, fileName);
      string playlistPath = null;
      if (video.TargetIsLive == false)
      {
        playlistPath = Path.Combine(context.SegmentDir, MediaConverter.PLAYLIST_TEMP_FILE_NAME);
      }
      else
      {
        playlistPath = Path.Combine(context.SegmentDir, MediaConverter.PLAYLIST_FILE_NAME);
      }
      DateTime waitStart = DateTime.Now;

      //Thread.Sleep(2000); //Ensure that writing is completed. Is there a better way?
      if (Path.GetExtension(MediaConverter.PLAYLIST_FILE_NAME) == Path.GetExtension(fileName)) //playlist file
      {
        while (File.Exists(completePath) == false)
        {
          if (context.Running == false) return false;
          if ((DateTime.Now - waitStart).TotalMilliseconds > HLS_PLAYLIST_TIMEOUT) return false;
          Thread.Sleep(100);
        }
        containerEnum = VideoContainer.Hls;
        MemoryStream memStream = new MemoryStream(PlaylistManifest.CorrectPlaylistUrls(video.HlsBaseUrl, completePath));
        memStream.Position = 0;
        fileData = memStream;
      }
      if (Path.GetExtension(HLS_SEGMENT_FILE_TEMPLATE) == Path.GetExtension(fileName)) //segment file
      {
        long sequenceNumber = GetSegmentSequence(fileName);
        while (true)
        {
          if (File.Exists(completePath) == false)
          {
            if (context.CurrentSegment > sequenceNumber)
            {
              // Probably rewinding
              return false;
            }
            if ((sequenceNumber - context.CurrentSegment) > 2)
            {
              //Probably forwarding
              return false;
            }
          }
          else
          {
            //If playlist generated by ffmpeg contains the file it must be done
            if (File.Exists(playlistPath) == true)
            {
              string playlistContents = File.ReadAllText(playlistPath, Encoding.UTF8);
              if (playlistContents.Contains(fileName)) break;
            }
          }
          if (context.Running == false) return false;
          if ((DateTime.Now - waitStart).TotalSeconds > _hlsSegmentTimeInSeconds) return false;
          Thread.Sleep(100);
        }
        containerEnum = VideoContainer.Mpeg2Ts;
        if (sequenceNumber >= 0)
        {
          context.CurrentSegment = sequenceNumber;
        }
        fileData = GetReadyFileBuffer(completePath);
      }
      if (Path.GetExtension(HLS_SEGMENT_SUB_TEMPLATE) == Path.GetExtension(fileName)) //subtitle file
      {
        while (File.Exists(completePath) == false)
        {
          if (context.Running == false) return false;
          if ((DateTime.Now - waitStart).TotalSeconds > _hlsSegmentTimeInSeconds) return false;
          Thread.Sleep(100);
        }
        containerEnum = SubtitleCodec.WebVtt;
        fileData = GetReadyFileBuffer(completePath);
      }
      if (fileData != null) return true;
      return false;
    }

    #endregion

    #region MIME

    public static string GetSubtitleMime(SubtitleCodec codec)
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
        case SubtitleCodec.WebVtt:
          return "text/vtt";
      }
      return "text/plain";
    }

    #endregion

    #region Metadata

    public static TranscodedAudioMetadata GetTranscodedAudioMetadata(AudioTranscoding audio)
    {
      TranscodedAudioMetadata metadata = new TranscodedAudioMetadata
      {
        TargetAudioBitrate = audio.TargetAudioBitrate,
        TargetAudioCodec = audio.TargetAudioCodec,
        TargetAudioContainer = audio.TargetAudioContainer,
        TargetAudioFrequency = audio.TargetAudioFrequency
      };
      if (audio.TargetAudioContainer == AudioContainer.Unknown)
      {
        metadata.TargetAudioContainer = audio.SourceAudioContainer;
      }
      if (Checks.IsAudioStreamChanged(audio))
      {
        if (audio.TargetAudioCodec == AudioCodec.Unknown)
        {
          switch (audio.TargetAudioContainer)
          {
            case AudioContainer.Unknown:
              break;
            case AudioContainer.Ac3:
              metadata.TargetAudioCodec = AudioCodec.Ac3;
              break;
            case AudioContainer.Adts:
              metadata.TargetAudioCodec = AudioCodec.Aac;
              break;
            case AudioContainer.Asf:
              metadata.TargetAudioCodec = AudioCodec.Wma;
              break;
            case AudioContainer.Flac:
              metadata.TargetAudioCodec = AudioCodec.Flac;
              break;
            case AudioContainer.Lpcm:
              metadata.TargetAudioCodec = AudioCodec.Lpcm;
              break;
            case AudioContainer.Mp4:
              metadata.TargetAudioCodec = AudioCodec.Aac;
              break;
            case AudioContainer.Mp3:
              metadata.TargetAudioCodec = AudioCodec.Mp3;
              break;
            case AudioContainer.Mp2:
              metadata.TargetAudioCodec = AudioCodec.Mp2;
              break;
            case AudioContainer.Ogg:
              metadata.TargetAudioCodec = AudioCodec.Vorbis;
              break;
            case AudioContainer.Rtp:
              metadata.TargetAudioCodec = AudioCodec.Lpcm;
              break;
            case AudioContainer.Rtsp:
              metadata.TargetAudioCodec = AudioCodec.Lpcm;
              break;
            default:
              metadata.TargetAudioCodec = audio.SourceAudioCodec;
              break;
          }
        }
        long frequency = Validators.GetAudioFrequency(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioFrequency, audio.TargetAudioFrequency);
        if (frequency > 0)
        {
          metadata.TargetAudioFrequency = frequency;
        }
        if (audio.TargetAudioContainer != AudioContainer.Lpcm)
        {
          metadata.TargetAudioBitrate = Validators.GetAudioBitrate(audio.SourceAudioBitrate, audio.TargetAudioBitrate);
        }
      }
      metadata.TargetAudioChannels = Validators.GetAudioNumberOfChannels(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioChannels, audio.TargetForceAudioStereo);
      return metadata;
    }

    public static TranscodedImageMetadata GetTranscodedImageMetadata(ImageTranscoding image)
    {
      TranscodedImageMetadata metadata = new TranscodedImageMetadata
      {
        TargetMaxHeight = image.SourceHeight,
        TargetMaxWidth = image.SourceWidth,
        TargetOrientation = image.SourceOrientation,
        TargetImageCodec = image.TargetImageCodec
      };
      if (metadata.TargetImageCodec == ImageContainer.Unknown)
      {
        metadata.TargetImageCodec = image.SourceImageCodec;
      }
      metadata.TargetPixelFormat = image.TargetPixelFormat;
      if (metadata.TargetPixelFormat == PixelFormat.Unknown)
      {
        metadata.TargetPixelFormat = image.SourcePixelFormat;
      }
      if (Checks.IsImageStreamChanged(image) == true)
      {
        metadata.TargetMaxHeight = image.SourceHeight;
        metadata.TargetMaxWidth = image.SourceWidth;
        if (metadata.TargetMaxHeight > image.TargetHeight && image.TargetHeight > 0)
        {
          double scale = (double)image.SourceWidth / (double)image.SourceHeight;
          metadata.TargetMaxHeight = image.TargetHeight;
          metadata.TargetMaxWidth = Convert.ToInt32(scale * (double)metadata.TargetMaxHeight);
        }
        if (metadata.TargetMaxWidth > image.TargetWidth && image.TargetWidth > 0)
        {
          double scale = (double)image.SourceHeight / (double)image.SourceWidth;
          metadata.TargetMaxWidth = image.TargetWidth;
          metadata.TargetMaxHeight = Convert.ToInt32(scale * (double)metadata.TargetMaxWidth);
        }

        if (image.TargetAutoRotate == true)
        {
          if (image.SourceOrientation > 4)
          {
            int iTemp = metadata.TargetMaxWidth;
            metadata.TargetMaxWidth = metadata.TargetMaxHeight;
            metadata.TargetMaxHeight = iTemp;
          }
          metadata.TargetOrientation = 0;
        }
      }
      return metadata;
    }

    public static TranscodedVideoMetadata GetTranscodedVideoMetadata(VideoTranscoding video)
    {
      TranscodedVideoMetadata metadata = new TranscodedVideoMetadata
      {
        TargetAudioBitrate = video.TargetAudioBitrate,
        TargetAudioCodec = video.TargetAudioCodec,
        TargetAudioFrequency = video.TargetAudioFrequency,
        TargetVideoFrameRate = video.SourceFrameRate,
        TargetLevel = video.TargetLevel,
        TargetPreset = video.TargetPreset,
        TargetProfile = video.TargetProfile,
        TargetVideoPixelFormat = video.TargetPixelFormat
      };
      if (metadata.TargetVideoPixelFormat == PixelFormat.Unknown)
      {
        metadata.TargetVideoPixelFormat = PixelFormat.Yuv420;
      }
      metadata.TargetVideoAspectRatio = video.TargetVideoAspectRatio;
      if (metadata.TargetVideoAspectRatio <= 0)
      {
        metadata.TargetVideoAspectRatio = 16.0F / 9.0F;
      }
      metadata.TargetVideoBitrate = video.TargetVideoBitrate;
      metadata.TargetVideoCodec = video.TargetVideoCodec;
      if (metadata.TargetVideoCodec == VideoCodec.Unknown)
      {
        metadata.TargetVideoCodec = video.SourceVideoCodec;
      }
      metadata.TargetVideoContainer = video.TargetVideoContainer;
      if (metadata.TargetVideoContainer == VideoContainer.Unknown)
      {
        metadata.TargetVideoContainer = video.SourceVideoContainer;
      }
      metadata.TargetVideoTimestamp = Timestamp.None;
      if (metadata.TargetVideoContainer == VideoContainer.M2Ts)
      {
        metadata.TargetVideoTimestamp = Timestamp.Valid;
      }

      metadata.TargetVideoMaxWidth = video.SourceVideoWidth;
      metadata.TargetVideoMaxHeight = video.SourceVideoHeight;
      if (metadata.TargetVideoMaxHeight <= 0)
      {
        metadata.TargetVideoMaxHeight = 1080;
      }
      float newPixelAspectRatio = video.SourceVideoPixelAspectRatio;
      if (newPixelAspectRatio <= 0)
      {
        newPixelAspectRatio = 1.0F;
      }

      Size newSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      Size newContentSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      bool pixelARChanged = false;
      bool videoARChanged = false;
      bool videoHeightChanged = false;
      _ffMpegCommandline.GetVideoDimensions(video, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);
      metadata.TargetVideoPixelAspectRatio = newPixelAspectRatio;
      metadata.TargetVideoMaxWidth = newSize.Width;
      metadata.TargetVideoMaxHeight = newSize.Height;

      metadata.TargetVideoFrameRate = video.SourceFrameRate;
      if (metadata.TargetVideoFrameRate > 23.9 && metadata.TargetVideoFrameRate < 23.99)
        metadata.TargetVideoFrameRate = 23.976F;
      else if (metadata.TargetVideoFrameRate >= 23.99 && metadata.TargetVideoFrameRate < 24.1)
        metadata.TargetVideoFrameRate = 24;
      else if (metadata.TargetVideoFrameRate >= 24.99 && metadata.TargetVideoFrameRate < 25.1)
        metadata.TargetVideoFrameRate = 25;
      else if (metadata.TargetVideoFrameRate >= 29.9 && metadata.TargetVideoFrameRate < 29.99)
        metadata.TargetVideoFrameRate = 29.97F;
      else if (metadata.TargetVideoFrameRate >= 29.99 && metadata.TargetVideoFrameRate < 30.1)
        metadata.TargetVideoFrameRate = 30;
      else if (metadata.TargetVideoFrameRate >= 49.9 && metadata.TargetVideoFrameRate < 50.1)
        metadata.TargetVideoFrameRate = 50;
      else if (metadata.TargetVideoFrameRate >= 59.9 && metadata.TargetVideoFrameRate < 59.99)
        metadata.TargetVideoFrameRate = 59.94F;
      else if (metadata.TargetVideoFrameRate >= 59.99 && metadata.TargetVideoFrameRate < 60.1)
        metadata.TargetVideoFrameRate = 60;

      metadata.TargetAudioChannels = Validators.GetAudioNumberOfChannels(video.SourceAudioCodec, video.TargetAudioCodec, video.SourceAudioChannels, video.TargetForceAudioStereo);
      long frequency = Validators.GetAudioFrequency(video.SourceAudioCodec, video.TargetAudioCodec, video.SourceAudioFrequency, video.TargetAudioFrequency);
      if (frequency != -1)
      {
        metadata.TargetAudioFrequency = frequency;
      }
      if (video.TargetAudioCodec != AudioCodec.Lpcm)
      {
        metadata.TargetAudioBitrate = Validators.GetAudioBitrate(video.SourceAudioBitrate, video.TargetAudioBitrate);
      }
      return metadata;
    }

    #endregion

    #region HW Acelleration

    private static bool RegisterHardwareEncoder(EncoderHandler encoder, int maximumStreams, List<VideoCodec> supportedCodecs)
    {
      if (encoder == EncoderHandler.Software)
        return false;
      else if (encoder == EncoderHandler.HardwareIntel && _supportIntelHW == false)
        return false;
      else if (encoder == EncoderHandler.HardwareNvidia && _supportNvidiaHW == false)
        return false;
      _ffMpegEncoderHandler.RegisterEncoder(encoder, maximumStreams, supportedCodecs);
      return true;
    }

    private static void UnregisterHardwareEncoder(EncoderHandler encoder)
    {
      _ffMpegEncoderHandler.UnregisterEncoder(encoder);
    }

    #endregion

    #region Cache

    private static void TouchFile(string filePath)
    {
      if (File.Exists(filePath))
      {
        try
        {
          File.SetCreationTime(filePath, DateTime.Now);
        }
        catch { }
      }
    }

    private static void TouchDirectory(string folderPath)
    {
      if (Directory.Exists(folderPath))
      {
        try
        {
          Directory.SetCreationTime(folderPath, DateTime.Now);
        }
        catch { }
      }
    }

    public static bool IsTranscodeRunning(string clientId, string transcodeId)
    {
      lock (_runningClientTranscodes)
      {
        return _runningClientTranscodes.ContainsKey(clientId) && _runningClientTranscodes[clientId].ContainsKey(transcodeId);
      }
    }

    public static void StopTranscode(string clientId, string transcodeId)
    {
      lock (_runningClientTranscodes)
      {
        if (_runningClientTranscodes.ContainsKey(clientId) && _runningClientTranscodes[clientId].ContainsKey(transcodeId))
        {
          foreach (TranscodeContext context in _runningClientTranscodes[clientId][transcodeId])
          {
            try
            {
              context.Dispose();
            }
            catch
            {
              if (context.Live) _logger.Debug("MediaConverter: Error disposing transcode context for live stream");
              else _logger.Debug("MediaConverter: Error disposing transcode context for file '{0}'", context.TargetFile);
            }
          }
        }
      }
    }

    public static void StopAllTranscodes()
    {
      lock (_runningClientTranscodes)
      {
        foreach (string clientId in _runningClientTranscodes.Keys)
        {
          foreach (string transcodeId in _runningClientTranscodes[clientId].Keys)
          {
            foreach (TranscodeContext context in _runningClientTranscodes[clientId][transcodeId])
            {
              try
              {
                context.Dispose();
              }
              catch
              {
                if (context.Live) _logger.Debug("MediaConverter: Error disposing transcode context for live stream");
                else _logger.Debug("MediaConverter: Error disposing transcode context for file '{0}'", context.TargetFile);
              }
            }
          }
        }
      }
    }

    public static void CleanUpTranscodeCache()
    {
      lock (_cachePath)
      {
        if (Directory.Exists(_cachePath) == true)
        {
          int maxTries = 10;
          SortedDictionary<DateTime, string> fileList = new SortedDictionary<DateTime, string>();
          long cacheSize = 0;
          List<string> dirObjects = new List<string>(Directory.GetFiles(_cachePath, "*.mp*"));
          dirObjects.AddRange(Directory.GetDirectories(_cachePath, "*" + PlaylistManifest.PLAYLIST_FOLDER_SUFFIX));
          foreach (string dirObject in dirObjects)
          {
            string[] tokens = dirObject.Split('.');
            if (tokens.Length >= 3)
            {
              if (Directory.Exists(dirObject) == true)
              {
                DirectoryInfo info;
                try
                {
                  info = new DirectoryInfo(dirObject);
                }
                catch
                {
                  continue;
                }
                FileInfo[] folderFiles = info.GetFiles();
                foreach (FileInfo folderFile in folderFiles)
                {
                  if (folderFile.Length == 0)
                  {
                    try
                    {
                      folderFile.Delete();
                    }
                    catch
                    {
                    }
                    continue;
                  }
                  cacheSize += folderFile.Length;
                }
                if (fileList.ContainsKey(info.CreationTime) == false)
                {
                  fileList.Add(info.CreationTime, dirObject);
                }
                else
                {
                  DateTime fileTime = info.CreationTime.AddMilliseconds(1);
                  while (fileList.ContainsKey(fileTime) == true)
                  {
                    fileTime = fileTime.AddMilliseconds(1);
                  }
                  fileList.Add(fileTime, dirObject);
                }
              }
              else
              {
                FileInfo info;
                try
                {
                  info = new FileInfo(dirObject);
                }
                catch
                {
                  continue;
                }
                if (info.Length == 0)
                {
                  try
                  {
                    File.Delete(dirObject);
                  }
                  catch
                  {
                  }
                  continue;
                }
                cacheSize += info.Length;
                if (fileList.ContainsKey(info.CreationTime) == false)
                {
                  fileList.Add(info.CreationTime, dirObject);
                }
                else
                {
                  DateTime fileTime = info.CreationTime.AddMilliseconds(1);
                  while (fileList.ContainsKey(fileTime) == true)
                  {
                    fileTime = fileTime.AddMilliseconds(1);
                  }
                  fileList.Add(fileTime, dirObject);
                }
              }
            }
          }

          bool bDeleting = true;
          int tryCount = 0;
          while (fileList.Count > 0 && bDeleting && _cacheMaximumAge > 0 && tryCount < maxTries)
          {
            tryCount++;
            bDeleting = false;
            KeyValuePair<DateTime, string> dirObject = fileList.First();
            if ((DateTime.Now - dirObject.Key).TotalDays > _cacheMaximumAge)
            {
              bDeleting = true;
              fileList.Remove(dirObject.Key);
              if (Directory.Exists(dirObject.Value) == true)
              {
                try
                {
                  Directory.Delete(dirObject.Value, true);
                }
                catch { }
              }
              else
              {
                try
                {
                  File.Delete(dirObject.Value);
                }
                catch { }
              }
            }
          }

          tryCount = 0;
          while (fileList.Count > 0 && cacheSize > (_cacheMaximumSize * 1024 * 1024 * 1024) && _cacheMaximumSize > 0 && tryCount < maxTries)
          {
            tryCount++;
            KeyValuePair<DateTime, string> dirObject = fileList.First();
            if (Directory.Exists(dirObject.Value) == true)
            {
              DirectoryInfo info;
              try
              {
                info = new DirectoryInfo(dirObject.Value);
              }
              catch
              {
                fileList.Remove(dirObject.Key);
                continue;
              }
              FileInfo[] folderFiles = info.GetFiles();
              cacheSize = folderFiles.Aggregate(cacheSize, (current, folderFile) => current - folderFile.Length);
              fileList.Remove(dirObject.Key);
              try
              {
                info.Delete(true);
              }
              catch { }
            }
            else
            {
              FileInfo info;
              try
              {
                info = new FileInfo(dirObject.Value);
              }
              catch
              {
                fileList.Remove(dirObject.Key);
                continue;
              }
              cacheSize -= info.Length;
              fileList.Remove(dirObject.Key);
              try
              {
                info.Delete();
              }
              catch { }
            }
          }
        }
      }
    }

    #endregion

    #region Subtitles

    private static SubtitleStream FindSubtitle(VideoTranscoding video)
    {
      if (video.SourceSubtitleStreamIndex == NO_SUBTITLE) return null;
      List<SubtitleStream> allSubs = GetSubtitleStreams(video);
      if (video.SourceSubtitleStreamIndex >= 0 && allSubs.Count > video.SourceSubtitleStreamIndex)
      {
        return allSubs[video.SourceSubtitleStreamIndex];
      }

      SubtitleStream currentEmbeddedSub = null;
      SubtitleStream currentExternalSub = null;

      SubtitleStream defaultEmbeddedSub = null;
      SubtitleStream englishEmbeddedSub = null;
      List<SubtitleStream> subsEmbedded = new List<SubtitleStream>();
      List<SubtitleStream> langSubsEmbedded = new List<SubtitleStream>();

      foreach (SubtitleStream sub in allSubs)
      {
        if (sub.IsEmbedded == false)
        {
          continue;
        }
        if (sub.Default == true)
        {
          defaultEmbeddedSub = sub;
        }
        else if (string.Compare(sub.Language, "EN", true, CultureInfo.InvariantCulture) == 0)
        {
          englishEmbeddedSub = sub;
        }
        if (string.IsNullOrEmpty(video.TargetSubtitleLanguages) == false)
        {
          string[] langs = video.TargetSubtitleLanguages.Split(',');
          foreach (string lang in langs)
          {
            if (string.IsNullOrEmpty(lang) == false && string.Compare(sub.Language, lang, true, CultureInfo.InvariantCulture) == 0)
            {
              langSubsEmbedded.Add(sub);
            }
          }
        }
        else
        {
          subsEmbedded.Add(sub);
        }
      }
      if (currentEmbeddedSub == null && langSubsEmbedded.Count > 0)
      {
        currentEmbeddedSub = langSubsEmbedded[0];
      }

      SubtitleStream defaultSub = null;
      SubtitleStream englishSub = null;
      List<SubtitleStream> subs = new List<SubtitleStream>();
      List<SubtitleStream> langSubs = new List<SubtitleStream>();
      foreach (SubtitleStream sub in allSubs)
      {
        if (sub.IsEmbedded == true)
        {
          continue;
        }
        if (sub.Default == true)
        {
          defaultSub = sub;
        }
        else if (string.Compare(sub.Language, "EN", true, CultureInfo.InvariantCulture) == 0)
        {
          englishSub = sub;
        }
        if (string.IsNullOrEmpty(video.TargetSubtitleLanguages) == false)
        {
          string[] langs = video.TargetSubtitleLanguages.Split(',');
          foreach (string lang in langs)
          {
            if (string.IsNullOrEmpty(lang) == false && string.Compare(sub.Language, lang, true, CultureInfo.InvariantCulture) == 0)
            {
              langSubs.Add(sub);
            }
          }
        }
        else
        {
          subs.Add(sub);
        }
      }
      if (currentExternalSub == null && langSubs.Count > 0)
      {
        currentExternalSub = langSubs[0];
      }

      //Best language subtitle
      if (currentExternalSub != null)
      {
        return currentExternalSub;
      }
      if (currentEmbeddedSub != null)
      {
        return currentEmbeddedSub;
      }

      //Best default subtitle
      if (currentExternalSub == null && defaultSub != null)
      {
        currentExternalSub = defaultSub;
      }
      if (currentEmbeddedSub == null && defaultEmbeddedSub != null)
      {
        currentEmbeddedSub = defaultEmbeddedSub;
      }
      if (currentExternalSub != null)
      {
        return currentExternalSub;
      }
      if (currentEmbeddedSub != null)
      {
        return currentEmbeddedSub;
      }

      //Best english
      if (currentExternalSub == null && englishSub != null)
      {
        currentExternalSub = englishSub;
      }
      if (currentEmbeddedSub == null && englishEmbeddedSub != null)
      {
        currentEmbeddedSub = englishEmbeddedSub;
      }
      if (currentExternalSub != null)
      {
        return currentExternalSub;
      }
      if (currentEmbeddedSub != null)
      {
        return currentEmbeddedSub;
      }

      //Best remaining subtitle
      if (currentExternalSub == null && subs.Count > 0)
      {
        currentExternalSub = subs[0];
      }
      if (currentEmbeddedSub == null && subsEmbedded.Count > 0)
      {
        currentEmbeddedSub = subsEmbedded[0];
      }
      if (currentExternalSub != null)
      {
        return currentExternalSub;
      }
      if (currentEmbeddedSub != null)
      {
        return currentEmbeddedSub;
      }
      return null;
    }

    public static List<SubtitleStream> FindExternalSubtitles(ILocalFsResourceAccessor lfsra)
    {
      List<SubtitleStream> externalSubtitles = new List<SubtitleStream>();
      if (lfsra.Exists)
      {
        // Impersonation
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
        {
          string[] files = Directory.GetFiles(Path.GetDirectoryName(lfsra.LocalFileSystemPath), Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath) + "*.*");
          foreach (string file in files)
          {
            SubtitleStream sub = new SubtitleStream();
            sub.StreamIndex = -1;
            sub.Codec = SubtitleCodec.Unknown;
            if (string.Compare(Path.GetExtension(file), ".srt", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.Srt;
            }
            else if (string.Compare(Path.GetExtension(file), ".smi", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.Smi;
            }
            else if (string.Compare(Path.GetExtension(file), ".ass", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.Ass;
            }
            else if (string.Compare(Path.GetExtension(file), ".ssa", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.Ssa;
            }
            else if (string.Compare(Path.GetExtension(file), ".sub", true, CultureInfo.InvariantCulture) == 0)
            {
              if (File.Exists(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".idx")) == true)
              {
                sub.Codec = SubtitleCodec.VobSub;
              }
              else
              {
                string subContent = File.ReadAllText(file);
                if (subContent.Contains("[INFORMATION]")) sub.Codec = SubtitleCodec.SubView;
                else if (subContent.Contains("}{")) sub.Codec = SubtitleCodec.MicroDvd;
              }
            }
            else if (string.Compare(Path.GetExtension(file), ".vtt", true, CultureInfo.InvariantCulture) == 0)
            {
              sub.Codec = SubtitleCodec.WebVtt;
            }
            if (sub.Codec != SubtitleCodec.Unknown)
            {
              sub.Source = file;
              if (SubtitleAnalyzer.IsImageBasedSubtitle(sub.Codec) == false)
              {
                sub.Language = SubtitleAnalyzer.GetLanguage(lfsra, file, _subtitleDefaultEncoding, _subtitleDefaultLanguage);
              }
              externalSubtitles.Add(sub);
            }
          }
        }
      }
      return externalSubtitles;
    }

    public static BufferedStream GetSubtitleStream(string clientId, VideoTranscoding video)
    {
      Subtitle sub = GetSubtitle(clientId, video, 0);
      if (sub == null || sub.SourceFile == null)
      {
        return null;
      }
      if (IsTranscodeRunning(clientId, video.TranscodeId) == false)
      {
        TouchFile(sub.SourceFile);
      }
      return GetReadyFileBuffer(sub.SourceFile);
    }

    private static bool SubtitleIsUnicode(string encoding)
    {
      if (string.IsNullOrEmpty(encoding))
      {
        return false;
      }
      if (encoding.ToUpperInvariant().StartsWith("UTF-") || encoding.ToUpperInvariant().StartsWith("UNICODE"))
      {
        return true;
      }
      return false;
    }

    private static Subtitle GetSubtitle(string clientId, VideoTranscoding video, double timeStart)
    {
      SubtitleStream sourceSubtitle = FindSubtitle(video);
      if (sourceSubtitle == null) return null;
      if (video.TargetSubtitleSupport == SubtitleSupport.None) return null;

      Subtitle res = new Subtitle
      {
        Codec = sourceSubtitle.Codec,
        Language = sourceSubtitle.Language,
        SourceFile = sourceSubtitle.Source
      };
      if (SubtitleAnalyzer.IsImageBasedSubtitle(res.Codec) == false)
      {
        res.CharacterEncoding = SubtitleAnalyzer.GetEncoding((ILocalFsResourceAccessor)video.SourceMedia, sourceSubtitle.Source, sourceSubtitle.Language, _subtitleDefaultEncoding);
      }

      // SourceSubtitle == TargetSubtitleCodec -> just return
      if (video.TargetSubtitleCodec != SubtitleCodec.Unknown && video.TargetSubtitleCodec == sourceSubtitle.Codec && timeStart == 0)
      {
        return res;
      }

      // create a file name for the output file which contains the subtitle informations
      string transcodingFile = video.TranscodeId;
      if (sourceSubtitle != null && string.IsNullOrEmpty(sourceSubtitle.Language) == false)
      {
        transcodingFile += "." + sourceSubtitle.Language;
      }
      if (timeStart > 0)
      {
        transcodingFile = DateTime.Now.Ticks.ToString();
      }
      transcodingFile += ".mpts";
      transcodingFile = Path.Combine(_cachePath, transcodingFile);

      SubtitleCodec targetCodec = video.TargetSubtitleCodec;
      if (targetCodec == SubtitleCodec.Unknown)
      {
        targetCodec = sourceSubtitle.Codec;
      }

      // the file already exists in the cache -> just return
      if (File.Exists(transcodingFile))
      {
        if (IsTranscodeRunning(clientId, video.TranscodeId) == false)
        {
          TouchFile(transcodingFile);
        }
        res.Codec = targetCodec;
        res.SourceFile = transcodingFile;
        if (SubtitleIsUnicode(res.CharacterEncoding) == false)
        {
          res.CharacterEncoding = "UTF-8";
        }
        return res;
      }

      // subtitle is embedded in the source file
      if (sourceSubtitle.IsEmbedded)
      {
        _ffMpegCommandline.ExtractSubtitleFile(video, sourceSubtitle, res.CharacterEncoding, transcodingFile, timeStart);
        if (File.Exists(transcodingFile))
        {
          res.Codec = targetCodec;
          res.CharacterEncoding = "UTF-8";
          res.SourceFile = transcodingFile;
          return res;
        }
        return null;
      }

      // Burn external subtitle into video
      if (res.SourceFile == null)
      {
        return null;
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
        IResourceAccessor resourceAccessor = new LocalFsResourceAccessor(localFsResourceProvider, res.SourceFile);
        data.InputResourceAccessor = resourceAccessor;
      }
      else
      {
        if (SubtitleIsUnicode(res.CharacterEncoding) == false)
        {
          if (string.IsNullOrEmpty(res.CharacterEncoding) == false)
          {
            tempFile = transcodingFile + ".tmp";
            File.WriteAllText(tempFile, File.ReadAllText(res.SourceFile, Encoding.GetEncoding(res.CharacterEncoding)), Encoding.UTF8);
            res.CharacterEncoding = "UTF-8";
            res.SourceFile = tempFile;
            _logger.Debug("MediaConverter: Converted subtitle file '{0}' to UTF-8 for transcode '{1}'", sourceSubtitle.Source, data.TranscodeId);
          }
        }

        // TODO: not sure if this is working
        LocalFsResourceProvider localFsResourceProvider = new LocalFsResourceProvider();
        IResourceAccessor resourceAccessor = new LocalFsResourceAccessor(localFsResourceProvider, res.SourceFile);
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

      _logger.Debug("MediaConverter: Invoking transcoder to transcode subtitle file '{0}' for transcode '{1}'", res.SourceFile, data.TranscodeId);
      bool success = FFMpegBinary.FFMpegExecuteWithResourceAccessAsync((ILocalFsResourceAccessor)data.InputResourceAccessor, data.TranscoderArguments, ProcessPriorityClass.Normal, _transcoderTimeout).Result.Success;
      if (success && File.Exists(transcodingFile) == true)
      {
        if (tempFile != null) File.Delete(tempFile);
        res.SourceFile = transcodingFile;
        return res;
      }
      return null;
    }

    private static bool IsExternalSubtitleAvailable(ILocalFsResourceAccessor lfsra)
    {
      if (lfsra.Exists)
      {
        // Impersonation
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
        {
          string[] files = Directory.GetFiles(Path.GetDirectoryName(lfsra.LocalFileSystemPath), Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath) + "*.*");
          foreach (string file in files)
          {
            if (string.Compare(Path.GetExtension(file), ".srt", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".smi", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".ass", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".ssa", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".sub", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
            else if (string.Compare(Path.GetExtension(file), ".vtt", true, CultureInfo.InvariantCulture) == 0)
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    public static bool IsSubtitleAvailable(VideoTranscoding video)
    {
      if (video.SourceSubtitles != null && video.SourceSubtitles.Count > 0) return true;
      if (video.SourceMedia is ILocalFsResourceAccessor)
      {
        if (IsExternalSubtitleAvailable((ILocalFsResourceAccessor)video.SourceMedia)) return true;
      }
      return false;
    }

    public static List<SubtitleStream> GetSubtitleStreams(VideoTranscoding video)
    {
      List<SubtitleStream> allSubs = new List<SubtitleStream>();
      if (video.SourceSubtitles != null && video.SourceSubtitles.Count > 0)
      {
        //Only add embedded subtitles
        allSubs.AddRange(video.SourceSubtitles.Where(sub => sub.IsEmbedded == true));
      }

      //Refresh external subtitles
      if (video.SourceMedia is ILocalFsResourceAccessor)
      {
        ILocalFsResourceAccessor lfsra = (ILocalFsResourceAccessor)video.SourceMedia;
        allSubs.AddRange(FindExternalSubtitles(lfsra));
      }
      return allSubs;
    }

    #endregion

    #region Transcoding

    private static bool AssignExistingTranscodeContext(string clientId, string transcodeId, ref TranscodeContext context)
    {
      lock (_runningClientTranscodes)
      {
        if (_runningClientTranscodes.ContainsKey(clientId))
        {
          if (_runningClientTranscodes[clientId].ContainsKey(transcodeId))
          {
            List<TranscodeContext> runningContexts = _runningClientTranscodes[clientId][transcodeId];
            if (runningContexts != null)
            {
              //Non partial have first priority
              for (int contextNo = 0; contextNo < runningContexts.Count; contextNo++)
              {
                if (runningContexts[contextNo].Partial == false)
                {
                  context = runningContexts[contextNo];
                  return true;
                }
              }
            }
          }
        }
      }
      return false;
    }

    public static TranscodeContext GetMediaStream(string clientId, BaseTranscoding transcodingInfo, double timeStart, double timeDuration, bool waitForBuffer)
    {
      if (transcodingInfo.SourceMedia is ILocalFsResourceAccessor)
      {
        if (((ILocalFsResourceAccessor)transcodingInfo.SourceMedia).Exists == false)
        {
          _logger.Error("MediaConverter: File '{0}' does not exist for transcode '{1}'", transcodingInfo.SourceMedia, transcodingInfo.TranscodeId);
          return null;
        }
      }
      if (transcodingInfo is ImageTranscoding)
      {
        return TranscodeImage(clientId, transcodingInfo as ImageTranscoding, waitForBuffer);
      }
      else if (transcodingInfo is AudioTranscoding)
      {
        return TranscodeAudio(clientId, transcodingInfo as AudioTranscoding, timeStart, timeDuration, waitForBuffer);
      }
      else if (transcodingInfo is VideoTranscoding)
      {
        return TranscodeVideo(clientId, transcodingInfo as VideoTranscoding, timeStart, timeDuration, waitForBuffer);
      }
      _logger.Error("MediaConverter: Transcoding info is not valid for transcode '{0}'", transcodingInfo.TranscodeId);
      return null;
    }

    private static TranscodeContext TranscodeVideo(string clientId, VideoTranscoding video, double timeStart, double timeDuration, bool waitForBuffer)
    {
      TranscodeContext context = new TranscodeContext(_cacheEnabled, _cachePath) { Failed = false };
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
      video.TargetSubtitleMime = GetSubtitleMime(video.TargetSubtitleCodec);

      Subtitle currentSub = GetSubtitle(clientId, video, timeStart);
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
            existingContext.AssignStream(GetReadyFileBuffer(transcodingFile));
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
          context.AssignStream(GetReadyFileBuffer(transcodingFile));
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
        string pathName = PlaylistManifest.GetPlaylistFolderFromTranscodeFile(_cachePath, transcodingFile);
        string playlist = Path.Combine(pathName, PLAYLIST_MANIFEST_FILE_NAME);
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
                existingContext.AssignStream(GetReadyFileBuffer(playlist));
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
            context.AssignStream(GetReadyFileBuffer(playlist));
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
        if (currentSub != null && string.IsNullOrEmpty(currentSub.SourceFile) == false)
        {
          data.InputSubtitleFilePath = currentSub.SourceFile;
          context.TargetSubtitle = currentSub.SourceFile;
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

        _ffMpegCommandline.AddTimeParameters(timeStart, timeDuration, video.SourceDuration.TotalSeconds, ref data);

        FFMpegEncoderConfig encoderConfig = _ffMpegEncoderHandler.GetEncoderConfig(data.Encoder);
        _ffMpegCommandline.AddVideoParameters(video, data.TranscodeId, currentSub, encoderConfig, ref data);

        string fileName = transcodingFile;
        long startSegment = 0;
        _ffMpegCommandline.AddTargetVideoFormatAndOutputFileParameters(video, currentSub, ref fileName, out startSegment, timeStart, ref data);
        context.TargetFile = fileName;
        context.CurrentSegment = startSegment;
        if (currentSub != null && string.IsNullOrEmpty(currentSub.SourceFile) == false)
        {
          context.TargetSubtitle = currentSub.SourceFile;
        }
        _ffMpegCommandline.AddVideoAudioParameters(video, ref data);
        if (currentSub != null && embeddedSupported)
        {
          _ffMpegCommandline.AddSubtitleEmbeddingParameters(currentSub, embeddedSubCodec, timeStart, ref data);
        }
        else
        {
          embeddedSupported = false;
          data.OutputArguments.Add("-sn");
        }
        _ffMpegCommandline.AddStreamMapParameters(video.SourceVideoStreamIndex, video.SourceAudioStreamIndex, embeddedSupported, ref data);
      }

      _logger.Info("MediaConverter: Invoking transcoder to transcode video file '{0}' for transcode '{1}' with arguments '{2}'", video.SourceMedia.Path, video.TranscodeId, String.Join(", ", data.OutputArguments.ToArray()));
      context.Start();
      context.AssignStream(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    private static TranscodeContext TranscodeAudio(string clientId, AudioTranscoding audio, double timeStart, double timeDuration, bool waitForBuffer)
    {
      TranscodeContext context = new TranscodeContext(_cacheEnabled, _cachePath) { Failed = false };
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
            existingContext.AssignStream(GetReadyFileBuffer(transcodingFile));

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
          context.AssignStream(GetReadyFileBuffer(transcodingFile));
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

      _logger.Debug("MediaConverter: Invoking transcoder to transcode audio file '{0}' for transcode '{1}'", audio.SourceMedia, audio.TranscodeId);
      context.Start();
      context.AssignStream(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    private static TranscodeContext TranscodeImage(string clientId, ImageTranscoding image, bool waitForBuffer)
    {
      TranscodeContext context = new TranscodeContext(_cacheEnabled, _cachePath) { Failed = false };
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
            existingContext.AssignStream(GetReadyFileBuffer(transcodingFile));
          return existingContext;
        }
        else
        {
          //Presume that it is a cached file
          TouchFile(transcodingFile);
          context.Partial = false;
          context.TargetFile = transcodingFile;
          context.AssignStream(GetReadyFileBuffer(transcodingFile));
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

      _logger.Debug("MediaConverter: Invoking transcoder to transcode image file '{0}' for transcode '{1}'", image.SourceMedia, image.TranscodeId);
      context.Start();
      context.AssignStream(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    public static BufferedStream GetReadyFileBuffer(ILocalFsResourceAccessor lfsra)
    {
      // Impersonation
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
      {
        return GetReadyFileBuffer(lfsra.LocalFileSystemPath);
      }
    }

    public static BufferedStream GetReadyFileBuffer(string filePath)
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
            _logger.Debug(string.Format("MediaConverter: Serving ready file '{0}'", filePath));
            BufferedStream stream = new BufferedStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            return stream;
          }
        }
        iTry--;
        Thread.Sleep(500);
      }
      _logger.Error("MediaConverter: Timed out waiting for ready file '{0}'", filePath);
      return null;
    }

    #endregion

    #region Transcoder

    private static void AddTranscodeContext(string clientId, string transcodeId, TranscodeContext context)
    {
      try
      {
        lock (_runningClientTranscodes)
        {
          context.CompleteEvent.Reset();
          if (_runningClientTranscodes.ContainsKey(clientId) == false)
          {
            _runningClientTranscodes.Add(clientId, new Dictionary<string, List<TranscodeContext>>());
          }
          if (_runningClientTranscodes[clientId].Count > 0 &&
            (_runningClientTranscodes[clientId].ContainsKey(transcodeId) == false || context.Partial == false))
          {
            //Don't waste resources on transcoding if the client wants different media item
            _logger.Debug("MediaConverter: Ending {0} transcodes for client {1}", _runningClientTranscodes[clientId].Count, clientId);
            foreach (var transcodeContexts in _runningClientTranscodes[clientId].Values)
            {
              foreach (var transcodeContext in transcodeContexts)
              {
                transcodeContext.Stop();
              }
            }
            _runningClientTranscodes[clientId].Clear();
          }
          else if (_runningClientTranscodes[clientId].Count > 0)
          {
            //Don't waste resources on transcoding multiple partial transcodes
            _logger.Debug("MediaConverter: Ending partial transcodes for client {0}", clientId);
            List<TranscodeContext> contextList = new List<TranscodeContext>(_runningClientTranscodes[clientId][transcodeId]);
            foreach (var transcodeContext in contextList)
            {
              if (transcodeContext.Partial == true && transcodeContext != context)
              {
                transcodeContext.Stop();
              }
            }
          }
          if (_runningClientTranscodes[clientId].ContainsKey(transcodeId) == false)
          {
            _runningClientTranscodes[clientId].Add(transcodeId, new List<TranscodeContext>());
          }
          _runningClientTranscodes[clientId][transcodeId].Add(context);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error adding context for '{0}'", ex, transcodeId);
      }
    }

    private static void RemoveTranscodeContext(string clientId, string transcodeId, TranscodeContext context)
    {
      try
      {
        lock (_runningClientTranscodes)
        {
          if (_runningClientTranscodes.ContainsKey(clientId) == true)
          {
            if (_runningClientTranscodes[clientId].ContainsKey(transcodeId) == true)
            {
              context.CompleteEvent.Set();
              _runningClientTranscodes[clientId][transcodeId].Remove(context);
              if (_runningClientTranscodes[clientId][transcodeId].Count == 0)
                _runningClientTranscodes[clientId].Remove(transcodeId);
            }
            if (_runningClientTranscodes[clientId].Count == 0)
              _runningClientTranscodes.Remove(clientId);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error removing context for '{0}'", ex, transcodeId);
      }
    }

    private static Stream GetTranscodedFileBuffer(FFMpegTranscodeData data, TranscodeContext context)
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
              if(data.LiveStream != null)
                streamReady = data.LiveStream.CanRead;
            }
            catch { }
            if (streamReady)
            {
              _logger.Debug(string.Format("MediaConverter: Serving transcoded stream '{0}'", data.TranscodeId));
              return new BufferedStream(data.LiveStream);
            }
            iTry--;
            Thread.Sleep(500);
          }
          _logger.Error("MediaConverter: Timed out waiting for transcoded stream '{0}'", data.TranscodeId);
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
                _logger.Debug(string.Format("MediaConverter: Serving transcoded file '{0}'", fileReturnPath));
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
          _logger.Error("MediaConverter: Timed out waiting for transcoded file '{0}'", fileReturnPath);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error waiting for transcoding '{0}'", ex, data.TranscodeId);
      }
      return null;
    }

    private static Stream ExecuteTranscodingProcess(FFMpegTranscodeData data, TranscodeContext context, bool waitForBuffer)
    {
      if (context.Partial == true || IsTranscodeRunning(data.ClientId, data.TranscodeId) == false)
      {
        try
        {
          AddTranscodeContext(data.ClientId, data.TranscodeId, context);
          context.TargetFile = Path.Combine(data.WorkPath, data.SegmentPlaylist != null ? data.SegmentPlaylist : data.OutputFilePath);
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

    private static void TranscodeProcessor(object args)
    {
      FFMpegTranscodeThreadData data = (FFMpegTranscodeThreadData)args;
      bool isStream = false;
      if (data.Context.Live == true && data.Context.Segmented == false)
      {
        isStream = true;
      }

      _logger.Debug("MediaConverter: Transcoder '{0}' invoked with command line arguments '{1}'", data.TranscodeData.TranscoderBinPath, data.TranscodeData.TranscoderArguments);
      //Task<ProcessExecutionResult> executionResult = ServiceRegistration.Get<IFFMpegLib>().FFMpegExecuteWithResourceAccessAsync((ILocalFsResourceAccessor)data.Data.InputResourceAccessor, data.Data.TranscoderArguments, ProcessPriorityClass.Normal, ProcessUtils.INFINITE);

      if (data.Context.Segmented == true)
      {
        PlaylistManifest.CreatePlaylistFiles(data.TranscodeData);
      }

      ProcessStartInfo startInfo = new ProcessStartInfo()
      {
        FileName = data.TranscodeData.TranscoderBinPath,
        Arguments = data.TranscodeData.TranscoderArguments,
        WorkingDirectory = data.TranscodeData.WorkPath,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        RedirectStandardInput = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8
      };

      bool isFile = true;
      if (data.TranscodeData.InputResourceAccessor is INetworkResourceAccessor)
      {
        isFile = false;
      }
      data.Context.CompleteEvent.Reset();
      data.Context.Start();
      data.Context.Failed = false;
      int exitCode = -1;
      try
      {
        //TODO: Fix usages of obsolete and depricated methods when alternative is available
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor((data.TranscodeData.InputResourceAccessor).CanonicalLocalResourcePath))
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
                ffmpeg.StandardInput.WriteLine("q");
                ffmpeg.StandardInput.Close();
                if (ffmpeg.WaitForExit(2000) == false)
                {
                  ffmpeg.Kill();
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
        if (isStream)
        {
          _logger.Error("MediaConverter: Transcoder command failed for stream '{0}'", ex, data.TranscodeData.TranscodeId);
        }
        else
        {
          _logger.Error("MediaConverter: Transcoder command failed for file '{0}'", ex, data.TranscodeData.OutputFilePath);
        }
        data.Context.Failed = true;
      }
      if (exitCode > 0)
      {
        data.Context.Failed = true;
      }
      data.Context.Stop();
      data.Context.CompleteEvent.Set();
      _ffMpegEncoderHandler.EndEncoding(data.TranscodeData.Encoder, data.TranscodeData.TranscodeId);

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
          if (isStream)
          {
            _logger.Debug("MediaConverter: Transcoder command failed with error {1} for stream '{0}'", data.TranscodeData.TranscodeId, exitCode);
          }
          else
          {
            _logger.Debug("MediaConverter: Transcoder command failed with error {1} for file '{0}'", data.TranscodeData.OutputFilePath, exitCode);
          }
        }
        if (data.Context.Aborted == true)
        {
          if (isStream)
          {
            _logger.Debug("MediaConverter: Transcoder command aborted for stream '{0}'", data.TranscodeData.TranscodeId);
          }
          else
          {
            _logger.Debug("MediaConverter: Transcoder command aborted for file '{0}'", data.TranscodeData.OutputFilePath);
          }
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
