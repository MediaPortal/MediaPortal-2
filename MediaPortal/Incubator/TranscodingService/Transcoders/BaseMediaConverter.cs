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
using System.Text;
using System.IO;
using System.Threading;
using System.Linq;
using System.Globalization;
using System.Drawing;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;
using MediaPortal.Plugins.Transcoding.Interfaces.Analyzers;
using MediaPortal.Plugins.Transcoding.Interfaces.SlimTv;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders
{
  public abstract class BaseMediaConverter : IMediaConverter
  {
    public const string PLAYLIST_FILE_NAME = "playlist.m3u8";
    public const string PLAYLIST_SUBTITLE_FILE_NAME = PLAYLIST_FILE_NAME + "_vtt.m3u8";
    public const string PLAYLIST_TEMP_FILE_NAME = "temp_playlist.m3u8";
    public const string PLAYLIST_TEMP_SUBTITLE_FILE_NAME = PLAYLIST_TEMP_FILE_NAME + "_vtt.m3u8";

    public const string HLS_SEGMENT_FILE_TEMPLATE = "%05d.ts";
    public const string HLS_SEGMENT_SUB_TEMPLATE = "sub.vtt";
    public const int HLS_PLAYLIST_TIMEOUT = 25000; //Can take long time to start for RTSP

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

    protected Dictionary<string, Dictionary<string, List<TranscodeContext>>> _runningClientTranscodes = new Dictionary<string, Dictionary<string, List<TranscodeContext>>>();
    protected string _cachePath;
    protected long _cacheMaximumSize;
    protected long _cacheMaximumAge;
    protected bool _cacheEnabled;
    protected int _transcoderMaximumThreads;
    protected int _transcoderTimeout;
    protected int _hlsSegmentTimeInSeconds;
    protected string _subtitleDefaultEncoding;
    protected string _subtitleDefaultLanguage;
    protected ILogger _logger;
    protected bool _supportHardcodedSubs = true;
    protected SlimTvHandler _slimtTvHandler = null;

    public BaseMediaConverter()
    {
      _slimtTvHandler = new SlimTvHandler();
      _logger = ServiceRegistration.Get<ILogger>();

      LoadSettings();
    }

    public void LoadSettings()
    {
      _cacheEnabled = TranscodingServicePlugin.Settings.CacheEnabled;
      _cachePath = TranscodingServicePlugin.Settings.CachePath;
      _cacheMaximumSize = TranscodingServicePlugin.Settings.CacheMaximumSizeInGB * 1024 * 1024 * 1024; //Bytes
      _cacheMaximumAge = TranscodingServicePlugin.Settings.CacheMaximumAgeInDays; //Days
      _transcoderMaximumThreads = TranscodingServicePlugin.Settings.TranscoderMaximumThreads;
      _transcoderTimeout = TranscodingServicePlugin.Settings.TranscoderTimeout;
      _hlsSegmentTimeInSeconds = TranscodingServicePlugin.Settings.HLSSegmentTimeInSeconds;
      _subtitleDefaultLanguage = TranscodingServicePlugin.Settings.SubtitleDefaultLanguage;
      _subtitleDefaultEncoding = TranscodingServicePlugin.Settings.SubtitleDefaultEncoding;
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
        playlistPath = Path.Combine(Context.SegmentDir, PLAYLIST_TEMP_FILE_NAME);
      }
      else
      {
        playlistPath = Path.Combine(Context.SegmentDir, PLAYLIST_FILE_NAME);
      }
      DateTime waitStart = DateTime.Now;

      //Thread.Sleep(2000); //Ensure that writing is completed. Is there a better way?
      if (Path.GetExtension(PLAYLIST_FILE_NAME) == Path.GetExtension(FileName)) //playlist file
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

    protected abstract void GetVideoDimensions(VideoTranscoding video, out Size newSize, out Size newContentSize, out float newPixelAspectRatio, out bool pixelARChanged, out bool videoARChanged, out bool videoHeightChanged);

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
        GetVideoDimensions(TranscodingInfo, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);
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

    #region Cache

    protected void TouchFile(string filePath)
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

    protected void TouchDirectory(string folderPath)
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

          //Check for file age
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

          //Check for file size
          tryCount = 0;
          while (fileList.Count > 0 && cacheSize > _cacheMaximumSize && _cacheMaximumSize > 0 && tryCount < maxTries)
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

    protected SubtitleStream FindSubtitle(VideoTranscoding video)
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

    protected SubtitleStream ConvertSubtitleToUtf8(SubtitleStream sub, string targetFileName)
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

    protected abstract bool ExtractSubtitleFile(VideoTranscoding video, SubtitleStream subtitle, string subtitleEncoding, string targetFilePath, double timeStart);

    protected abstract bool ConvertSubtitleFile(string clientId, VideoTranscoding video, double timeStart, string transcodingFile, SubtitleStream sourceSubtitle, ref SubtitleStream res);

    protected SubtitleStream GetSubtitle(string clientId, VideoTranscoding video, double timeStart)
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
        if (ExtractSubtitleFile(video, sourceSubtitle, res.CharacterEncoding, transcodingFile, timeStart) && File.Exists(transcodingFile))
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

      if(ConvertSubtitleFile(clientId, video, timeStart, transcodingFile, sourceSubtitle, ref res))
      {
        return res;
      }
      return null;
    }

    #endregion

    #region Transcoding

    protected bool AssignExistingTranscodeContext(string clientId, string transcodeId, ref TranscodeContext context)
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

    public TranscodeContext GetLiveStream(string ClientId, BaseTranscoding TranscodingInfo, int ChannelId, bool WaitForBuffer)
    {
      TranscodingInfo.SourceMedia = new TranscodeLiveAccessor(ChannelId);
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

    protected abstract TranscodeContext TranscodeVideo(string clientId, VideoTranscoding video, double timeStart, double timeDuration, bool waitForBuffer);

    protected abstract TranscodeContext TranscodeAudio(string clientId, AudioTranscoding audio, double timeStart, double timeDuration, bool waitForBuffer);

    protected abstract TranscodeContext TranscodeImage(string clientId, ImageTranscoding image, bool waitForBuffer);

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

    protected void AddTranscodeContext(string clientId, string transcodeId, TranscodeContext context)
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

    protected void RemoveTranscodeContext(string clientId, string transcodeId, TranscodeContext context)
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

    #endregion
  }
}
