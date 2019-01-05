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
using System.Diagnostics;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Utilities.Process;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Analyzers;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using System.Text;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseH264Info
  {
    #region Protected fields and classes

    protected static readonly object FFPROBE_THROTTLE_LOCK = new object();

    #endregion

    internal static void ParseH264Info(MetadataContainer info, IResourceAccessor res, Dictionary<float, long> h264MaxDpbMbs, int transcoderTimeout)
    {
      if (info.Video.Codec == VideoCodec.H264)
      {
        if (res is ILocalFsResourceAccessor fileRes && !fileRes.IsFile)
          return;
        if (!(res is INetworkResourceAccessor))
          return;

        //TODO: Remove this debug code when error found
        string debug = "";
        string tempFileName = Path.GetTempPath() + Guid.NewGuid() + ".bin";
        try
        {
          byte[] data = null;
          //string arguments = string.Format("-i \"{0}\" -frames:v 1 -c:v copy -f h264", info.Metadata.Source);
          //if (info.Metadata.VideoContainerType != VideoContainer.Mpeg2Ts)
          //{
          //  arguments += " -bsf:v h264_mp4toannexb";
          //}
          //arguments += string.Format(" -an \"{0}\"", tempFileName);
          //debug = arguments;

          //ProcessExecutionResult result;
          //lock (FFPROBE_THROTTLE_LOCK)
          //  result = FFMpegBinary.FFMpegExecuteWithResourceAccessAsync((ILocalFsResourceAccessor)info.Metadata.Source, arguments, ProcessPriorityClass.Idle, transcoderTimeout).Result;
          //debug = result.StandardError;
          //if (!result.Success || !File.Exists(tempFileName))
          //{
          //  ServiceRegistration.Get<ILogger>().Warn("MediaAnalyzer: Failed to extract h264 annex b header information for resource: '{0}'", info.Metadata.Source);
          //  return;
          //}
          //data = File.ReadAllBytes(tempFileName);

          string arguments = string.Format("-i \"{0}\" -frames:v 1 -c:v copy -f h264", res);
          if (info.Metadata.VideoContainerType != VideoContainer.Mpeg2Ts)
          {
            arguments += " -bsf:v h264_mp4toannexb";
          }
          arguments += " -an -";
          lock (FFPROBE_THROTTLE_LOCK)
            data = ProbeResource(res, arguments);

          debug = "Parse binary dump: " + tempFileName;
          H264Analyzer avcAnalyzer = new H264Analyzer();
          if (avcAnalyzer.Parse(File.ReadAllBytes(tempFileName)) == true)
          {
            switch (avcAnalyzer.HeaderProfile)
            {
              case H264Analyzer.H264HeaderProfile.ConstrainedBaseline:
                info.Video.ProfileType = EncodingProfile.Baseline;
                break;
              case H264Analyzer.H264HeaderProfile.Baseline:
                info.Video.ProfileType = EncodingProfile.Baseline;
                break;
              case H264Analyzer.H264HeaderProfile.Main:
                info.Video.ProfileType = EncodingProfile.Main;
                break;
              case H264Analyzer.H264HeaderProfile.Extended:
                info.Video.ProfileType = EncodingProfile.Main;
                break;
              case H264Analyzer.H264HeaderProfile.High:
                info.Video.ProfileType = EncodingProfile.High;
                break;
              case H264Analyzer.H264HeaderProfile.High_10:
                info.Video.ProfileType = EncodingProfile.High10;
                break;
              case H264Analyzer.H264HeaderProfile.High_422:
                info.Video.ProfileType = EncodingProfile.High422;
                break;
              case H264Analyzer.H264HeaderProfile.High_444:
                info.Video.ProfileType = EncodingProfile.High444;
                break;
            }
            info.Video.HeaderLevel = avcAnalyzer.HeaderLevel;
            int refFrames = avcAnalyzer.HeaderRefFrames;

            debug = "File parsed";
            if (info.Video.Width > 0 && info.Video.Height > 0 && refFrames > 0 && refFrames <= 16)
            {
              long dpbMbs = Convert.ToInt64(((float)info.Video.Width * (float)info.Video.Height * (float)refFrames) / 256F);
              foreach (KeyValuePair<float, long> levelDbp in h264MaxDpbMbs)
              {
                if (levelDbp.Value > dpbMbs)
                {
                  info.Video.RefLevel = levelDbp.Key;
                  break;
                }
              }
            }
            if (info.Video.HeaderLevel == 0 && info.Video.RefLevel == 0)
            {
              if (Logger != null) Logger.Warn("MediaAnalyzer: Couldn't resolve H264 profile/level/reference frames for resource: '{0}'", info.Metadata.Source);
            }
          }
          if (Logger != null) Logger.Debug("MediaAnalyzer: Successfully decoded H264 header: H264 profile {0}, level {1}/level {2}", info.Video.ProfileType, info.Video.HeaderLevel, info.Video.RefLevel);
        }
        catch (Exception e)
        {
          if (Logger != null)
          {
            Logger.Error("MediaAnalyzer: Failed to analyze H264 information for resource '{0}':\n{1}", info.Metadata.Source, e.Message);
            Logger.Error("MediaAnalyzer: Debug info: {0}", debug);
          }
        }
        try
        {
          if (File.Exists(tempFileName))
            File.Delete(tempFileName);
        }
        catch { }
      }
    }

    private static byte[] ProbeResource(IResourceAccessor accessor, string arguments)
    {
      ProcessStartInfo startInfo = new ProcessStartInfo()
      {
        FileName = FFMpegBinary.FFMpegPath,
        Arguments = arguments,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8
      };
#if !TRANSCODE_CONSOLE_TEST
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor((mediaAccessor).CanonicalLocalResourcePath))
      {
        //Only when the server is running as a service it will have elevation rights
        using (ImpersonationProcess ffmpeg = new ImpersonationProcess { StartInfo = startInfo })
        {
          IntPtr userToken = IntPtr.Zero;
          if (!ImpersonationHelper.GetTokenByProcess(out userToken, true))
            return null;
#else
      {
        {
          Process ffmpeg = new Process() { StartInfo = startInfo };
#endif
#if !TRANSCODE_CONSOLE_TEST
          ffmpeg.StartAsUser(userToken);
#else
          ffmpeg.Start();
#endif
          ffmpeg.BeginErrorReadLine();

          var stream = ffmpeg.StandardOutput.BaseStream;
          ffmpeg.WaitForExit();
          //ffmpeg.ExitCode;
          //iExitCode = executionResult.Result.ExitCode;
          //if (data.TranscodeData.InputResourceAccessor is FFMpegLiveAccessor)
          //{
          //  ffmpeg.StandardInput.Close();
          //}
          ffmpeg.Close();

          byte[] data = new byte[stream.Length];
          stream.Position = 0;
          stream.Write(data, 0, data.Length);
          stream.Dispose();
#if !TRANSCODE_CONSOLE_TEST
          NativeMethods.CloseHandle(userToken);
#endif
          return data;
        }
      }
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
