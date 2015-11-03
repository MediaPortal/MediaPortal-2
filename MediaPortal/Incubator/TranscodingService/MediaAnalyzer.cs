#region Copyright (C) 2007-2012 Team MediaPortal

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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.SystemAPI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Process;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Plugins.Transcoding.Service.Interfaces;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers;

namespace MediaPortal.Plugins.Transcoding.Service
{
  public class MediaAnalyzer : IMediaAnalyzer
  {
    #region Constants

    /// <summary>
    /// Default maximum duration for analyzing file.
    /// </summary>
    protected const int PROCESS_TIMEOUT_MS = 30000;

    /// <summary>
    /// Maximum duration for analyzing H264 stream.
    /// </summary>
    protected const int H264_TIMEOUT_MS = 3000;

    #endregion

    #region Protected fields

    protected static readonly object FFPROBE_THROTTLE_LOCK = new object();

    #endregion

    public int AnalyzerTimeout { get; set; }
    public int AnalyzerMaximumThreads { get; set; }
   
    public ILogger Logger { get; set;  }

    private readonly Dictionary<string, CultureInfo> _countryCodesMapping = new Dictionary<string, CultureInfo>();
    private readonly Dictionary<float, long> _h264MaxDpbMbs = new Dictionary<float, long>();
   
    public MediaAnalyzer()
    {
      AnalyzerTimeout = PROCESS_TIMEOUT_MS;
      AnalyzerMaximumThreads = 0;

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
      string arguments = string.Format("-threads {0} -i \"{1}\"", AnalyzerMaximumThreads, fileName);

      ProcessExecutionResult executionResult;
      lock (FFPROBE_THROTTLE_LOCK)
        executionResult = FFMpegBinary.FFProbeExecuteWithResourceAccessAsync(lfsra, arguments, ProcessPriorityClass.Idle, AnalyzerTimeout).Result;
      
      // My guess (agree with dtb's comment): AFAIK ffmpeg uses stdout to pipe out binary data(multimedia, snapshots, etc.)
      // and stderr is used for logging purposes. In your example you use stdout.
      // http://stackoverflow.com/questions/4246758/why-doesnt-this-method-redirect-my-output-from-exe-ffmpeg
      if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
      {
        if (Logger != null) Logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
        MetadataContainer info = new MetadataContainer { Metadata = { Source = lfsra } };
        info.Metadata.Mime = MimeTypeDetector.GetMimeType(fileName);
        info.Metadata.Size = lfsra.Size;
        FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);
        FFMpegParseH264Info.ParseH264Info(ref info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
        FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(ref info);
        return info;
      }

      if (executionResult != null) 
        Logger.Error("MediaAnalyzer: Failed to extract media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", fileName, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
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
        executionResult = FFMpegBinary.FFProbeExecuteAsync(arguments, ProcessPriorityClass.Idle, AnalyzerTimeout).Result;
      
      if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
      {
        if (Logger != null) Logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
        MetadataContainer info = new MetadataContainer { Metadata = { Source = streamLink } };
        info.Metadata.Mime = MimeTypeDetector.GetMimeType(streamLink.URL);
        info.Metadata.Size = 0;
        FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);
        FFMpegParseH264Info.ParseH264Info(ref info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
        FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(ref info);
        return info;
      }

      if (Logger != null) Logger.Error("MediaAnalyzer: Failed to extract media type information for resource '{0}'", streamLink);
      
      return null;
    }
  }
}
