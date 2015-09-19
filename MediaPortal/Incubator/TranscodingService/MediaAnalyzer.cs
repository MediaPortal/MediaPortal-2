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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.SystemAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.ImpersonationService;
using MediaPortal.Utilities.Process;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;

namespace MediaPortal.Plugins.Transcoding.Service
{
  public class MediaAnalyzer
  {
    #region Constants

    /// <summary>
    /// Maximum duration for creating a single video thumbnail.
    /// </summary>
    protected const int PROCESS_TIMEOUT_MS = 30000;

    /// <summary>
    /// Name of the Assembly to execute
    /// </summary>
    protected const string PROCESS_ASSEMBLY_NAME = "ffprobe.exe";

    #endregion

    #region Protected fields and classes

    protected static readonly object FFPROBE_THROTTLE_LOCK = new object();

    #endregion

    public string AnalyzerBinPath { get; set; }
    public int TranscoderTimeout { get; set; }
    public int TranscoderMaximumThreads { get; set; }
    public string SubtitleDefaultEncoding { get; set; }
    public string SubtitleDefaultLanguage { get; set; }
    public ILogger Logger { get; set;  }

    private readonly Dictionary<string, CultureInfo> _countryCodesMapping = new Dictionary<string, CultureInfo>();
    private readonly Dictionary<float, long> _h264MaxDpbMbs = new Dictionary<float, long>();
   
    public MediaAnalyzer()
    {
      AnalyzerBinPath = ServiceRegistration.Get<IFFMpegLib>().FFProbeBinaryPath;
      TranscoderTimeout = 2500;
      TranscoderMaximumThreads = 0;
      SubtitleDefaultEncoding = "";
      SubtitleDefaultLanguage = "";

      _h264MaxDpbMbs.Add(1F, 396);
      _h264MaxDpbMbs.Add(1.1F, 396);
      _h264MaxDpbMbs.Add(1.2F, 900);
      _h264MaxDpbMbs.Add(1.3F, 2376);
      _h264MaxDpbMbs.Add(2F, 2376);
      _h264MaxDpbMbs.Add(2.1F, 4752);
      _h264MaxDpbMbs.Add(2.2F, 8100);
      _h264MaxDpbMbs.Add(3F, 8100);
      _h264MaxDpbMbs.Add(3.1F, 18000);
      _h264MaxDpbMbs.Add(3.2F, 20480);
      _h264MaxDpbMbs.Add(4F, 32768);
      _h264MaxDpbMbs.Add(4.1F, 32768);
      _h264MaxDpbMbs.Add(4.2F, 34816);
      _h264MaxDpbMbs.Add(5F, 110400);
      _h264MaxDpbMbs.Add(5.1F, 184320);
      _h264MaxDpbMbs.Add(5.2F, 184320);

      CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
      foreach (CultureInfo culture in cultures)
      {
        try
        {
          _countryCodesMapping[culture.ThreeLetterISOLanguageName.ToUpperInvariant()] = culture;
        }
        catch { }
      }
    }

