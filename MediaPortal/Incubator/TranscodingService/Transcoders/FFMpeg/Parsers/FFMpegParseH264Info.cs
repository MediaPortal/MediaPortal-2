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
using System.Data;
using System.Diagnostics;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Analyzers;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using System.Text;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseH264Info
  {
    internal static void ParseH264Info(IResourceAccessor res, MetadataContainer info, Dictionary<float, long> h264MaxDpbMbs, int transcoderTimeout)
    {
      if (info.Video[Editions.DEFAULT_EDITION].Codec == VideoCodec.H264)
      {
        if (res is ILocalFsResourceAccessor fileRes && !fileRes.IsFile)
          return;
        //if (!(res is INetworkResourceAccessor))
        //  return;

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
          if (info.Metadata[Editions.DEFAULT_EDITION].VideoContainerType != VideoContainer.Mpeg2Ts)
          {
            arguments += " -bsf:v h264_mp4toannexb";
          }
          arguments += " -an -";
          data = ProbeResource(res, arguments, transcoderTimeout);
          if (data == null)
          {
            Logger.Error("MediaAnalyzer: Timed out analyzing H264 information for resource '{0}'", res);
            return;
          }

          debug = "Parse binary dump";
          H264Analyzer avcAnalyzer = new H264Analyzer();
          if (avcAnalyzer.Parse(data) == true)
          {
            switch (avcAnalyzer.HeaderProfile)
            {
              case H264Analyzer.H264HeaderProfile.ConstrainedBaseline:
                info.Video[Editions.DEFAULT_EDITION].ProfileType = EncodingProfile.Baseline;
                break;
              case H264Analyzer.H264HeaderProfile.Baseline:
                info.Video[Editions.DEFAULT_EDITION].ProfileType = EncodingProfile.Baseline;
                break;
              case H264Analyzer.H264HeaderProfile.Main:
                info.Video[Editions.DEFAULT_EDITION].ProfileType = EncodingProfile.Main;
                break;
              case H264Analyzer.H264HeaderProfile.Extended:
                info.Video[Editions.DEFAULT_EDITION].ProfileType = EncodingProfile.Main;
                break;
              case H264Analyzer.H264HeaderProfile.High:
                info.Video[Editions.DEFAULT_EDITION].ProfileType = EncodingProfile.High;
                break;
              case H264Analyzer.H264HeaderProfile.High_10:
                info.Video[Editions.DEFAULT_EDITION].ProfileType = EncodingProfile.High10;
                break;
              case H264Analyzer.H264HeaderProfile.High_422:
                info.Video[Editions.DEFAULT_EDITION].ProfileType = EncodingProfile.High422;
                break;
              case H264Analyzer.H264HeaderProfile.High_444:
                info.Video[Editions.DEFAULT_EDITION].ProfileType = EncodingProfile.High444;
                break;
            }
            info.Video[Editions.DEFAULT_EDITION].HeaderLevel = avcAnalyzer.HeaderLevel;
            int refFrames = avcAnalyzer.HeaderRefFrames;

            debug = "File parsed";
            if (info.Video[Editions.DEFAULT_EDITION].Width > 0 && info.Video[Editions.DEFAULT_EDITION].Height > 0 && refFrames > 0 && refFrames <= 16)
            {
              long dpbMbs = Convert.ToInt64(((float)info.Video[Editions.DEFAULT_EDITION].Width * (float)info.Video[Editions.DEFAULT_EDITION].Height * (float)refFrames) / 256F);
              foreach (KeyValuePair<float, long> levelDbp in h264MaxDpbMbs)
              {
                if (levelDbp.Value > dpbMbs)
                {
                  info.Video[Editions.DEFAULT_EDITION].RefLevel = levelDbp.Key;
                  break;
                }
              }
            }
            if (info.Video[Editions.DEFAULT_EDITION].HeaderLevel == 0 && info.Video[Editions.DEFAULT_EDITION].RefLevel == 0)
            {
              Logger.Warn("MediaAnalyzer: Couldn't resolve H264 profile/level/reference frames for resource: '{0}'", res);
            }
          }
          Logger.Debug("MediaAnalyzer: Successfully decoded H264 header: H264 profile {0}, level {1}/level {2}", 
            info.Video[Editions.DEFAULT_EDITION].ProfileType, info.Video[Editions.DEFAULT_EDITION].HeaderLevel, info.Video[Editions.DEFAULT_EDITION].RefLevel);
        }
        catch (Exception e)
        {
          if (Logger != null)
          {
            Logger.Error("MediaAnalyzer: Failed to analyze H264 information for resource '{0}':\n{1}", res, e.Message);
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

    private static byte[] ProbeResource(IResourceAccessor accessor, string arguments, int transcoderTimeout)
    {
      ProcessStartInfo startInfo = new ProcessStartInfo()
      {
        FileName = FFMpegBinary.FFMpegPath,
        Arguments = arguments,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8
      };
#if !TRANSCODE_CONSOLE_TEST
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(accessor.CanonicalLocalResourcePath))
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
          if (!ffmpeg.WaitForExit(transcoderTimeout))
          {
            ffmpeg.Kill();
            stream.Dispose();
            return null;
          }

          ffmpeg.Close();
          byte[] data = ReadToEnd(stream);
          stream.Dispose();
#if !TRANSCODE_CONSOLE_TEST
          NativeMethods.CloseHandle(userToken);
#endif
          return data;
        }
      }
    }

    private static byte[] ReadToEnd(System.IO.Stream stream)
    {
      if (stream == null)
        return null;

      if (stream.CanSeek)
        stream.Position = 0;

      byte[] readBuffer = new byte[4096];
      int totalBytesRead = 0;
      int bytesRead;

      while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
      {
        totalBytesRead += bytesRead;
        if (totalBytesRead == readBuffer.Length)
        {
          int nextByte = stream.ReadByte();
          if (nextByte != -1)
          {
            byte[] temp = new byte[readBuffer.Length * 2];
            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
            readBuffer = temp;
            totalBytesRead++;
          }
        }
      }

      byte[] buffer = readBuffer;
      if (readBuffer.Length != totalBytesRead)
      {
        buffer = new byte[totalBytesRead];
        Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
      }
      return buffer;
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
