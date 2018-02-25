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
using System.Globalization;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Process;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;
using System.IO;
using MediaPortal.Plugins.Transcoding.Interfaces;
using System.Threading.Tasks;
using System.Threading;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg
{
  public class FFMpegMediaAnalyzer : BaseMediaAnalyzer
  {
    #region Constants

    /// <summary>
    /// Maximum duration for analyzing H264 stream.
    /// </summary>
    private const int H264_TIMEOUT_MS = 3000;

    #endregion

    private readonly SemaphoreSlim _probeLock = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
    private readonly Dictionary<string, CultureInfo> _countryCodesMapping = new Dictionary<string, CultureInfo>();
    private readonly string ANALYSIS_CACHE_PATH = Path.Combine(DEFAULT_ANALYSIS_CACHE_PATH, "FFMpeg");

    public FFMpegMediaAnalyzer()
    {
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

    private async Task<ProcessExecutionResult> ParseFileAsync(ILocalFsResourceAccessor lfsra, string arguments)
    {
      await _probeLock.WaitAsync();
      try
      {
        ProcessExecutionResult executionResult = await FFMpegBinary.FFProbeExecuteWithResourceAccessAsync(lfsra, arguments, ProcessPriorityClass.BelowNormal, _analyzerTimeout);

        // My guess (agree with dtb's comment): AFAIK ffmpeg uses stdout to pipe out binary data(multimedia, snapshots, etc.)
        // and stderr is used for logging purposes. In your example you use stdout.
        // http://stackoverflow.com/questions/4246758/why-doesnt-this-method-redirect-my-output-from-exe-ffmpeg
        return executionResult;
      }
      catch (Exception ex)
      {
        _logger.Error("FFMpegMediaAnalyzer: Failed to parse file '{0}'", ex, lfsra.LocalFileSystemPath);
      }
      finally
      {
        _probeLock.Release();
      }
      return null;
    }

    public override async Task<MetadataContainer> ParseMediaStreamAsync(IResourceAccessor MediaResource)
    {
      if (MediaResource is ILocalFsResourceAccessor)
      {
        ILocalFsResourceAccessor fileResource = (ILocalFsResourceAccessor)MediaResource;
        string fileName = fileResource.LocalFileSystemPath;
        if (!(HasImageExtension(fileName) || HasVideoExtension(fileName) || HasAudioExtension(fileName)))
          return null;
        string arguments = "";

        //Check cache
        MetadataContainer info = await LoadAnalysisAsync(MediaResource);
        if (info != null)
          return info;

        if (HasImageExtension(fileName))
        {
          //Default image decoder (image2) fails if file name contains å, ø, ö etc., so force format to image2pipe
          arguments = string.Format("-threads {0} -f image2pipe -i \"{1}\"", _analyzerMaximumThreads, fileName);
        }
        else
        {
          arguments = string.Format("-threads {0} -i \"{1}\"", _analyzerMaximumThreads, fileName);
        }

        ProcessExecutionResult executionResult = await ParseFileAsync(fileResource, arguments);
        if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
        {
          //_logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
          info = new MetadataContainer { Metadata = { Source = MediaResource } };
          info.Metadata.Size = fileResource.Size;
          FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);

          // Special handling for files like OGG which will be falsely identified as videos
          if (info.Metadata.VideoContainerType != VideoContainer.Unknown && info.Video.Codec == VideoCodec.Unknown)
          {
            info.Metadata.VideoContainerType = VideoContainer.Unknown;
          }

          if (info.IsImage || HasImageExtension(fileName))
          {
            info.Metadata.Mime = MimeDetector.GetFileMime(fileResource, "image/unknown");
          }
          else if (info.IsVideo|| HasVideoExtension(fileName))
          {
            info.Metadata.Mime = MimeDetector.GetFileMime(fileResource, "video/unknown");
            FFMpegParseH264Info.ParseH264Info(ref info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
            FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(ref info);
          }
          else if (info.IsAudio || HasAudioExtension(fileName))
          {
            info.Metadata.Mime = MimeDetector.GetFileMime(fileResource, "audio/unknown");
          }
          else
          {
            info.Metadata.Mime = MimeDetector.GetFileMime(fileResource, "unknown/unknown");
          }
          await SaveAnalysisAsync(MediaResource, info);
          return info;
        }

        if (executionResult != null)
          _logger.Error("FFMpegMediaAnalyzer: Failed to extract media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", fileName, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
        else
          _logger.Error("FFMpegMediaAnalyzer: Failed to extract media type information for resource '{0}', executionResult=null", fileName);
      }
      else if (MediaResource is INetworkResourceAccessor)
      {
        string url = ((INetworkResourceAccessor)MediaResource).URL;
        if (!(HasImageExtension(url) || HasVideoExtension(url) || HasAudioExtension(url)))
          return null;

        //Check cache
        MetadataContainer info = await LoadAnalysisAsync(MediaResource);
        if (info != null)
          return info;

        string arguments = "";
        if (url.StartsWith("rtsp://", StringComparison.InvariantCultureIgnoreCase) == true)
        {
          arguments += "-rtsp_transport +tcp+udp ";
        }
        arguments += "-analyzeduration " + _analyzerStreamTimeout + " ";
        arguments += string.Format("-i \"{0}\"", url);

        ProcessExecutionResult executionResult = null;
        await _probeLock.WaitAsync();
        try
        {
          executionResult = FFMpegBinary.FFProbeExecuteAsync(arguments, ProcessPriorityClass.BelowNormal, _analyzerTimeout).Result;
        }
        catch (Exception ex)
        {
          _logger.Error("FFMpegMediaAnalyzer: Failed to parse url '{0}'", ex, url);
        }
        finally
        {
          _probeLock.Release();
        }

        if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
        {
          //_logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
          info = new MetadataContainer { Metadata = { Source = MediaResource } };
          info.Metadata.Size = 0;
          FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);

          // Special handling for files like OGG which will be falsely identified as videos
          if (info.Metadata.VideoContainerType != VideoContainer.Unknown && info.Video.Codec == VideoCodec.Unknown)
          {
            info.Metadata.VideoContainerType = VideoContainer.Unknown;
          }

          if (info.IsImage)
          {
            info.Metadata.Mime = MimeDetector.GetUrlMime(url, "image/unknown");
          }
          else if (info.IsVideo)
          {
            info.Metadata.Mime = MimeDetector.GetUrlMime(url, "video/unknown");
            FFMpegParseH264Info.ParseH264Info(ref info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
            FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(ref info);
          }
          else if (info.IsAudio)
          {
            info.Metadata.Mime = MimeDetector.GetUrlMime(url, "audio/unknown");
          }
          else
          {
            info.Metadata.Mime = MimeDetector.GetUrlMime(url, "unknown/unknown");
          }
          await SaveAnalysisAsync(MediaResource, info);
          return info;
        }

        if (executionResult != null)
          _logger.Error("FFMpegMediaAnalyzer: Failed to extract media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", url, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
        else
          _logger.Error("FFMpegMediaAnalyzer: Failed to extract media type information for resource '{0}', executionResult=null", url);
      }
      return null;
    }
  }
}
