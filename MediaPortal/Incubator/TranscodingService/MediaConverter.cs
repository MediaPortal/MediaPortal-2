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
using System.Drawing;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Plugins.Transcoding.Service
{
  public class MediaConverter
  {
    public string TranscoderCachePath { get; set; }
    public string TranscoderBinPath { get; set; }
    public long TranscoderMaximumCacheSize { get; set; }
    public long TranscoderMaximumCacheAge { get; set; }
    public int TranscoderMaximumThreads { get; set; }
    public int TranscoderTimeout { get; set; }
    public int HLSSegmentTimeInSeconds { get; set; }
    public string HLSSegmentFileTemplate { get; set; }
    public string SubtitleDefaultEncoding { get; set; }
    public ILogger Logger { get; set; }
    public bool AllowNvidiaHWAccelleration { get; set; }
    public bool AllowIntelHWAccelleration { get; set; }

    public static Dictionary<string, TranscodeContext> RunningTranscodes = new Dictionary<string, TranscodeContext>();

    private readonly List<long> _validAudioBitrates = new List<long>();
    private readonly Dictionary<AudioCodec, int> _maxChannelNumber = new Dictionary<AudioCodec, int>();
    private readonly Dictionary<string, string> _filerPathEncoding = new Dictionary<string, string>();
    private bool _supportHardcodedSubs = true;
    private bool _supportNvidiaHW = true;
    private bool _supportIntelHW = true;

    private class TranscodeData
    {
      private string _overrideParams = null;

      public TranscodeData(string binTranscoder, string workPath)
      {
        TranscoderBinPath = binTranscoder;
        WorkPath = workPath;
      }

      public string TranscodeId;
      public string TranscoderBinPath;
      public List<string> GlobalArguments = new List<string>();
      public List<string> InputArguments = new List<string>();
      public List<string> InputSubtitleArguments = new List<string>();
      public List<string> OutputArguments = new List<string>();
      public List<string> OutputFilter = new List<string>();
      public IResourceAccessor InputResourceAccessor;
      public string InputSubtitleFilePath;
      public string OutputFilePath;
      public string SegmentPlaylist = null;
      public string WorkPath;

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern uint GetShortPathName([MarshalAs(UnmanagedType.LPTStr)] string lpszLongPath, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszShortPath, uint cchBuffer);
      private string GetFileShortName(string fileName)
      { 
        StringBuilder shortNameBuffer = new StringBuilder(256);
        uint result = GetShortPathName(fileName, shortNameBuffer, 256);
        return shortNameBuffer.ToString();
      }

      public string TranscoderArguments
      {
        set
        {
          _overrideParams = value;
        }
        get
        {
          if (_overrideParams == null)
          {
            StringBuilder result = new StringBuilder();
            foreach (string arg in GlobalArguments)
            {
              result.Append(arg + " ");
            }
            if (InputResourceAccessor != null)
            {
              foreach (string arg in InputArguments)
              {
                result.Append(arg + " ");
              }
              result.Append("-i \"" + GetFileShortName(((ILocalFsResourceAccessor)InputResourceAccessor).LocalFileSystemPath) + "\" ");
            }
            if (string.IsNullOrEmpty(InputSubtitleFilePath) == false)
            {
              foreach (string arg in InputSubtitleArguments)
              {
                result.Append(arg + " ");
              }
              result.Append("-i \"" + GetFileShortName(InputSubtitleFilePath) + "\" ");
            }
            if (string.IsNullOrEmpty(OutputFilePath) == false)
            {
              foreach (string arg in OutputArguments)
              {
                result.Append(arg + " ");
              }
              if (OutputFilter.Count > 0)
              {
                result.Append("-vf \"");
                bool firstFilter = true;
                foreach (string filter in OutputFilter)
                {
                  if (firstFilter == false) result.Append(",");
                  result.Append(filter);
                  firstFilter = false;
                }
                result.Append("\" ");
              }
              result.Append("\"" + OutputFilePath + "\" ");
            }
            return result.ToString().Trim();
          }
          else
          {
            string arg = _overrideParams;
            if (InputResourceAccessor != null)
            {
              arg = arg.Replace("{input}", "\"" + GetFileShortName(((ILocalFsResourceAccessor)InputResourceAccessor).LocalFileSystemPath) + "\"");
            }
            if (string.IsNullOrEmpty(InputSubtitleFilePath) == false)
            {
              arg = arg.Replace("{subtitle}", "\"" + GetFileShortName(InputSubtitleFilePath) + "\"");
            }
            if (string.IsNullOrEmpty(OutputFilePath) == false)
            {
              arg = arg.Replace("{output}", "\"" + OutputFilePath + "\"");
            }
            return arg;
          }
        }
      }
    }

    private class Subtitle
    {
      public SubtitleCodec Codec = SubtitleCodec.Unknown;
      public string Language = "";
      public string SourceFile = "";
      public string CharacterEncoding = "";
    }

    public MediaConverter()
    {
      InitSettings();
    }

    #region Cache

    private void InitSettings()
    {
      if (_maxChannelNumber.Count > 0)
      {
        //Already inited
        return;
      }

      _validAudioBitrates.Add(32);
      _validAudioBitrates.Add(48);
      _validAudioBitrates.Add(56);
      _validAudioBitrates.Add(64);
      _validAudioBitrates.Add(80);
      _validAudioBitrates.Add(96);
      _validAudioBitrates.Add(112);
      _validAudioBitrates.Add(128);
      _validAudioBitrates.Add(160);
      _validAudioBitrates.Add(192);
      _validAudioBitrates.Add(224);
      _validAudioBitrates.Add(256);
      _validAudioBitrates.Add(320);
      _validAudioBitrates.Add(384);
      _validAudioBitrates.Add(448);
      _validAudioBitrates.Add(512);
      _validAudioBitrates.Add(576);
      _validAudioBitrates.Add(640);

      _maxChannelNumber.Add(AudioCodec.Ac3, 6);
      _maxChannelNumber.Add(AudioCodec.Dts, 6);
      _maxChannelNumber.Add(AudioCodec.DtsHd, 6);
      _maxChannelNumber.Add(AudioCodec.Mp1, 2);
      _maxChannelNumber.Add(AudioCodec.Mp2, 2);
      _maxChannelNumber.Add(AudioCodec.Mp3, 2);
      _maxChannelNumber.Add(AudioCodec.Wma, 2);
      _maxChannelNumber.Add(AudioCodec.WmaPro, 6);
      _maxChannelNumber.Add(AudioCodec.Lpcm, 2);

      _filerPathEncoding.Add(@"\", @"\\");
      _filerPathEncoding.Add(",", @"\,");
      _filerPathEncoding.Add(":", @"\:");
      _filerPathEncoding.Add(";", @"\;");
      _filerPathEncoding.Add("'", @"\'");
      _filerPathEncoding.Add("[", @"\[");
      _filerPathEncoding.Add("]", @"\]");

      TranscoderCachePath = Path.Combine(Path.GetTempPath(), "MPTranscodes");
      if(Directory.Exists(TranscoderCachePath) == false)
      {
        Directory.CreateDirectory(TranscoderCachePath);
      }
      TranscoderBinPath = "";
      TranscoderMaximumCacheSize = 10; //GB
      TranscoderMaximumCacheAge = 30; //Days
      TranscoderMaximumThreads = 0;
      TranscoderTimeout = 5000;
      HLSSegmentTimeInSeconds = 10;
      HLSSegmentFileTemplate = "segment%05d.ts";
      SubtitleDefaultEncoding = "";
      TranscoderBinPath = ServiceRegistration.Get<IFFMpegLib>().FFMpegBinaryPath;
      AllowIntelHWAccelleration = false;
      AllowNvidiaHWAccelleration = false;
      string result;
      using (Process process = new Process { StartInfo = new ProcessStartInfo(TranscoderBinPath, "") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true } })
      {
        process.Start();
        using (process.StandardError)
        {
          result = process.StandardError.ReadToEnd();
        }
        if (!process.HasExited)
          process.Close();
      }

      if (result.IndexOf("--enable-libass") == -1)
      {
        if(Logger != null) Logger.Warn("MediaConverter: FFMPEG is not compiled with libass support, hardcoded subtitles will not work.");
        _supportHardcodedSubs = false;
      }
      if (result.IndexOf("--enable-nvenc") == -1)
      {
        if (Logger != null) Logger.Warn("MediaConverter: FFMPEG is not compiled with libnvenc support, Nvidia hardware acceleration will not work.");
        _supportNvidiaHW = false;
      }
      if (result.IndexOf("--enable-libmfx") == -1)
      {
        if (Logger != null) Logger.Warn("MediaConverter: FFMPEG is not compiled with libmfx support, Intel hardware acceleration will not work.");
        _supportIntelHW = false;
      }
    }

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

    public void CleanUpTranscodeCache()
    {
      if (Directory.Exists(TranscoderCachePath) == true)
      {
        int maxTries = 10;
        SortedDictionary<DateTime, string> fileList = new SortedDictionary<DateTime, string>();
        long cacheSize = 0;
        List<string> dirObjects = new List<string>(Directory.GetFiles(TranscoderCachePath, "*.mp*"));
        dirObjects.AddRange(Directory.GetDirectories(TranscoderCachePath, "*_mptf"));
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
        while (fileList.Count > 0 && bDeleting && TranscoderMaximumCacheAge > 0 && tryCount < maxTries)
        {
          tryCount++;
          bDeleting = false;
          KeyValuePair<DateTime, string> dirObject = fileList.First();
          if ((DateTime.Now - dirObject.Key).TotalDays > TranscoderMaximumCacheAge)
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
        while (fileList.Count > 0 && cacheSize > (TranscoderMaximumCacheSize * 1024 * 1024 * 1024) && TranscoderMaximumCacheSize > 0 && tryCount < maxTries)
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
            foreach (FileInfo folderFile in folderFiles)
            {
              cacheSize -= folderFile.Length;
            }
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

    public bool IsFileInTranscodeCache(string transcodeId)
    {
      if (RunningTranscodes.ContainsKey(transcodeId) == false)
      {
        List<string> dirObjects = new List<string>(Directory.GetFiles(TranscoderCachePath, "*.mp*"));
        foreach (string file in dirObjects)
        {
          if (file.StartsWith(transcodeId + ".mp") == true)
          {
            return true;
          }
        }
      }
      return false;
    }

    #endregion

    #region Validators

    private string GetValidFramerate(double validFramerate)
    {
      string normalizedFps = validFramerate.ToString();
      if (validFramerate < 23.99)
        normalizedFps = "23.976";
      else if (validFramerate >= 23.99 && validFramerate < 24.1)
        normalizedFps = "24";
      else if (validFramerate >= 24.99 && validFramerate < 25.1)
        normalizedFps = "25";
      else if (validFramerate >= 29.9 && validFramerate < 29.99)
        normalizedFps = "29.97";
      else if (validFramerate >= 29.99 && validFramerate < 30.1)
        normalizedFps = "30";
      else if (validFramerate >= 49.9 && validFramerate < 50.1)
        normalizedFps = "50";
      else if (validFramerate >= 59.9 && validFramerate < 59.99)
        normalizedFps = "59.94";
      else if (validFramerate >= 59.99 && validFramerate < 60.1)
        normalizedFps = "60";

      if (normalizedFps == "23.976")
        return "24000/1001";
      if (normalizedFps == "29.97")
        return "30000/1001";
      if (normalizedFps == "59.94")
        return "60000/1001";

      return normalizedFps;
    }

    private int GetMaxNumberOfChannels(AudioCodec codec)
    {
      if (codec != AudioCodec.Unknown && _maxChannelNumber.ContainsKey(codec))
      {
        return _maxChannelNumber[codec];
      }
      return 2;
    }

    private int GetAudioNumberOfChannels(AudioCodec sourceCodec, AudioCodec targetCodec, int sourceChannels, bool forceStereo)
    {
      bool downmixingSupported = sourceCodec != AudioCodec.Flac;
      if (sourceChannels <= 0)
      {
        if (forceStereo)
          return 2;
      }
      else
      {
        int maxChannels = GetMaxNumberOfChannels(targetCodec);
        if (sourceChannels > 2 && forceStereo && downmixingSupported)
        {
          return 2;
        }
        if (maxChannels > 0 && maxChannels < sourceChannels)
        {
          return maxChannels;
        }
        return sourceChannels;
      }
      return -1;
    }

    private long GetAudioBitrate(long sourceBitrate, long targetBitrate)
    {
      if (targetBitrate > 0)
      {
        return targetBitrate;
      }
      long bitrate = sourceBitrate;
      if (bitrate > 0 && _validAudioBitrates.Contains(bitrate) == false)
      {
        bitrate = FindNearestValidBitrate(bitrate);
      }
      long maxBitrate = 192;
      if (bitrate > 0 && bitrate < maxBitrate)
      {
        return bitrate;
      }
      return maxBitrate;
    }

    private int FindNearestValidBitrate(double itemBitrate)
    {
      if (itemBitrate < 0)
      {
        itemBitrate = 0;
      }
      int nearest = -1;
      double smallestDiff = double.MaxValue;
      foreach (int validRate in _validAudioBitrates)
      {
        double d = Math.Abs(itemBitrate - validRate);
        if (d < smallestDiff)
        {
          nearest = validRate;
          smallestDiff = d;
        }
      }
      return nearest;
    }

    private long GetAudioFrequency(AudioCodec sourceCodec, AudioCodec targetCodec, long sourceFrequency, long targetSampleRate)
    {
      if (targetSampleRate > 0)
      {
        return targetSampleRate;
      }
      bool isLPCM = sourceCodec == AudioCodec.Lpcm || targetCodec == AudioCodec.Lpcm;
      long minfrequency = 48000;
      bool frequencyRequired = true;
      if (sourceFrequency >= 44100)
      {
        minfrequency = sourceFrequency;
        frequencyRequired = false;
      }
      if (isLPCM || frequencyRequired)
      {
        return minfrequency;
      }
      return -1;
    }

    #endregion

    #region Checkers

    private bool IsVideoDimensionChanged(VideoTranscoding video)
    {
      return IsVideoHeightChangeNeeded(video.SourceVideoHeight, video.TargetVideoMaxHeight) ||
        IsVideoAspectRatioChanged(video.SourceVideoWidth, video.SourceVideoHeight, video.SourceVideoPixelAspectRatio, video.TargetVideoAspectRatio) ||
        IsSquarePixelNeeded(video);
    }

    private bool IsVideoHeightChangeNeeded(int newHeight, int targetMaximumHeight)
    {
      return (newHeight > 0 && targetMaximumHeight > 0 && newHeight > targetMaximumHeight);
    }

    private bool IsSquarePixelNeeded(VideoTranscoding video)
    {
      bool squarePixels = IsSquarePixel(video.SourceVideoPixelAspectRatio);
      return (video.TargetVideoContainer == VideoContainer.Asf || video.TargetVideoContainer == VideoContainer.Flv) && squarePixels == false;
    }

    private bool IsVideoAspectRatioChanged(int newWidth, int newHeight, double pixelAspectRatio, double targetAspectRatio)
    {
      return targetAspectRatio > 0 && newWidth > 0 && newHeight > 0 && 
        (Math.Round(targetAspectRatio, 2, MidpointRounding.AwayFromZero) != Math.Round(pixelAspectRatio * (double)newWidth / (double)newHeight, 2, MidpointRounding.AwayFromZero));
    }

    private bool IsSquarePixel(double pixelAspectRatio)
    {
      if (pixelAspectRatio <= 0)
      {
        return true;
      }
      return Math.Abs(1.0 - pixelAspectRatio) < 0.01;
    }

    private bool IsVideoStreamChanged(VideoTranscoding video)
    {
      bool notChanged = true;
      notChanged &= video.TargetForceVideoTranscoding == false;
      notChanged &= (video.TargetSubtitleSupport == SubtitleSupport.None || video.SourceSubtitle == null || (video.TargetSubtitleSupport == SubtitleSupport.HardCoded && _supportHardcodedSubs == false));
      notChanged &= (video.TargetVideoCodec == VideoCodec.Unknown || video.TargetVideoCodec == video.SourceVideoCodec);
      notChanged &= IsVideoDimensionChanged(video) == false;
      notChanged &= video.TargetVideoBitrate <= 0;

      return notChanged == false;
    }

    private bool IsMPEGTSContainer(VideoContainer container)
    {
      return container == VideoContainer.Mpeg2Ts || container == VideoContainer.Wtv || container == VideoContainer.Hls || container == VideoContainer.M2Ts;
    }

    private bool IsAudioStreamChanged(BaseTranscoding media)
    {
      AudioCodec sourceCodec = AudioCodec.Unknown;
      AudioCodec targetCodec = AudioCodec.Unknown;
      long sourceBitrate = 0;
      long targetBitrate = 0;
      long sourceFrequency = 0;
      long targetFrequency = 0;
      if (media is VideoTranscoding)
      {
        VideoTranscoding video = (VideoTranscoding)media;
        sourceCodec = video.SourceAudioCodec;
        sourceBitrate = video.SourceAudioBitrate;
        sourceFrequency = video.SourceAudioFrequency;
        targetCodec = video.TargetAudioCodec;
        targetBitrate = video.TargetAudioBitrate;
        targetFrequency = video.TargetAudioFrequency;
      }
      if (media is AudioTranscoding)
      {
        AudioTranscoding audio = (AudioTranscoding)media;
        sourceCodec = audio.SourceAudioCodec;
        sourceBitrate = audio.SourceAudioBitrate;
        sourceFrequency = audio.SourceAudioFrequency;
        targetCodec = audio.TargetAudioCodec;
        targetBitrate = audio.TargetAudioBitrate;
        targetFrequency = audio.TargetAudioFrequency;
      }

      bool notChanged = true;
      notChanged &= (sourceCodec != AudioCodec.Unknown && targetCodec != AudioCodec.Unknown && sourceCodec == targetCodec);
      notChanged &= (sourceBitrate > 0 && targetBitrate > 0 && sourceBitrate == targetBitrate);
      notChanged &= (sourceFrequency > 0 && targetFrequency > 0 && sourceFrequency == targetFrequency);

      return notChanged == false;
    }

    private bool IsImageStreamChanged(ImageTranscoding image)
    {
      bool notChanged = true;
      notChanged &= (image.SourceOrientation == 0 || image.TargetAutoRotate == false);
      notChanged &= (image.SourceHeight > 0 && image.SourceHeight <= image.TargetHeight);
      notChanged &= (image.SourceWidth > 0 && image.SourceWidth <= image.TargetWidth);
      notChanged &= (image.TargetPixelFormat == PixelFormat.Unknown || image.SourcePixelFormat == image.TargetPixelFormat);
      notChanged &= (image.TargetImageCodec == ImageContainer.Unknown && image.SourceImageCodec == image.TargetImageCodec);

      return notChanged == false;
    }

    #endregion

    #region Commandline

    private void InitTranscodingParameters(IResourceAccessor sourceFile, TranscodeData data)
    {
      data.InputResourceAccessor = sourceFile;
      AddInputOptions(data);
      data.OutputArguments.Add("-y");
    }

    private void AddInputOptions(TranscodeData data)
    {
      Logger.Debug("Media Converter: AddInputOptions() is NetworkResource: {0}", data.InputResourceAccessor.ParentProvider.Metadata.NetworkResource);
      if (data.InputResourceAccessor.ParentProvider.Metadata.NetworkResource)
        if (((INetworkResourceAccessor)data.InputResourceAccessor).URL.StartsWith("rtsp://", StringComparison.InvariantCultureIgnoreCase))
        {
          data.GlobalArguments.Add("-rtsp_transport +tcp+udp");
          data.GlobalArguments.Add("-analyzeduration 10000000");
        }
    }

    private void AddTranscodingThreadsParameters(bool useOutputThreads, TranscodeData data)
    {
      data.InputArguments.Add(string.Format("-threads {0}", TranscoderMaximumThreads));
      if (useOutputThreads)
      {
        data.OutputArguments.Add(string.Format("-threads {0}", TranscoderMaximumThreads));
      }
    }

    private void AddTargetVideoFormatAndOutputFileParameters(VideoTranscoding video, string transcodingFile, TranscodeData data)
    {
      if (video.TargetVideoContainer == VideoContainer.Hls)
      {
        string pathName = Path.Combine(TranscoderCachePath, Path.GetFileNameWithoutExtension(transcodingFile).Replace(".", "_") + "_mptf");
        if (Directory.Exists(pathName) == false)
        {
          Directory.CreateDirectory(pathName);
        }
        data.WorkPath = pathName;
        data.SegmentPlaylist = "playlist.m3u8";

        //Segment muxer
        //data.OutputArguments.Add(string.Format("-f {0}", GetVideoContainer(video.TargetVideoContainer)));
        //data.OutputArguments.Add(string.Format("-segment_format {0}", GetVideoContainer(VideoContainer.Mpeg2Ts)));
        //data.OutputArguments.Add(string.Format("-segment_time {0}", HLSSegmentTimeInSeconds));
        //data.OutputArguments.Add("-segment_list_flags live");
        //data.OutputArguments.Add("-segment_list_type hls");
        //data.OutputArguments.Add("-segment_list_size 0");
        //data.OutputArguments.Add(string.Format("-segment_list {0}", "\"" + data.SegmentPlaylist + "\""));
        //data.OutputFilePath = HLSSegmentFileTemplate;

        //HLS muxer
        data.OutputArguments.Add("-hls_list_size 0");
        data.OutputArguments.Add("-hls_allow_cache 0");
        data.OutputArguments.Add(string.Format("-hls_time {0}", HLSSegmentTimeInSeconds));
        data.OutputArguments.Add(string.Format("-hls_segment_filename {0}", "\"" + HLSSegmentFileTemplate + "\""));
        data.OutputFilePath = data.SegmentPlaylist;
      }
      else
      {
        data.OutputArguments.Add(string.Format("-f {0}", FFMpegGetVideoContainer.GetVideoContainer(video.TargetVideoContainer)));
        data.OutputFilePath = transcodingFile;
      }

      if (video.Movflags != null)
      {
        data.OutputArguments.Add(string.Format("-movflags {0}", video.Movflags));
      }
    }

    private void AddStreamMapParameters(int videoStreamIndex, int audioStreamIndex, bool embeddedSubtitle, TranscodeData data)
    {
      if (videoStreamIndex != -1)
      {
        data.OutputArguments.Add(string.Format("-map 0:{0}", videoStreamIndex));
      }
      if (audioStreamIndex != -1)
      {
        data.OutputArguments.Add(string.Format("-map 0:{0}", audioStreamIndex));
      }
      if (embeddedSubtitle)
      {
        data.OutputArguments.Add("-map 1:0");
      }
    }

    private string ExtractSubtitleFile(VideoTranscoding video, SubtitleStream subtitle, string subtitleEncoding, string targetFilePath)
    {
      string subtitleEncoder = "copy";
      SubtitleCodec targetCodec = video.TargetSubtitleCodec;
      if (targetCodec == SubtitleCodec.Unknown)
      {
        targetCodec = subtitle.Codec;
      }
      if (targetCodec == SubtitleCodec.Unknown)
      {
        targetCodec = SubtitleCodec.Ass;
      }
      if (subtitle.Codec != targetCodec)
      {
        subtitleEncoder = FFMpegGetSubtitleContainer.GetSubtitleContainer(targetCodec);
      }
      string subtitleFormat = FFMpegGetSubtitleContainer.GetSubtitleContainer(subtitle.Codec);
      TranscodeData data = new TranscodeData(TranscoderBinPath, TranscoderCachePath);
      InitTranscodingParameters(video.SourceFile, data);
      AddSubtitleExtractionParameters(video, subtitle, subtitleEncoding, subtitleEncoder, subtitleFormat, data);
      data.OutputFilePath = targetFilePath;

      if (Logger != null) Logger.Debug("MediaConverter: Invoking transcoder to extract subtitle from file '{0}'", video.SourceFile);
      FileProcessor(data);
      if (File.Exists(targetFilePath) == false)
      {
        if (Logger != null) Logger.Error("MediaConverter: Failed to extract subtitle from file '{0}'", video.SourceFile);
        return null;
      }
      return targetFilePath;
    }

    private void AddSubtitleEmbeddingParameters(Subtitle subtitle, SubtitleCodec codec, TranscodeData data)
    {
      if (codec == SubtitleCodec.Unknown) return;
      if (subtitle == null) return;

      data.InputSubtitleFilePath = subtitle.SourceFile;

      string subtitleFormat = FFMpegGetSubtitleContainer.GetSubtitleContainer(subtitle.Codec);
      data.InputSubtitleArguments.Add(string.Format("-f {0}", subtitleFormat));
      string subtitleEncoder = FFMpegGetSubtitleContainer.GetSubtitleContainer(codec);
      data.OutputArguments.Add(string.Format("-c:s {0}", subtitleEncoder));
      if(string.IsNullOrEmpty(subtitle.Language) == false)
      {
        string languageName = null;
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        foreach (CultureInfo culture in cultures)
        {
          if (culture.TwoLetterISOLanguageName.ToUpperInvariant() == subtitle.Language)
          {
            languageName = culture.ThreeLetterISOLanguageName;
            break;
          }
        }
        if(string.IsNullOrEmpty(languageName) == false)
        {
          data.OutputArguments.Add(string.Format("-metadata:s:s:0 language={0}", languageName.ToLowerInvariant()));
        }
      }
    }

    private void AddSubtitleExtractionParameters(VideoTranscoding video, SubtitleStream subtitle, string subtitleEncoding, string subtitleEncoder, string subtitleFormat, TranscodeData data)
    {
      if (string.IsNullOrEmpty(subtitleEncoding) == false)
      {
        data.OutputArguments.Add(string.Format("-sub_charenc {0}", subtitleEncoding));
      }
      data.OutputArguments.Add("-vn");
      data.OutputArguments.Add("-an");
      data.OutputArguments.Add(string.Format("-map 0:{0}", subtitle.StreamIndex));
      data.OutputArguments.Add(string.Format("-c:s {0}", subtitleEncoder));
      data.OutputArguments.Add(string.Format("-f {0}", subtitleFormat));
    }

    private void AddImageFilterParameters(ImageTranscoding image, TranscodeData data)
    {
      int iHeight = image.SourceHeight;
      int iWidth = image.SourceWidth;
      if (iHeight > image.TargetHeight && image.TargetHeight > 0)
      {
        double scale = (double)image.SourceWidth / (double)image.SourceHeight;
        iHeight = image.TargetHeight;
        iWidth = Convert.ToInt32(scale * (double)iHeight);
      }
      if (iWidth > image.TargetWidth && image.TargetWidth > 0)
      {
        double scale = (double)image.SourceHeight / (double)image.SourceWidth;
        iWidth = image.TargetWidth;
        iHeight = Convert.ToInt32(scale * (double)iWidth);
      }

      if (image.TargetAutoRotate == true)
      {
        if (image.SourceOrientation > 4)
        {
          int iTemp = iWidth;
          iWidth = iHeight;
          iHeight = iTemp;
        }

        if (image.SourceOrientation > 1)
        {
          if (image.SourceOrientation == 2)
          {
            data.OutputFilter.Add("hflip");
          }
          else if (image.SourceOrientation == 3)
          {
            data.OutputFilter.Add("hflip");
            data.OutputFilter.Add("vflip");
          }
          else if (image.SourceOrientation == 4)
          {
            data.OutputFilter.Add("vflip");
          }
          else if (image.SourceOrientation == 5)
          {
            data.OutputFilter.Add("transpose=0");
          }
          else if (image.SourceOrientation == 6)
          {
            data.OutputFilter.Add("transpose=1");
          }
          else if (image.SourceOrientation == 7)
          {
            data.OutputFilter.Add("transpose=2");
            data.OutputFilter.Add("hflip");
          }
          else if (image.SourceOrientation == 8)
          {
            data.OutputFilter.Add("transpose=2");
          }
        }
      }
      data.OutputFilter.Add(string.Format("scale={0}:{1}", iWidth, iHeight));
    }

    private void AddAudioParameters(AudioTranscoding audio, TranscodeData data)
    {
      if (IsAudioStreamChanged(audio) == false)
      {
        data.OutputArguments.Add("-c:a copy");
      }
      else
      {
        long frequency = GetAudioFrequency(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioFrequency, audio.TargetAudioFrequency);
        if (frequency > 0)
        {
          data.OutputArguments.Add(string.Format("-ar {0}", frequency));
        }
        if (audio.TargetAudioContainer != AudioContainer.Lpcm)
        {
          long audioBitrate = GetAudioBitrate(audio.SourceAudioBitrate, audio.TargetAudioBitrate);
          data.OutputArguments.Add(string.Format("-b:a {0}k", audioBitrate));
        }
      }
      if (audio.TargetAudioContainer == AudioContainer.Mp3)
      {
        data.OutputArguments.Add("-id3v2_version 3");
      }
      AddAudioChannelsNumberParameters(audio, data);

      if (audio.TargetCoder == Coder.Arithmic)
      {
        data.OutputArguments.Add("-coder ac");
      }
      else if (audio.TargetCoder == Coder.Deflate)
      {
        data.OutputArguments.Add("-coder deflate");
      }
      else if (audio.TargetCoder == Coder.Raw)
      {
        data.OutputArguments.Add("-coder raw");
      }
      else if (audio.TargetCoder == Coder.RunLength)
      {
        data.OutputArguments.Add("-coder rle");
      }
      else if (audio.TargetCoder == Coder.VariableLength)
      {
        data.OutputArguments.Add("-coder vlc");
      }
    }

    private void AddImageParameters(ImageTranscoding image, TranscodeData data)
    {
      if (IsImageStreamChanged(image) == false)
      {
        data.OutputArguments.Add("-c:v copy");
      }
      else
      {
        AddImageFilterParameters(image, data);
        if (image.TargetPixelFormat != PixelFormat.Unknown)
        {
          data.OutputArguments.Add(string.Format("-pix_fmt {0}", FFMpegGetPixelFormat.GetPixelFormat(image.TargetPixelFormat)));
        }
        if (image.TargetImageQuality == QualityMode.Default || image.TargetImageQuality == QualityMode.Best)
        {
          data.OutputArguments.Add("-q:v 0");
        }
        else
        {
          data.OutputArguments.Add(string.Format("-q:v {0}", image.TargetImageQualityFactor));
        }
        if (image.TargetImageCodec != ImageContainer.Unknown)
        {
          data.OutputArguments.Add(string.Format("-c:v {0}", FFMpegGetImageCodec.GetImageCodec(image.TargetImageCodec)));
        }
      }

      if (image.TargetCoder == Coder.Arithmic)
      {
        data.OutputArguments.Add("-coder ac");
      }
      else if (image.TargetCoder == Coder.Deflate)
      {
        data.OutputArguments.Add("-coder deflate");
      }
      else if (image.TargetCoder == Coder.Raw)
      {
        data.OutputArguments.Add("-coder raw");
      }
      else if (image.TargetCoder == Coder.RunLength)
      {
        data.OutputArguments.Add("-coder rle");
      }
      else if (image.TargetCoder == Coder.VariableLength)
      {
        data.OutputArguments.Add("-coder vlc");
      }
    }

    private void AddVideoParameters(VideoTranscoding video, Subtitle subtitle, TranscodeData data)
    {
      if (video.TargetVideoCodec == VideoCodec.Unknown)
      {
        video.TargetVideoCodec = video.SourceVideoCodec;
      }
      if (video.TargetVideoAspectRatio <= 0)
      {
        video.TargetVideoAspectRatio = 16.0F / 9.0F;
      }
      if (video.SourceVideoPixelAspectRatio <= 0)
      {
        video.SourceVideoPixelAspectRatio = 1.0F;
      }
      if (video.TargetVideoMaxHeight <= 0)
      {
        video.TargetVideoMaxHeight = 1080;
      }
      bool vCodecCopy = false;
      if (IsVideoStreamChanged(video) == false)
      {
        vCodecCopy = true;
        data.OutputArguments.Add("-c:v copy");
        data.GlobalArguments.Add("-fflags +genpts");
      }
      else
      {
        data.OutputArguments.Add(string.Format("-c:v {0}", FFMpegGetVideoCodec.GetVideoCodec(video.TargetVideoCodec, AllowNvidiaHWAccelleration, AllowIntelHWAccelleration, _supportNvidiaHW, _supportIntelHW)));

        if (video.TargetPixelFormat == PixelFormat.Unknown)
        {
          video.TargetPixelFormat = PixelFormat.Yuv420;
        }
        data.OutputArguments.Add(string.Format("-pix_fmt {0}", FFMpegGetPixelFormat.GetPixelFormat(video.TargetPixelFormat)));

        if (video.TargetVideoCodec == VideoCodec.H265)
        {
          if (video.TargetH264Preset == H264Preset.Ultrafast)
          {
            data.OutputArguments.Add("-preset ultrafast");
          }
          else if (video.TargetH264Preset == H264Preset.Superfast)
          {
            data.OutputArguments.Add("-preset superfast");
          }
          else if (video.TargetH264Preset == H264Preset.Default || video.TargetH264Preset == H264Preset.Veryfast)
          {
            data.OutputArguments.Add("-preset veryfast");
          }
          else if (video.TargetH264Preset == H264Preset.Faster)
          {
            data.OutputArguments.Add("-preset faster");
          }
          else if (video.TargetH264Preset == H264Preset.Fast)
          {
            data.OutputArguments.Add("-preset fast");
          }
          else if (video.TargetH264Preset == H264Preset.Medium)
          {
            data.OutputArguments.Add("-preset medium");
          }
          else if (video.TargetH264Preset == H264Preset.Slow)
          {
            data.OutputArguments.Add("-preset slow");
          }
          else if (video.TargetH264Preset == H264Preset.Slower)
          {
            data.OutputArguments.Add("-preset slower");
          }
          else if (video.TargetH264Preset == H264Preset.Veryslow)
          {
            data.OutputArguments.Add("-preset veryslow");
          }
          else if (video.TargetH264Preset == H264Preset.Placebo)
          {
            data.OutputArguments.Add("-preset placebo");
          }

          AddVideoBitrateParameters(video, data);

          data.OutputArguments.Add("-x265-params");
          if (video.TargetVideoQuality == QualityMode.Default || video.TargetVideoQuality == QualityMode.Best)
          {
            data.OutputArguments.Add("-crf 10");
          }
          else
          {
            data.OutputArguments.Add(string.Format("-crf {0}", video.TargetH264QualityFactor));
          }
        }
        else if (video.TargetVideoCodec == VideoCodec.H264)
        {
          if (video.TargetH264Profile == H264Profile.Baseline)
          {
            data.OutputArguments.Add("-profile:v baseline");
          }
          else if (video.TargetH264Profile == H264Profile.Main)
          {
            data.OutputArguments.Add("-profile:v main");
          }
          else if (video.TargetH264Profile == H264Profile.High && video.TargetPixelFormat == PixelFormat.Yuv422)
          {
            data.OutputArguments.Add("-profile:v high422");
          }
          else if (video.TargetH264Profile == H264Profile.High && video.TargetPixelFormat == PixelFormat.Yuv444)
          {
            data.OutputArguments.Add("-profile:v high444");
          }
          else if (video.TargetH264Profile == H264Profile.High)
          {
            data.OutputArguments.Add("-profile:v high");
          }
          data.OutputArguments.Add(string.Format("-level {0}", video.TargetH264Level.ToString("0.0", CultureInfo.InvariantCulture)));

          if (video.TargetH264Preset == H264Preset.Ultrafast)
          {
            data.OutputArguments.Add("-preset ultrafast");
          }
          else if (video.TargetH264Preset == H264Preset.Superfast)
          {
            data.OutputArguments.Add("-preset superfast");
          }
          else if (video.TargetH264Preset == H264Preset.Default || video.TargetH264Preset == H264Preset.Veryfast)
          {
            data.OutputArguments.Add("-preset veryfast");
          }
          else if (video.TargetH264Preset == H264Preset.Faster)
          {
            data.OutputArguments.Add("-preset faster");
          }
          else if (video.TargetH264Preset == H264Preset.Fast)
          {
            data.OutputArguments.Add("-preset fast");
          }
          else if (video.TargetH264Preset == H264Preset.Medium)
          {
            data.OutputArguments.Add("-preset medium");
          }
          else if (video.TargetH264Preset == H264Preset.Slow)
          {
            data.OutputArguments.Add("-preset slow");
          }
          else if (video.TargetH264Preset == H264Preset.Slower)
          {
            data.OutputArguments.Add("-preset slower");
          }
          else if (video.TargetH264Preset == H264Preset.Veryslow)
          {
            data.OutputArguments.Add("-preset veryslow");
          }
          else if (video.TargetH264Preset == H264Preset.Placebo)
          {
            data.OutputArguments.Add("-preset placebo");
          }

          AddVideoBitrateParameters(video, data);
          if (video.TargetVideoQuality == QualityMode.Default || video.TargetVideoQuality == QualityMode.Best)
          {
            data.OutputArguments.Add("-crf 10");
          }
          else
          {
            data.OutputArguments.Add(string.Format("-crf {0}", video.TargetH264QualityFactor));
          }
        }
        else
        {
          if (AddVideoBitrateParameters(video, data) == false)
          {
            if (video.TargetVideoQuality == QualityMode.Default || video.TargetVideoQuality == QualityMode.Best)
            {
              data.OutputArguments.Add("-qscale:v 1");
            }
            else
            {
              data.OutputArguments.Add(string.Format("-qscale:v {0}", video.TargetVideoQualityFactor));
            }
          }
        }

        AddVideoFiltersParameters(video, subtitle, data);
        if (video.SourceFrameRate > 0)
        {
          data.OutputArguments.Add(string.Format("-r {0}", GetValidFramerate(video.SourceFrameRate)));
        }
        data.OutputArguments.Add("-g 15");
      }
      if (vCodecCopy && video.SourceVideoCodec == VideoCodec.H264 && !IsMPEGTSContainer(video.SourceVideoContainer) && IsMPEGTSContainer(video.TargetVideoContainer))
      {
        data.OutputArguments.Add("-bsf:v h264_mp4toannexb");
      }
      else if (!vCodecCopy && video.TargetVideoCodec == VideoCodec.H264 && IsMPEGTSContainer(video.TargetVideoContainer))
      {
        data.OutputArguments.Add("-bsf:v h264_mp4toannexb");
        data.OutputArguments.Add("-flags -global_header");
      }
      if (video.TargetVideoContainer == VideoContainer.M2Ts)
      {
        data.OutputArguments.Add("-mpegts_m2ts_mode 1");
      }

      if (video.TargetCoder == Coder.Arithmic)
      {
        data.OutputArguments.Add("-coder ac");
      }
      else if (video.TargetCoder == Coder.Deflate)
      {
        data.OutputArguments.Add("-coder deflate");
      }
      else if (video.TargetCoder == Coder.Raw)
      {
        data.OutputArguments.Add("-coder raw");
      }
      else if (video.TargetCoder == Coder.RunLength)
      {
        data.OutputArguments.Add("-coder rle");
      }
      else if (video.TargetCoder == Coder.VariableLength)
      {
        data.OutputArguments.Add("-coder vlc");
      }
    }

    private bool AddVideoBitrateParameters(VideoTranscoding video, TranscodeData data)
    {
      if (video.TargetVideoBitrate > 0)
      {
        data.OutputArguments.Add(string.Format("-b:v {0}", video.TargetVideoBitrate + "k"));
        data.OutputArguments.Add(string.Format("-maxrate:v {0}", video.TargetVideoBitrate + "k"));
        data.OutputArguments.Add(string.Format("-bufsize:v {0}", video.TargetVideoBitrate + "k"));

        return true;
      }
      return false;
    }

    private void GetVideoDimensions(VideoTranscoding video, out Size newSize, out Size newContentSize, out float newPixelAspectRatio, out bool pixelARChanged, out bool videoARChanged, out bool videoHeightChanged)
    {
      newSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      newContentSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      newPixelAspectRatio = video.SourceVideoPixelAspectRatio;
      pixelARChanged = false;
      videoARChanged = false;
      videoHeightChanged = false;

      if (IsSquarePixelNeeded(video) == true)
      {
        newSize.Width = Convert.ToInt32(Math.Round((double)video.SourceVideoWidth * video.SourceVideoPixelAspectRatio));
        newSize.Height = video.SourceVideoHeight;
        newContentSize.Width = newSize.Width;
        newContentSize.Height = newSize.Height;
        newPixelAspectRatio = 1;
        pixelARChanged = true;
      }
      if (IsVideoAspectRatioChanged(newSize.Width, newSize.Height, newPixelAspectRatio, video.TargetVideoAspectRatio) == true)
      {
        double sourceNewAspectRatio = (double)newSize.Width / (double)newSize.Height * video.SourceVideoAspectRatio;
        if (sourceNewAspectRatio < video.SourceVideoAspectRatio)
          newSize.Width = Convert.ToInt32(Math.Round((double)newSize.Height * video.TargetVideoAspectRatio / newPixelAspectRatio));
        else
          newSize.Height = Convert.ToInt32(Math.Round((double)newSize.Width * newPixelAspectRatio / video.TargetVideoAspectRatio));
          
        videoARChanged = true;
      }
      if (IsVideoHeightChangeNeeded(newSize.Height, video.TargetVideoMaxHeight) == true)
      {
        double oldWidth = newSize.Width;
        double oldHeight = newSize.Height;
        newSize.Width = Convert.ToInt32(Math.Round(newSize.Width * ((double)video.TargetVideoMaxHeight / (double)newSize.Height)));
        newSize.Height = video.TargetVideoMaxHeight;
        newContentSize.Width = Convert.ToInt32(Math.Round((double)newContentSize.Width * ((double)newSize.Width / oldWidth)));
        newContentSize.Height = Convert.ToInt32(Math.Round((double)newContentSize.Height * ((double)newSize.Height / oldHeight)));
        videoHeightChanged = true;
      }
      //Correct widths
      newSize.Width = ((newSize.Width + 1) / 2) * 2;
      newContentSize.Width = ((newContentSize.Width + 1) / 2) * 2;
    }

    private void AddVideoFiltersParameters(VideoTranscoding video, Subtitle subtitle, TranscodeData data)
    {
      bool sourceSquarePixels = IsSquarePixel(video.SourceVideoPixelAspectRatio);
      Size newSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      Size newContentSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      float newPixelAspectRatio = video.SourceVideoPixelAspectRatio;
      bool pixelARChanged = false;
      bool videoARChanged = false;
      bool videoHeightChanged = false;

      GetVideoDimensions(video, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);

      if (videoARChanged || pixelARChanged || videoHeightChanged)
      {
        if (videoHeightChanged || pixelARChanged)
        {
          data.OutputFilter.Add(string.Format("scale={0}:{1}", newContentSize.Width, newContentSize.Height));
        }
        if (videoARChanged)
        {
          int posX = Convert.ToInt32(Math.Abs(newSize.Width - newContentSize.Width) / 2);
          int posY = Convert.ToInt32(Math.Abs(newSize.Height - newContentSize.Height) / 2);
          data.OutputFilter.Add(string.Format("pad={0}:{1}:{2}:{3}:black", newSize.Width, newSize.Height, posX, posY));
          data.OutputFilter.Add(string.Format("setdar={0}/{1}", newSize.Width, newSize.Height));
        }
        if (pixelARChanged)
        {
          data.OutputFilter.Add("setsar=1");
        }
        else if (sourceSquarePixels == false)
        {
          data.OutputFilter.Add("setsar=" + video.SourceVideoPixelAspectRatio.ToString("0.00", CultureInfo.InvariantCulture));
        }
      }

      if (subtitle != null && subtitle.SourceFile != null && _supportHardcodedSubs == true && video.TargetSubtitleSupport == SubtitleSupport.HardCoded)
      {
        string encoding = "UTF-8";
        if (string.IsNullOrEmpty(subtitle.CharacterEncoding) == false)
        {
          encoding = subtitle.CharacterEncoding;
        }
        data.OutputFilter.Add(string.Format("subtitles=filename='{0}':original_size={1}x{2}:charenc='{3}'", EncodeFilePath(subtitle.SourceFile), newSize.Width, newSize.Height, encoding));
      }
    }

    private string EncodeFilePath(string filePath)
    {
      foreach (KeyValuePair<string, string> enc in _filerPathEncoding)
      {
        filePath = filePath.Replace(enc.Key, enc.Value);
      }
      return filePath;
    }

    private void AddVideoAudioParameters(VideoTranscoding video, TranscodeData data)
    {
      if (video.SourceAudioCodec == AudioCodec.Unknown)
      {
        data.OutputArguments.Add("-an");
        return;
      }
      if (IsAudioStreamChanged(video) == false)
      {
        data.OutputArguments.Add("-c:a copy");
      }
      else
      {
        data.OutputArguments.Add(string.Format("-c:a {0}", FFMpegGetAudioCodec.GetAudioCodec(video.TargetAudioCodec)));
        //if (video.TargetAudioCodec == AudioCodec.Aac || video.TargetAudioCodec == AudioCodec.Dts) //aac encoder not libvo_aacenc is experimental
        if (video.TargetAudioCodec == AudioCodec.Dts)
        {
          data.OutputArguments.Add("-strict experimental");
        }
        long frequency = GetAudioFrequency(video.SourceAudioCodec, video.TargetAudioCodec, video.SourceAudioFrequency, video.TargetAudioFrequency);
        if (frequency != -1)
        {
          data.OutputArguments.Add(string.Format("-ar {0}", frequency));
        }
        if (video.TargetAudioCodec != AudioCodec.Lpcm)
        {
          data.OutputArguments.Add(string.Format("-b:a {0}k", GetAudioBitrate(video.SourceAudioBitrate, video.TargetAudioBitrate)));
        }
        AddAudioChannelsNumberParameters(video, data);
      }
    }

    private void AddAudioChannelsNumberParameters(BaseTranscoding media, TranscodeData data)
    {
      int channels = -1;
      if (media is VideoTranscoding)
      {
        VideoTranscoding video = (VideoTranscoding)media;
        channels = GetAudioNumberOfChannels(video.SourceAudioCodec, video.TargetAudioCodec, video.SourceAudioChannels, video.TargetForceAudioStereo);
      }
      if (media is AudioTranscoding)
      {
        AudioTranscoding audio = (AudioTranscoding)media;
        channels = GetAudioNumberOfChannels(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioChannels, audio.TargetForceAudioStereo);
      }
      if (channels > 0)
      {
        data.OutputArguments.Add(string.Format("-ac {0}", channels));
      }
    }

    #endregion

    #region Transcoding

    public TranscodedAudioMetadata GetTranscodedAudioMetadata(AudioTranscoding audio)
    {
      TranscodedAudioMetadata metadata = new TranscodedAudioMetadata();
      metadata.TargetAudioBitrate = audio.TargetAudioBitrate;
      metadata.TargetAudioCodec = audio.TargetAudioCodec;
      metadata.TargetAudioContainer = audio.TargetAudioContainer;
      metadata.TargetAudioFrequency = audio.TargetAudioFrequency;
      if (IsAudioStreamChanged(audio) == true)
      {
        long frequency = GetAudioFrequency(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioFrequency, audio.TargetAudioFrequency);
        if (frequency > 0)
        {
          metadata.TargetAudioFrequency = frequency;
        }
        if (audio.TargetAudioContainer != AudioContainer.Lpcm)
        {
          metadata.TargetAudioBitrate = GetAudioBitrate(audio.SourceAudioBitrate, audio.TargetAudioBitrate);
        }
      }
      metadata.TargetAudioChannels = GetAudioNumberOfChannels(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioChannels, audio.TargetForceAudioStereo);
      return metadata;
    }

    public TranscodedImageMetadata GetTranscodedImageMetadata(ImageTranscoding image)
    {
      TranscodedImageMetadata metadata = new TranscodedImageMetadata();
      metadata.TargetMaxHeight = image.SourceHeight;
      metadata.TargetMaxWidth = image.SourceWidth;
      metadata.TargetOrientation = image.SourceOrientation;
      metadata.TargetImageCodec = image.TargetImageCodec;
      if (metadata.TargetImageCodec == ImageContainer.Unknown)
      {
        metadata.TargetImageCodec = image.SourceImageCodec;
      }
      metadata.TargetPixelFormat = image.TargetPixelFormat;
      if (metadata.TargetPixelFormat == PixelFormat.Unknown)
      {
        metadata.TargetPixelFormat = image.SourcePixelFormat;
      }
      if (IsImageStreamChanged(image) == true)
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

    public TranscodedVideoMetadata GetTranscodedVideoMetadata(VideoTranscoding video)
    {
      TranscodedVideoMetadata metadata = new TranscodedVideoMetadata
      {
        TargetAudioBitrate = video.TargetAudioBitrate,
        TargetAudioCodec = video.TargetAudioCodec,
        TargetAudioFrequency = video.TargetAudioFrequency,
        TargetVideoFrameRate = video.SourceFrameRate,
        TargetH264Level = video.TargetH264Level,
        TargetH264Preset = video.TargetH264Preset,
        TargetH264Profile = video.TargetH264Profile,
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
      metadata.TargetVideoTimestamp = Timestamp.None;
      if (video.TargetVideoContainer == VideoContainer.M2Ts)
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
      GetVideoDimensions(video, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);
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

      metadata.TargetAudioChannels = GetAudioNumberOfChannels(video.SourceAudioCodec, video.TargetAudioCodec, video.SourceAudioChannels, video.TargetForceAudioStereo);
      long frequency = GetAudioFrequency(video.SourceAudioCodec, video.TargetAudioCodec, video.SourceAudioFrequency, video.TargetAudioFrequency);
      if (frequency != -1)
      {
        metadata.TargetAudioFrequency = frequency;
      }
      if (video.TargetAudioCodec != AudioCodec.Lpcm)
      {
        metadata.TargetAudioBitrate = GetAudioBitrate(video.SourceAudioBitrate, video.TargetAudioBitrate);
      }
      return metadata;
    }

    public TranscodeContext GetMediaStream(BaseTranscoding transcodingInfo, bool waitForBuffer)
    {
      InitSettings();
      if (((ILocalFsResourceAccessor)transcodingInfo.SourceFile).Exists == false)
      {
        if (Logger != null) Logger.Error("MediaConverter: File '{0}' does not exist for transcode '{1}'", transcodingInfo.SourceFile, transcodingInfo.TranscodeID);
        return null;
      }
      else if (transcodingInfo is ImageTranscoding)
      {
        return TranscodeImageFile(transcodingInfo as ImageTranscoding, waitForBuffer);
      }
      else if (transcodingInfo is AudioTranscoding)
      {
        return TranscodeAudioFile(transcodingInfo as AudioTranscoding, waitForBuffer);
      }
      else if (transcodingInfo is VideoTranscoding)
      {
        return TranscodeVideoFile(transcodingInfo as VideoTranscoding, waitForBuffer);
      }
      if (Logger != null) Logger.Error("MediaConverter: Transcoding info is not valid for transcode '{0}'", transcodingInfo.TranscodeID);
      return null;
    }

    private TranscodeContext TranscodeVideoFile(VideoTranscoding video, bool waitForBuffer)
    {
      TranscodeContext context = new TranscodeContext();
      context.Failed = false;
      string transcodingFile = Path.Combine(TranscoderCachePath, video.TranscodeID);
      transcodingFile += ".A" + video.SourceAudioStreamIndex;
      bool embeddedSupported = false;
      SubtitleCodec embeddedSubCodec = SubtitleCodec.Unknown;
      if (video.TargetSubtitleSupport == SubtitleSupport.Embedded)
      {
        if(video.TargetVideoContainer == VideoContainer.Matroska)
        {
          embeddedSupported = true;
          embeddedSubCodec = SubtitleCodec.Ass;
          video.TargetSubtitleCodec = SubtitleCodec.Ass;
        }
        else if (video.TargetVideoContainer == VideoContainer.Mp4)
        {
          embeddedSupported = true;
          embeddedSubCodec = SubtitleCodec.MovTxt;
          video.TargetSubtitleCodec = SubtitleCodec.Ass;
        }
        //else if (video.TargetVideoContainer == VideoContainer.Mpeg2Ts)
        //{
        //  embeddedSupported = true;
        //  embeddedSubCodec = SubtitleCodec.DvbSub;
        //  video.TargetSubtitleCodec = SubtitleCodec.Ass;
        //}
      }
      Subtitle currentSub = GetSubtitle(video);
      if (currentSub != null && _supportHardcodedSubs == true && (embeddedSupported || video.TargetSubtitleSupport == SubtitleSupport.HardCoded))
      {
        if (string.IsNullOrEmpty(currentSub.Language) == false)
        {
          transcodingFile += ".S" + currentSub.Language;
        }
      }
      transcodingFile += ".mptv";

      if (File.Exists(transcodingFile) == true)
      {
        if (RunningTranscodes.ContainsKey(video.TranscodeID) == false)
        {
          TouchFile(transcodingFile);
        }
        context.TargetFile = transcodingFile;
        context.Start(GetReadyFileBuffer(transcodingFile));
        return context;
      }
      if(video.TargetVideoContainer == VideoContainer.Hls)
      {
        string pathName = Path.Combine(TranscoderCachePath, Path.GetFileNameWithoutExtension(transcodingFile).Replace(".", "_") + "_mptf");
        string playlist = Path.Combine(pathName, "playlist.m3u8");
        if (File.Exists(playlist) == true)
        {
          if (RunningTranscodes.ContainsKey(video.TranscodeID) == false)
          {
            TouchDirectory(pathName);
          }
          context.TargetFile = playlist;
          context.SegmentDir = pathName;
          context.Start(GetReadyFileBuffer(playlist));
          return context;
        }
      }

      TranscodeData data = new TranscodeData(TranscoderBinPath, TranscoderCachePath);
      data.TranscodeId = video.TranscodeID;
      if (string.IsNullOrEmpty(video.TranscoderBinPath) == false)
      {
        data.TranscoderBinPath = video.TranscoderBinPath;
      }
      if (string.IsNullOrEmpty(video.TranscoderArguments) == false)
      {
        data.TranscoderArguments = video.TranscoderArguments;
        data.InputResourceAccessor = video.SourceFile;
        if (video.SourceSubtitle != null)
        {
          data.InputSubtitleFilePath = video.SourceSubtitle.Source;
        }
        data.OutputFilePath = transcodingFile;
      }
      else
      {
        InitTranscodingParameters(video.SourceFile, data);

        bool useX26XLib = false;
        if (video.TargetVideoCodec == VideoCodec.H264 || video.TargetVideoCodec == VideoCodec.H265)
        {
          useX26XLib = true;
        }
        AddTranscodingThreadsParameters(!useX26XLib, data);

        AddVideoParameters(video, currentSub, data);
        AddTargetVideoFormatAndOutputFileParameters(video, transcodingFile, data);
        AddVideoAudioParameters(video, data);
        if (currentSub != null && embeddedSupported)
        {
          AddSubtitleEmbeddingParameters(currentSub, embeddedSubCodec, data);
        }
        else
        {
          embeddedSupported = false;
          data.OutputArguments.Add("-sn");
        }
        AddStreamMapParameters(video.SourceVideoStreamIndex, video.SourceAudioStreamIndex, embeddedSupported, data);
      }
      context.TargetFile = transcodingFile;

      if (Logger != null) Logger.Info("MediaConverter: Invoking transcoder to transcode video file '{0}' for transcode '{1}' with arguments '{2}'", video.SourceFile, video.TranscodeID, String.Join(", ", data.OutputArguments.ToArray()));
      context.Start(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    private TranscodeContext TranscodeAudioFile(AudioTranscoding audio, bool waitForBuffer)
    {
      TranscodeContext context = new TranscodeContext();
      context.Failed = false;
      string transcodingFile = Path.Combine(TranscoderCachePath, audio.TranscodeID + ".mpta");
      if (File.Exists(transcodingFile) == true)
      {
        if (RunningTranscodes.ContainsKey(audio.TranscodeID) == false)
        {
          TouchFile(transcodingFile);
        }
        context.TargetFile = transcodingFile;
        context.Start(GetReadyFileBuffer(transcodingFile));
        return context;
      }

      TranscodeData data = new TranscodeData(TranscoderBinPath, TranscoderCachePath);
      data.TranscodeId = audio.TranscodeID;
      if (string.IsNullOrEmpty(audio.TranscoderBinPath) == false)
      {
        data.TranscoderBinPath = audio.TranscoderBinPath;
      }
      if (string.IsNullOrEmpty(audio.TranscoderArguments) == false)
      {
        data.TranscoderArguments = audio.TranscoderArguments;
        data.InputResourceAccessor = audio.SourceFile;
      }
      else
      {
        InitTranscodingParameters(audio.SourceFile, data);
        AddTranscodingThreadsParameters(true, data);

        AddAudioParameters(audio, data);

        data.OutputArguments.Add(string.Format("-f {0}", FFMpegGetAudioContainer.GetAudioContainer(audio.TargetAudioContainer)));
        data.OutputArguments.Add("-vn");
      }
      data.OutputFilePath = transcodingFile;
      context.TargetFile = transcodingFile;

      if (Logger != null) Logger.Debug("MediaConverter: Invoking transcoder to transcode audio file '{0}' for transcode '{1}'", audio.SourceFile, audio.TranscodeID);
      context.Start(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    private TranscodeContext TranscodeImageFile(ImageTranscoding image, bool waitForBuffer)
    {
      TranscodeContext context = new TranscodeContext();
      context.Failed = false;
      string transcodingFile = Path.Combine(TranscoderCachePath, image.TranscodeID + ".mpti");
      if (File.Exists(transcodingFile) == true)
      {
        if (RunningTranscodes.ContainsKey(image.TranscodeID) == false)
        {
          TouchFile(transcodingFile);
        }
        context.TargetFile = transcodingFile;
        context.Start(GetReadyFileBuffer(transcodingFile));
        return context;
      }

      TranscodeData data = new TranscodeData(TranscoderBinPath, TranscoderCachePath);
      data.TranscodeId = image.TranscodeID;
      if (string.IsNullOrEmpty(image.TranscoderBinPath) == false)
      {
        data.TranscoderBinPath = image.TranscoderBinPath;
      }
      if (string.IsNullOrEmpty(image.TranscoderArguments) == false)
      {
        data.TranscoderArguments = image.TranscoderArguments;
        data.InputResourceAccessor = image.SourceFile;
      }
      else
      {
        InitTranscodingParameters(image.SourceFile, data);
        AddTranscodingThreadsParameters(true, data);

        AddImageParameters(image, data);

        data.OutputArguments.Add("-f image2");
      }
      data.OutputFilePath = transcodingFile;
      context.TargetFile = transcodingFile;

      if (Logger != null) Logger.Debug("MediaConverter: Invoking transcoder to transcode image file '{0}' for transcode '{1}'", image.SourceFile, image.TranscodeID);
      context.Start(ExecuteTranscodingProcess(data, context, waitForBuffer));
      return context;
    }

    public BufferedStream GetSubtitleStream(VideoTranscoding video)
    {
      Subtitle sub = GetSubtitle(video);
      if (sub == null || sub.SourceFile == null)
      {
        return null;
      }
      if (RunningTranscodes.ContainsKey(video.TranscodeID) == false)
      {
        TouchFile(sub.SourceFile);
      }
      return GetReadyFileBuffer(sub.SourceFile);
    }

    private bool SubtitleIsUnicode(string encoding)
    {
      if(string.IsNullOrEmpty(encoding))
      {
        return false;
      }
      if(encoding.ToUpperInvariant().StartsWith("UTF-") || encoding.ToUpperInvariant().StartsWith("UNICODE"))
      {
        return true;
      }
      return false;
    }

    private Subtitle GetSubtitle(VideoTranscoding video)
    {
      if (video.SourceSubtitle == null) return null;
      if (video.TargetSubtitleSupport == SubtitleSupport.None) return null;

      Subtitle res = new Subtitle
      {
        Codec = video.SourceSubtitle.Codec,
        Language = video.SourceSubtitle.Language,
        SourceFile = video.SourceSubtitle.Source,
        CharacterEncoding = SubtitleAnalyzer.GetEncoding(video.SourceSubtitle.Source, video.SourceSubtitle.Language, SubtitleDefaultEncoding)
      };

      // SourceSubtitle == TargetSubtitleCodec -> just return
      if(video.TargetSubtitleCodec != SubtitleCodec.Unknown && video.TargetSubtitleCodec == video.SourceSubtitle.Codec)
      {
        return res;
      }

      // create a file name for the output file which contains the subtitle informations
      string transcodingFile = Path.Combine(TranscoderCachePath, video.TranscodeID);
      if (video.SourceSubtitle != null && string.IsNullOrEmpty(video.SourceSubtitle.Language) == false)
      {
        transcodingFile += "." + video.SourceSubtitle.Language;
      }
      transcodingFile += ".mpts";
      SubtitleCodec targetCodec = video.TargetSubtitleCodec;
      if(targetCodec == SubtitleCodec.Unknown)
      {
        targetCodec = video.SourceSubtitle.Codec;
      }

      // the file already exists in the cache -> just return
      if (File.Exists(transcodingFile))
      {
        if (RunningTranscodes.ContainsKey(video.TranscodeID) == false)
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
      if (video.SourceSubtitle.IsEmbedded)
      {
        ExtractSubtitleFile(video, video.SourceSubtitle, res.CharacterEncoding, transcodingFile);
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

      TranscodeData data = new TranscodeData(TranscoderBinPath, TranscoderCachePath);
      data.TranscodeId = video.TranscodeID + "_sub";
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
            string newFile = transcodingFile.Replace(".mpts", ".utf8.mpts");
            File.WriteAllText(newFile, File.ReadAllText(res.SourceFile, Encoding.GetEncoding(res.CharacterEncoding)), Encoding.UTF8);
            res.CharacterEncoding = "UTF-8";
            res.SourceFile = newFile;
            if (Logger != null) Logger.Debug("MediaConverter: Converted subtitle file '{0}' to UTF-8 for transcode '{1}'", video.SourceSubtitle.Source, data.TranscodeId);
          }
        }

        // TODO: not sure if this is working
        data.TranscoderArguments = video.TranscoderArguments;
        LocalFsResourceProvider localFsResourceProvider = new LocalFsResourceProvider();
        IResourceAccessor resourceAccessor = new LocalFsResourceAccessor(localFsResourceProvider, res.SourceFile);
        InitTranscodingParameters(resourceAccessor, data);
        data.InputArguments.Add(string.Format("-f {0}", FFMpegGetSubtitleContainer.GetSubtitleContainer(video.SourceSubtitle.Codec)));

        res.Codec = targetCodec;
        string subtitleEncoder = "copy";
        if (res.Codec == SubtitleCodec.Unknown)
        {
          res.Codec = SubtitleCodec.Ass;
        }
        if (video.SourceSubtitle.Codec != res.Codec)
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

      if (Logger != null) Logger.Debug("MediaConverter: Invoking transcoder to transcode subtitle file '{0}' for transcode '{1}'", res.SourceFile, data.TranscodeId);
      FileProcessor(data);
      if (File.Exists(transcodingFile) == true)
      {
        res.SourceFile = transcodingFile;
        return res;
      }
      return null;
    }

    public BufferedStream GetReadyFileBuffer(ILocalFsResourceAccessor lfsra)
    {
      int iTry = 60;
      while (iTry > 0)
      {
        if (lfsra.Exists)
        {
          if (lfsra.Size > 0)
          {
            if (Logger != null) Logger.Debug(string.Format("MediaConverter: Serving transcoded file '{0}'", lfsra.LocalFileSystemPath));
            // Impersonation
            using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
            {
              BufferedStream stream = new BufferedStream(new FileStream(lfsra.LocalFileSystemPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
              return stream;
            }
          }
        }
        iTry--;
        Thread.Sleep(500);
      }
      if (Logger != null) Logger.Error("MediaConverter: Timed out waiting for transcoded file '{0}'", lfsra.LocalFileSystemPath);
      return null;
    }

    public BufferedStream GetReadyFileBuffer(string filePath)
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
            if (Logger != null) Logger.Debug(string.Format("MediaConverter: Serving transcoded file '{0}'", filePath));
            BufferedStream stream = new BufferedStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            return stream;
          }
        }
        iTry--;
        Thread.Sleep(500);
      }
      if (Logger != null) Logger.Error("MediaConverter: Timed out waiting for transcoded file '{0}'", filePath);
      return null;
    }

    #endregion

    #region Transcoder

    private BufferedStream GetTranscodedFileBuffer(TranscodeData data)
    {
      string filePath = "";
      if (data.SegmentPlaylist != null)
      {
        filePath = Path.Combine(data.WorkPath, data.SegmentPlaylist);
      }
      else
      {
        filePath = Path.Combine(data.WorkPath, data.OutputFilePath);
      }

      TranscodeContext context = RunningTranscodes[data.TranscodeId];
      int iTry = 60;
      while (iTry > 0 && context.Failed == false && context.Aborted == false)
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
            if (Logger != null) Logger.Debug(string.Format("MediaConverter: Serving transcoded file '{0}'", filePath));
            BufferedStream stream = new BufferedStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            return stream;
          }
        }
        iTry--;
        Thread.Sleep(500);
      }
      if (Logger != null) Logger.Error("MediaConverter: Timed out waiting for transcoded file '{0}'", filePath);
      return null;
    }

    private BufferedStream ExecuteTranscodingProcess(TranscodeData data, TranscodeContext context, bool waitForBuffer)
    {
      if (RunningTranscodes.ContainsKey(data.TranscodeId) == false)
      {
        try
        {

          RunningTranscodes.Add(data.TranscodeId, context);
          Thread transcodeThread = new Thread(TranscodeProcessor)
          {
            IsBackground = true,
            Name = "MP Transcode - " + data.TranscodeId,
            Priority = ThreadPriority.Normal
          };
          transcodeThread.Start(data);
        }
        catch
        {
          RunningTranscodes.Remove(data.TranscodeId);
          context.Running = false;
          context.Failed = true;
          throw;
        }
      }

      if (waitForBuffer == false) return null;
      return GetTranscodedFileBuffer(data);
    }

    private void TranscodeProcessor(object args)
    {
      TranscodeData data = (TranscodeData)args;

      //Process ffmpeg = new Process();
      TranscodeContext context = RunningTranscodes[data.TranscodeId];
      context.TargetFile = Path.Combine(data.WorkPath, data.SegmentPlaylist != null ? data.SegmentPlaylist : data.OutputFilePath);
      context.SegmentDir = null;
      if(data.SegmentPlaylist != null)
      {
        context.SegmentDir = data.WorkPath;
      }

      if (Logger != null) Logger.Debug("MediaConverter: Transcoder '{0}' invoked with command line arguments '{1}'", ServiceRegistration.Get<IFFMpegLib>().FFMpegBinaryPath, data.TranscoderArguments);
      Task<ProcessExecutionResult> executionResult = ServiceRegistration.Get<IFFMpegLib>().FFMpegExecuteWithResourceAccessAsync((ILocalFsResourceAccessor)data.InputResourceAccessor, data.TranscoderArguments, ProcessPriorityClass.Normal, ProcessUtils.INFINITE);

      //ffmpeg.StartInfo.FileName = data.TranscoderBinPath;
      //ffmpeg.StartInfo.Arguments = data.TranscoderArguments;
      //ffmpeg.StartInfo.WorkingDirectory = data.WorkPath;
      //if (Logger != null) Logger.Debug("MediaConverter: Transcoder '{0}' invoked with command line arguments '{1}'", ffmpeg.StartInfo.FileName, ffmpeg.StartInfo.Arguments);
      //ffmpeg.StartInfo.CreateNoWindow = true;
      //ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
      //ffmpeg.StartInfo.UseShellExecute = false;
      /*ffmpeg.StartInfo.RedirectStandardError = true;
      ffmpeg.StartInfo.RedirectStandardOutput = true;
      ffmpeg.OutputDataReceived += context.FFMPEG_OutputDataReceived;
      ffmpeg.ErrorDataReceived += context.FFMPEG_ErrorDataReceived;*/

      context.Running = true;
      context.Failed = false;
      /*ffmpeg.Start();
      ffmpeg.BeginErrorReadLine();
      ffmpeg.BeginOutputReadLine();*/
      int iExitCode = -1;
      Logger.Info("Above While");
      while (executionResult.Status == TaskStatus.Running)
      {
        if (context.Running == false)
        {
          // TODO: Implement process abort
          context.Aborted = true;
          //ffmpeg.Kill();
          break;
        }
        Thread.Sleep(5);
      }
      //ffmpeg.WaitForExit();
      //iExitCode = ffmpeg.ExitCode;
      iExitCode = executionResult.Result.ExitCode;
      RunningTranscodes.Remove(data.TranscodeId);
      //ffmpeg.Close();
      //ffmpeg.Dispose();
      if(iExitCode > 0)
      {
        context.Failed = true;
      }
      context.Running = false;

      string deletePath = context.TargetFile;
      bool isFolder = false;
      if (deletePath.EndsWith(".m3u8") == true)
      {
        deletePath = context.SegmentDir;
        isFolder = true;
      }
      if (iExitCode > 0 || context.Aborted == true)
      {
        if (iExitCode > 0)
        {
          if (Logger != null) Logger.Debug("MediaConverter: Transcoder command failed with error {1} for file '{0}'", data.OutputFilePath, iExitCode);
        }
        if (context.Aborted == true)
        {
          context.Stop();
          if (Logger != null) Logger.Debug("MediaConverter: Transcoder command aborted for file '{0}'", data.OutputFilePath);
        }

        int iTry = 5;
        while (iTry > 0)
        {
          if (isFolder == false)
          {
            if (File.Exists(deletePath))
            {
              try
              {
                File.Delete(deletePath);
              }
              catch
              {
              }
            }
            else
            {
              break;
            }
          }
          else
          {
            if (Directory.Exists(deletePath))
            {
              try
              {
                Directory.Delete(deletePath, true);
              }
              catch
              {
              }
            }
            else
            {
              break;
            }
          }
          Thread.Sleep(500);
          iTry--;
        }
      }
      else
      {
        if (isFolder == false)
        {
          TouchFile(deletePath);
        }
        else
        {
          TouchDirectory(deletePath);
        }
      }
    }

    private void FileProcessor(TranscodeData data)
    {
      DateTime dtStart = DateTime.Now;
      Process ffmpeg = new Process();
      ffmpeg.StartInfo.FileName = data.TranscoderBinPath;
      ffmpeg.StartInfo.Arguments = data.TranscoderArguments;
      ffmpeg.StartInfo.WorkingDirectory = data.WorkPath;
      ffmpeg.StartInfo.CreateNoWindow = true;
      ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
      ffmpeg.Start();
      while (ffmpeg.HasExited == false && DateTime.Now < dtStart.AddMilliseconds(TranscoderTimeout))
      {
        Thread.Sleep(5);
      }
      ffmpeg.Close();
      ffmpeg.Dispose();
    }

    #endregion
  }

  public class TranscodeContext
  {
    StringBuilder _errorOutput = new StringBuilder();
    StringBuilder _standardOutput = new StringBuilder();
    public string TargetFile { get; internal set; }
    public string SegmentDir { get; internal set; }
    public bool Aborted { get; internal set; }
    public bool Failed { get; internal set; }
    public string ConsoleErrorOutput 
    { 
      get
      {
        return _errorOutput.ToString();
      }
    }
    public string ConsoleOutput
    {
      get
      {
        return _standardOutput.ToString();
      }
    }
    public bool Running { get; internal set; }
    public BufferedStream TranscodedStream { get; private set; }

    internal void FFMPEG_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
      _errorOutput.Append(e.Data);
    }

    internal void FFMPEG_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
      _standardOutput.Append(e.Data);
    }

    public void Start(BufferedStream stream)
    {
      Running = true;
      Aborted = false;
      TranscodedStream = stream;
    }

    public void Stop()
    {
      Running = false;
    }
  }
}
