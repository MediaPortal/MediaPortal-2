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

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Process;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;
using MediaPortal.Plugins.Transcoding.Service.Metadata;

namespace MediaPortal.Plugins.Transcoding.Service.Analyzers
{
  public class MediaAnalyzer
  {
    #region Constants

    /// <summary>
    /// Maximum duration for analyzing H264 stream.
    /// </summary>
    private const int H264_TIMEOUT_MS = 3000;

    #endregion

    private static readonly object FFPROBE_THROTTLE_LOCK = new object();
    private static int _analyzerMaximumThreads;
    private static int _analyzerTimeout;
    private static long _analyzerStreamTimeout;
    private static ILogger _logger { get; set; }
    private static readonly Dictionary<string, CultureInfo> _countryCodesMapping = new Dictionary<string, CultureInfo>();
    private static readonly Dictionary<float, long> _h264MaxDpbMbs = new Dictionary<float, long>();

    static MediaAnalyzer()
    {
      _analyzerMaximumThreads = TranscodingServicePlugin.Settings.AnalyzerMaximumThreads;
      _analyzerTimeout = TranscodingServicePlugin.Settings.AnalyzerTimeout;
      _analyzerStreamTimeout = TranscodingServicePlugin.Settings.AnalyzerStreamTimeout;
      _logger = ServiceRegistration.Get<ILogger>();

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

    private static ProcessExecutionResult ParseFile(ILocalFsResourceAccessor lfsra, string arguments)
    {
      ProcessExecutionResult executionResult;
      lock (FFPROBE_THROTTLE_LOCK)
        executionResult = FFMpegBinary.FFProbeExecuteWithResourceAccessAsync(lfsra, arguments, ProcessPriorityClass.Idle, _analyzerTimeout).Result;

      // My guess (agree with dtb's comment): AFAIK ffmpeg uses stdout to pipe out binary data(multimedia, snapshots, etc.)
      // and stderr is used for logging purposes. In your example you use stdout.
      // http://stackoverflow.com/questions/4246758/why-doesnt-this-method-redirect-my-output-from-exe-ffmpeg
      return executionResult;
    }

    /// <summary>
    /// Pareses a local image file using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="lfsra">ILocalFsResourceAccessor to the image file</param>
    /// <returns>a Metadata Container with all information about the mediaitem</returns>
    public static MetadataContainer ParseImageFile(ILocalFsResourceAccessor lfsra)
    {
      string fileName = lfsra.LocalFileSystemPath;
      //Default image decoder (image2) fails if file name contains å, ø, ö etc., so force format to image2pipe
      string arguments = string.Format("-threads {0} -f image2pipe -i \"{1}\"", _analyzerMaximumThreads, fileName);

      ProcessExecutionResult executionResult = ParseFile(lfsra, arguments);
      if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
      {
        _logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
        MetadataContainer info = new MetadataContainer { Metadata = { Source = lfsra } };
        info.Metadata.Mime = MimeDetector.GetFileMime(lfsra, "Image/Unknown");
        info.Metadata.Size = lfsra.Size;
        FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);
        return info;
      }

      if (executionResult != null)
        _logger.Error("MediaAnalyzer: Failed to extract image media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", fileName, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
      else
        _logger.Error("MediaAnalyzer: Failed to extract image media type information for resource '{0}', executionResult=null", fileName);

      return null;
    }

    /// <summary>
    /// Pareses a local video file using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="lfsra">ILocalFsResourceAccessor to the video file</param>
    /// <returns>a Metadata Container with all information about the mediaitem</returns>
    public static MetadataContainer ParseVideoFile(ILocalFsResourceAccessor lfsra)
    {
      string fileName = lfsra.LocalFileSystemPath;
      string arguments = string.Format("-threads {0} -i \"{1}\"", _analyzerMaximumThreads, fileName);

      ProcessExecutionResult executionResult = ParseFile(lfsra, arguments);
      if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
      {
        //_logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
        MetadataContainer info = new MetadataContainer { Metadata = { Source = lfsra } };
        info.Metadata.Mime = MimeDetector.GetFileMime(lfsra, "Video/Unknown");
        info.Metadata.Size = lfsra.Size;
        FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);
        FFMpegParseH264Info.ParseH264Info(ref info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
        FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(ref info);
        return info;
      }

      if (executionResult != null)
        _logger.Error("MediaAnalyzer: Failed to extract video media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", fileName, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
      else
        _logger.Error("MediaAnalyzer: Failed to extract video media type information for resource '{0}', executionResult=null", fileName);

      return null;
    }

    /// <summary>
    /// Pareses a local audio file using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="lfsra">ILocalFsResourceAccessor to the file</param>
    /// <returns>a Metadata Container with all information about the mediaitem</returns>
    public static MetadataContainer ParseAudioFile(ILocalFsResourceAccessor lfsra)
    {
      string fileName = lfsra.LocalFileSystemPath;
      string arguments = string.Format("-threads {0} -i \"{1}\"", _analyzerMaximumThreads, fileName);

      ProcessExecutionResult executionResult = ParseFile(lfsra, arguments);
      if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
      {
        //_logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
        MetadataContainer info = new MetadataContainer { Metadata = { Source = lfsra } };
        info.Metadata.Mime = MimeDetector.GetFileMime(lfsra, "Audio/Unknown");
        info.Metadata.Size = lfsra.Size;
        FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);
        return info;
      }

      if (executionResult != null)
        _logger.Error("MediaAnalyzer: Failed to extract audio media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", fileName, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
      else
        _logger.Error("MediaAnalyzer: Failed to extract audio media type information for resource '{0}', executionResult=null", fileName);

      return null;
    }

    /// <summary>
    /// Pareses a video URL using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="streamLink">INetworkResourceAccessor to the video URL</param>
    /// <returns>a Metadata Container with all information about the URL</returns>
    public static MetadataContainer ParseVideoStream(INetworkResourceAccessor streamLink)
    {
      string arguments = "";
      if (streamLink.URL.StartsWith("rtsp://", System.StringComparison.InvariantCultureIgnoreCase) == true)
      {
        arguments += "-rtsp_transport +tcp+udp ";
      }
      arguments += "-analyzeduration " + _analyzerStreamTimeout + " ";
      arguments += string.Format("-i \"{0}\"", streamLink.URL);

      ProcessExecutionResult executionResult;
      lock (FFPROBE_THROTTLE_LOCK)
        executionResult = FFMpegBinary.FFProbeExecuteAsync(arguments, ProcessPriorityClass.Idle, _analyzerTimeout).Result;

      if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
      {
        _logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
        MetadataContainer info = new MetadataContainer { Metadata = { Source = streamLink } };
        info.Metadata.Mime = MimeDetector.GetUrlMime(streamLink.URL, "Video/Unknown");
        info.Metadata.Size = 0;
        FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);
        FFMpegParseH264Info.ParseH264Info(ref info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
        FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(ref info);
        return info;
      }

      _logger.Error("MediaAnalyzer: Failed to extract video media type information for resource '{0}'", streamLink);

      return null;
    }

    /// <summary>
    /// Pareses a video URL using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="streamLink">INetworkResourceAccessor to the audio URL</param>
    /// <returns>a Metadata Container with all information about the URL</returns>
    public static MetadataContainer ParseAudioStream(INetworkResourceAccessor streamLink)
    {
      string arguments = "";
      if (streamLink.URL.StartsWith("rtsp://", System.StringComparison.InvariantCultureIgnoreCase) == true)
      {
        arguments += "-rtsp_transport +tcp+udp ";
      }
      arguments += "-analyzeduration " + _analyzerStreamTimeout + " ";
      arguments += string.Format("-i \"{0}\"", streamLink.URL);

      ProcessExecutionResult executionResult;
      lock (FFPROBE_THROTTLE_LOCK)
        executionResult = FFMpegBinary.FFProbeExecuteAsync(arguments, ProcessPriorityClass.Idle, _analyzerTimeout).Result;

      if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
      {
        _logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
        MetadataContainer info = new MetadataContainer { Metadata = { Source = streamLink } };
        info.Metadata.Mime = MimeDetector.GetUrlMime(streamLink.URL, "audio/Unknown");
        info.Metadata.Size = 0;
        FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);
        return info;
      }

      _logger.Error("MediaAnalyzer: Failed to extract video media type information for resource '{0}'", streamLink);

      return null;
    }
  }
}
