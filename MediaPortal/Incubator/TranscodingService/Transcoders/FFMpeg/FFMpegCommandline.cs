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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Diagnostics;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Converters;
using MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Encoders;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using MediaPortal.Extensions.TranscodingService.Interfaces.Analyzers;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;
using System.Threading.Tasks;
using MediaPortal.Utilities.Process;
using MediaPortal.Extensions.TranscodingService.Interfaces.Settings;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg
{
  internal class FFMpegCommandline
  {
    private const int DEFAULT_SOURCE_HEIGHT = 0;
    private const int DEFAULT_SOURCE_WIDTH = 0;

    private int _transcoderMaximumThreads;
    private int _transcoderTimeout;
    private string _transcoderCachePath;
    private int _hlsSegmentTimeInSeconds;
    private string _hlsSegmentFileTemplate;
    private readonly Dictionary<string, string> _filerPathEncoding = new Dictionary<string, string>()
    {
      {@"\", @"\\"},
      {",", @"\,"},
      {":", @"\:"},
      {";", @"\;"},
      {"'", @"\'"},
      {"[", @"\["},
      {"]", @"\]"}
    };
    private readonly Dictionary<string, string> _isoMap = new Dictionary<string, string>
    {
      { "bod", "tib" },
      { "ces", "cze" },
      { "cym", "wel" },
      { "deu", "ger" },
      { "ell", "gre" },
      { "eus", "baq" },
      { "fas", "per" },
      { "fra", "fre" },
      { "hye", "arm" },
      { "isl", "ice" },
      { "kat", "geo" },
      { "mkd", "mac" },
      { "mri", "mao" },
      { "msa", "may" },
      { "mya", "bur" },
      { "nld", "dut" },
      { "ron", "rum" },
      { "slk", "slo" },
      { "sqi", "alb" },
      { "zho", "chi" },
    };
    private readonly Dictionary<QualityMode, string> VideoQualityModes = new Dictionary<QualityMode, string>()
    {
      { QualityMode.Default, "25" },
      { QualityMode.Best, "10" },
      { QualityMode.Normal, "25" },
      { QualityMode.Low, "35" }
    };
    private readonly Dictionary<QualityMode, string> VideoScaleModes = new Dictionary<QualityMode, string>()
    {
      { QualityMode.Default, "2" },
      { QualityMode.Best, "1" },
      { QualityMode.Normal, "2" },
      { QualityMode.Low, "10" }
    };
    private readonly Dictionary<QualityMode, string> ImageScaleModes = new Dictionary<QualityMode, string>()
    {
      { QualityMode.Default, "2" },
      { QualityMode.Best, "0" },
      { QualityMode.Normal, "2" },
      { QualityMode.Low, "10" }
    };
    private readonly Dictionary<Coder, string> Coders = new Dictionary<Coder, string>()
    {
      { Coder.Default, "" },
      { Coder.Arithmic, "-coder ac" },
      { Coder.Deflate, "-coder deflate" },
      { Coder.None, "" },
      { Coder.Raw, "-coder raw" },
      { Coder.RunLength, "-coder rle" },
      { Coder.VariableLength, "-coder vlc" }
    };


    internal FFMpegCommandline(int maxThreads, int commandTimeout, string cachePath, int hlsSegmentDuration, string hlsSegmentTemplate)
    {
      _transcoderMaximumThreads = maxThreads;
      _transcoderTimeout = commandTimeout;
      _transcoderCachePath = cachePath;
      _hlsSegmentTimeInSeconds = hlsSegmentDuration;
      _hlsSegmentFileTemplate = hlsSegmentTemplate;
    }

    internal void GetVideoDimensions(VideoTranscoding video, out Size newSize, out Size newContentSize, out float newPixelAspectRatio, out bool pixelARChanged, out bool videoARChanged, out bool videoHeightChanged)
    {
      newSize = new Size(video.FirstSourceVideoStream.Width ?? DEFAULT_SOURCE_WIDTH, video.FirstSourceVideoStream.Height ?? DEFAULT_SOURCE_HEIGHT);
      newContentSize = new Size(video.FirstSourceVideoStream.Width ?? DEFAULT_SOURCE_WIDTH, video.FirstSourceVideoStream.Height ?? DEFAULT_SOURCE_HEIGHT);
      newPixelAspectRatio = video.FirstSourceVideoStream.PixelAspectRatio ?? 1;
      pixelARChanged = false;
      videoARChanged = false;
      videoHeightChanged = false;

      if (Checks.IsSquarePixelNeeded(video) && video.FirstSourceVideoStream.Width.HasValue && video.FirstSourceVideoStream.Height.HasValue)
      {
        newSize.Width = Convert.ToInt32(Math.Round((double)video.FirstSourceVideoStream.Width.Value * video.FirstSourceVideoStream.PixelAspectRatio ?? 1));
        newSize.Height = video.FirstSourceVideoStream.Height.Value;
        newContentSize.Width = newSize.Width;
        newContentSize.Height = newSize.Height;
        newPixelAspectRatio = 1;
        pixelARChanged = true;
      }
      if (Checks.IsVideoAspectRatioChanged(newSize.Width, newSize.Height, newPixelAspectRatio, video.TargetVideoAspectRatio) &&
        video.FirstSourceVideoStream.AspectRatio.HasValue && video.TargetVideoAspectRatio.HasValue)
      {
        double sourceNewAspectRatio = (double)newSize.Width / (double)newSize.Height * video.FirstSourceVideoStream.AspectRatio.Value;
        if (sourceNewAspectRatio < video.FirstSourceVideoStream.AspectRatio)
          newSize.Width = Convert.ToInt32(Math.Round((double)newSize.Height * video.TargetVideoAspectRatio.Value / newPixelAspectRatio));
        else
          newSize.Height = Convert.ToInt32(Math.Round((double)newSize.Width * newPixelAspectRatio / video.TargetVideoAspectRatio.Value));

        videoARChanged = true;
      }
      if (Checks.IsVideoHeightChangeNeeded(newSize.Height, video.TargetVideoMaxHeight) && video.TargetVideoMaxHeight.HasValue)
      {
        double oldWidth = newSize.Width;
        double oldHeight = newSize.Height;
        newSize.Width = Convert.ToInt32(Math.Round(newSize.Width * ((double)video.TargetVideoMaxHeight.Value / (double)newSize.Height)));
        newSize.Height = video.TargetVideoMaxHeight.Value;
        newContentSize.Width = Convert.ToInt32(Math.Round((double)newContentSize.Width * ((double)newSize.Width / oldWidth)));
        newContentSize.Height = Convert.ToInt32(Math.Round((double)newContentSize.Height * ((double)newSize.Height / oldHeight)));
        videoHeightChanged = true;
      }
      //Correct widths
      newSize.Width = ((newSize.Width + 1) / 2) * 2;
      newContentSize.Width = ((newContentSize.Width + 1) / 2) * 2;
    }

    internal void InitTranscodingParameters(Dictionary<int, IResourceAccessor> sourceFiles, ref FFMpegTranscodeData data)
    {
      foreach (var mediaSourceIndex in sourceFiles.Keys)
        data.InputArguments.Add(mediaSourceIndex, new List<string>());

      data.InputResourceAccessor = sourceFiles;
      AddInputOptions(ref data);
      data.OutputArguments.Add("-y");
    }

    private void AddInputOptions(ref FFMpegTranscodeData data)
    {
      if(TranscodingServicePlugin.Settings.HardwareAcceleration == HWAcceleration.Auto)
      {
        data.GlobalArguments.Add("-hwaccel auto");
      }
      else if (TranscodingServicePlugin.Settings.HardwareAcceleration == HWAcceleration.DirectX11)
      {
        data.GlobalArguments.Add("-hwaccel d3d11va");
      }
      else if (TranscodingServicePlugin.Settings.HardwareAcceleration == HWAcceleration.DXVA2)
      {
        data.GlobalArguments.Add("-hwaccel dxva2");
      }
      else if (TranscodingServicePlugin.Settings.HardwareAcceleration == HWAcceleration.Intel)
      {
        data.GlobalArguments.Add("-hwaccel qsv");
      }
      else if (TranscodingServicePlugin.Settings.HardwareAcceleration == HWAcceleration.Nvidia)
      {
        data.GlobalArguments.Add("-hwaccel cuvid");
      }

      bool isNetworkResource = false;
      if (data.FirstResourceAccessor is INetworkResourceAccessor)
        isNetworkResource = true;

      Logger.Debug("FFMpegMediaConverter: AddInputOptions() is NetworkResource: {0}", isNetworkResource);
      if (isNetworkResource)
      {
        var accessor = data.FirstResourceAccessor as INetworkResourceAccessor;
        if (accessor != null)
        {
          if (accessor.URL.StartsWith("rtsp://", StringComparison.InvariantCultureIgnoreCase))
            data.GlobalArguments.Add("-rtsp_transport +tcp+udp");

          data.GlobalArguments.Add("-analyzeduration 10000000");
        }
      }
    }

    internal void AddTranscodingThreadsParameters(bool useOutputThreads, ref FFMpegTranscodeData data)
    {
      data.InputArguments[0].Add(string.Format("-threads {0}", _transcoderMaximumThreads));
      if (useOutputThreads)
        data.OutputArguments.Add(string.Format("-threads {0}", _transcoderMaximumThreads));
    }

    internal async Task<(string TranscodingFile, long StartSegment)> AddTargetVideoFormatAndOutputFileParametersAsync(VideoTranscoding video, string transcodingFile, double timeStart, FFMpegTranscodeData data)
    {
      try
      {
        data.SegmentManifestData = null;
        data.SegmentPlaylistData = null;
        data.SegmentSubsPlaylistData = null;
        long startSegment = 0;
        if (video.TargetVideoContainer == VideoContainer.Hls)
        {
          data.WorkPath = FFMpegPlaylistManifest.GetPlaylistFolderFromTranscodeFile(_transcoderCachePath, transcodingFile);
          SubtitleStream sub = video.FirstPreferredSourceSubtitle;

          string outputFileName = BaseMediaConverter.PLAYLIST_FILE_NAME;
          startSegment = Convert.ToInt64(timeStart / Convert.ToDouble(_hlsSegmentTimeInSeconds));
          if (video.TargetSubtitleSupport == SubtitleSupport.Embedded)
            data.SegmentManifestData = await PlaylistManifest.CreatePlaylistManifestAsync(video, sub).ConfigureAwait(false);
          else
            data.SegmentManifestData = await PlaylistManifest.CreatePlaylistManifestAsync(video, null).ConfigureAwait(false);

          //Below can be used create the full playlists to better support seeking for content not yet fully transcoded
          //Because segment durations are not always exactly the size specified this can cause stuttering
          if (video.TargetIsLive == false)
          {
            outputFileName = BaseMediaConverter.PLAYLIST_TEMP_FILE_NAME;
            data.SegmentPlaylistData = await PlaylistManifest.CreateVideoPlaylistAsync(video, startSegment).ConfigureAwait(false);
            if (video.TargetSubtitleSupport == SubtitleSupport.Embedded && sub != null)
              data.SegmentSubsPlaylistData = await PlaylistManifest.CreateSubsPlaylistAsync(video, startSegment).ConfigureAwait(false);
          }
          data.SegmentPlaylist = Path.Combine(data.WorkPath, PlaylistManifest.PLAYLIST_MANIFEST_FILE_NAME);
          data.SegmentBaseUrl = video.HlsBaseUrl;

          string fileSegments = Path.Combine(data.WorkPath, _hlsSegmentFileTemplate);

          //HLS muxer
          data.OutputArguments.Add("-hls_allow_cache 0");
          data.OutputArguments.Add(string.Format("-hls_time {0}", _hlsSegmentTimeInSeconds));

          //Single file segment nicer but uses version 4 playlists
          //data.OutputArguments.Add("-hls_list_size 0");
          //data.OutputArguments.Add("-hls_flags single_file");
          //data.OutputArguments.Add(string.Format("-hls_segment_filename {0}", "\"segment.ts\""));

          //Multi file segments more compatible
          long segmentBufferSize = Convert.ToInt64(300.0 / Convert.ToDouble(_hlsSegmentTimeInSeconds)) + 1; //5 min buffer
          if (video.TargetIsLive)
          {
            data.OutputArguments.Add(string.Format("-hls_list_size {0}", segmentBufferSize));
            data.OutputArguments.Add(string.Format("-hls_segment_filename {0}", "\"" + fileSegments + "\""));
            data.OutputArguments.Add(string.Format("-hls_wrap {0}", segmentBufferSize * 2));
            data.OutputArguments.Add("-hls_flags delete_segments");
            data.IsLive = true;
          }
          else
          {
            if (video.SourceMediaDurations.Count > 0)
              segmentBufferSize = Convert.ToInt64(video.SourceMediaTotalDuration.TotalSeconds / Convert.ToDouble(_hlsSegmentTimeInSeconds)) + 1;

            data.OutputArguments.Add(string.Format("-hls_list_size {0}", segmentBufferSize));
            data.OutputArguments.Add(string.Format("-hls_segment_filename {0}", "\"" + fileSegments + "\""));
            data.OutputArguments.Add(string.Format("-start_number {0}", startSegment));
          }
          data.OutputArguments.Add("-segment_list_flags +live");
          data.OutputArguments.Add(string.Format("-hls_base_url {0}", "\"" + PlaylistManifest.URL_PLACEHOLDER + "\""));
          data.OutputFilePath = Path.Combine(data.WorkPath, outputFileName);
          transcodingFile = data.SegmentPlaylist;
          data.IsStream = false;
        }
        else
        {
          data.OutputArguments.Add(string.Format("-f {0}", FFMpegGetVideoContainer.GetVideoContainer(video.TargetVideoContainer)));
          if (video.TargetIsLive)
          {
            var accessor = data.FirstResourceAccessor as INetworkResourceAccessor;
            if (accessor == null)
              data.InputArguments[0].Add("-re"); //Simulate live stream from file

            data.IsLive = true;
            data.IsStream = true;
            data.OutputFilePath = null;
            transcodingFile = "";
          }
          else
          {
            data.OutputFilePath = transcodingFile;
            data.IsStream = false;
          }
        }

        if (video.Movflags != null)
          data.OutputArguments.Add(string.Format("-movflags {0}", video.Movflags));

        return (transcodingFile, startSegment);
      }
      catch (Exception ex)
      {
        Logger?.Error("FFMpegMediaConverter: Error adding input and output parameters ", ex);
      }
      return ("", 0);
    }

    internal Task<string> AddTargetAudioFormatAndOutputFileParametersAsync(AudioTranscoding audio, string transcodingFile, FFMpegTranscodeData data)
    {
      data.OutputArguments.Add(string.Format("-f {0}", FFMpegGetAudioContainer.GetAudioContainer(audio.TargetAudioContainer)));
      if (audio.TargetIsLive)
      {
        var accessor = data.FirstResourceAccessor as INetworkResourceAccessor;
        if (accessor == null)
          data.InputArguments[0].Add("-re"); //Simulate live stream from file

        data.IsLive = true;
        data.IsStream = true;
        data.OutputFilePath = null;
      }
      else
      {
        data.OutputFilePath = transcodingFile;
        data.IsStream = false;
      }
      return Task.FromResult(transcodingFile);
    }

    internal async Task<bool> ExtractSubtitleFileAsync(int sourceMediaIndex, VideoTranscoding video, SubtitleStream subtitle, string subtitleEncoding, string targetFilePath, double timeStart)
    {
      string subtitleEncoder = "copy";
      SubtitleCodec targetCodec = video.TargetSubtitleCodec;
      if (targetCodec == SubtitleCodec.Unknown)
        targetCodec = subtitle.Codec;
   
      if (targetCodec == SubtitleCodec.Unknown)
        targetCodec = SubtitleCodec.Ass;
   
      if (subtitle.Codec != targetCodec)
        subtitleEncoder = FFMpegGetSubtitleContainer.GetSubtitleContainer(targetCodec);
 
      string subtitleFormat = FFMpegGetSubtitleContainer.GetSubtitleContainer(subtitle.Codec);
      FFMpegTranscodeData data = new FFMpegTranscodeData(_transcoderCachePath);
      InitTranscodingParameters(video.SourceMedia, ref data);
      AddSubtitleExtractionParameters(subtitle, subtitleEncoding, subtitleEncoder, subtitleFormat, timeStart, data);
      data.OutputFilePath = targetFilePath;

      Logger?.Debug("FFMpegMediaConverter: Invoking transcoder to extract subtitle from file '{0}'", video.SourceMedia[sourceMediaIndex]);

      ProcessExecutionResult result = await FFMpegBinary.FFMpegExecuteWithResourceAccessAsync((ILocalFsResourceAccessor)data.InputResourceAccessor[sourceMediaIndex], data.TranscoderArguments, ProcessPriorityClass.Normal, _transcoderTimeout);
      bool success = result.Success;
      if (success && File.Exists(targetFilePath) == false)
      {
        Logger?.Error("FFMpegMediaConverter: Failed to extract subtitle from file '{0}'", video.SourceMedia[sourceMediaIndex]);
        return false;
      }
      return true;
    }

    internal void AddSubtitleCopyParameters(SubtitleStream subtitle, FFMpegTranscodeData data)
    {
      if (subtitle == null)
        return;

      data.OutputArguments.Add("-c:s copy");
      if (string.IsNullOrEmpty(subtitle.Language) == false)
      {
        string languageName = Get3LetterLanguage(subtitle.Language);
        if (string.IsNullOrEmpty(languageName) == false)
          data.OutputArguments.Add(string.Format("-metadata:s:s:0 language={0}", languageName.ToLowerInvariant()));
      }
    }

    internal void AddSubtitleEmbeddingParameters(int mediaSourceIndex, List<SubtitleStream> subtitles, SubtitleCodec codec, double timeStart, FFMpegTranscodeData data)
    {
      if (codec == SubtitleCodec.Unknown)
        return;

      if (subtitles == null || subtitles.Count == 0)
        return;

      foreach (var subtitle in subtitles)
      {
        if (subtitle == null || string.IsNullOrEmpty(subtitle.Source))
          continue;

        data.AddSubtitle(mediaSourceIndex, subtitle.Source);

        string subtitleFormat = FFMpegGetSubtitleContainer.GetSubtitleContainer(subtitle.Codec);
        data.AddSubtitleArgument(mediaSourceIndex, (data.InputSubtitleFilePaths[mediaSourceIndex]?.Count ?? 1) - 1, string.Format("-f {0}", subtitleFormat));
        string languageName = Get3LetterLanguage(subtitle.Language);
        int inputNo = data.InputResourceAccessor.Count + data.InputSubtitleFilePaths.SelectMany(s => s.Value).Count() - 1;
        if (string.IsNullOrEmpty(languageName) == false)
          data.OutputArguments.Add(string.Format("-metadata:s:s:{0} language={1}", inputNo - 1, languageName.ToLowerInvariant())); // subtitle metadata stream index needs to be 1 less
        data.OutputArguments.Add(string.Format("-map {0}:s:0", inputNo));
        string subtitleEncoder = FFMpegGetSubtitleContainer.GetSubtitleContainer(codec);
        data.OutputArguments.Add(string.Format("-c:s:{0} {1}", inputNo, subtitleEncoder));
      }
    }

    private void AddSubtitleExtractionParameters(SubtitleStream subtitle, string subtitleEncoding, string subtitleEncoder, string subtitleFormat, double timeStart, FFMpegTranscodeData data)
    {
      if(timeStart > 0)
      {
        data.OutputArguments.Add(string.Format(CultureInfo.InvariantCulture, "-ss {0:0.0}", timeStart));
      }
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

    private void AddImageFilterParameters(ImageTranscoding image, ref FFMpegTranscodeData data)
    {
      int height = image.SourceHeight;
      int width = image.SourceWidth;
      if (height > image.TargetHeight && image.TargetHeight > 0)
      {
        double scale = (double)image.SourceWidth / (double)image.SourceHeight;
        height = image.TargetHeight;
        width = Convert.ToInt32(scale * (double)height);
      }
      if (width > image.TargetWidth && image.TargetWidth > 0)
      {
        double scale = (double)image.SourceHeight / (double)image.SourceWidth;
        width = image.TargetWidth;
        height = Convert.ToInt32(scale * (double)width);
      }

      if (image.TargetAutoRotate == true)
      {
        if (image.SourceOrientation > 4)
        {
          int iTemp = width;
          width = height;
          height = iTemp;
        }

        if (image.SourceOrientation > 1)
        {
          if (image.SourceOrientation == 2)
          {
            data.OutputFilter.Add("hflip,");
          }
          else if (image.SourceOrientation == 3)
          {
            data.OutputFilter.Add("hflip,");
            data.OutputFilter.Add("vflip,");
          }
          else if (image.SourceOrientation == 4)
          {
            data.OutputFilter.Add("vflip,");
          }
          else if (image.SourceOrientation == 5)
          {
            data.OutputFilter.Add("transpose=0,");
          }
          else if (image.SourceOrientation == 6)
          {
            data.OutputFilter.Add("transpose=1,");
          }
          else if (image.SourceOrientation == 7)
          {
            data.OutputFilter.Add("transpose=2,");
            data.OutputFilter.Add("hflip,");
          }
          else if (image.SourceOrientation == 8)
          {
            data.OutputFilter.Add("transpose=2,");
          }
        }
      }
      data.OutputFilter.Add(string.Format("scale={0}:{1}", width, height));
    }

    internal void AddAudioParameters(AudioTranscoding audio, FFMpegTranscodeData data)
    {
      if (Checks.IsAudioStreamChanged(0, 0, audio) == false || audio.TargetForceCopy == true)
      {
        data.OutputArguments.Add("-c:a copy");
      }
      else
      {
        if (audio.TargetAudioCodec == AudioCodec.Unknown)
        {
          switch (audio.TargetAudioContainer)
          {
            case AudioContainer.Unknown:
              break;
            case AudioContainer.Ac3:
              audio.TargetAudioCodec = AudioCodec.Ac3;
              break;
            case AudioContainer.Adts:
              audio.TargetAudioCodec = AudioCodec.Aac;
              break;
            case AudioContainer.Asf:
              audio.TargetAudioCodec = AudioCodec.Wma;
              break;
            case AudioContainer.Flac:
              audio.TargetAudioCodec = AudioCodec.Flac;
              break;
            case AudioContainer.Lpcm:
              audio.TargetAudioCodec = AudioCodec.Lpcm;
              break;
            case AudioContainer.Mp4:
              audio.TargetAudioCodec = AudioCodec.Aac;
              break;
            case AudioContainer.Mp3:
              audio.TargetAudioCodec = AudioCodec.Mp3;
              break;
            case AudioContainer.Mp2:
              audio.TargetAudioCodec = AudioCodec.Mp2;
              break;
            case AudioContainer.Ogg:
              audio.TargetAudioCodec = AudioCodec.Vorbis;
              break;
            case AudioContainer.Rtp:
              audio.TargetAudioCodec = AudioCodec.Lpcm;
              break;
            case AudioContainer.Rtsp:
              audio.TargetAudioCodec = AudioCodec.Lpcm;
              break;
            default:
              audio.TargetAudioCodec = audio.SourceAudioCodec;
              break;
          }
        }
        data.OutputArguments.Add(string.Format("-c:a {0}", FFMpegGetAudioCodec.GetAudioCodec(audio.TargetAudioCodec)));
        long? frequency = Validators.GetAudioFrequency(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioFrequency, audio.TargetAudioFrequency);
        if (frequency > 0)
          data.OutputArguments.Add(string.Format("-ar {0}", frequency.Value));
    
        if (audio.TargetAudioContainer != AudioContainer.Lpcm)
        {
          long? audioBitrate = Validators.GetAudioBitrate(audio.SourceAudioBitrate, audio.TargetAudioBitrate);
          if(audioBitrate.HasValue)
            data.OutputArguments.Add(string.Format("-b:a {0}k", audioBitrate.Value));
        }
      }
      if (audio.TargetAudioContainer == AudioContainer.Mp3)
        data.OutputArguments.Add("-id3v2_version 3");

      AddAudioChannelsNumberParameters(0, 0, audio, data);

      string coder = null;
      Coders.TryGetValue(audio.TargetCoder, out coder);
      if(string.IsNullOrEmpty(coder) == false)
        data.OutputArguments.Add(coder);
    }

    internal void AddImageParameters(ImageTranscoding image, FFMpegTranscodeData data)
    {
      if (Checks.IsImageStreamChanged(image) == false)
      {
        data.OutputArguments.Add("-c:v copy");
      }
      else
      {
        AddImageFilterParameters(image, ref data);
        if (image.TargetPixelFormat != PixelFormat.Unknown)
        {
          data.OutputArguments.Add(string.Format("-pix_fmt {0}", FFMpegGetPixelFormat.GetPixelFormat(image.TargetPixelFormat)));
        }
        string scale = null;
        ImageScaleModes.TryGetValue(image.TargetImageQuality, out scale);
        if (image.TargetImageQuality == QualityMode.Custom)
        {
          data.OutputArguments.Add(string.Format("-q:v {0}", image.TargetImageQualityFactor));
        }
        else if (string.IsNullOrEmpty(scale) == false)
        {
          data.OutputArguments.Add(string.Format("-q:v {0}", scale));
        }

        if (image.TargetImageCodec != ImageContainer.Unknown)
          data.OutputArguments.Add(string.Format("-c:v {0}", FFMpegGetImageCodec.GetImageCodec(image.TargetImageCodec)));
      }

      string coder = null;
      Coders.TryGetValue(image.TargetCoder, out coder);
      if (string.IsNullOrEmpty(coder) == false)
        data.OutputArguments.Add(coder);
    }

    internal void AddVideoParameters(VideoTranscoding video, string transcodeId, FFMpegEncoderConfig encoderConfig, FFMpegTranscodeData data)
    {
      if (video.TargetVideoCodec == VideoCodec.Unknown)
        video.TargetVideoCodec = video.FirstSourceVideoStream.Codec;

      if (video.TargetVideoAspectRatio <= 0)
      {
        if (video.FirstSourceVideoStream.Height > 0 && video.FirstSourceVideoStream.Width > 0)
        {
          video.TargetVideoAspectRatio = (float)video.FirstSourceVideoStream.Width / (float)video.FirstSourceVideoStream.Height;
        }
        else
        {
          video.TargetVideoAspectRatio = 16.0F / 9.0F;
        }
      }
      if (!(video.FirstSourceVideoStream.PixelAspectRatio > 0))
        video.FirstSourceVideoStream.PixelAspectRatio = 1.0F;

      if (video.TargetVideoMaxHeight <= 0)
        video.TargetVideoMaxHeight = 1080;
   
      bool vCodecCopy = false;
      if (video.SourceMedia.Count == 1 && (Checks.IsVideoStreamChanged(video) == false || video.TargetForceVideoCopy == true))
      {
        vCodecCopy = true;
        data.OutputArguments.Add("-c:v copy");
        data.GlobalArguments.Add("-fflags +genpts");
      }
      else
      {
        data.OutputArguments.Add(string.Format("-c:v {0}", FFMpegGetVideoCodec.GetVideoCodec(video.TargetVideoCodec, data.Encoder)));

        if (video.TargetPixelFormat == PixelFormat.Unknown)
          video.TargetPixelFormat = PixelFormat.Yuv420;
       
        data.OutputArguments.Add(string.Format("-pix_fmt {0}", FFMpegGetPixelFormat.GetPixelFormat(video.TargetPixelFormat)));

        if (video.TargetVideoCodec == VideoCodec.H265)
        {
          string profile = encoderConfig.GetEncoderProfile(video.TargetVideoCodec, video.TargetProfile);
          if (string.IsNullOrEmpty(profile) == false)
            data.OutputArguments.Add(profile);
    
          if (video.TargetLevel > 0)
            data.OutputArguments.Add(string.Format("-level {0}", video.TargetLevel.Value.ToString("0.0", CultureInfo.InvariantCulture)));
   
          string preset = encoderConfig.GetEncoderPreset(video.TargetVideoCodec, video.TargetPreset);
          if (string.IsNullOrEmpty(preset) == false)
            data.OutputArguments.Add(preset);

          AddVideoBitrateParameters(video, data);

          string quality = null;
          VideoQualityModes.TryGetValue(video.TargetVideoQuality, out quality);
          if (video.TargetVideoQuality == QualityMode.Custom)
          {
            data.OutputArguments.Add(string.Format("-crf {0}", video.TargetQualityFactor));
          }
          else if (string.IsNullOrEmpty(quality) == false)
          {
            data.OutputArguments.Add(string.Format("-crf {0}", quality));
          }

          if (data.Encoder == FFMpegEncoderHandler.EncoderHandler.Software)
          {
            data.OutputArguments.Add("-x265-params");
            string args = "";
            if (video.TargetVideoQuality == QualityMode.Custom)
            {
              args += string.Format("-crf={0}", video.TargetQualityFactor);
            }
            else if (string.IsNullOrEmpty(quality) == false)
            {
              args += string.Format("-crf={0}", quality);
            }
            if (video.FirstSourceVideoStream.Framerate > 0)
            {
              args += string.Format(":fps={0}", Validators.GetValidFramerate(video.FirstSourceVideoStream.Framerate.Value));
            }
            if (video.TargetLevel > 0)
            {
              args += string.Format(":level={0}", video.TargetLevel.Value.ToString("0.0", CultureInfo.InvariantCulture));
            }
            data.OutputArguments.Add(args);
          }
        }
        else if (video.TargetVideoCodec == VideoCodec.H264)
        {
          if (video.TargetProfile == EncodingProfile.High && video.TargetPixelFormat == PixelFormat.Yuv422)
          {
            video.TargetProfile = EncodingProfile.High422;
          }
          else if (video.TargetProfile == EncodingProfile.High && video.TargetPixelFormat == PixelFormat.Yuv444)
          {
            video.TargetProfile = EncodingProfile.High444;
          }

          string profile = encoderConfig.GetEncoderProfile(video.TargetVideoCodec, video.TargetProfile);
          if (string.IsNullOrEmpty(profile) == false)
            data.OutputArguments.Add(profile);

          if (video.TargetLevel > 0)
            data.OutputArguments.Add(string.Format("-level {0}", video.TargetLevel.Value.ToString("0.0", CultureInfo.InvariantCulture)));

          string preset = encoderConfig.GetEncoderPreset(video.TargetVideoCodec, video.TargetPreset);
          if (string.IsNullOrEmpty(preset) == false)
            data.OutputArguments.Add(preset);

          AddVideoBitrateParameters(video, data);

          string quality = null;
          VideoQualityModes.TryGetValue(video.TargetVideoQuality, out quality);
          if (video.TargetVideoQuality == QualityMode.Custom)
          {
            data.OutputArguments.Add(string.Format("-crf {0}", video.TargetQualityFactor));
          }
          else if (string.IsNullOrEmpty(quality) == false)
          {
            data.OutputArguments.Add(string.Format("-crf {0}", quality));
          }
        }
        else
        {
          string profile = encoderConfig.GetEncoderProfile(video.TargetVideoCodec, video.TargetProfile);
          if (string.IsNullOrEmpty(profile) == false)
            data.OutputArguments.Add(profile);

          string preset = encoderConfig.GetEncoderPreset(video.TargetVideoCodec, video.TargetPreset);
          if (string.IsNullOrEmpty(preset) == false)
            data.OutputArguments.Add(preset);

          if (AddVideoBitrateParameters(video, data) == false)
          {
            string scale = null;
            VideoScaleModes.TryGetValue(video.TargetVideoQuality, out scale);
            if (video.TargetVideoQuality == QualityMode.Custom)
            {
              data.OutputArguments.Add(string.Format("-qscale:v {0}", video.TargetVideoQualityFactor));
            }
            else if (string.IsNullOrEmpty(scale) == false)
            {
              data.OutputArguments.Add(string.Format("-qscale:v {0}", scale));
            }
          }
        }

        AddVideoFiltersParameters(video, data);
        if (video.FirstSourceVideoStream.Framerate > 0)
          data.OutputArguments.Add(string.Format("-r {0}", Validators.GetValidFramerate(video.FirstSourceVideoStream.Framerate.Value)));

        data.OutputArguments.Add("-g 15");
      }
      if (vCodecCopy && video.FirstSourceVideoStream.Codec == VideoCodec.H264 && !Checks.IsMPEGTSContainer(video.FirstSourceVideoContainer) && Checks.IsMPEGTSContainer(video.TargetVideoContainer))
      {
        data.OutputArguments.Add("-bsf:v h264_mp4toannexb");
      }
      else if (!vCodecCopy && video.TargetVideoCodec == VideoCodec.H264 && Checks.IsMPEGTSContainer(video.TargetVideoContainer))
      {
        data.OutputArguments.Add("-bsf:v h264_mp4toannexb");
        data.OutputArguments.Add("-flags -global_header");
      }
      if (video.TargetVideoContainer == VideoContainer.M2Ts)
        data.OutputArguments.Add("-mpegts_m2ts_mode 1");

      string coder = null;
      Coders.TryGetValue(video.TargetCoder, out coder);
      if (string.IsNullOrEmpty(coder) == false)
        data.OutputArguments.Add(coder);
    }

    private bool AddVideoBitrateParameters(VideoTranscoding video, FFMpegTranscodeData data)
    {
      if (video.TargetVideoBitrate > 0)
      {
        if (video.TargetVideoCodec == VideoCodec.H264 || video.TargetVideoCodec == VideoCodec.H265)
        {
          if (data.Encoder == FFMpegEncoderHandler.EncoderHandler.HardwareNvidia)
            data.OutputArguments.Add("-cbr 1");
        }
        data.OutputArguments.Add(string.Format("-b:v {0}", video.TargetVideoBitrate + "k"));
        data.OutputArguments.Add(string.Format("-maxrate:v {0}", video.TargetVideoBitrate + "k"));
        data.OutputArguments.Add(string.Format("-bufsize:v {0}", video.TargetVideoBitrate + "k"));

        return true;
      }
      return false;
    }

    private string GetVideoFilters(VideoTranscoding video, out Size newSize, FFMpegTranscodeData data)
    {
      bool sourceSquarePixels = Checks.IsSquarePixel(video.FirstSourceVideoStream.PixelAspectRatio);
      newSize = new Size(video.FirstSourceVideoStream.Width ?? DEFAULT_SOURCE_WIDTH, video.FirstSourceVideoStream.Height ?? DEFAULT_SOURCE_HEIGHT);
      Size newContentSize = new Size(video.FirstSourceVideoStream.Width ?? DEFAULT_SOURCE_WIDTH, video.FirstSourceVideoStream.Height ?? DEFAULT_SOURCE_HEIGHT);
      float newPixelAspectRatio = video.FirstSourceVideoStream.PixelAspectRatio ?? 1;
      bool pixelARChanged = false;
      bool videoARChanged = false;
      bool videoHeightChanged = false;

      GetVideoDimensions(video, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);

      string vidFilter = "";

      if (videoARChanged || pixelARChanged || videoHeightChanged)
      {
        if (videoHeightChanged || pixelARChanged)
          vidFilter += string.Format("scale={0}:{1}", newContentSize.Width, newContentSize.Height);
    
        if (videoARChanged)
        {
          int posX = Convert.ToInt32(Math.Abs(newSize.Width - newContentSize.Width) / 2);
          int posY = Convert.ToInt32(Math.Abs(newSize.Height - newContentSize.Height) / 2);
          vidFilter += string.Format("pad={0}:{1}:{2}:{3}:black", newSize.Width, newSize.Height, posX, posY);
          vidFilter += string.Format("setdar={0}/{1}", newSize.Width, newSize.Height);
        }
        if (pixelARChanged)
        {
          vidFilter += "setsar=1";
        }
        else if (sourceSquarePixels == false)
        {
          if(video.FirstSourceVideoStream.PixelAspectRatio.HasValue)
            vidFilter += "setsar=" + video.FirstSourceVideoStream.PixelAspectRatio.Value.ToString("0.00", CultureInfo.InvariantCulture);
        }
      }
      return vidFilter;
    }

    private string GetSubtitleFilter(int subtitleInputNo, VideoTranscoding video, SubtitleStream subtitle, Size newSize, FFMpegTranscodeData data)
    {
      if (subtitle != null && subtitle.Source != null)
      {
        if(subtitle.IsEmbedded)
        {
          return string.Format("[{0}:s:{1}]", subtitleInputNo, subtitle.StreamIndex);
        }
        else if(subtitle.Codec == SubtitleCodec.Unknown)
        {
          return null;
        }
        else if (SubtitleAnalyzer.IsImageBasedSubtitle(subtitle.Codec) == false)
        {
          string encoding = "UTF-8";
          if (string.IsNullOrEmpty(subtitle.CharacterEncoding) == false)
            encoding = subtitle.CharacterEncoding;
          
          string subFilter = string.Format("subtitles=filename='{0}':original_size={1}x{2}:charenc='{3}'", EncodeFilePath(subtitle.Source), newSize.Width, newSize.Height, encoding);
          List<string> style = new List<string>();
          if (!string.IsNullOrEmpty(video.TargetSubtitleFont))
            style.Add("FontName=" + video.TargetSubtitleFont);
          if (!string.IsNullOrEmpty(video.TargetSubtitleFontSize))
            style.Add("FontSize=" + video.TargetSubtitleFontSize);
          if (!string.IsNullOrEmpty(video.TargetSubtitleColor))
            style.Add("PrimaryColour=&H" + video.TargetSubtitleColor);
          if (video.TargetSubtitleBox)
            style.Add("BorderStyle=3");

          if (style.Count > 0)
          {
            subFilter += ":force_style='";
            subFilter += string.Join(",", style);
            subFilter += "'";
          }
          return subFilter;
        }
        else
        {
          return string.Format("[{0}:s:0]overlay", subtitleInputNo);
        }
      }
      return "";
    }

    private void AddVideoFiltersParameters(VideoTranscoding video, FFMpegTranscodeData data)
    {
      if (data.FirstResourceAccessor is TranscodeLiveAccessor)
      {
        data.OutputFilter.Add("yadif=0:-1:0");
      }
      else
      {
        Size newSize = new Size();
        string vidFilter = GetVideoFilters(video, out newSize, data);
        if (video.SourceMedia.Count > 1 || !string.IsNullOrEmpty(vidFilter))
        {
          //Add video graph
          int inputNo = 0;
          foreach (var media in video.SourceMedia)
          {
            data.OutputFilter.Add(string.Format("[{0}:v:{1}]", inputNo, video.SourceVideoStreams[media.Key].StreamIndex)); //Only first video stream supported
            if (video.PreferredSourceSubtitles != null && video.TargetSubtitleSupport == SubtitleSupport.HardCoded)
            {
              foreach (var sub in video.PreferredSourceSubtitles[media.Key])
              {
                if (SubtitleAnalyzer.IsImageBasedSubtitle(sub.Codec) == true || sub.IsEmbedded)
                  data.AddSubtitle(media.Key, sub.Source);
             
                data.OutputFilter.Add(GetSubtitleFilter(data.InputResourceAccessor.Count + data.InputSubtitleFilePaths.Count - 1, video, sub, newSize, data));
              }
            }
            data.OutputFilter.Add(string.Format("[v{0}];", inputNo));
            inputNo++;
          }

          //Add audio graph
          inputNo = 0;
          int audioCount = 0;
          foreach (var media in video.SourceMedia)
          {
            audioCount = 0;
            data.OutputFilter.Add(string.Format("[v{0}]", inputNo));
            if (Checks.AreMultipleAudioStreamsSupported(video))
            {
              foreach(var audio in video.SourceAudioStreams)
              {
                data.OutputFilter.Add(string.Format("[{0}:a:{1}]", inputNo, audio.Key));
                audioCount++;
              }             
            }
            else
            {
              var audio = video.SourceAudioStreams.First();
              data.OutputFilter.Add(string.Format("[{0}:a:{1}]", inputNo, audio.Key));
              audioCount++;
            }
            inputNo++;
          }

          if (string.IsNullOrEmpty(vidFilter))
          {
            data.OutputFilter.Add(string.Format(" concat=n={0}:v=1:a={1}[v][a]", video.SourceMedia.Count, audioCount));
          }
          else
          {
            data.OutputFilter.Add(string.Format(" concat=n={0}:v=1:a={1}[vf][a];", video.SourceMedia.Count, audioCount));
            data.OutputFilter.Add(string.Format(" [vf]{0}[v]", vidFilter));
          }
          data.OutputArguments.Add("-map \"[v]\"");
          data.OutputArguments.Add("-map \"[a]\"");
        }
        else
        {
          data.OutputArguments.Add(string.Format("-map 0:v:{0}", video.SourceMedia.First().Key));
          if (Checks.AreMultipleAudioStreamsSupported(video))
          {
            foreach (var audio in video.SourceAudioStreams)
            {
              data.OutputArguments.Add(string.Format("-map 0:a:{0}", audio.Key));
            }
          }
          else
          {
            var audio = video.SourceAudioStreams.First();
            data.OutputArguments.Add(string.Format("-map 0:a:{0}", audio.Key));
          }

          if (video.PreferredSourceSubtitles?.Count > 0)
          {
            foreach (var sub in video.PreferredSourceSubtitles.SelectMany(s => s.Value).Where(s => !s.IsPartial))
            {
              if (sub.IsEmbedded)
              {
                data.AddSubtitle(-1, sub.Source);
                data.OutputArguments.Add(string.Format("-map {0}:s:{1}", data.InputResourceAccessor.Count + data.InputSubtitleFilePaths.Count - 1, sub.StreamIndex));
              }
            }
          }
        }
      }
    }

    private string EncodeFilePath(string filePath)
    {
      return _filerPathEncoding.Aggregate(filePath, (current, enc) => current.Replace(enc.Key, enc.Value));
    }

    

    private string Get3LetterLanguage(string iso2language)
    {
      if (string.IsNullOrEmpty(iso2language) == false)
      {
        //ffmpeg uses ISO 639-2/B but .net uses ISO 639-2/T so need to map
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        var lang = cultures.FirstOrDefault(c => string.Compare(c.TwoLetterISOLanguageName, iso2language, true) == 0)?.ThreeLetterISOLanguageName;
        if (_isoMap.ContainsKey(lang))
          return _isoMap[lang];
        return lang;
      }
      return null;
    }

    internal void AddVideoAudioParameters(VideoTranscoding video, FFMpegTranscodeData data)
    {
      int mediaStreamIndex = video.FirstAudioStreamIndex;
      int inputNo = 0;
      if (mediaStreamIndex < 0)
        return;

      foreach (var audio in video.SourceAudioStreams[mediaStreamIndex])
      {
        if (audio.Codec == AudioCodec.Unknown)
        {
          data.OutputArguments.Add(string.Format("-an:a:{0}", audio.StreamIndex));
          continue;
        }

        if (Checks.IsAudioStreamChanged(mediaStreamIndex, audio.StreamIndex, video) == false || video.TargetForceAudioCopy == true)
        {
          data.OutputArguments.Add(string.Format("-c:a:{0} copy", inputNo));
        }
        else
        {
          data.OutputArguments.Add(string.Format("-c:a:{0} {1}", inputNo, FFMpegGetAudioCodec.GetAudioCodec(video.TargetAudioCodec)));

          long? frequency = Validators.GetAudioFrequency(audio.Codec, video.TargetAudioCodec, audio.Frequency, video.TargetAudioFrequency);
          if (frequency.HasValue)
            data.OutputArguments.Add(string.Format("-ar:a:{0} {1}", inputNo, frequency.Value));

          if (video.TargetAudioCodec != AudioCodec.Lpcm)
            data.OutputArguments.Add(string.Format("-b:a:{0} {1}k", inputNo, Validators.GetAudioBitrate(audio.Bitrate, video.TargetAudioBitrate)));

          string languageName = Get3LetterLanguage(audio.Language);
          if (string.IsNullOrEmpty(languageName) == false)
            data.OutputArguments.Add(string.Format("-metadata:s:a:{0} language={1}", inputNo, languageName.ToLowerInvariant()));

          AddAudioChannelsNumberParameters(inputNo, audio.StreamIndex, video, data);
          inputNo++;
        }
        if (!Checks.AreMultipleAudioStreamsSupported(video))
          break;
      }      
    }

    private void AddAudioChannelsNumberParameters(int inputNo, int audioStreamIndex, BaseTranscoding media, FFMpegTranscodeData data)
    {
      int? channels = null;
      int? streamIndex = null;
      if (media is VideoTranscoding)
      {
        VideoTranscoding video = (VideoTranscoding)media;
        int mediaStreamIndex = video.FirstAudioStreamIndex;
        channels = Validators.GetAudioNumberOfChannels(video.SourceAudioStreams[mediaStreamIndex].First(s => s.StreamIndex == audioStreamIndex).Codec, 
          video.TargetAudioCodec, video.SourceAudioStreams[mediaStreamIndex].First(s => s.StreamIndex == audioStreamIndex).Channels, video.TargetForceAudioStereo);
        streamIndex = audioStreamIndex;
      }
      if (media is AudioTranscoding)
      {
        AudioTranscoding audio = (AudioTranscoding)media;
        channels = Validators.GetAudioNumberOfChannels(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioChannels, audio.TargetForceAudioStereo);
      }
      if (channels.HasValue)
      {
        data.OutputArguments.Add(string.Format("-ac:{0} {1}", inputNo, channels.Value));
      }
    }

    internal void AddTimeParameters(BaseTranscoding media, double timeStart, double timeDuration, FFMpegTranscodeData data)
    {
      if (timeStart > 0.0)
      {
        foreach (var dur in media.SourceMediaDurations)
        {
          if (timeStart < dur.Value.TotalSeconds)
          {
            foreach(var subKey in data.InputSubtitleArguments[dur.Key].Keys)
              data.InputSubtitleArguments[dur.Key][subKey].Add(string.Format(CultureInfo.InvariantCulture, "-ss {0:0.0}", timeStart));
            data.InputArguments[dur.Key].Add(string.Format(CultureInfo.InvariantCulture, "-ss {0:0.0}", timeStart));
            break;
          }
        }
        data.OutputArguments.Add("-avoid_negative_ts 1");
        if (timeDuration > 0)
        {
          data.OutputArguments.Add(string.Format(CultureInfo.InvariantCulture, "-t {0:0.0}", timeDuration));
        }
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
