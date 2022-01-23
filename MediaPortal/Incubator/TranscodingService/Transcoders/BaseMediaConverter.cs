#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Collections.Concurrent;
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
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using MediaPortal.Extensions.TranscodingService.Interfaces.Analyzers;
using MediaPortal.Extensions.TranscodingService.Interfaces.SlimTv;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Utilities.Threading;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders
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

    private const int FILE_STREAM_TIMEOUT = 30000;

    private readonly static Regex SRT_LINE_REGEX = new Regex(@"(?<sequence>\d+)\r\n(?<start>\d{2}\:\d{2}\:\d{2},\d{3}) --\> (?<end>\d{2}\:\d{2}\:\d{2},\d{3})\r\n(?<text>[\s\S]*?\r\n\r\n)",
     RegexOptions.Compiled | RegexOptions.ECMAScript);

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

    protected ConcurrentDictionary<string, Dictionary<string, List<TranscodeContext>>> _runningClientTranscodes = new ConcurrentDictionary<string, Dictionary<string, List<TranscodeContext>>>();
    protected AsyncReaderWriterLock _transcodeLock = new AsyncReaderWriterLock();
    protected SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
    protected string _cachePath;
    protected long _cacheMaximumSize;
    protected long _cacheMaximumAge;
    protected bool _cacheEnabled;
    protected int _transcoderMaximumThreads;
    protected int _transcoderTimeout;
    protected int _hlsSegmentTimeInSeconds;
    protected string _subtitleDefaultEncoding;
    protected ILogger _logger;
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
      _subtitleDefaultEncoding = TranscodingServicePlugin.Settings.SubtitleDefaultEncoding;
    }

    #region HLS

    public long GetSegmentSequence(string fileName)
    {
      long sequenceNumber = -1;
      long.TryParse(Path.GetFileNameWithoutExtension(fileName), out sequenceNumber);
      return sequenceNumber;
    }

    public async Task<(Stream FileData, dynamic ContainerEnum)?> GetSegmentFileAsync(VideoTranscoding transcodingInfo, TranscodeContext context, string fileName)
    {
      (Stream FileData, dynamic ContainerEnum)? nullVal = null;
      try
      {
        string completePath = Path.Combine(context.SegmentDir, fileName);
        string playlistPath = null;
        if (transcodingInfo.TargetIsLive == false)
        {
          playlistPath = Path.Combine(context.SegmentDir, PLAYLIST_TEMP_FILE_NAME);
        }
        else
        {
          playlistPath = Path.Combine(context.SegmentDir, PLAYLIST_FILE_NAME);
        }
        DateTime waitStart = DateTime.UtcNow;

        //Ensure that writing is completed. Is there a better way?
        if (Path.GetExtension(PLAYLIST_FILE_NAME) == Path.GetExtension(fileName)) //playlist file
        {
          while (!File.Exists(completePath))
          {
            if ((DateTime.UtcNow - waitStart).TotalMilliseconds > HLS_PLAYLIST_TIMEOUT)
              return nullVal;

            await Task.Delay(10).ConfigureAwait(false);
          }

          return (await PlaylistManifest.CorrectPlaylistUrlsAsync(transcodingInfo.HlsBaseUrl, completePath).ConfigureAwait(false), VideoContainer.Hls);
        }
        if (Path.GetExtension(HLS_SEGMENT_FILE_TEMPLATE) == Path.GetExtension(fileName)) //segment file
        {
          long sequenceNumber = GetSegmentSequence(fileName);
          while (context.Running)
          {
            if (!File.Exists(completePath))
            {
              if (context.CurrentSegment > sequenceNumber)
                return nullVal; // Probably rewinding
              if ((sequenceNumber - context.CurrentSegment) > 2)
                return nullVal; //Probably forwarding
            }
            else
            {
              //If playlist generated by ffmpeg contains the file it must be done
              if (File.Exists(playlistPath))
              {
                bool fileFound = false;
                using (StreamReader reader = new StreamReader(playlistPath, Encoding.UTF8))
                {
                  while (!reader.EndOfStream)
                  {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line.Contains(fileName))
                    {
                      fileFound = true;
                      break;
                    }
                  }
                }
                if (fileFound)
                  break;
              }
            }
            if ((DateTime.UtcNow - waitStart).TotalSeconds > _hlsSegmentTimeInSeconds)
              return nullVal;

            await Task.Delay(10).ConfigureAwait(false);
          }

          if (sequenceNumber >= 0)
            context.CurrentSegment = sequenceNumber;
          return (await GetFileStreamAsync(completePath).ConfigureAwait(false), VideoContainer.Mpeg2Ts);
        }
        if (Path.GetExtension(HLS_SEGMENT_SUB_TEMPLATE) == Path.GetExtension(fileName)) //subtitle file
        {
          while (!File.Exists(completePath))
          {
            if (!context.Running)
              return nullVal;
            if ((DateTime.UtcNow - waitStart).TotalMilliseconds > _hlsSegmentTimeInSeconds)
              return nullVal;

            await Task.Delay(10).ConfigureAwait(false);
          }

          return (await GetFileStreamAsync(completePath).ConfigureAwait(false), SubtitleCodec.WebVtt);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error getting segment file '{0}'", ex, fileName);
      }
      return nullVal;
    }

    #endregion

    #region Metadata

    public TranscodedAudioMetadata GetTranscodedAudioMetadata(AudioTranscoding transcodingInfo)
    {
      TranscodedAudioMetadata metadata = new TranscodedAudioMetadata
      {
        TargetAudioBitrate = transcodingInfo.TargetAudioBitrate,
        TargetAudioCodec = transcodingInfo.TargetAudioCodec,
        TargetAudioContainer = transcodingInfo.TargetAudioContainer,
        TargetAudioFrequency = transcodingInfo.TargetAudioFrequency
      };
      if (transcodingInfo.TargetForceCopy)
      {
        metadata.TargetAudioBitrate = transcodingInfo.SourceAudioBitrate;
        metadata.TargetAudioCodec = transcodingInfo.SourceAudioCodec;
        metadata.TargetAudioContainer = transcodingInfo.SourceAudioContainer;
        metadata.TargetAudioFrequency = transcodingInfo.SourceAudioFrequency;
        metadata.TargetAudioChannels = transcodingInfo.SourceAudioChannels;
      }
      if (transcodingInfo.TargetAudioContainer == AudioContainer.Unknown)
      {
        metadata.TargetAudioContainer = transcodingInfo.SourceAudioContainer;
      }

      if (transcodingInfo.TargetForceCopy == false)
      {
        if (Checks.IsAudioStreamChanged(0, transcodingInfo))
        {
          if (transcodingInfo.TargetAudioCodec == AudioCodec.Unknown)
          {
            switch (transcodingInfo.TargetAudioContainer)
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
                metadata.TargetAudioCodec = transcodingInfo.SourceAudioCodec;
                break;
            }
          }
          long? frequency = Validators.GetAudioFrequency(transcodingInfo.SourceAudioCodec, transcodingInfo.TargetAudioCodec, transcodingInfo.SourceAudioFrequency, transcodingInfo.TargetAudioFrequency);
          if (frequency > 0)
          {
            metadata.TargetAudioFrequency = frequency;
          }
          if (transcodingInfo.TargetAudioContainer != AudioContainer.Lpcm)
          {
            metadata.TargetAudioBitrate = Validators.GetAudioBitrate(transcodingInfo.SourceAudioBitrate, transcodingInfo.TargetAudioBitrate);
          }
        }
        metadata.TargetAudioChannels = Validators.GetAudioNumberOfChannels(transcodingInfo.SourceAudioCodec, transcodingInfo.TargetAudioCodec, transcodingInfo.SourceAudioChannels, transcodingInfo.TargetForceAudioStereo);
      }
      return metadata;
    }

    public TranscodedImageMetadata GetTranscodedImageMetadata(ImageTranscoding transcodingInfo)
    {
      TranscodedImageMetadata metadata = new TranscodedImageMetadata
      {
        TargetMaxHeight = transcodingInfo.SourceHeight,
        TargetMaxWidth = transcodingInfo.SourceWidth,
        TargetOrientation = transcodingInfo.SourceOrientation,
        TargetImageCodec = transcodingInfo.TargetImageCodec
      };
      if (metadata.TargetImageCodec == ImageContainer.Unknown)
      {
        metadata.TargetImageCodec = transcodingInfo.SourceImageCodec;
      }
      metadata.TargetPixelFormat = transcodingInfo.TargetPixelFormat;
      if (metadata.TargetPixelFormat == PixelFormat.Unknown)
      {
        metadata.TargetPixelFormat = transcodingInfo.SourcePixelFormat;
      }
      if (Checks.IsImageStreamChanged(transcodingInfo) == true)
      {
        metadata.TargetMaxHeight = transcodingInfo.SourceHeight;
        metadata.TargetMaxWidth = transcodingInfo.SourceWidth;
        if (metadata.TargetMaxHeight > transcodingInfo.TargetHeight && transcodingInfo.TargetHeight > 0)
        {
          double scale = (double)transcodingInfo.SourceWidth / (double)transcodingInfo.SourceHeight;
          metadata.TargetMaxHeight = transcodingInfo.TargetHeight;
          metadata.TargetMaxWidth = Convert.ToInt32(scale * (double)metadata.TargetMaxHeight);
        }
        if (metadata.TargetMaxWidth > transcodingInfo.TargetWidth && transcodingInfo.TargetWidth > 0)
        {
          double scale = (double)transcodingInfo.SourceHeight / (double)transcodingInfo.SourceWidth;
          metadata.TargetMaxWidth = transcodingInfo.TargetWidth;
          metadata.TargetMaxHeight = Convert.ToInt32(scale * (double)metadata.TargetMaxWidth);
        }

        if (transcodingInfo.TargetAutoRotate == true)
        {
          if (transcodingInfo.SourceOrientation > 4)
          {
            int? tempWidth = metadata.TargetMaxWidth.Value;
            metadata.TargetMaxWidth = metadata.TargetMaxHeight;
            metadata.TargetMaxHeight = tempWidth;
          }
          metadata.TargetOrientation = 0;
        }
      }
      return metadata;
    }

    protected abstract void GetVideoDimensions(VideoTranscoding video, out Size newSize, out Size newContentSize, out float newPixelAspectRatio, out bool pixelARChanged, out bool videoARChanged, out bool videoHeightChanged);

    public TranscodedVideoMetadata GetTranscodedVideoMetadata(VideoTranscoding transcodingInfo)
    {
      VideoContainer srcContainer = transcodingInfo.SourceVideoContainer;
      VideoStream srcVideo = transcodingInfo.SourceVideoStream;
      AudioStream srcAudio = transcodingInfo.FirstSourceAudioStream;
      
      //Handle video without audio track
      if (srcAudio == null)
      {
        srcAudio = new AudioStream
        {
          Codec = AudioCodec.Unknown,
          Channels = 0,
          Frequency = 0,
          Bitrate = 0
        };
      }

      TranscodedVideoMetadata metadata = new TranscodedVideoMetadata
      {
        TargetAudioBitrate = transcodingInfo.TargetAudioBitrate ?? srcAudio.Bitrate,
        TargetAudioCodec = transcodingInfo.TargetAudioCodec == AudioCodec.Unknown ? srcAudio.Codec : transcodingInfo.TargetAudioCodec,
        TargetAudioFrequency = transcodingInfo.TargetAudioFrequency ?? srcAudio.Frequency,
        TargetVideoFrameRate = srcVideo.Framerate,
        TargetLevel = transcodingInfo.TargetLevel,
        TargetPreset = transcodingInfo.TargetPreset,
        TargetProfile = transcodingInfo.TargetProfile,
        TargetVideoPixelFormat = transcodingInfo.TargetPixelFormat
      };
      if (transcodingInfo.TargetForceVideoCopy)
      {
        metadata.TargetVideoContainer = srcContainer;
        metadata.TargetVideoAspectRatio = srcVideo.AspectRatio;
        metadata.TargetVideoBitrate = srcVideo.Bitrate;
        metadata.TargetVideoCodec = srcVideo.Codec;
        metadata.TargetVideoFrameRate = srcVideo.Framerate;
        metadata.TargetVideoPixelFormat = srcVideo.PixelFormatType;
        metadata.TargetVideoMaxWidth = srcVideo.Width;
        metadata.TargetVideoMaxHeight = srcVideo.Height;
      }
      if (transcodingInfo.TargetForceAudioCopy)
      {
        metadata.TargetAudioBitrate = srcAudio.Bitrate;
        metadata.TargetAudioCodec = srcAudio.Codec;
        metadata.TargetAudioFrequency = srcAudio.Frequency;
        metadata.TargetAudioChannels = srcAudio.Channels;
      }

      metadata.TargetVideoMaxWidth = srcVideo.Width;
      metadata.TargetVideoMaxHeight = srcVideo.Height ?? 1080;
      metadata.TargetVideoAspectRatio = transcodingInfo.TargetVideoAspectRatio ?? 16.0F / 9.0F;
      metadata.TargetVideoBitrate = transcodingInfo.TargetVideoBitrate;
      metadata.TargetVideoCodec = transcodingInfo.TargetVideoCodec == VideoCodec.Unknown ? srcVideo.Codec : transcodingInfo.TargetVideoCodec;
      metadata.TargetVideoContainer = transcodingInfo.TargetVideoContainer == VideoContainer.Unknown ? srcContainer : transcodingInfo.TargetVideoContainer;
      metadata.TargetVideoTimestamp = Timestamp.None;
      if (metadata.TargetVideoContainer == VideoContainer.M2Ts)
        metadata.TargetVideoTimestamp = Timestamp.Valid;
      if (metadata.TargetVideoPixelFormat == PixelFormat.Unknown)
        metadata.TargetVideoPixelFormat = PixelFormat.Yuv420;

      if (transcodingInfo.TargetForceVideoCopy == false)
      {
        float newPixelAspectRatio = 1.0F;
        if (srcVideo.PixelAspectRatio.HasValue)
          newPixelAspectRatio = srcVideo.PixelAspectRatio.Value;

        Size newSize = new Size(srcVideo.Width ?? 0, srcVideo.Height ?? 0);
        Size newContentSize = new Size(srcVideo.Width ?? 0, srcVideo.Height ?? 0);
        bool pixelARChanged = false;
        bool videoARChanged = false;
        bool videoHeightChanged = false;
        GetVideoDimensions(transcodingInfo, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);
        metadata.TargetVideoPixelAspectRatio = newPixelAspectRatio;
        metadata.TargetVideoMaxWidth = newSize.Width;
        metadata.TargetVideoMaxHeight = newSize.Height;
        metadata.TargetVideoFrameRate = Validators.GetNormalizedFramerate(srcVideo.Framerate);
      }
      if (transcodingInfo.TargetForceAudioCopy == false)
      {
        metadata.TargetAudioChannels = Validators.GetAudioNumberOfChannels(srcAudio.Codec, transcodingInfo.TargetAudioCodec, srcAudio.Channels, transcodingInfo.TargetForceAudioStereo);
        long? frequency = Validators.GetAudioFrequency(srcAudio.Codec, transcodingInfo.TargetAudioCodec, srcAudio.Frequency, transcodingInfo.TargetAudioFrequency);
        if (frequency.HasValue)
        {
          metadata.TargetAudioFrequency = frequency;
        }
        if (transcodingInfo.TargetAudioCodec != AudioCodec.Lpcm)
        {
          metadata.TargetAudioBitrate = Validators.GetAudioBitrate(srcAudio.Bitrate, transcodingInfo.TargetAudioBitrate);
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

    public Task<bool> IsTranscodeRunningAsync(string clientId, string transcodeId)
    {
      return Task.FromResult(_runningClientTranscodes.Any(t => t.Key == clientId && t.Value.ContainsKey(transcodeId)));
    }

    public async Task StopTranscodeAsync(string clientId, string transcodeId)
    {
      using (await _transcodeLock.WriterLockAsync().ConfigureAwait(false))
      {
        if (_runningClientTranscodes.TryGetValue(clientId, out var clientTranscodings) && clientTranscodings.TryGetValue(transcodeId, out var transcodings))
        {
          foreach (TranscodeContext context in transcodings)
          {
            try
            {
              context?.Dispose();
            }
            catch (Exception ex)
            {
              if (context?.Live ?? false) _logger.Error("MediaConverter: Error disposing transcode context for live stream", ex);
              else _logger.Error("MediaConverter: Error disposing transcode context for file '{0}'", ex, context?.TargetFile);
            }
          }
        }
      }
    }

    public async Task StopAllTranscodesAsync()
    {
      using (await _transcodeLock.WriterLockAsync().ConfigureAwait(false))
      {
        foreach (TranscodeContext context in _runningClientTranscodes.Values.SelectMany(t => t.Values.SelectMany(c => c)))
        {
          try
          {
            context?.Dispose();
          }
          catch (Exception ex)
          {
            if (context?.Live ?? false) _logger.Error("MediaConverter: Error disposing transcode context for live stream", ex);
            else _logger.Error("MediaConverter: Error disposing transcode context for file '{0}'", ex, context?.TargetFile);
          }
        }
      }
    }

    public async Task CleanUpTranscodeCacheAsync()
    {
      await _cacheLock.WaitAsync().ConfigureAwait(false);
      try
      {
        if (Directory.Exists(_cachePath) == true)
        {
          int maxTries = 10;
          SortedDictionary<DateTime, string> fileList = new SortedDictionary<DateTime, string>();
          long cacheSize = 0;
          List<string> dirObjects = new List<string>(Directory.GetFiles(_cachePath, "*.*"));
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
              cacheSize -= folderFiles.Sum(f => f.Length);
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
                if(!info.IsReadOnly && !info.Attributes.HasFlag(FileAttributes.System) && !info.Attributes.HasFlag(FileAttributes.Hidden))
                  info.Delete();
              }
              catch { }
            }
          }
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error cleaning cache", ex);
      }
      finally
      {
        _cacheLock.Release();
      }
    }

    #endregion

    #region Subtitles

    protected Dictionary<int, SubtitleStream> FindPrimarySubtitle(VideoTranscoding video)
    {
      if (video.SourceSubtitles == null)
        return null;
      if (video.TargetSubtitleLanguages == null)
        return null;

      Dictionary<int, SubtitleStream> currentEmbeddedSub = new Dictionary<int, SubtitleStream>();
      Dictionary<int, SubtitleStream> currentExternalSub = new Dictionary<int, SubtitleStream>();
      Dictionary<int, SubtitleStream> defaultEmbeddedSub = new Dictionary<int, SubtitleStream>();
      Dictionary<int, SubtitleStream> englishEmbeddedSub = new Dictionary<int, SubtitleStream>();
      Dictionary<int, SubtitleStream> subsEmbedded = new Dictionary<int, SubtitleStream>();
      Dictionary<int, SubtitleStream> langSubsEmbedded = new Dictionary<int, SubtitleStream>();
      Dictionary<int, SubtitleStream> defaultSub = new Dictionary<int, SubtitleStream>();
      Dictionary<int, SubtitleStream> englishSub = new Dictionary<int, SubtitleStream>();
      Dictionary<int, SubtitleStream> subs = new Dictionary<int, SubtitleStream>();
      Dictionary<int, SubtitleStream> langSubs = new Dictionary<int, SubtitleStream>();

      //Find embedded sub
      foreach (var mediaSourceIndex in video.SourceSubtitles.Keys)
      {
        foreach (var sub in video.SourceSubtitles.Where(s => s.Key == mediaSourceIndex).SelectMany(s => s.Value).Where(s => s.IsEmbedded))
        {
          if (sub.Default)
          {
            if(!defaultEmbeddedSub.ContainsKey(mediaSourceIndex))
              defaultEmbeddedSub.Add(mediaSourceIndex, sub);
          }
          else if (string.Compare(sub.Language, "EN", true, CultureInfo.InvariantCulture) == 0)
          {
            if (!englishEmbeddedSub.ContainsKey(mediaSourceIndex))
              englishEmbeddedSub.Add(mediaSourceIndex, sub);
          }
          if (video.TargetSubtitleLanguages.Any())
          {
            foreach (string lang in video.TargetSubtitleLanguages)
            {
              if (string.IsNullOrEmpty(lang) == false && string.Compare(sub.Language, lang, true, CultureInfo.InvariantCulture) == 0)
              {
                if (!langSubsEmbedded.ContainsKey(mediaSourceIndex))
                  langSubsEmbedded.Add(mediaSourceIndex, sub);
              }
            }
          }
          if (!subsEmbedded.ContainsKey(mediaSourceIndex))
            subsEmbedded.Add(mediaSourceIndex, sub);
        }

        if (!currentEmbeddedSub.ContainsKey(mediaSourceIndex) && langSubsEmbedded.ContainsKey(mediaSourceIndex))
        {
          currentEmbeddedSub.Add(mediaSourceIndex, langSubsEmbedded[mediaSourceIndex]);
        }

        //Find external sub
        foreach (SubtitleStream sub in video.SourceSubtitles.Where(s => s.Key == mediaSourceIndex).SelectMany(s => s.Value).Where(s => !s.IsEmbedded))
        {
          if (sub.Default == true)
          {
            if (!defaultSub.ContainsKey(mediaSourceIndex))
              defaultSub.Add(mediaSourceIndex, sub);
          }
          else if (string.Compare(sub.Language, "EN", true, CultureInfo.InvariantCulture) == 0)
          {
            if (!englishSub.ContainsKey(mediaSourceIndex))
              englishSub.Add(mediaSourceIndex, sub);
          }
          if (video.TargetSubtitleLanguages.Any())
          {
            foreach (string lang in video.TargetSubtitleLanguages)
            {
              if (string.IsNullOrEmpty(lang) == false && string.Compare(sub.Language, lang, true, CultureInfo.InvariantCulture) == 0)
              {
                if (!langSubs.ContainsKey(mediaSourceIndex))
                  langSubs.Add(mediaSourceIndex, sub);
              }
            }
          }
          if (!subs.ContainsKey(mediaSourceIndex))
            subs.Add(mediaSourceIndex, sub);
        }
        if (!currentExternalSub.ContainsKey(mediaSourceIndex) && langSubs.ContainsKey(mediaSourceIndex))
        {
          currentExternalSub.Add(mediaSourceIndex, langSubs[mediaSourceIndex]);
        }
      }

      //Best language subtitle
      if (currentExternalSub.Count > 0)
      {
        return currentExternalSub;
      }
      if (currentEmbeddedSub.Count > 0)
      {
        return currentEmbeddedSub;
      }

      //Best default subtitle
      if (currentExternalSub.Count == 0 && defaultSub.Count > 0)
      {
        currentExternalSub = defaultSub;
      }
      if (currentEmbeddedSub.Count == 0 && defaultEmbeddedSub.Count > 0)
      {
        currentEmbeddedSub = defaultEmbeddedSub;
      }
      if (currentExternalSub.Count > 0)
      {
        return currentExternalSub;
      }
      if (currentEmbeddedSub.Count > 0)
      {
        return currentEmbeddedSub;
      }

      //Best english
      if (currentExternalSub.Count == 0 && englishSub.Count > 0)
      {
        currentExternalSub = englishSub;
      }
      if (currentEmbeddedSub.Count == 0 && englishEmbeddedSub.Count > 0)
      {
        currentEmbeddedSub = englishEmbeddedSub;
      }
      if (currentExternalSub.Count > 0)
      {
        return currentExternalSub;
      }
      if (currentEmbeddedSub.Count > 0)
      {
        return currentEmbeddedSub;
      }

      //Best remaining subtitle
      if (currentExternalSub.Count == 0 && subs.Count > 0)
      {
        currentExternalSub = subs;
      }
      if (currentEmbeddedSub.Count == 0 && subsEmbedded.Count > 0)
      {
        currentEmbeddedSub = subsEmbedded;
      }
      if (currentExternalSub.Count > 0)
      {
        return currentExternalSub;
      }
      if (currentEmbeddedSub.Count > 0)
      {
        return currentEmbeddedSub;
      }
      return null;
    }

    protected async Task<SubtitleStream> ConvertSubtitleEncodingAsync(SubtitleStream sub, string targetFileName, string charEncoding)
    {
      try
      {
        if (string.IsNullOrEmpty(charEncoding))
          charEncoding = "UTF-8";

        string sourceCharEncoding = sub.CharacterEncoding;
        if (string.IsNullOrEmpty(sourceCharEncoding))
          sourceCharEncoding = _subtitleDefaultEncoding;

        if (!SubtitleHelper.SubtitleIsUnicode(sourceCharEncoding) && !SubtitleHelper.SubtitleIsImage(sourceCharEncoding))
        {
          if (!string.IsNullOrEmpty(sourceCharEncoding) && !SubtitleAnalyzer.IsImageBasedSubtitle(sub.Codec))
          {
            string path = Path.GetDirectoryName(targetFileName);
            if (!Directory.Exists(path))
              Directory.CreateDirectory(path);

            string sourceFileName = sub.GetFileSystemPath();
            using (var sourceReader = new StreamReader(sourceFileName, Encoding.GetEncoding(sourceCharEncoding)))
            using (var targetWriter = new StreamWriter(targetFileName, false, Encoding.GetEncoding(charEncoding)))
            {
              while (!sourceReader.EndOfStream)
                await targetWriter.WriteLineAsync(await sourceReader.ReadLineAsync());
            };
            sub.CharacterEncoding = charEncoding;
            sub.SourcePath = LocalFsResourceProviderBase.ToProviderPath(targetFileName);
            _logger.Debug("MediaConverter: Converted subtitle file '{0}' to {1}", sourceFileName, charEncoding);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error converting subtitle", ex);
      }
      return sub;
    }

    public async Task<StreamContext> GetSubtitleStreamAsync(string clientId, VideoTranscoding transcodingInfo)
    {
      try
      {
        Dictionary<int, List<SubtitleStream>> subs = await GetSubtitlesAsync(clientId, transcodingInfo, 0).ConfigureAwait(false);
        if (subs == null)
          return null;

        SubtitleStream sub = subs.SelectMany(s => s.Value).FirstOrDefault(s => !s.IsPartial);
        var subPath = sub?.GetFileSystemPath();
        if (!string.IsNullOrEmpty(subPath) && File.Exists(subPath))
        {
          if (await IsTranscodeRunningAsync(clientId, transcodingInfo.TranscodeId).ConfigureAwait(false) == false)
          {
            if (subPath.StartsWith(_cachePath, StringComparison.InvariantCultureIgnoreCase))
              TouchFile(subPath);
          }
          return new StreamContext() { Stream = await GetFileStreamAsync(subPath).ConfigureAwait(false) };
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error getting subtitle stream", ex);
      }
      return null;
    }

    protected abstract Task<bool> ExtractSubtitleFileAsync(int sourceMediaIndex, VideoTranscoding video, SubtitleStream subtitle, string subtitleEncoding, string targetFilePath, double timeStart);

    protected abstract Task<bool> ConvertSubtitleFileAsync(string clientId, VideoTranscoding video, double timeStart, string transcodingFile, SubtitleStream sourceSubtitle, SubtitleStream res);

    protected async Task<Dictionary<int, List<SubtitleStream>>> GetSubtitlesAsync(string clientId, VideoTranscoding video, double timeStart)
    {
      try
      {
        if (video.TargetSubtitleSupport == SubtitleSupport.None)
          return new Dictionary<int, List<SubtitleStream>>();

        Dictionary<int, List<SubtitleStream>> sourceSubtitles = new Dictionary<int, List<SubtitleStream>>();
        Dictionary<int, SubtitleStream> primarySubs = FindPrimarySubtitle(video);
        if (primarySubs == null)
          return new Dictionary<int, List<SubtitleStream>>();
        foreach (var primarySub in primarySubs)
        {
          sourceSubtitles.Add(primarySub.Key, new List<SubtitleStream>() { primarySub.Value });
        }

        Dictionary<int, List<SubtitleStream>> allSubs = video.SourceSubtitles;
        foreach (var srcSub in allSubs)
        {
          if (sourceSubtitles.ContainsKey(srcSub.Key))
          {
            sourceSubtitles[srcSub.Key].AddRange(srcSub.Value.Where(s => !sourceSubtitles[srcSub.Key].Contains(s)));
          }
        }

        Dictionary<int, List<SubtitleStream>> res = new Dictionary<int, List<SubtitleStream>>();
        foreach (var sourceMediaIndex in sourceSubtitles.Keys)
        {
          res.Add(sourceMediaIndex, new List<SubtitleStream>());
          foreach (var srcStream in sourceSubtitles[sourceMediaIndex])
          {
            SubtitleStream sub = new SubtitleStream
            {
              StreamIndex = srcStream.StreamIndex,
              Codec = srcStream.Codec,
              Language = srcStream.Language,
              SourcePath = srcStream.SourcePath,
              CharacterEncoding = string.IsNullOrEmpty(srcStream.CharacterEncoding) ? _subtitleDefaultEncoding : srcStream.CharacterEncoding,
              IsPartial = video.SourceMediaPaths.Count > 1
            };
            if (SubtitleAnalyzer.IsSubtitleSupportedByContainer(srcStream.Codec, video.SourceVideoContainer, video.TargetVideoContainer) == true)
            {
              if (srcStream.IsEmbedded)
              {
                //Subtitle stream can be copied directly
                if (!res.ContainsKey(sourceMediaIndex))
                  res.Add(sourceMediaIndex, new List<SubtitleStream>());
                res[sourceMediaIndex].Add(sub);
                continue;
              }
            }

            SubtitleCodec targetCodec = video.TargetSubtitleCodec;
            if (targetCodec == SubtitleCodec.Unknown)
            {
              targetCodec = srcStream.Codec;
            }

            // Create a file name for the output file which contains the subtitle informations
            string transcodingFile = GetSubtitleTranscodingFileName(video, timeStart, srcStream, targetCodec, sourceSubtitles.Count > 1 ? sourceMediaIndex : (int?)null);
            transcodingFile = Path.Combine(_cachePath, transcodingFile);

            // The file already exists in the cache -> just return
            if (File.Exists(transcodingFile))
            {
              if (await IsTranscodeRunningAsync(clientId, video.TranscodeId).ConfigureAwait(false) == false)
              {
                TouchFile(transcodingFile);
              }
              sub.Codec = targetCodec;
              sub.SourcePath = LocalFsResourceProviderBase.ToProviderPath(transcodingFile);
              sub.CharacterEncoding = video.TargetSubtitleCharacterEncoding;
              if (!res[sourceMediaIndex].Any(s => s.SourcePath.Equals(sub.SourcePath, StringComparison.InvariantCultureIgnoreCase)))
                res[sourceMediaIndex].Add(sub);
              continue;
            }

            // Subtitle is embedded in the source file
            if (srcStream.IsEmbedded)
            {
              if (await ExtractSubtitleFileAsync(sourceMediaIndex, video, srcStream, sub.CharacterEncoding, transcodingFile, timeStart).ConfigureAwait(false) && File.Exists(transcodingFile))
              {
                sub.StreamIndex = -1;
                sub.Codec = targetCodec;
                sub.SourcePath = LocalFsResourceProviderBase.ToProviderPath(transcodingFile);
                res[sourceMediaIndex].Add(sub);
                continue;
              }
            }

            // SourceSubtitle == TargetSubtitleCodec -> just return
            if (video.TargetSubtitleCodec != SubtitleCodec.Unknown && video.TargetSubtitleCodec == srcStream.Codec && timeStart == 0)
            {
              sub = await ConvertSubtitleEncodingAsync(sub, transcodingFile, video.TargetSubtitleCharacterEncoding).ConfigureAwait(false);
              res[sourceMediaIndex].Add(sub);
              continue;
            }

            // Burn external subtitle into video
            if (string.IsNullOrEmpty(sub?.SourcePath))
            {
              return new Dictionary<int, List<SubtitleStream>>();
            }

            if (await ConvertSubtitleFileAsync(clientId, video, timeStart, transcodingFile, srcStream, sub).ConfigureAwait(false))
            {
              res[sourceMediaIndex].Add(sub);
              continue;
            }
          }
        }

        //Merge srt subtitles if necessary and possible
        Dictionary<int, SubtitleStream> partSrtSubs = new Dictionary<int, SubtitleStream>();
        foreach (var key in res.Keys)
        {
          if (res[key].Any(s => s.SourcePath != null && s.Codec == SubtitleCodec.Srt && s.IsPartial))
            partSrtSubs.Add(key, res[key].First(s => !string.IsNullOrEmpty(s?.SourcePath) && s.Codec == SubtitleCodec.Srt && s.IsPartial));
        }
        if (partSrtSubs.Count() > 1)
        {
          string transcodingFile = GetSubtitleTranscodingFileName(video, timeStart, partSrtSubs.First().Value, SubtitleCodec.Srt);
          transcodingFile = Path.Combine(_cachePath, transcodingFile);

          if (await MergeSrtSubtitlesAsync(transcodingFile, partSrtSubs, video.SourceMediaDurations.ToDictionary(d => d.Key, d => d.Value.TotalSeconds), timeStart).ConfigureAwait(false))
          {
            res.Add(-1, new List<SubtitleStream>()
            {
              new SubtitleStream
              {
                CharacterEncoding = partSrtSubs.First().Value.CharacterEncoding,
                Codec = SubtitleCodec.Srt,
                Language = partSrtSubs.First().Value.Language,
                StreamIndex = -1,
                SourcePath = LocalFsResourceProviderBase.ToProviderPath(transcodingFile),
                IsPartial = false
              }
            });
          }
        }

        return res;
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error getting subtitle", ex);
      }
      return new Dictionary<int, List<SubtitleStream>>();
    }

    protected async Task<bool> MergeSrtSubtitlesAsync(string mergeFile, Dictionary<int, SubtitleStream> subtitles, Dictionary<int, double> subtitleTimeOffsets, double timeStart)
    {
      try
      {
        if (subtitles.Any(s => string.IsNullOrEmpty(s.Value?.SourcePath) || s.Value.Codec != SubtitleCodec.Srt))
          return false;

        int sequence = 0;
        using (StreamWriter output = new StreamWriter(mergeFile, false, Encoding.UTF8))
        {
          foreach (var sub in subtitles)
          {
            using (StreamReader input = new StreamReader(sub.Value.GetFileSystemPath(), Encoding.UTF8))
            {
              await output.WriteAsync(SRT_LINE_REGEX.Replace(await input.ReadToEndAsync().ConfigureAwait(false), (m) =>
              {
                if ((subtitleTimeOffsets[sub.Key] - timeStart) >= 0)
                {
                  return m.Value.Replace($@"{m.Groups["sequence"].Value}\r\n{m.Groups["start"].Value} --> {m.Groups["end"].Value}\r\n",
                    string.Format("{0}\r\n{1:HH\\:mm\\:ss\\,fff} --> {2:HH\\:mm\\:ss\\,fff}\r\n",
                        sequence++,
                        DateTime.Parse(m.Groups["start"].Value.Replace(",", ".")).AddSeconds(subtitleTimeOffsets[sub.Key] - timeStart),
                        DateTime.Parse(m.Groups["end"].Value.Replace(",", ".")).AddSeconds(subtitleTimeOffsets[sub.Key] - timeStart)));
                }
                else
                {
                  return "";
                }
              })).ConfigureAwait(false);
            }
          }
        }
        return File.Exists(mergeFile);
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error merging subtitle", ex);
      }
      return false;
    }

    #endregion

    #region Transcoding

    protected string GetSubtitleTranscodingFileName(VideoTranscoding video, double timeStart, SubtitleStream sourceSubtitle, SubtitleCodec targetCodec, int? sourceMediaIndex = null)
    {
      string transcodingFile = Path.GetFileNameWithoutExtension(GetTranscodingVideoFileName(video, timeStart, false));
      if (sourceMediaIndex.HasValue)
      {
        transcodingFile += "." + sourceMediaIndex;
      }
      if (string.IsNullOrEmpty(sourceSubtitle.Language) == false)
      {
        transcodingFile += "." + sourceSubtitle.Language;
      }
      transcodingFile += "." + SubtitleHelper.GetSubtitleExtension(targetCodec);
      return transcodingFile;
    }

    protected string GetTranscodingAudioFileName(AudioTranscoding audio, double timeStart)
    {
      string transcodingFile = audio.TranscodeId;
      if (timeStart > 0)
      {
        transcodingFile += "." + Convert.ToInt64(timeStart).ToString();
      }
      transcodingFile += "." + AudioHelper.GetAudioExtension(audio.TargetAudioContainer);
      return transcodingFile;
    }

    protected string GetTranscodingImageFileName(ImageTranscoding image)
    {
      string transcodingFile = image.TranscodeId;
      transcodingFile += "." + ImageHelper.GetImageExtension(image.TargetImageCodec);
      return transcodingFile;
    }

    protected string GetTranscodingVideoFileName(VideoTranscoding video, double timeStart, bool embeddedSupported)
    {
      string transcodingFile = video.TranscodeId;
      if (timeStart > 0)
      {
        transcodingFile += "." + Convert.ToInt64(timeStart).ToString();
      }
      else
      {
        if (video.FirstSourceAudioStream != null)
          transcodingFile += ".A" + video.FirstSourceAudioStream.StreamIndex;
        if (video.TargetAudioMultiTrackSupport && video.SourceAudioStreams.Count > 1)
          transcodingFile += ".MultiA";
        if ((video.PreferredSourceSubtitles?.Any() ?? false) && video.TargetSubtitleSupport == SubtitleSupport.HardCoded)
        {
          string subLanguage = video.FirstPreferredSourceSubtitle.Language;
          if (string.IsNullOrEmpty(subLanguage) == false)
          {
            transcodingFile += ".HC" + subLanguage;
          }
          else
          {
            transcodingFile += ".HC";
          }
        }
        else if ((video.PreferredSourceSubtitles?.Any() ?? false) && embeddedSupported)
        {
          transcodingFile += ".MultiS";
        }
      }
      transcodingFile += "." + VideoHelper.GetVideoExtension(video.TargetVideoContainer);
      return transcodingFile;
    }

    protected async Task<TranscodeContext> GetExistingTranscodeContextAsync(string clientId, string transcodeId)
    {
      try
      {
        using (await _transcodeLock.ReaderLockAsync().ConfigureAwait(false))
        {
          if (_runningClientTranscodes.TryGetValue(clientId, out var clientTranscodings) && clientTranscodings.TryGetValue(transcodeId, out var transcodings))
          {
            //Non partial have first priority
            return transcodings?.FirstOrDefault(c => !c.Partial);
          }
          return null;
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error assigning context", ex);
      }
      return null;
    }

    public async Task<TranscodeContext> GetLiveStreamAsync(string clientId, BaseTranscoding transcodingInfo, int channelId, bool waitForBuffer)
    {
      try
      {
        transcodingInfo.SourceMediaPaths = new Dictionary<int, string> { { 0, (new TranscodeLiveAccessor(channelId)).CanonicalLocalResourcePath.Serialize() } };
        if (transcodingInfo is AudioTranscoding at)
        {
          at.TargetIsLive = true;
          return await TranscodeAudioAsync(clientId, at, 0, 0, waitForBuffer);
        }
        else if (transcodingInfo is VideoTranscoding vt)
        {
          vt.TargetIsLive = true;
          return await TranscodeVideoAsync(clientId, vt, 0, 0, waitForBuffer);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error getting live stream", ex);
      }
      return null;
    }

    public async Task<TranscodeContext> GetMediaStreamAsync(string clientId, BaseTranscoding transcodingInfo, double startTime, double duration, bool waitForBuffer)
    {
      try
      {
        if (transcodingInfo == null)
          return null;

        foreach (var path in transcodingInfo.SourceMediaPaths.Values)
        {
          var p = ResourcePath.Deserialize(path);
          if (p.TryCreateLocalResourceAccessor(out var res))
          {
            try
            {
              using (var rah = new LocalFsResourceAccessorHelper(res))
              {
                if (rah.LocalFsResourceAccessor.Exists)
                  continue;
              }
            }
            finally
            {
              res.Dispose();
            }
          }
          _logger.Error("MediaConverter: File '{0}' does not exist for transcode '{1}'", path, transcodingInfo.TranscodeId);
          return null;
        }
        if (transcodingInfo is ImageTranscoding it)
        {
          return await TranscodeImageAsync(clientId, it, waitForBuffer);
        }
        else if (transcodingInfo is AudioTranscoding at)
        {
          return await TranscodeAudioAsync(clientId, at, startTime, duration, waitForBuffer);
        }
        else if (transcodingInfo is VideoTranscoding vt)
        {
          return await TranscodeVideoAsync(clientId, vt, startTime, duration, waitForBuffer);
        }
        _logger.Error("MediaConverter: Transcoding info is not valid for transcode '{0}'", transcodingInfo.TranscodeId);
        return null;
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error getting stream", ex);
      }
      return null;
    }

    protected abstract Task<TranscodeContext> TranscodeVideoAsync(string clientId, VideoTranscoding video, double timeStart, double timeDuration, bool waitForBuffer);

    protected abstract Task<TranscodeContext> TranscodeAudioAsync(string clientId, AudioTranscoding audio, double timeStart, double timeDuration, bool waitForBuffer);

    protected abstract Task<TranscodeContext> TranscodeImageAsync(string clientId, ImageTranscoding image, bool waitForBuffer);

    public async Task<StreamContext> GetFileStreamAsync(ResourcePath filePath)
    {
      var context = new StreamContext();
      // Impersonation
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(filePath))
      {
        if (filePath.TryCreateLocalResourceAccessor(out var res))
        {
          var rah = new LocalFsResourceAccessorHelper(res);
          var accessToken = rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess();
          if (accessToken != null)
            context.StreamDisposables.Add(accessToken);
          context.StreamDisposables.Add(rah);
          context.StreamDisposables.Add(res);
          context.Stream = await GetFileStreamAsync(rah.LocalFsResourceAccessor.LocalFileSystemPath);
          return context;
        }
      }
      return null;
    }

    protected async Task<Stream> GetFileStreamAsync(string fileSystemPath)
    {
      try
      {
        DateTime waitStart = DateTime.UtcNow;
        long length = 0;
        while (!File.Exists(fileSystemPath) || length == 0)
        {
          try
          {
            if (File.Exists(fileSystemPath))
              length = new FileInfo(fileSystemPath).Length;
          }
          catch { }

          if ((DateTime.UtcNow - waitStart).TotalMilliseconds > FILE_STREAM_TIMEOUT)
          {
            _logger.Error("MediaConverter: Timed out waiting for ready file '{0}'", fileSystemPath);
            return null;
          }

          await Task.Delay(500).ConfigureAwait(false);
        }

        _logger.Debug(string.Format("MediaConverter: Serving ready file '{0}'", fileSystemPath));
        BufferedStream stream = new BufferedStream(new FileStream(fileSystemPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        return stream;
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error serving ready file '{0}'", ex, fileSystemPath);
      }
      return null;
    }

    #endregion

    #region Transcoder

    protected async Task AddTranscodeContextAsync(string clientId, string transcodeId, TranscodeContext context)
    {
      using (await _transcodeLock.WriterLockAsync().ConfigureAwait(false))
      {
        try
        {
          List<TranscodeContext> transcodings = new List<TranscodeContext>();
          if (_runningClientTranscodes.TryGetValue(clientId, out var clientTranscodings))
          {
            if (!context.Partial)
            {
              //Don't waste resources on transcoding if the client wants different media item
              _logger.Debug("MediaConverter: Ending {0} transcodes for client {1}", clientTranscodings.Count, clientId);
              foreach (var transcodeContext in clientTranscodings.Values.SelectMany(t => t))
              {
                transcodeContext?.Stop();
              }
              clientTranscodings.Clear();
              clientTranscodings.Add(transcodeId, transcodings);
            }
            else if (clientTranscodings.Count > 0)
            {
              //Don't waste resources on transcoding multiple partial transcodes
              if (clientTranscodings.TryGetValue(transcodeId, out transcodings))
              {
                _logger.Debug("MediaConverter: Ending partial transcodes for client {0}", clientId);
                foreach (var transcodeContext in transcodings.Where(c => c.Partial && c != context).ToList())
                {
                  transcodings.Remove(transcodeContext);
                  transcodeContext?.Stop();
                }
              }
              else
              {
                transcodings = new List<TranscodeContext>();
                clientTranscodings.Add(transcodeId, transcodings);
              }
            }
          }
          else
          {
            clientTranscodings = new Dictionary<string, List<TranscodeContext>>();
            if (_runningClientTranscodes.TryAdd(clientId, clientTranscodings))
            {
              clientTranscodings.Add(transcodeId, transcodings);
            }
          }
          transcodings.Add(context);
        }
        catch (Exception ex)
        {
          _logger.Error("MediaConverter: Error adding context for '{0}'", ex, transcodeId);
        }
      }
    }

    protected async Task RemoveTranscodeContextAsync(string clientId, string transcodeId, TranscodeContext context)
    {
      using (await _transcodeLock.WriterLockAsync().ConfigureAwait(false))
      {
        try
        {
          context?.Stop();
          if (_runningClientTranscodes.TryGetValue(clientId, out var clientTranscodings))
          {
            if (clientTranscodings.TryGetValue(transcodeId, out var transcodings))
            {
              transcodings.Remove(context);
              if (transcodings.Count == 0)
                clientTranscodings.Remove(transcodeId);
            }

            if (clientTranscodings.Count == 0)
              _runningClientTranscodes.TryRemove(clientId, out _);
          }
        }
        catch (Exception ex)
        {
          _logger.Error("MediaConverter: Error removing context for '{0}'", ex, transcodeId);
        }
      }
    }

    #endregion
  }
}
