#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseH264Info
  {

    #region Protected fields and classes

    protected static readonly object FFPROBE_THROTTLE_LOCK = new object();

    #endregion

    private static int TranscoderTimeout { get; set; }

    internal static void ParseH264Info(ref MetadataContainer info, Dictionary<float, long> h264MaxDpbMbs, int transcoderTimeout)
    {
      TranscoderTimeout = transcoderTimeout;

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
            success = TryExecuteBinary(arguments, out h264Stream, (ILocalFsResourceAccessor)info.Metadata.Source, ProcessPriorityClass.BelowNormal);

          if (success == false || h264Stream == null)
          {
            ServiceRegistration.Get<ILogger>().Warn("MediaAnalyzer: Failed to extract h264 annex b header information for resource: '{0}'", info.Metadata.VideoContainerType);
            return;
          }

          H264Analyzer avcAnalyzer = new H264Analyzer();
          if (avcAnalyzer.Parse(h264Stream) == true)
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
          if (Logger != null) Logger.Error("MediaAnalyzer: Failed to analyze H264 information for resource '{0}':\n {1}", info.Metadata.Source, e.Message);
        }
      }
    }

    // TODO: Should be in the FFMpegLib
    private static bool TryExecuteBinary(string arguments, out byte[] result, ILocalFsResourceAccessor lfsra, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal)
    {
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
      {
        using (Process process = new Process { StartInfo = new ProcessStartInfo(FFMpegBinary.FFMpegPath, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true } })
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

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
