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
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters;
using MediaPortal.Utilities.Process;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Encoders;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;
using MediaPortal.Plugins.Transcoding.Interfaces.Analyzers;
using MediaPortal.Plugins.Transcoding.Interfaces.SlimTv;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Plugins.Transcoding.Service
{
  public class MediaConverter : IMediaConverter
  {
    public const string PLAYLIST_FILE_NAME = "playlist.m3u8";
    public const string PLAYLIST_SUBTITLE_FILE_NAME = PLAYLIST_FILE_NAME + "_vtt.m3u8";
    public const string PLAYLIST_TEMP_FILE_NAME = "temp_playlist.m3u8";
    public const string PLAYLIST_TEMP_SUBTITLE_FILE_NAME = PLAYLIST_TEMP_FILE_NAME + "_vtt.m3u8";

    private const string HLS_SEGMENT_FILE_TEMPLATE = "%05d.ts";
    private const string HLS_SEGMENT_SUB_TEMPLATE = "sub.vtt";
    private const int HLS_PLAYLIST_TIMEOUT = 25000; //Can take long time to start for RTSP

    public int HLSSegmentTimeInSeconds
    {
      get
      {
        return _hlsSegmentTimeInSeconds;
      }
    }

    public string HLSMediaPlayListName
    {
      get
      {
        return PLAYLIST_FILE_NAME;
      }
    }

    public string HLSSubtitlePlayListName
    {
      get
      {
        return PLAYLIST_SUBTITLE_FILE_NAME;
      }
    }

    private Dictionary<string, Dictionary<string, List<FFMpegTranscodeContext>>> _runningClientTranscodes = new Dictionary<string, Dictionary<string, List<FFMpegTranscodeContext>>>();
    private FFMpegEncoderHandler _ffMpegEncoderHandler;
    private FFMpegCommandline _ffMpegCommandline;
    private string _cachePath;
    private long _cacheMaximumSize;
    private long _cacheMaximumAge;
    private bool _cacheEnabled;
    private string _transcoderBinPath;
    private int _transcoderMaximumThreads;
    private int _transcoderTimeout;
    private int _hlsSegmentTimeInSeconds;
    private string _subtitleDefaultEncoding;
    private string _subtitleDefaultLanguage;
    private ILogger _logger;
    private bool _supportHardcodedSubs = true;
    private bool _supportNvidiaHW = true;
    private bool _supportIntelHW = true;
    private SlimTvHandler _slimtTvHandler = null;

    public MediaConverter()
    {
      _slimtTvHandler = new SlimTvHandler();
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
        if (RegisterHardwareEncoder(FFMpegEncoderHandler.EncoderHandler.HardwareIntel, TranscodingServicePlugin.Settings.IntelHWMaximumStreams,
          new List<VideoCodec>(TranscodingServicePlugin.Settings.IntelHWSupportedCodecs)) == false)
        {
          _logger.Warn("MediaConverter: Failed to register Intel hardware acceleration");
        }
      }
      if (TranscodingServicePlugin.Settings.NvidiaHWAccelerationAllowed && _supportNvidiaHW)
      {
        if (RegisterHardwareEncoder(FFMpegEncoderHandler.EncoderHandler.HardwareNvidia, TranscodingServicePlugin.Settings.NvidiaHWMaximumStreams,
          new List<VideoCodec>(TranscodingServicePlugin.Settings.NvidiaHWSupportedCodecs)) == false)
        {
          _logger.Warn("MediaConverter: Failed to register Nvidia hardware acceleration");
        }
      }

      _ffMpegEncoderHandler = new FFMpegEncoderHandler();
      LoadSettings();
    }

    public void LoadSettings()
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
        if (RegisterHardwareEncoder(FFMpegEncoderHandler.EncoderHandler.HardwareIntel, TranscodingServicePlugin.Settings.IntelHWMaximumStreams,
          new List<VideoCodec>(TranscodingServicePlugin.Settings.IntelHWSupportedCodecs)) == false)
        {
          _logger.Warn("MediaConverter: Failed to register Intel hardware acceleration");
        }
      }
      else
      {
        UnregisterHardwareEncoder(FFMpegEncoderHandler.EncoderHandler.HardwareIntel);
      }
      if (TranscodingServicePlugin.Settings.NvidiaHWAccelerationAllowed && _supportNvidiaHW)
      {
        if (RegisterHardwareEncoder(FFMpegEncoderHandler.EncoderHandler.HardwareNvidia, TranscodingServicePlugin.Settings.NvidiaHWMaximumStreams,
          new List<VideoCodec>(TranscodingServicePlugin.Settings.NvidiaHWSupportedCodecs)) == false)
        {
          _logger.Warn("MediaConverter: Failed to register Nvidia hardware acceleration");
        }
      }
      else
      {
        UnregisterHardwareEncoder(FFMpegEncoderHandler.EncoderHandler.HardwareNvidia);
      }

      _ffMpegCommandline = new FFMpegCommandline(_transcoderMaximumThreads, _transcoderTimeout, _cachePath, _hlsSegmentTimeInSeconds, HLS_SEGMENT_FILE_TEMPLATE, _supportHardcodedSubs);
    }

    #region HLS

    public long GetSegmentSequence(string FileName)
    {
      long sequenceNumber = -1;
      long.TryParse(Path.GetFileNameWithoutExtension(FileName), out sequenceNumber);
      return sequenceNumber;
    }

    public bool GetSegmentFile(VideoTranscoding TranscodingInfo, TranscodeContext Context, string FileName, out Stream FileData, out dynamic ContainerEnum)
    {
      FileData = null;
      ContainerEnum = null;
      string completePath = Path.Combine(Context.SegmentDir, FileName);
      string playlistPath = null;
      if (TranscodingInfo.TargetIsLive == false)
      {
        playlistPath = Path.Combine(Context.SegmentDir, MediaConverter.PLAYLIST_TEMP_FILE_NAME);
      }
      else
      {
        playlistPath = Path.Combine(Context.SegmentDir, MediaConverter.PLAYLIST_FILE_NAME);
      }
      DateTime waitStart = DateTime.Now;

      //Thread.Sleep(2000); //Ensure that writing is completed. Is there a better way?
      if (Path.GetExtension(MediaConverter.PLAYLIST_FILE_NAME) == Path.GetExtension(FileName)) //playlist file
      {
        while (File.Exists(completePath) == false)
        {
          if (Context.Running == false) return false;
          if ((DateTime.Now - waitStart).TotalMilliseconds > HLS_PLAYLIST_TIMEOUT) return false;
          Thread.Sleep(100);
        }
        ContainerEnum = VideoContainer.Hls;
        MemoryStream memStream = new MemoryStream(PlaylistManifest.CorrectPlaylistUrls(TranscodingInfo.HlsBaseUrl, completePath));
        memStream.Position = 0;
        FileData = memStream;
      }
      if (Path.GetExtension(HLS_SEGMENT_FILE_TEMPLATE) == Path.GetExtension(FileName)) //segment file
      {
        long sequenceNumber = GetSegmentSequence(FileName);
        while (true)
        {
          if (File.Exists(completePath) == false)
          {
            if (Context.CurrentSegment > sequenceNumber)
            {
              // Probably rewinding
              return false;
            }
            if ((sequenceNumber - Context.CurrentSegment) > 2)
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
              if (playlistContents.Contains(FileName)) break;
            }
          }
          if (Context.Running == false) return false;
          if ((DateTime.Now - waitStart).TotalSeconds > _hlsSegmentTimeInSeconds) return false;
          Thread.Sleep(100);
        }
        ContainerEnum = VideoContainer.Mpeg2Ts;
        if (sequenceNumber >= 0)
        {
          Context.CurrentSegment = sequenceNumber;
        }
        FileData = GetFileStream(completePath);
      }
      if (Path.GetExtension(HLS_SEGMENT_SUB_TEMPLATE) == Path.GetExtension(FileName)) //subtitle file
      {
        while (File.Exists(completePath) == false)
        {
          if (Context.Running == false) return false;
          if ((DateTime.Now - waitStart).TotalSeconds > _hlsSegmentTimeInSeconds) return false;
          Thread.Sleep(100);
        }
        ContainerEnum = SubtitleCodec.WebVtt;
        FileData = GetFileStream(completePath);
      }
      if (FileData != null) return true;
      return false;
    }

    #endregion

    #region Metadata

    public TranscodedAudioMetadata GetTranscodedAudioMetadata(AudioTranscoding TranscodingInfo)
    {
      TranscodedAudioMetadata metadata = new TranscodedAudioMetadata
      {
        TargetAudioBitrate = TranscodingInfo.TargetAudioBitrate,
        TargetAudioCodec = TranscodingInfo.TargetAudioCodec,
        TargetAudioContainer = TranscodingInfo.TargetAudioContainer,
        TargetAudioFrequency = TranscodingInfo.TargetAudioFrequency
      };
      if (TranscodingInfo.TargetForceCopy)
      {
        metadata.TargetAudioBitrate = TranscodingInfo.SourceAudioBitrate;
        metadata.TargetAudioCodec = TranscodingInfo.SourceAudioCodec;
        metadata.TargetAudioContainer = TranscodingInfo.SourceAudioContainer;
        metadata.TargetAudioFrequency = TranscodingInfo.SourceAudioFrequency;
        metadata.TargetAudioChannels = TranscodingInfo.SourceAudioChannels;
      }
      if (TranscodingInfo.TargetAudioContainer == AudioContainer.Unknown)
      {
        metadata.TargetAudioContainer = TranscodingInfo.SourceAudioContainer;
      }

      if (TranscodingInfo.TargetForceCopy == false)
      {
        if (Checks.IsAudioStreamChanged(TranscodingInfo))
        {
          if (TranscodingInfo.TargetAudioCodec == AudioCodec.Unknown)
          {
            switch (TranscodingInfo.TargetAudioContainer)
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
                metadata.TargetAudioCodec = TranscodingInfo.SourceAudioCodec;
                break;
            }
          }
          long frequency = Validators.GetAudioFrequency(TranscodingInfo.SourceAudioCodec, TranscodingInfo.TargetAudioCodec, TranscodingInfo.SourceAudioFrequency, TranscodingInfo.TargetAudioFrequency);
          if (frequency > 0)
          {
            metadata.TargetAudioFrequency = frequency;
          }
          if (TranscodingInfo.TargetAudioContainer != AudioContainer.Lpcm)
          {
            metadata.TargetAudioBitrate = Validators.GetAudioBitrate(TranscodingInfo.SourceAudioBitrate, TranscodingInfo.TargetAudioBitrate);
          }
        }
        metadata.TargetAudioChannels = Validators.GetAudioNumberOfChannels(TranscodingInfo.SourceAudioCodec, TranscodingInfo.TargetAudioCodec, TranscodingInfo.SourceAudioChannels, TranscodingInfo.TargetForceAudioStereo);
      }
      return metadata;
    }

    public TranscodedImageMetadata GetTranscodedImageMetadata(ImageTranscoding TranscodingInfo)
    {
      TranscodedImageMetadata metadata = new TranscodedImageMetadata
      {
        TargetMaxHeight = TranscodingInfo.SourceHeight,
        TargetMaxWidth = TranscodingInfo.SourceWidth,
        TargetOrientation = TranscodingInfo.SourceOrientation,
        TargetImageCodec = TranscodingInfo.TargetImageCodec
      };
      if (metadata.TargetImageCodec == ImageContainer.Unknown)
      {
        metadata.TargetImageCodec = TranscodingInfo.SourceImageCodec;
      }
      metadata.TargetPixelFormat = TranscodingInfo.TargetPixelFormat;
      if (metadata.TargetPixelFormat == PixelFormat.Unknown)
      {
        metadata.TargetPixelFormat = TranscodingInfo.SourcePixelFormat;
      }
      if (Checks.IsImageStreamChanged(TranscodingInfo) == true)
      {
        metadata.TargetMaxHeight = TranscodingInfo.SourceHeight;
        metadata.TargetMaxWidth = TranscodingInfo.SourceWidth;
        if (metadata.TargetMaxHeight > TranscodingInfo.TargetHeight && TranscodingInfo.TargetHeight > 0)
        {
          double scale = (double)TranscodingInfo.SourceWidth / (double)TranscodingInfo.SourceHeight;
          metadata.TargetMaxHeight = TranscodingInfo.TargetHeight;
          metadata.TargetMaxWidth = Convert.ToInt32(scale * (double)metadata.TargetMaxHeight);
        }
        if (metadata.TargetMaxWidth > TranscodingInfo.TargetWidth && TranscodingInfo.TargetWidth > 0)
        {
          double scale = (double)TranscodingInfo.SourceHeight / (double)TranscodingInfo.SourceWidth;
          metadata.TargetMaxWidth = TranscodingInfo.TargetWidth;
          metadata.TargetMaxHeight = Convert.ToInt32(scale * (double)metadata.TargetMaxWidth);
        }

        if (TranscodingInfo.TargetAutoRotate == true)
        {
          if (TranscodingInfo.SourceOrientation > 4)
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

    public TranscodedVideoMetadata GetTranscodedVideoMetadata(VideoTranscoding TranscodingInfo)
    {
      TranscodedVideoMetadata metadata = new TranscodedVideoMetadata
      {
        TargetAudioBitrate = TranscodingInfo.TargetAudioBitrate,
        TargetAudioCodec = TranscodingInfo.TargetAudioCodec,
        TargetAudioFrequency = TranscodingInfo.TargetAudioFrequency,
        TargetVideoFrameRate = TranscodingInfo.SourceFrameRate,
        TargetLevel = TranscodingInfo.TargetLevel,
        TargetPreset = TranscodingInfo.TargetPreset,
        TargetProfile = TranscodingInfo.TargetProfile,
        TargetVideoPixelFormat = TranscodingInfo.TargetPixelFormat
      };
      if(TranscodingInfo.TargetForceVideoCopy)
      {
        metadata.TargetVideoContainer = TranscodingInfo.SourceVideoContainer;
        metadata.TargetVideoAspectRatio = TranscodingInfo.SourceVideoAspectRatio;
        metadata.TargetVideoBitrate = TranscodingInfo.SourceVideoBitrate;
        metadata.TargetVideoCodec = TranscodingInfo.SourceVideoCodec;
        metadata.TargetVideoFrameRate = TranscodingInfo.SourceFrameRate;
        metadata.TargetVideoPixelFormat = TranscodingInfo.SourcePixelFormat;
        metadata.TargetVideoMaxWidth = TranscodingInfo.SourceVideoWidth;
        metadata.TargetVideoMaxHeight = TranscodingInfo.SourceVideoHeight;
      }
      if (TranscodingInfo.TargetForceAudioCopy)
      {
        metadata.TargetAudioBitrate = TranscodingInfo.SourceAudioBitrate;
        metadata.TargetAudioCodec = TranscodingInfo.SourceAudioCodec;
        metadata.TargetAudioFrequency = TranscodingInfo.SourceAudioFrequency;
        metadata.TargetAudioChannels = TranscodingInfo.SourceAudioChannels;
        metadata.TargetAudioBitrate = TranscodingInfo.SourceAudioBitrate;
      }
      if (metadata.TargetVideoPixelFormat == PixelFormat.Unknown)
      {
        metadata.TargetVideoPixelFormat = PixelFormat.Yuv420;
      }
      metadata.TargetVideoAspectRatio = TranscodingInfo.TargetVideoAspectRatio;
      if (metadata.TargetVideoAspectRatio <= 0)
      {
        metadata.TargetVideoAspectRatio = 16.0F / 9.0F;
      }
      metadata.TargetVideoBitrate = TranscodingInfo.TargetVideoBitrate;
      metadata.TargetVideoCodec = TranscodingInfo.TargetVideoCodec;
      if (metadata.TargetVideoCodec == VideoCodec.Unknown)
      {
        metadata.TargetVideoCodec = TranscodingInfo.SourceVideoCodec;
      }
      metadata.TargetVideoContainer = TranscodingInfo.TargetVideoContainer;
      if (metadata.TargetVideoContainer == VideoContainer.Unknown)
      {
        metadata.TargetVideoContainer = TranscodingInfo.SourceVideoContainer;
      }
      metadata.TargetVideoTimestamp = Timestamp.None;
      if (metadata.TargetVideoContainer == VideoContainer.M2Ts)
      {
        metadata.TargetVideoTimestamp = Timestamp.Valid;
      }

      metadata.TargetVideoMaxWidth = TranscodingInfo.SourceVideoWidth;
      metadata.TargetVideoMaxHeight = TranscodingInfo.SourceVideoHeight;
      if (metadata.TargetVideoMaxHeight <= 0)
      {
        metadata.TargetVideoMaxHeight = 1080;
      }

      if (TranscodingInfo.TargetForceVideoCopy == false)
      {
        float newPixelAspectRatio = TranscodingInfo.SourceVideoPixelAspectRatio;
        if (newPixelAspectRatio <= 0)
        {
          newPixelAspectRatio = 1.0F;
        }

        Size newSize = new Size(TranscodingInfo.SourceVideoWidth, TranscodingInfo.SourceVideoHeight);
        Size newContentSize = new Size(TranscodingInfo.SourceVideoWidth, TranscodingInfo.SourceVideoHeight);
        bool pixelARChanged = false;
        bool videoARChanged = false;
        bool videoHeightChanged = false;
        _ffMpegCommandline.GetVideoDimensions(TranscodingInfo, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);
        metadata.TargetVideoPixelAspectRatio = newPixelAspectRatio;
        metadata.TargetVideoMaxWidth = newSize.Width;
        metadata.TargetVideoMaxHeight = newSize.Height;

        metadata.TargetVideoFrameRate = TranscodingInfo.SourceFrameRate;
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
      }
      if (TranscodingInfo.TargetForceAudioCopy == false)
      {
        metadata.TargetAudioChannels = Validators.GetAudioNumberOfChannels(TranscodingInfo.SourceAudioCodec, TranscodingInfo.TargetAudioCodec, TranscodingInfo.SourceAudioChannels, TranscodingInfo.TargetForceAudioStereo);
        long frequency = Validators.GetAudioFrequency(TranscodingInfo.SourceAudioCodec, TranscodingInfo.TargetAudioCodec, TranscodingInfo.SourceAudioFrequency, TranscodingInfo.TargetAudioFrequency);
        if (frequency != -1)
        {
          metadata.TargetAudioFrequency = frequency;
        }
        if (TranscodingInfo.TargetAudioCodec != AudioCodec.Lpcm)
        {
          metadata.TargetAudioBitrate = Validators.GetAudioBitrate(TranscodingInfo.SourceAudioBitrate, TranscodingInfo.TargetAudioBitrate);
        }
      }
      return metadata;
    }

    #endregion

    #region HW Acelleration

    private bool RegisterHardwareEncoder(FFMpegEncoderHandler.EncoderHandler encoder, int maximumStreams, List<VideoCodec> supportedCodecs)
    {
      if (encoder == FFMpegEncoderHandler.EncoderHandler.Software)
        return false;
      else if (encoder == FFMpegEncoderHandler.EncoderHandler.HardwareIntel && _supportIntelHW == false)
        return false;
      else if (encoder == FFMpegEncoderHandler.EncoderHandler.HardwareNvidia && _supportNvidiaHW == false)
        return false;
      _ffMpegEncoderHandler.RegisterEncoder(encoder, maximumStreams, supportedCodecs);
      return true;
    }

    private void UnregisterHardwareEncoder(FFMpegEncoderHandler.EncoderHandler encoder)
    {
      _ffMpegEncoderHandler.UnregisterEncoder(encoder);
    }

    #endregion

    #region Cache

    private void TouchFile(string filePath)
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

    private void TouchDirectory(string folderPath)
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

    public bool IsTranscodeRunning(string ClientId, string TranscodeId)
    {
      lock (_runningClientTranscodes)
      {
        return _runningClientTranscodes.ContainsKey(ClientId) && _runningClientTranscodes[ClientId].ContainsKey(TranscodeId);
      }
    }

    public void StopTranscode(string ClientId, string TranscodeId)
    {
      lock (_runningClientTranscodes)
      {
        if (_runningClientTranscodes.ContainsKey(ClientId) && _runningClientTranscodes[ClientId].ContainsKey(TranscodeId))
        {
          foreach (TranscodeContext context in _runningClientTranscodes[ClientId][TranscodeId])
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

    public void StopAllTranscodes()
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

    public void CleanUpTranscodeCache()
    {
      lock (_cachePath)
      {
        if (Directory.Exists(_cachePath) == true)
        {
          int maxTries = 10;
          SortedDictionary<DateTime, string> fileList = new SortedDictionary<DateTime, string>();
          long cacheSize = 0;
          List<string> dirObjects = new List<string>(Directory.GetFiles(_cachePath, "*.mp*"));
          dirObjects.AddRange(Directory.GetDirectories(_cachePath, "*" + FFMpegPlaylistManifest.PLAYLIST_FOLDER_SUFFIX));
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

    private SubtitleStream FindSubtitle(VideoTranscoding video)
    {
      if (video.SourceSubtitleStreamIndex == Subtitles.NO_SUBTITLE) return null;

      SubtitleStream currentEmbeddedSub = null;
      SubtitleStream currentExternalSub = null;

      SubtitleStream defaultEmbeddedSub = null;
      SubtitleStream englishEmbeddedSub = null;
      List<SubtitleStream> subsEmbedded = new List<SubtitleStream>();
      List<SubtitleStream> langSubsEmbedded = new List<SubtitleStream>();

      List<SubtitleStream> allSubs = Subtitles.GetSubtitleStreams(video, _subtitleDefaultEncoding, _subtitleDefaultLanguage);
      foreach (SubtitleStream sub in allSubs)
      {
        if (sub.IsEmbedded == false)
        {
          continue;
        }
        if (video.SourceSubtitleStreamIndex >= 0 && sub.StreamIndex == video.SourceSubtitleStreamIndex)
        {
          return sub;
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
        subsEmbedded.Add(sub);
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
        if (video.SourceSubtitleStreamIndex < Subtitles.NO_SUBTITLE &&
          sub.StreamIndex == video.SourceSubtitleStreamIndex)
        {
          return sub;
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
        subs.Add(sub);
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

    private SubtitleStream ConvertSubtitleToUtf8(SubtitleStream sub, string targetFileName)
    {
      if (Subtitles.SubtitleIsUnicode(sub.CharacterEncoding) == false)
      {
        if (string.IsNullOrEmpty(sub.CharacterEncoding) == false)
        {
          string sourceName = sub.Source;
          File.WriteAllText(targetFileName, File.ReadAllText(sourceName, Encoding.GetEncoding(sub.CharacterEncoding)), Encoding.UTF8);
          sub.CharacterEncoding = "UTF-8";
          sub.Source = targetFileName;
          _logger.Debug("MediaConverter: Converted subtitle file '{0}' to UTF-8", sourceName);
        }
      }
      return sub;
    }

    public Stream GetSubtitleStream(string ClientId, VideoTranscoding TranscodingInfo)
    {
      SubtitleStream sub = GetSubtitle(ClientId, TranscodingInfo, 0);
      if (sub == null || sub.Source == null)
      {
        return null;
      }
      if (IsTranscodeRunning(ClientId, TranscodingInfo.TranscodeId) == false)
      {
        TouchFile(sub.Source);
      }
      return GetFileStream(sub.Source);
    }

    private SubtitleStream GetSubtitle(string clientId, VideoTranscoding video, double timeStart)
    {
      SubtitleStream sourceSubtitle = FindSubtitle(video);
      if (sourceSubtitle == null) return null;
      if (video.TargetSubtitleSupport == SubtitleSupport.None) return null;

      SubtitleStream res = new SubtitleStream
      {
        StreamIndex = sourceSubtitle.StreamIndex,
        Codec = sourceSubtitle.Codec,
        Language = sourceSubtitle.Language,
        Source = sourceSubtitle.Source,
        CharacterEncoding = sourceSubtitle.CharacterEncoding
      };
      if (SubtitleAnalyzer.IsSubtitleSupportedByContainer(sourceSubtitle.Codec, video.SourceVideoContainer, video.TargetVideoContainer) == true)
      {
        if (sourceSubtitle.IsEmbedded)
        {
          //Subtitle stream can be copied directly
          return res;
        }
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
        res.Source = transcodingFile;
        if (Subtitles.SubtitleIsUnicode(res.CharacterEncoding) == false)
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
          res.Source = transcodingFile;
          return res;
        }
        return null;
      }

      // SourceSubtitle == TargetSubtitleCodec -> just return
      if (video.TargetSubtitleCodec != SubtitleCodec.Unknown && video.TargetSubtitleCodec == sourceSubtitle.Codec && timeStart == 0)
      {
        return ConvertSubtitleToUtf8(res, transcodingFile);
      }

      // Burn external subtitle into video
      if (res.Source == null)
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

      _logger.Debug("MediaConverter: Invoking transcoder to transcode subtitle file '{0}' for transcode '{1}'", res.Source, data.TranscodeId);
      bool success = FFMpegBinary.FFMpegExecuteWithResourceAccessAsync((ILocalFsResourceAccessor)data.InputResourceAccessor, data.TranscoderArguments, ProcessPriorityClass.Normal, _transcoderTimeout).Result.Success;
      if (success && File.Exists(transcodingFile) == true)
      {
        if (tempFile != null && File.Exists(tempFile)) File.Delete(tempFile);
        res.Source = transcodingFile;
        return res;
      }
      return null;
    }

    #endregion

    #region Transcoding

    private bool AssignExistingTranscodeContext(string clientId, string transcodeId, ref FFMpegTranscodeContext context)
    {
      lock (_runningClientTranscodes)
      {
        if (_runningClientTranscodes.ContainsKey(clientId))
        {
          if (_runningClientTranscodes[clientId].ContainsKey(transcodeId))
          {
            List<FFMpegTranscodeContext> runningContexts = _runningClientTranscodes[clientId][transcodeId];
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

    public TranscodeContext GetLiveStream(string ClientId, BaseTranscoding TranscodingInfo, int ChannelId, bool WaitForBuffer)
    {
      TranscodingInfo.SourceMedia = new FFMpegLiveAccessor(ChannelId);
      if (TranscodingInfo is AudioTranscoding)
      {
        ((AudioTranscoding)TranscodingInfo).TargetIsLive = true;
        return TranscodeAudio(ClientId, TranscodingInfo as AudioTranscoding, 0, 0, WaitForBuffer);
      }
      else if (TranscodingInfo is VideoTranscoding)
      {
        ((VideoTranscoding)TranscodingInfo).TargetIsLive = true;
        return TranscodeVideo(ClientId, TranscodingInfo as VideoTranscoding, 0, 0, WaitForBuffer);
      }
      return null;
    }

    public TranscodeContext GetMediaStream(string ClientId, BaseTranscoding TranscodingInfo, double StartTime, double Duration, bool WaitForBuffer)
    {
      if (TranscodingInfo.SourceMedia is ILocalFsResourceAccessor)
      {
        if (((ILocalFsResourceAccessor)TranscodingInfo.SourceMedia).Exists == false)
        {
          _logger.Error("MediaConverter: File '{0}' does not exist for transcode '{1}'", TranscodingInfo.SourceMedia, TranscodingInfo.TranscodeId);
          return null;
        }
      }
      if (TranscodingInfo is ImageTranscoding)
      {
        return TranscodeImage(ClientId, TranscodingInfo as ImageTranscoding, WaitForBuffer);
      }
      else if (TranscodingInfo is AudioTranscoding)
      {
        return TranscodeAudio(ClientId, TranscodingInfo as AudioTranscoding, StartTime, Duration, WaitForBuffer);
      }
      else if (TranscodingInfo is VideoTranscoding)
      {
        return TranscodeVideo(ClientId, TranscodingInfo as VideoTranscoding, StartTime, Duration, WaitForBuffer);
      }
      _logger.Error("MediaConverter: Transcoding info is not valid for transcode '{0}'", TranscodingInfo.TranscodeId);
      return null;
    }

    private FFMpegTranscodeContext TranscodeVideo(string clientId, VideoTranscoding video, double timeStart, double timeDuration, bool waitForBuffer)
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
        FFMpegTranscodeContext existingContext = null;
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
          FFMpegTranscodeContext existingContext = null;
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

      _logger.Info("MediaConverter: Invoking transcoder to transcode video file '{0}' for transcode '{1}' with arguments '{2}'", video.SourceMedia.Path, video.TranscodeId, String.Join(", ", data.OutputArguments.ToArray()));
      context.Start();
      context.AssignStream(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    private FFMpegTranscodeContext TranscodeAudio(string clientId, AudioTranscoding audio, double timeStart, double timeDuration, bool waitForBuffer)
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
        FFMpegTranscodeContext existingContext = null;
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

      _logger.Debug("MediaConverter: Invoking transcoder to transcode audio file '{0}' for transcode '{1}'", audio.SourceMedia, audio.TranscodeId);
      context.Start();
      context.AssignStream(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    private FFMpegTranscodeContext TranscodeImage(string clientId, ImageTranscoding image, bool waitForBuffer)
    {
      FFMpegTranscodeContext context = new FFMpegTranscodeContext(_cacheEnabled, _cachePath) { Failed = false };
      context.Partial = false;
      string transcodingFile = Path.Combine(_cachePath, image.TranscodeId + ".mpti");

      if (File.Exists(transcodingFile) == true)
      {
        //Use exisitng contaxt if possible
        FFMpegTranscodeContext existingContext = null;
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

      _logger.Debug("MediaConverter: Invoking transcoder to transcode image file '{0}' for transcode '{1}'", image.SourceMedia, image.TranscodeId);
      context.Start();
      context.AssignStream(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    public Stream GetFileStream(ILocalFsResourceAccessor FileResource)
    {
      // Impersonation
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(FileResource.CanonicalLocalResourcePath))
      {
        return GetFileStream(FileResource.LocalFileSystemPath);
      }
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

    private void AddTranscodeContext(string clientId, string transcodeId, FFMpegTranscodeContext context)
    {
      try
      {
        lock (_runningClientTranscodes)
        {
          context.CompleteEvent.Reset();
          if (_runningClientTranscodes.ContainsKey(clientId) == false)
          {
            _runningClientTranscodes.Add(clientId, new Dictionary<string, List<FFMpegTranscodeContext>>());
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
            _runningClientTranscodes[clientId].Add(transcodeId, new List<FFMpegTranscodeContext>());
          }
          _runningClientTranscodes[clientId][transcodeId].Add(context);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error adding context for '{0}'", ex, transcodeId);
      }
    }

    private void RemoveTranscodeContext(string clientId, string transcodeId, FFMpegTranscodeContext context)
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
      if (data.TranscodeData.InputResourceAccessor is IFFMpegLiveAccessor)
      {
        isSlimTv = true;
        liveChannelId = ((IFFMpegLiveAccessor)data.TranscodeData.InputResourceAccessor).ChannelId;
      }

      data.Context.CompleteEvent.Reset();
      data.Context.Start();
      data.Context.Failed = false;
      int exitCode = -1;

      IResourceAccessor mediaAccessor = data.TranscodeData.InputResourceAccessor;
      if (isSlimTv)
      {
        if (_slimtTvHandler.StartTuning(data.TranscodeData.ClientId, liveChannelId, out liveItem) == false)
        {
          _logger.Error("MediaConverter: Transcoder unable to start timeshifting for channel {0}", liveChannelId);
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

      _logger.Debug("MediaConverter: Transcoder '{0}' invoked with command line arguments '{1}'", data.TranscodeData.TranscoderBinPath, data.TranscodeData.TranscoderArguments);
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
                _slimtTvHandler.AttachConverterStreamHook(data.TranscodeData.ClientId, ffmpeg.StandardInput.BaseStream);
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
                  if (!(data.TranscodeData.InputResourceAccessor is FFMpegLiveAccessor))
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
            _logger.Error("MediaConverter: Transcoder command failed for stream '{0}'", ex, data.TranscodeData.TranscodeId);
          }
          else
          {
            _logger.Error("MediaConverter: Transcoder command failed for file '{0}'", ex, data.TranscodeData.OutputFilePath);
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
        if (_slimtTvHandler.EndTuning(data.TranscodeData.ClientId) == false)
        {
          _logger.Error("MediaConverter: Transcoder unable to stop timeshifting for channel {0}", liveChannelId);
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
            _logger.Debug("MediaConverter: Transcoder command failed with error {1} for stream '{0}'", data.TranscodeData.TranscodeId, exitCode);
          }
          else
          {
            _logger.Debug("MediaConverter: Transcoder command failed with error {1} for file '{0}'", data.TranscodeData.OutputFilePath, exitCode);
          }
        }
        if (data.Context.Aborted == true)
        {
          if (isStream || data.TranscodeData.OutputFilePath == null)
          {
            _logger.Debug("MediaConverter: Transcoder command aborted for stream '{0}'", data.TranscodeData.TranscodeId);
          }
          else
          {
            _logger.Debug("MediaConverter: Transcoder command aborted for file '{0}'", data.TranscodeData.OutputFilePath);
          }
        }
        else
        {
          _logger.Debug("MediaConverter: FFMpeg error \n {0}", data.Context.ConsoleErrorOutput);
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
