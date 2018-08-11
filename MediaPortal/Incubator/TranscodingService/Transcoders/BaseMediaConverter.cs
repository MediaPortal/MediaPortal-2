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
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using MediaPortal.Extensions.TranscodingService.Interfaces.Analyzers;
using MediaPortal.Extensions.TranscodingService.Interfaces.SlimTv;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

    protected Dictionary<string, Dictionary<string, List<TranscodeContext>>> _runningClientTranscodes = new Dictionary<string, Dictionary<string, List<TranscodeContext>>>();
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

    public long GetSegmentSequence(string FileName)
    {
      long sequenceNumber = -1;
      long.TryParse(Path.GetFileNameWithoutExtension(FileName), out sequenceNumber);
      return sequenceNumber;
    }

    public async Task<(Stream FileData, dynamic ContainerEnum)?> GetSegmentFileAsync(VideoTranscoding TranscodingInfo, TranscodeContext Context, string FileName)
    {
      (Stream FileData, dynamic ContainerEnum)? nullVal = null;
      try
      {
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

        //Ensure that writing is completed. Is there a better way?
        if (Path.GetExtension(PLAYLIST_FILE_NAME) == Path.GetExtension(FileName)) //playlist file
        {
          while (!File.Exists(completePath))
          {
            if ((DateTime.Now - waitStart).TotalMilliseconds > HLS_PLAYLIST_TIMEOUT)
              return nullVal;

            await Task.Delay(10).ConfigureAwait(false);
          }

          return (await PlaylistManifest.CorrectPlaylistUrlsAsync(TranscodingInfo.HlsBaseUrl, completePath).ConfigureAwait(false), VideoContainer.Hls);
        }
        if (Path.GetExtension(HLS_SEGMENT_FILE_TEMPLATE) == Path.GetExtension(FileName)) //segment file
        {
          long sequenceNumber = GetSegmentSequence(FileName);
          while (Context.Running)
          {
            if (!File.Exists(completePath))
            {
              if (Context.CurrentSegment > sequenceNumber)
                return nullVal; // Probably rewinding
              if ((sequenceNumber - Context.CurrentSegment) > 2)
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
                    if (line.Contains(FileName))
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
            if ((DateTime.Now - waitStart).TotalSeconds > _hlsSegmentTimeInSeconds)
              return nullVal;

            await Task.Delay(10).ConfigureAwait(false);
          }

          if (sequenceNumber >= 0)
            Context.CurrentSegment = sequenceNumber;
          return (await GetFileStreamAsync(completePath).ConfigureAwait(false), VideoContainer.Mpeg2Ts);
        }
        if (Path.GetExtension(HLS_SEGMENT_SUB_TEMPLATE) == Path.GetExtension(FileName)) //subtitle file
        {
          while (!File.Exists(completePath))
          {
            if (!Context.Running)
              return nullVal;
            if ((DateTime.Now - waitStart).TotalMilliseconds > _hlsSegmentTimeInSeconds)
              return nullVal;

            await Task.Delay(10).ConfigureAwait(false);
          }

          return (await GetFileStreamAsync(completePath).ConfigureAwait(false), SubtitleCodec.WebVtt);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error getting segment file '{0}'", ex, FileName);
      }
      return nullVal;
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
        if (Checks.IsAudioStreamChanged(0, 0, TranscodingInfo))
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
          long? frequency = Validators.GetAudioFrequency(TranscodingInfo.SourceAudioCodec, TranscodingInfo.TargetAudioCodec, TranscodingInfo.SourceAudioFrequency, TranscodingInfo.TargetAudioFrequency);
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

    public TranscodedVideoMetadata GetTranscodedVideoMetadata(VideoTranscoding TranscodingInfo)
    {
      VideoContainer srcContainer = TranscodingInfo.FirstSourceVideoContainer;
      VideoStream srcVideo = TranscodingInfo.FirstSourceVideoStream;
      AudioStream srcAudio = TranscodingInfo.FirstSourceAudioStream;
      
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
        TargetAudioBitrate = TranscodingInfo.TargetAudioBitrate ?? srcAudio.Bitrate,
        TargetAudioCodec = TranscodingInfo.TargetAudioCodec == AudioCodec.Unknown ? srcAudio.Codec : TranscodingInfo.TargetAudioCodec,
        TargetAudioFrequency = TranscodingInfo.TargetAudioFrequency ?? srcAudio.Frequency,
        TargetVideoFrameRate = srcVideo.Framerate,
        TargetLevel = TranscodingInfo.TargetLevel,
        TargetPreset = TranscodingInfo.TargetPreset,
        TargetProfile = TranscodingInfo.TargetProfile,
        TargetVideoPixelFormat = TranscodingInfo.TargetPixelFormat
      };
      if (TranscodingInfo.TargetForceVideoCopy)
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
      if (TranscodingInfo.TargetForceAudioCopy)
      {
        metadata.TargetAudioBitrate = srcAudio.Bitrate;
        metadata.TargetAudioCodec = srcAudio.Codec;
        metadata.TargetAudioFrequency = srcAudio.Frequency;
        metadata.TargetAudioChannels = srcAudio.Channels;
      }

      metadata.TargetVideoMaxWidth = srcVideo.Width;
      metadata.TargetVideoMaxHeight = srcVideo.Height ?? 1080;
      metadata.TargetVideoAspectRatio = TranscodingInfo.TargetVideoAspectRatio ?? 16.0F / 9.0F;
      metadata.TargetVideoBitrate = TranscodingInfo.TargetVideoBitrate;
      metadata.TargetVideoCodec = TranscodingInfo.TargetVideoCodec == VideoCodec.Unknown ? srcVideo.Codec : TranscodingInfo.TargetVideoCodec;
      metadata.TargetVideoContainer = TranscodingInfo.TargetVideoContainer == VideoContainer.Unknown ? srcContainer : TranscodingInfo.TargetVideoContainer;
      metadata.TargetVideoTimestamp = Timestamp.None;
      if (metadata.TargetVideoContainer == VideoContainer.M2Ts)
        metadata.TargetVideoTimestamp = Timestamp.Valid;
      if (metadata.TargetVideoPixelFormat == PixelFormat.Unknown)
        metadata.TargetVideoPixelFormat = PixelFormat.Yuv420;

      if (TranscodingInfo.TargetForceVideoCopy == false)
      {
        float newPixelAspectRatio = 1.0F;
        if (srcVideo.PixelAspectRatio.HasValue)
          newPixelAspectRatio = srcVideo.PixelAspectRatio.Value;

        Size newSize = new Size(srcVideo.Width ?? 0, srcVideo.Height ?? 0);
        Size newContentSize = new Size(srcVideo.Width ?? 0, srcVideo.Height ?? 0);
        bool pixelARChanged = false;
        bool videoARChanged = false;
        bool videoHeightChanged = false;
        GetVideoDimensions(TranscodingInfo, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);
        metadata.TargetVideoPixelAspectRatio = newPixelAspectRatio;
        metadata.TargetVideoMaxWidth = newSize.Width;
        metadata.TargetVideoMaxHeight = newSize.Height;
        metadata.TargetVideoFrameRate = Validators.GetNormalizedFramerate(srcVideo.Framerate);
      }
      if (TranscodingInfo.TargetForceAudioCopy == false)
      {
        metadata.TargetAudioChannels = Validators.GetAudioNumberOfChannels(srcAudio.Codec, TranscodingInfo.TargetAudioCodec, srcAudio.Channels, TranscodingInfo.TargetForceAudioStereo);
        long? frequency = Validators.GetAudioFrequency(srcAudio.Codec, TranscodingInfo.TargetAudioCodec, srcAudio.Frequency, TranscodingInfo.TargetAudioFrequency);
        if (frequency.HasValue)
        {
          metadata.TargetAudioFrequency = frequency;
        }
        if (TranscodingInfo.TargetAudioCodec != AudioCodec.Lpcm)
        {
          metadata.TargetAudioBitrate = Validators.GetAudioBitrate(srcAudio.Bitrate, TranscodingInfo.TargetAudioBitrate);
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

    public async Task<bool> IsTranscodeRunningAsync(string ClientId, string TranscodeId)
    {
      using (await _transcodeLock.ReaderLockAsync().ConfigureAwait(false))
      {
        return _runningClientTranscodes.Where(t => t.Key == ClientId && t.Value.ContainsKey(TranscodeId)).Any();
      }
    }

    public async Task StopTranscodeAsync(string ClientId, string TranscodeId)
    {
      using (await _transcodeLock.WriterLockAsync().ConfigureAwait(false))
      {
        foreach (TranscodeContext context in _runningClientTranscodes.Where(t => t.Key == ClientId && t.Value.ContainsKey(TranscodeId)).SelectMany(t => t.Value[TranscodeId]))
        {
          try
          {
            context.Dispose();
          }
          catch (Exception ex)
          {
            if (context.Live) _logger.Error("MediaConverter: Error disposing transcode context for live stream", ex);
            else _logger.Error("MediaConverter: Error disposing transcode context for file '{0}'", ex, context.TargetFile);
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
            context.Dispose();
          }
          catch (Exception ex)
          {
            if (context.Live) _logger.Error("MediaConverter: Error disposing transcode context for live stream", ex);
            else _logger.Error("MediaConverter: Error disposing transcode context for file '{0}'", ex, context.TargetFile);
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
      if (video.SourceSubtitles == null) return null;

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

      Dictionary<int, List<SubtitleStream>> allSubs = video.SourceSubtitles;
      //Find embedded sub
      foreach (var mediaSourceIndex in allSubs.Keys)
      {
        foreach (var sub in allSubs.Where(s => s.Key == mediaSourceIndex).SelectMany(s => s.Value).Where(s => s.IsEmbedded))
        {
          if (sub.Default == true)
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
        foreach (SubtitleStream sub in allSubs.Where(s => s.Key == mediaSourceIndex).SelectMany(s => s.Value).Where(s => !s.IsEmbedded))
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

            string sourceName = sub.Source;
            using (var sourceReader = new StreamReader(sourceName, Encoding.GetEncoding(sourceCharEncoding)))
            using (var targetWriter = new StreamWriter(targetFileName, false, Encoding.GetEncoding(charEncoding)))
            {
              while (!sourceReader.EndOfStream)
                await targetWriter.WriteLineAsync(await sourceReader.ReadLineAsync());
            };
            sub.CharacterEncoding = charEncoding;
            sub.Source = targetFileName;
            _logger.Debug("MediaConverter: Converted subtitle file '{0}' to {1}", sourceName, charEncoding);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error converting subtitle", ex);
      }
      return sub;
    }

    public async Task<Stream> GetSubtitleStreamAsync(string ClientId, VideoTranscoding TranscodingInfo)
    {
      try
      {
        Dictionary<int, List<SubtitleStream>> subs = await GetSubtitlesAsync(ClientId, TranscodingInfo, 0).ConfigureAwait(false);
        if (subs == null)
          return null;

        SubtitleStream sub = subs.SelectMany(s => s.Value).FirstOrDefault(s => !s.IsPartial);
        if (sub != null && File.Exists(sub.Source))
        {
          if (await IsTranscodeRunningAsync(ClientId, TranscodingInfo.TranscodeId).ConfigureAwait(false) == false)
          {
            TouchFile(sub.Source);
          }
          return await GetFileStreamAsync(sub.Source).ConfigureAwait(false);
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
          return null;

        Dictionary<int, List<SubtitleStream>> sourceSubtitles = new Dictionary<int, List<SubtitleStream>>();
        Dictionary<int, SubtitleStream> primarySubs = FindPrimarySubtitle(video);
        if (primarySubs == null) return null;
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
              Source = srcStream.Source,
              CharacterEncoding = string.IsNullOrEmpty(srcStream.CharacterEncoding) ? _subtitleDefaultEncoding : srcStream.CharacterEncoding,
              IsPartial = video.SourceMedia.Count > 1
            };
            if (SubtitleAnalyzer.IsSubtitleSupportedByContainer(srcStream.Codec, video.FirstSourceVideoContainer, video.TargetVideoContainer) == true)
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

            // create a file name for the output file which contains the subtitle informations
            string transcodingFile = GetSubtitleTranscodingFileName(video, timeStart, srcStream, targetCodec, sourceSubtitles.Count > 1 ? sourceMediaIndex : (int?)null);
            transcodingFile = Path.Combine(_cachePath, transcodingFile);

            // the file already exists in the cache -> just return
            if (File.Exists(transcodingFile))
            {
              if (await IsTranscodeRunningAsync(clientId, video.TranscodeId).ConfigureAwait(false) == false)
              {
                TouchFile(transcodingFile);
              }
              sub.Codec = targetCodec;
              sub.Source = transcodingFile;
              sub.CharacterEncoding = video.TargetSubtitleCharacterEncoding;
              if (!res[sourceMediaIndex].Any(s => s.Source == transcodingFile))
                res[sourceMediaIndex].Add(sub);
              continue;
            }

            // subtitle is embedded in the source file
            if (srcStream.IsEmbedded)
            {
              if (await ExtractSubtitleFileAsync(sourceMediaIndex, video, srcStream, sub.CharacterEncoding, transcodingFile, timeStart).ConfigureAwait(false) && File.Exists(transcodingFile))
              {
                sub.StreamIndex = -1;
                sub.Codec = targetCodec;
                sub.Source = transcodingFile;
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
            if (sub.Source == null)
            {
              return null;
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
          if (res[key].Any(s => s.Source != null && s.Codec == SubtitleCodec.Srt && s.IsPartial))
            partSrtSubs.Add(key, res[key].First(s => s.Source != null && s.Codec == SubtitleCodec.Srt && s.IsPartial));
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
                Source = transcodingFile,
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
      return null;
    }

    protected async Task<bool> MergeSrtSubtitlesAsync(string mergeFile, Dictionary<int, SubtitleStream> subtitles, Dictionary<int, double> subtitleTimeOffsets, double timeStart)
    {
      try
      {
        if (subtitles.Any(s => s.Value.Source == null || s.Value.Codec != SubtitleCodec.Srt))
          return false;

        int sequence = 0;
        using (StreamWriter output = new StreamWriter(mergeFile, false, Encoding.UTF8))
        {
          foreach (var sub in subtitles)
          {
            using (StreamReader input = new StreamReader(sub.Value.Source, Encoding.UTF8))
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

    protected async Task<bool> AssignExistingTranscodeContextAsync(string clientId, string transcodeId, TranscodeContext context)
    {
      try
      {
        using (await _transcodeLock.ReaderLockAsync().ConfigureAwait(false))
        {
          if (_runningClientTranscodes.Where(t => t.Key == clientId && t.Value.ContainsKey(transcodeId)).Any())
          {
            List<TranscodeContext> runningContexts = _runningClientTranscodes[clientId][transcodeId];
            //Non partial have first priority
            context = runningContexts?.FirstOrDefault(c => !c.Partial);
            return context != null;
          }
          return false;
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error assigning context", ex);
      }
      return false;
    }

    public async Task<TranscodeContext> GetLiveStreamAsync(string ClientId, BaseTranscoding TranscodingInfo, int ChannelId, bool WaitForBuffer)
    {
      try
      {
        TranscodingInfo.SourceMedia = new Dictionary<int, IResourceAccessor> { { 0, new TranscodeLiveAccessor(ChannelId) } };
        if (TranscodingInfo is AudioTranscoding at)
        {
          at.TargetIsLive = true;
          return await TranscodeAudioAsync(ClientId, at, 0, 0, WaitForBuffer);
        }
        else if (TranscodingInfo is VideoTranscoding vt)
        {
          vt.TargetIsLive = true;
          return await TranscodeVideoAsync(ClientId, vt, 0, 0, WaitForBuffer);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error getting live stream", ex);
      }
      return null;
    }

    public async Task<TranscodeContext> GetMediaStreamAsync(string ClientId, BaseTranscoding TranscodingInfo, double StartTime, double Duration, bool WaitForBuffer)
    {
      try
      {
        if (TranscodingInfo.SourceMedia is ILocalFsResourceAccessor lfra)
        {
          if (lfra.Exists == false)
          {
            _logger.Error("MediaConverter: File '{0}' does not exist for transcode '{1}'", TranscodingInfo.SourceMedia, TranscodingInfo.TranscodeId);
            return null;
          }
        }
        if (TranscodingInfo is ImageTranscoding it)
        {
          return await TranscodeImageAsync(ClientId, it, WaitForBuffer);
        }
        else if (TranscodingInfo is AudioTranscoding at)
        {
          return await TranscodeAudioAsync(ClientId, at, StartTime, Duration, WaitForBuffer);
        }
        else if (TranscodingInfo is VideoTranscoding vt)
        {
          return await TranscodeVideoAsync(ClientId, vt, StartTime, Duration, WaitForBuffer);
        }
        _logger.Error("MediaConverter: Transcoding info is not valid for transcode '{0}'", TranscodingInfo.TranscodeId);
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

    public async Task<Stream> GetFileStreamAsync(ILocalFsResourceAccessor FileResource)
    {
      // Impersonation
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(FileResource.CanonicalLocalResourcePath))
      {
        return await GetFileStreamAsync(FileResource.LocalFileSystemPath);
      }
    }

    private async Task<Stream> GetFileStreamAsync(string filePath)
    {
      try
      {
        DateTime waitStart = DateTime.Now;
        long length = 0;
        while (!File.Exists(filePath) || length == 0)
        {
          try
          {
            if (File.Exists(filePath))
              length = new FileInfo(filePath).Length;
          }
          catch { }

          if ((DateTime.Now - waitStart).TotalMilliseconds > FILE_STREAM_TIMEOUT)
          {
            _logger.Error("MediaConverter: Timed out waiting for ready file '{0}'", filePath);
            return null;
          }

          await Task.Delay(500).ConfigureAwait(false);
        }

        _logger.Debug(string.Format("MediaConverter: Serving ready file '{0}'", filePath));
        BufferedStream stream = new BufferedStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        return stream;
      }
      catch (Exception ex)
      {
        _logger.Error("MediaConverter: Error serving ready file '{0}'", ex, filePath);
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
          if (!_runningClientTranscodes.ContainsKey(clientId))
            _runningClientTranscodes.Add(clientId, new Dictionary<string, List<TranscodeContext>>());

          if (_runningClientTranscodes[clientId].Count > 0 &&
            (!_runningClientTranscodes[clientId].ContainsKey(transcodeId) || !context.Partial))
          {
            //Don't waste resources on transcoding if the client wants different media item
            _logger.Debug("MediaConverter: Ending {0} transcodes for client {1}", _runningClientTranscodes[clientId].Count, clientId);
            foreach (var transcodeContext in _runningClientTranscodes[clientId].Values.SelectMany(t => t))
            {
              transcodeContext.Stop();
            }
            _runningClientTranscodes[clientId].Clear();
          }
          else if (_runningClientTranscodes[clientId].Count > 0)
          {
            //Don't waste resources on transcoding multiple partial transcodes
            _logger.Debug("MediaConverter: Ending partial transcodes for client {0}", clientId);
            List<TranscodeContext> contextList = new List<TranscodeContext>(_runningClientTranscodes[clientId][transcodeId]);
            foreach (var transcodeContext in _runningClientTranscodes[clientId][transcodeId].Where(c => c.Partial && c != context))
            {
              transcodeContext.Stop();
            }
          }
          if (_runningClientTranscodes[clientId].ContainsKey(transcodeId) == false)
            _runningClientTranscodes[clientId].Add(transcodeId, new List<TranscodeContext>());

          _runningClientTranscodes[clientId][transcodeId].Add(context);
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
          if (_runningClientTranscodes.Where(t => t.Key == clientId && t.Value.ContainsKey(transcodeId)).Any())
          {
            context.Stop();
            _runningClientTranscodes[clientId][transcodeId].Remove(context);
            if (_runningClientTranscodes[clientId][transcodeId].Count == 0)
              _runningClientTranscodes[clientId].Remove(transcodeId);
          }
          if (_runningClientTranscodes[clientId].Count == 0)
            _runningClientTranscodes.Remove(clientId);
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