    public MetadataContainer ParseFile(ILocalFsResourceAccessor lfsra)
    {
      string fileName = lfsra.LocalFileSystemPath;
      string arguments = string.Format("-threads {0} -i \"{1}\"", TranscoderMaximumThreads, fileName);

      ProcessExecutionResult executionResult;
      lock (FFPROBE_THROTTLE_LOCK)
        executionResult = ServiceRegistration.Get<IFFMpegLib>().FFProbeExecuteWithResourceAccessAsync(lfsra, arguments, ProcessPriorityClass.Idle, PROCESS_TIMEOUT_MS).Result;
      
      // My guess (agree with dtb's comment): AFAIK ffmpeg uses stdout to pipe out binary data(multimedia, snapshots, etc.)
      // and stderr is used for logging purposes. In your example you use stdout.
      // http://stackoverflow.com/questions/4246758/why-doesnt-this-method-redirect-my-output-from-exe-ffmpeg
      if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
      {
        if (Logger != null) Logger.Debug("DlnaMediaServer: Successfully ran {0}:\n {1}", PROCESS_ASSEMBLY_NAME, executionResult.StandardError);
        MetadataContainer info = new MetadataContainer { Metadata = { Source = lfsra } };
        info.Metadata.Mime = MimeTypeDetector.GetMimeType(fileName);
        info.Metadata.Size = lfsra.Size;
        ParseFFMpegOutput(executionResult.StandardError, ref info);
        ParseH264Info(ref info);
        ParseMPEG2TSInfo(ref info);
        ParseSubtitleFiles(ref info);
        return info;
      }

      if (executionResult != null) Logger.Error("MediaAnalyzer: Failed to extract media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", fileName, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
      else
        Logger.Error("MediaAnalyzer: Failed to extract media type information for resource '{0}', executionResult=null", fileName);

      return null;
    }

    public MetadataContainer ParseStream(INetworkResourceAccessor streamLink)
    {
      string arguments = "";
      if (streamLink.URL.StartsWith("rtsp://") == true)
      {
        arguments += "-rtsp_transport +tcp+udp ";
      }
      arguments += "-analyzeduration 10000000 ";
      arguments += string.Format("-i \"{0}\"", streamLink);

      ProcessExecutionResult executionResult;
      lock (FFPROBE_THROTTLE_LOCK)
        executionResult = ServiceRegistration.Get<IFFMpegLib>().FFProbeExecuteAsync(arguments, ProcessPriorityClass.Idle, PROCESS_TIMEOUT_MS).Result;
      
      if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
      {
        if (Logger != null) Logger.Debug("MediaAnalyzer: Successfully ran {0}:\n {1}", PROCESS_ASSEMBLY_NAME, executionResult.StandardError);
        MetadataContainer info = new MetadataContainer { Metadata = { Source = streamLink } };
        info.Metadata.Mime = MimeTypeDetector.GetMimeType(streamLink.URL);
        info.Metadata.Size = 0;
        ParseFFMpegOutput(executionResult.StandardError, ref info);
        ParseH264Info(ref info);
        ParseMPEG2TSInfo(ref info);
        return info;
      }

      if (Logger != null) Logger.Error("MediaAnalyzer: Failed to extract media type information for resource '{0}'", streamLink);
      
      return null;
    }

    public void ParseSubtitleFiles(ref MetadataContainer info)
    {
      ILocalFsResourceAccessor lfsra = (ILocalFsResourceAccessor)info.Metadata.Source;
      if (lfsra.Exists)
      {
        // Impersionation
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
              string subContent = File.ReadAllText(file);
              if (subContent.Contains("[INFORMATION]")) sub.Codec = SubtitleCodec.SubView;
              else if (subContent.Contains("}{")) sub.Codec = SubtitleCodec.MicroDvd;
            }
            if (sub.Codec != SubtitleCodec.Unknown)
            {
              sub.Source = file;
              sub.Language = SubtitleAnalyzer.GetLanguage(file, SubtitleDefaultEncoding, SubtitleDefaultLanguage);
              info.Subtitles.Add(sub);
            }
          }
        }
      }
    }

    private bool TryExecuteBinary(string executable, string arguments, out byte[] result, ILocalFsResourceAccessor lfsra, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal)
    {
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
      {
        using (Process process = new Process { StartInfo = new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true } })
        {
          process.Start();
          process.PriorityClass = priorityClass;
          List<byte> resultList = new List<byte>();
          bool abort = false;
          using (process.StandardOutput)
          {
            DateTime dtEnd = DateTime.Now.AddMilliseconds(TranscoderTimeout);
            using (BinaryReader br = new BinaryReader(process.StandardOutput.BaseStream))
            {
              while (abort == false && DateTime.Now < dtEnd)
              {
                try
                {
                  resultList.Add(br.ReadByte());
                }
                catch (EndOfStreamException)
                {
                  abort = true;
                }
              }
            }
          }
          result = resultList.ToArray();
          process.Close();
          if (abort == true)
            return true;
        }
      }
      return false;
    }

    #region Parsers

    private SubtitleCodec ParseSubtitleCodec(string token)
    {
      if (token != null)
      {
        if (token.Equals("ass", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Ass;
        if (token.Equals("ssa", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Ssa;
        if (token.Equals("mov_text", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.MovTxt;
        if (token.Equals("sami", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Smi;
        if (token.Equals("srt", StringComparison.InvariantCultureIgnoreCase) || token.Equals("subrip", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Srt;
        if (token.Equals("microdvd", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.MicroDvd;
        if (token.Equals("subviewer", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.SubView;
        if (token.Equals("webvtt", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Vtt;
        if (token.Equals("dvb_subtitle", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.DvbSub;
        if (token.Equals("dvb_teletext", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.DvbTxt;
      }
      return SubtitleCodec.Unknown;
    }

    private VideoContainer ParseVideoContainer(string token, ILocalFsResourceAccessor lfsra)
    {
      if (token != null)
      {
        if (token.Equals("asf", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Asf;
        if (token.Equals("avi", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Avi;
        if (token.Equals("flv", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Flv;
        if (token.Equals("3gp", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Gp3;
        if (token.Equals("applehttp", StringComparison.InvariantCultureIgnoreCase) || token.Equals("hls", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Hls;
        if (token.Equals("matroska", StringComparison.InvariantCultureIgnoreCase) || token.Equals("webm", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Matroska;
        if (token.Equals("mov", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mp4", StringComparison.InvariantCultureIgnoreCase))
        {
          if (lfsra.LocalFileSystemPath != null && lfsra.LocalFileSystemPath.EndsWith(".3g", StringComparison.InvariantCultureIgnoreCase))
          {
            return VideoContainer.Gp3;
          }
          return VideoContainer.Mp4;
        }
        if (token.Equals("m2ts", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.M2Ts;
        if (token.Equals("mpeg", StringComparison.InvariantCultureIgnoreCase) || token.Equals("vob", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Mpeg2Ps;
        if (token.Equals("mpegts", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Mpeg2Ts;
        if (token.Equals("mpegvideo", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Mpeg1;
        if (token.Equals("mjpeg", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.MJpeg;
        if (token.Equals("ogg", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Ogg;
        if (token.Equals("rm", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.RealMedia;
        if (token.Equals("rtp", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Rtp;
        if (token.Equals("rtsp", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Rtsp;
        if (token.Equals("wtv", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Wtv;
      }
      return VideoContainer.Unknown;
    }

    private VideoCodec ParseVideoCodec(string token)
    {
      if (token != null)
      {
        if (token.Equals("dvvideo", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.DvVideo;
        if (token.StartsWith("flv", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Flv;
        if (token.Equals("hevc", StringComparison.InvariantCultureIgnoreCase) || token.Equals("h265", StringComparison.InvariantCultureIgnoreCase) || 
          token.Equals("libx265", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.H265;
        if (token.Equals("avc", StringComparison.InvariantCultureIgnoreCase) || token.Equals("h264", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("libx264", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.H264;
        if (token.StartsWith("h263", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.H263;
        if (token.Equals("mpeg4", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Mpeg4;
        if (token.Equals("msmpeg4", StringComparison.InvariantCultureIgnoreCase) || token.Equals("msmpeg4v1", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("msmpeg4v2", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.MsMpeg4;
        if (token.Equals("mpeg2video", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Mpeg2;
        if (token.Equals("mpeg1video", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mpegvideo", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Mpeg1;
        if (token.Equals("mjpeg", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mjpegb", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.MJpeg;
        if (token.StartsWith("rv", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Real;
        if (token.Equals("theora", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Theora;
        if (token.Equals("vc1", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Vc1;
        if (token.StartsWith("vp6", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Vp6;
        if (token.StartsWith("vp8", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Vp8;
        if (token.Equals("wmv1", StringComparison.InvariantCultureIgnoreCase) || token.Equals("wmv2", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("wmv3", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Wmv;
      }
      return VideoCodec.Unknown;
    }

    private H264Profile ParseH264Profile(string token)
    {
      if (token != null)
      {
        if (token.Equals("constrained baseline", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.Baseline;
        if (token.Equals("baseline", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.Baseline;
        if (token.Equals("main", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.Main;
        if (token.Equals("high", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.High;
        if (token.Equals("high10", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.High;
        if (token.Equals("high422", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.High;
        if (token.Equals("high444", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.High;
      }
      return H264Profile.Unknown;
    }

    private AudioCodec ParseAudioCodec(string token, string detailToken)
    {
      if (token != null)
      {
        if (token.Equals("aac", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mpeg4aac", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("aac_latm", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Aac;
        if (token.Equals("ac3", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("ac-3", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("liba52", StringComparison.InvariantCultureIgnoreCase) || token.Equals("eac3", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Ac3;
        if (token.Equals("amrnb", StringComparison.InvariantCultureIgnoreCase) || token.Equals("amr_nb", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("amrwb", StringComparison.InvariantCultureIgnoreCase) || token.Equals("amr_wb", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Amr;
        if (token.StartsWith("dca", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("dts", StringComparison.InvariantCultureIgnoreCase))
        {
          if (detailToken != null && detailToken.Equals("dts-hd ma", StringComparison.InvariantCultureIgnoreCase))
          {
            return AudioCodec.DtsHd;
          }
          return AudioCodec.Dts;
        }
        if (token.Equals("dts-hd", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.DtsHd;
        if (token.Equals("flac", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Flac;
        if (token.Equals("lpcm", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("pcm_", StringComparison.InvariantCultureIgnoreCase) ||
          token.StartsWith("adpcm_", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Lpcm;
        if (token.Equals("mp3", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Mp3;
        if (token.Equals("mp2", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Mp2;
        if (token.Equals("mp1", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Mp1;
        if (token.Equals("ralf", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("real", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("sipr", StringComparison.InvariantCultureIgnoreCase) || token.Equals("cook", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Real;
        if (token.Equals("truehd", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.TrueHd;
        if (token.Equals("vorbis", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Vorbis;
        if (token.Equals("wmav1", StringComparison.InvariantCultureIgnoreCase) || token.Equals("wmav2", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Wma;
        if (token.Equals("wmapro", StringComparison.InvariantCultureIgnoreCase) || token.Equals("0x0162", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.WmaPro;
      }
      return AudioCodec.Unknown;
    }

    private AudioContainer ParseAudioContainer(string token)
    {
      if (token != null)
      {
        if (token.Equals("ac3", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ac3;
        if (token.Equals("adts", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Adts;
        if (token.Equals("ape", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ape;
        if (token.Equals("asf", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Asf;
        if (token.Equals("flac", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Flac;
        if (token.Equals("flv", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Flv;
        if (token.Equals("lpcm", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Lpcm;
        if (token.Equals("mov", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mp4", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("aac", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp4;
        if (token.Equals("mp3", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp3;
        if (token.Equals("mp2", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp2;
        if (token.Equals("musepack", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mpc", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.MusePack;
        if (token.Equals("ogg", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ogg;
        if (token.Equals("rtp", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Rtp;
        if (token.Equals("rtsp", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Rtsp;
        if (token.Equals("wavpack", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.WavPack;
      }
      return AudioContainer.Unknown;
    }

    private ImageContainer ParseImageContainer(string token)
    {
      if (token != null)
      {
        if (token.Equals("bmp", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Bmp;
        if (token.Equals("gif", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Gif;
        if (token.Equals("mjpeg", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Jpeg;
        if (token.Equals("png", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Png;
        if (token.Equals("raw", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Raw;
      }
      return ImageContainer.Unknown;
    }

    private PixelFormat ParsePixelFormat(string token)
    {
      if (token != null)
      {
        if (token.StartsWith("yuvj411p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv411p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv411;
        if (token.StartsWith("yuvj420p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv420p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv420;
        if (token.StartsWith("yuvj422p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv422p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv422;
        if (token.StartsWith("yuvj440p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv440p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv440;
        if (token.StartsWith("yuvj444p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv444p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv444;
      }
      return PixelFormat.Unknown;
    }

    private void ParseFFMpegOutput(string output, ref MetadataContainer info)
    {
      var input = output.Split('\n');
      if (!input[0].StartsWith("ffmpeg version") && !input[0].StartsWith("ffprobe version"))
        return;
      ParseFFMpegOutputLines(input, ref info);
    }

    private void ParseInputLine(string inputLine, ref MetadataContainer info)
    {
      inputLine = inputLine.Trim();
      int inputPos = inputLine.IndexOf("Input #0", StringComparison.InvariantCultureIgnoreCase);
      string ffmContainer = inputLine.Substring(inputPos + 10, inputLine.IndexOf(",", inputPos + 11) - 10).Trim();
      if (info.IsAudio)
      {
        info.Metadata.AudioContainerType = ParseAudioContainer(ffmContainer);
      }
      else if (info.IsVideo)
      {
        info.Metadata.VideoContainerType = ParseVideoContainer(ffmContainer, (ILocalFsResourceAccessor)info.Metadata.Source);
      }
      else if (info.IsImage)
      {
        info.Metadata.ImageContainerType = ParseImageContainer(ffmContainer);
      }
      else
      {
        info.Metadata.VideoContainerType = ParseVideoContainer(ffmContainer, (ILocalFsResourceAccessor)info.Metadata.Source);
        info.Metadata.AudioContainerType = ParseAudioContainer(ffmContainer);
        info.Metadata.ImageContainerType = ParseImageContainer(ffmContainer);
      }
    }

    private void ParseDurationLine(string durationLine, ref MetadataContainer info)
    {
      durationLine = durationLine.Trim();
      string[] tokens = durationLine.Split(',');
      foreach (string mediaToken in tokens)
      {
        string token = mediaToken.Trim();
        if (token.StartsWith("Duration: ", StringComparison.InvariantCultureIgnoreCase))
        {
          string duration = token.Substring(10).Trim();
          if (duration.IndexOf("N/A") == -1)
          {
            if (duration.Contains(".") == true)
            {
              string[] parts = duration.Split('.');
              duration = parts[0];
            }
            info.Metadata.Duration = TimeSpan.ParseExact(duration, @"hh\:mm\:ss", CultureInfo.InvariantCulture).TotalSeconds;
          }
        }
        else if (token.StartsWith("bitrate: ", StringComparison.InvariantCultureIgnoreCase))
        {
          string bitrateStr = token.Substring(9).Trim();
          int spacePos = bitrateStr.IndexOf(" ");
          if (spacePos > -1)
          {
            string value = bitrateStr.Substring(0, spacePos);
            string unit = bitrateStr.Substring(spacePos + 1);
            int bitrate = int.Parse(value, CultureInfo.InvariantCulture);
            if (unit.Equals("mb/s"))
            {
              bitrate = 1024 * bitrate;
            }
            info.Metadata.Bitrate = bitrate;
          }
        }
      }
    }

    private void ParseStreamVideoLine(string streamVideoLine, ref MetadataContainer info)
    {
      if (info.Video.Codec != VideoCodec.Unknown) return;

      streamVideoLine = streamVideoLine.Trim();
      string beforeVideo = streamVideoLine.Substring(0, streamVideoLine.IndexOf("Video:", StringComparison.InvariantCultureIgnoreCase));
      string afterVideo = streamVideoLine.Substring(beforeVideo.Length);
      string[] beforeVideoTokens = beforeVideo.Split(',');
      string[] afterVideoTokens = afterVideo.Split(',');
      foreach (string mediaToken in beforeVideoTokens)
      {
        string token = mediaToken.Trim();
        if (token.StartsWith("Stream", StringComparison.InvariantCultureIgnoreCase))
        {
          Match match = Regex.Match(token, @"#[\d][\.:](?<stream>[\d]{1,2}).*\((?<lang>(\w+))\)[\.:]", RegexOptions.IgnoreCase);
          if (match.Success)
          {
            info.Video.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
            if (match.Groups.Count == 4)
            {
              string lang = match.Groups["lang"].Value.Trim().ToUpperInvariant();
              if (_countryCodesMapping.ContainsKey(lang))
              {
                info.Video.Language = _countryCodesMapping[lang].TwoLetterISOLanguageName.ToUpperInvariant();
              }
            }
          }
          else
          {
            match = Regex.Match(token, @"#[\d][\.:](?<stream>[\d]{1,2}).*[\.:]", RegexOptions.IgnoreCase);
            if (match.Success)
            {
              info.Video.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
            }
          }
        }
      }
      bool nextTokenIsPixelFormat = false;
      foreach (string mediaToken in afterVideoTokens)
      {
        string token = mediaToken.Trim();
        if (token.StartsWith("Video:", StringComparison.InvariantCultureIgnoreCase))
        {
          string codecValue = token.Substring(token.Trim().IndexOf("Video: ", StringComparison.InvariantCultureIgnoreCase) + 7).Split(' ')[0];
          if ((codecValue != null) && (codecValue.StartsWith("drm", StringComparison.InvariantCultureIgnoreCase)))
          {
            throw new Exception(info.Metadata.Source + " is DRM protected");
          }

          string[] parts = token.Substring(token.IndexOf("Video: ", StringComparison.InvariantCultureIgnoreCase) + 7).Split(' ');
          string codec = parts[0];
          if ((codec != null) && (codec.StartsWith("drm", StringComparison.InvariantCultureIgnoreCase)))
          {
            throw new Exception(info.Metadata.Source + " is DRM protected");
          }
          string codecDetails = null;
          if (parts.Length > 1)
          {
            string details = string.Join(" ", parts).Trim();
            if (details.Contains("("))
            {
              int iIndex = details.IndexOf("(");
              codecDetails = details.Substring(iIndex + 1, details.IndexOf(")") - iIndex - 1);
            }
          }
          info.Video.Codec = ParseVideoCodec(codecValue);
          if (info.IsImage)
          {
            info.Metadata.ImageContainerType = ParseImageContainer(codecValue);
          }
          if(info.Video.Codec == VideoCodec.H264)
          {
            info.Video.H264ProfileType = ParseH264Profile(codecDetails);
          }

          string fourCC = token.Trim();
          if (token.Contains("("))
          {
            string fourCCBlock = fourCC.Substring(fourCC.LastIndexOf("(") + 1, fourCC.LastIndexOf(")") - fourCC.LastIndexOf("(") - 1);
            if (fourCCBlock.IndexOf("/") > -1)
            {
              fourCC = (fourCCBlock.Split('/')[0].Trim()).ToLowerInvariant();
              if (fourCC.IndexOf("[") == -1)
              {
                info.Video.FourCC = fourCC;
              }
            }
          }
          nextTokenIsPixelFormat = true;
        }
        else if (nextTokenIsPixelFormat)
        {
          nextTokenIsPixelFormat = false;
          info.Image.PixelFormatType = ParsePixelFormat(token.Trim());
          info.Video.PixelFormatType = ParsePixelFormat(token.Trim());
        }
        else if (token.IndexOf("x", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          string resolution = token.Trim();
          int aspectStart = resolution.IndexOf(" [");
          if (aspectStart > -1)
          {
            info.Video.PixelAspectRatio = 1.0F;
            string aspectDef = resolution.Substring(aspectStart + 2, resolution.IndexOf("]") - aspectStart - 2);
            int sarIndex = aspectDef.IndexOf("SAR"); //Sample AR
            if (sarIndex < 0)
            {
              sarIndex = aspectDef.IndexOf("PAR"); //Pixel AR
            }
            if (sarIndex > -1)
            {
              aspectDef = aspectDef.Substring(sarIndex + 4);
              string sar = aspectDef.Substring(0, aspectDef.IndexOf(" ")).Trim();
              string[] sarRatio = sar.Split(':');
              if (sarRatio.Length == 2)
              {
                try
                {
                  info.Video.PixelAspectRatio = Convert.ToSingle(sarRatio[0], CultureInfo.InvariantCulture) / Convert.ToSingle(sarRatio[1], CultureInfo.InvariantCulture);
                }
                catch
                { }
              }
            }

            resolution = resolution.Substring(0, aspectStart);
          }
          string[] res = resolution.Split('x');
          if (res.Length == 2)
          {
            try
            {
              if (info.IsImage)
              {
                info.Image.Width = Convert.ToInt32(res[0], CultureInfo.InvariantCulture);
                info.Image.Height = Convert.ToInt32(res[1], CultureInfo.InvariantCulture);
              }
              else
              {
                info.Video.Width = Convert.ToInt32(res[0], CultureInfo.InvariantCulture);
                info.Video.Height = Convert.ToInt32(res[1], CultureInfo.InvariantCulture);
              }
            }
            catch
            { }

            if (info.Video.Height > 0)
            {
              info.Video.AspectRatio = (float)info.Video.Width / (float)info.Video.Height;
            }
          }
        }
        else if (token.IndexOf("SAR", StringComparison.InvariantCultureIgnoreCase) > -1 || token.IndexOf("PAR", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          info.Video.PixelAspectRatio = 1.0F;
          string aspectDef = token.Trim();
          int sarIndex = aspectDef.IndexOf("SAR", StringComparison.InvariantCultureIgnoreCase); //Sample AR
          if (sarIndex < 0)
          {
            sarIndex = aspectDef.IndexOf("PAR", StringComparison.InvariantCultureIgnoreCase); //Pixel AR
          }
          if (sarIndex > -1)
          {
            aspectDef = aspectDef.Substring(sarIndex + 4);
            string sar = aspectDef.Substring(0, aspectDef.IndexOf(" ")).Trim();
            string[] sarRatio = sar.Split(':');
            if (sarRatio.Length == 2)
            {
              try
              {
                info.Video.PixelAspectRatio = Convert.ToSingle(sarRatio[0], CultureInfo.InvariantCulture) / Convert.ToSingle(sarRatio[1], CultureInfo.InvariantCulture);
              }
              catch
              { }
            }
          }
        }
        else if (token.IndexOf("kb/s", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          info.Video.Bitrate = int.Parse(token.Substring(0, token.IndexOf("kb/s", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture);
        }
        else if (token.IndexOf("mb/s", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          info.Video.Bitrate = int.Parse(token.Substring(0, token.IndexOf("mb/s", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture) * 1024;
        }
        else if (token.IndexOf("tbr", StringComparison.InvariantCultureIgnoreCase) > -1 || token.IndexOf("fps", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          if (info.Video.Framerate == 0)
          {
            string fpsValue = "23.976";
            if (token.IndexOf("tbr", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
              fpsValue = token.Substring(0, token.IndexOf("tbr", StringComparison.InvariantCultureIgnoreCase)).Trim();
            }
            else if (token.IndexOf("fps", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
              fpsValue = token.Substring(0, token.IndexOf("fps", StringComparison.InvariantCultureIgnoreCase)).Trim();
            }
            if (fpsValue.Contains("k"))
            {
              fpsValue = fpsValue.Replace("k", "000");
            }
            double fr = 0;
            float validFrameRate = 23.976F;
            if (double.TryParse(fpsValue, out fr) == true)
            {
              if (fr > 23.899999999999999D && fr < 23.989999999999998D)
                validFrameRate = 23.976F;
              else if (fr > 23.989999999999998D && fr < 24.100000000000001D)
                validFrameRate = 24;
              else if (fr >= 24.989999999999998D && fr < 25.100000000000001D)
                validFrameRate = 25;
              else if (fr > 29.899999999999999D && fr < 29.989999999999998D)
                validFrameRate = 29.97F;
              else if (fr >= 29.989999999999998D && fr < 30.100000000000001D)
                validFrameRate = 30;
              else if (fr > 49.899999999999999D && fr < 50.100000000000001D)
                validFrameRate = 50;
              else if (fr > 59.899999999999999D && fr < 59.990000000000002D)
                validFrameRate = 59.94F;
              else if (fr >= 59.990000000000002D && fr < 60.100000000000001D)
                validFrameRate = 60;
            }
            info.Video.Framerate = validFrameRate;
          }
        }
      }
    }

    private void ParseStreamAudioLine(string streamAudioLine, ref MetadataContainer info)
    {
      streamAudioLine = streamAudioLine.Trim();
      AudioStream audio = new AudioStream();
      string[] tokens = streamAudioLine.Split(',');
      foreach (string mediaToken in tokens)
      {
        string token = mediaToken.Trim();
        if (token.StartsWith("Stream", StringComparison.InvariantCultureIgnoreCase))
        {
          Match match = Regex.Match(token, @"#[\d][\.:](?<stream>[\d]{1,2}).*\((?<lang>(\w+))\)[\.:]", RegexOptions.IgnoreCase);
          if (match.Success)
          {
            audio.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
            if (match.Groups.Count == 4)
            {
              string lang = match.Groups["lang"].Value.Trim().ToUpperInvariant();
              if (_countryCodesMapping.ContainsKey(lang))
              {
                audio.Language = _countryCodesMapping[lang].TwoLetterISOLanguageName.ToUpperInvariant();
              }
            }
          }
          else
          {
            match = Regex.Match(token, @"#[\d][\.:](?<stream>[\d]{1,2}).*[\.:]", RegexOptions.IgnoreCase);
            if (match.Success)
            {
              audio.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
            }
          }

          string[] parts = token.Substring(token.IndexOf("Audio: ", StringComparison.InvariantCultureIgnoreCase) + 7).Split(' ');
          string codec = parts[0];
          string codecDetails = null;
          if (parts.Length > 1)
          {
            string details = string.Join(" ", parts).Trim();
            if (details.StartsWith("("))
            {
              int iIndex = details.IndexOf("(");
              codecDetails = details.Substring(iIndex + 1, details.IndexOf(")") - iIndex - 1);
            }
          }
          audio.Codec = ParseAudioCodec(codec, codecDetails);
        }
        else if (token.IndexOf("channels", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Channels = int.Parse(token.Substring(0, token.IndexOf("channels", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture);
        }
        else if (token.IndexOf("stereo", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Channels = 2;
        }
        else if (token.Contains("5.1"))
        {
          audio.Channels = 6;
        }
        else if (token.Contains("7.1"))
        {
          audio.Channels = 8;
        }
        else if (token.IndexOf("mono", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Channels = 1;
        }
        else if (token.IndexOf("Hz", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Frequency = long.Parse(token.Substring(0, token.IndexOf("Hz", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture);
        }
        else if (token.IndexOf("kb/s", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Bitrate = int.Parse(token.Substring(0, token.IndexOf("kb/s", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture);
        }
        else if (token.IndexOf("mb/s", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Bitrate = long.Parse(token.Substring(0, token.IndexOf("mb/s", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture) * 1024;
        }
      }
      audio.Default = streamAudioLine.IndexOf("(default)", StringComparison.InvariantCultureIgnoreCase) > -1;
      info.Audio.Add(audio);
    }

    private void ParseStreamSubtitleLine(string streamSubtitleLine, ref MetadataContainer info)
    {
      streamSubtitleLine = streamSubtitleLine.Trim();

      SubtitleStream sub = new SubtitleStream();
      Match match = Regex.Match(streamSubtitleLine, @"#[\d][\.:](?<stream>[\d]{1,2}).*\((?<lang>(\w+))\)[\.:]", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        sub.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
        if (match.Groups.Count == 4)
        {
          string lang = match.Groups["lang"].Value.Trim().ToUpperInvariant();
          if (_countryCodesMapping.ContainsKey(lang))
          {
            sub.Language = _countryCodesMapping[lang].TwoLetterISOLanguageName.ToUpperInvariant();
          }
        }
      }
      else
      {
        match = Regex.Match(streamSubtitleLine, @"#[\d][\.:](?<stream>[\d]{1,2}).*[\.:]", RegexOptions.IgnoreCase);
        if (match.Success)
        {
          sub.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
        }
      }

      string codecValue = streamSubtitleLine.Substring(streamSubtitleLine.IndexOf("Subtitle: ", StringComparison.InvariantCultureIgnoreCase) + 10).Split(' ')[0];
      sub.Codec = ParseSubtitleCodec(codecValue);
      sub.Default = streamSubtitleLine.IndexOf("(default)", StringComparison.InvariantCultureIgnoreCase) > -1;
      info.Subtitles.Add(sub);
    }

    private void ParseFFMpegOutputLines(string[] input, ref MetadataContainer info)
    {
      foreach (string inputLine in input)
      {
        string line = inputLine.Trim();
        if (line.IndexOf("Input #0") > -1)
        {
          ParseInputLine(line, ref info);
        }
        else if (line.IndexOf("major_brand") > -1)
        {
          string[] tokens = line.Split(':');
          info.Metadata.MajorBrand = tokens[1].Trim();
        }
        else if (line.IndexOf("Duration") > -1)
        {
          ParseDurationLine(line, ref info);
        }
        else if (line.IndexOf("Stream #0") > -1 && line.IndexOf("Video:") > -1)
        {
          ParseStreamVideoLine(line, ref info);
        }
        else if (line.IndexOf("Stream #0") > -1 && line.IndexOf("Audio:") > -1)
        {
          ParseStreamAudioLine(line, ref info);
        }
        else if (line.IndexOf("Stream #0") > -1 && line.IndexOf("Subtitle:") > -1)
        {
          ParseStreamSubtitleLine(line, ref info);
        }
      }
    }

    private void ParseH264Info(ref MetadataContainer info)
    {
      if (info.Video.Codec == VideoCodec.H264)
      {
        try
        {
          byte[] h264Stream = null;
          string arguments = string.Format("-i \"{0}\" -frames:v 1 -c:v copy -f h264", info.Metadata.Source);
          if (info.Metadata.VideoContainerType != VideoContainer.Mpeg2Ts)
          {
            arguments += " -bsf:v h264_mp4toannexb";
          }
          arguments += " -an pipe:";

          bool success;
          lock (FFPROBE_THROTTLE_LOCK)
            success = TryExecuteBinary(ServiceRegistration.Get<IFFMpegLib>().FFMpegBinaryPath, arguments, out h264Stream, (ILocalFsResourceAccessor)info.Metadata.Source, ProcessPriorityClass.BelowNormal);

          if (success == false || h264Stream == null)
          {
            ServiceRegistration.Get<ILogger>().Warn("MediaAnalyzer: Failed to extract h264 annex b header information for resource: '{0}'", info.Metadata.VideoContainerType);
            return;
          }

          H264Analyzer avcAnalyzer = new H264Analyzer();
          if(avcAnalyzer.Parse(h264Stream) == true)
          {
            switch (avcAnalyzer.HeaderProfile)
            {
              case H264Analyzer.H264HeaderProfile.ConstrainedBaseline:
                info.Video.H264ProfileType = H264Profile.Baseline;
                break;
              case H264Analyzer.H264HeaderProfile.Baseline:
                info.Video.H264ProfileType = H264Profile.Baseline;
                break;
              case H264Analyzer.H264HeaderProfile.Main:
                info.Video.H264ProfileType = H264Profile.Main;
                break;
              case H264Analyzer.H264HeaderProfile.Extended:
                info.Video.H264ProfileType = H264Profile.Main;
                break;
              case H264Analyzer.H264HeaderProfile.High:
                info.Video.H264ProfileType = H264Profile.High;
                break;
              case H264Analyzer.H264HeaderProfile.High_10:
                info.Video.H264ProfileType = H264Profile.High10;
                break;
              case H264Analyzer.H264HeaderProfile.High_422:
                info.Video.H264ProfileType = H264Profile.High422;
                break;
              case H264Analyzer.H264HeaderProfile.High_444:
                info.Video.H264ProfileType = H264Profile.High444;
                break;
            }
            info.Video.H264HeaderLevel = avcAnalyzer.HeaderLevel;
            int refFrames = avcAnalyzer.HeaderRefFrames;
            if (info.Video.Width > 0 && info.Video.Height > 0 && refFrames > 0 && refFrames <= 16)
            {
              long dpbMbs = Convert.ToInt64(((float)info.Video.Width * (float)info.Video.Height * (float)refFrames) / 256F);
              foreach (KeyValuePair<float, long> levelDbp in _h264MaxDpbMbs)
              {
                if (levelDbp.Value > dpbMbs)
                {
                  info.Video.H264RefLevel = levelDbp.Key;
                  break;
                }
              }
            }
            if (info.Video.H264HeaderLevel == 0 && info.Video.H264RefLevel == 0)
            {
              if (Logger != null) Logger.Warn("MediaAnalyzer: Couldn't resolve H264 profile/level/reference frames for resource: '{0}'", info.Metadata.Source);
            }
          }
          if (Logger != null) Logger.Debug("MediaAnalyzer: Successfully decoded H264 header: H264 profile {0}, level {1}/level {2}", info.Video.H264ProfileType, info.Video.H264HeaderLevel, info.Video.H264RefLevel);
        }
        catch (Exception e)
        {
          if (Logger != null) Logger.Error("MediaAnalyzer: Failed to analyze H264 information for resource '{0}':\n {1}", info.Metadata.Source, e.Message);
        }
      }
    }

    private void ParseMPEG2TSInfo(ref MetadataContainer info)
    {
      if (info.Metadata.VideoContainerType == VideoContainer.Mpeg2Ts || info.Metadata.VideoContainerType == VideoContainer.M2Ts)
      {
        info.Video.TimestampType = Timestamp.None;
        FileStream raf = null;
        ILocalFsResourceAccessor lfsra = (ILocalFsResourceAccessor)info.Metadata.Source;
        try
        {
          // Impersionation
          using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
          {
            raf = File.OpenRead(lfsra.LocalFileSystemPath);
            byte[] packetBuffer = new byte[193];
            raf.Read(packetBuffer, 0, packetBuffer.Length);
            if (packetBuffer[0] == 0x47) //Sync byte (Standard MPEG2 TS)
            {
              info.Video.TimestampType = Timestamp.None;
              if (Logger != null) Logger.Debug("MediaAnalyzer: Successfully found MPEG2TS timestamp in transport stream: {0}", info.Video.TimestampType);
            }
            else if (packetBuffer[4] == 0x47 && packetBuffer[192] == 0x47) //Sync bytes (BluRay MPEG2 TS)
            {
              if (packetBuffer[0] == 0x00 && packetBuffer[1] == 0x00 && packetBuffer[2] == 0x00 && packetBuffer[3] == 0x00)
              {
                info.Video.TimestampType = Timestamp.Zeros;
              }
              else
              {
                info.Video.TimestampType = Timestamp.Valid;
              }
              if (Logger != null) Logger.Debug("MediaAnalyzer: Successfully found MPEG2TS timestamp in transport stream: {0}", info.Video.TimestampType);
            }
            else
            {
              info.Video.TimestampType = Timestamp.None;
              if (Logger != null) Logger.Error("MediaAnalyzer: Failed to retreive MPEG2TS timestamp for resource '{0}'", info.Metadata.Source);
            }
          }
        }
        finally
        {
          if (raf != null)
          {
            raf.Close();
          }
        }
      }
    }

    #endregion
  }
}
