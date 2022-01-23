#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Parsers;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;
using System.IO;
using System.Linq;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using System.Threading.Tasks;
using System.Threading;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg
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

    private async Task<ProcessExecutionResult> ParseUrlAsync(string url, string arguments)
    {
      await _probeLock.WaitAsync();
      try
      {
        ProcessExecutionResult executionResult = await FFMpegBinary.FFProbeExecuteAsync(arguments, ProcessPriorityClass.BelowNormal, _analyzerTimeout);
        return executionResult;
      }
      catch (Exception ex)
      {
        _logger.Error("FFMpegMediaAnalyzer: Failed to parse url '{0}'", ex, url);
      }
      finally
      {
        _probeLock.Release();
      }
      return null;
    }

    public override async Task<MetadataContainer> ParseMediaStreamAsync(IEnumerable<IResourceAccessor> mediaResources)
    {
      bool isImage = true;
      bool isFileSystem = false;
      bool isNetwork = false;
      bool isUnsupported = false;

      //Check all files
      if (!mediaResources.Any())
        throw new ArgumentException($"FFMpegMediaAnalyzer: Resource list is empty", "mediaResources");

      foreach (var res in mediaResources)
      {
        if (res is IFileSystemResourceAccessor fileRes)
        {
          isFileSystem = true;
          if (!fileRes.IsFile)
            throw new ArgumentException($"FFMpegMediaAnalyzer: Resource '{res.ResourceName}' is not a file", "mediaResources");
        }
        else if (res is INetworkResourceAccessor urlRes)
        {
          isNetwork = true;
        }
        else
        {
          isUnsupported = true;
        }
      }

      if (isFileSystem && isNetwork)
        throw new ArgumentException($"FFMpegMediaAnalyzer: Resources are of mixed media formats", "mediaResources");

      if (isUnsupported)
        throw new ArgumentException($"FFMpegMediaAnalyzer: Resources are of unsupported media formats", "mediaResources");

      ProcessExecutionResult executionResult = null;
      string logFileName = "?";
      if (isFileSystem)
      {
        List<LocalFsResourceAccessorHelper> helpers = new List<LocalFsResourceAccessorHelper>();
        List<IDisposable> accessors = new List<IDisposable>();
        try
        {
          MetadataContainer info = null;
          logFileName = mediaResources.First().ResourceName;

          //Initialize and check file system resources
          foreach (var res in mediaResources)
          {
            string resName = res.ResourceName;
            if (!(HasImageExtension(resName) || HasVideoExtension(resName) || HasAudioExtension(resName) || HasOpticalDiscFileExtension(resName)))
              throw new ArgumentException($"FFMpegMediaAnalyzer: Resource '{res.ResourceName}' has unsupported file extension", "mediaResources");

            if (!(HasImageExtension(resName)))
              isImage = false;

            //Ensure availability
            var rah = new LocalFsResourceAccessorHelper(res);
            helpers.Add(rah);
            var accessor = rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess();
            if (accessor != null)
              accessors.Add(accessor);
          }

          string fileName;
          if (helpers.Count > 1) //Check if concatenation needed
            fileName = $"concat:\"{string.Join("|", helpers.Select(h => h.LocalFsResourceAccessor.LocalFileSystemPath))}\"";
          else
            fileName = $"\"{helpers.First().LocalFsResourceAccessor.LocalFileSystemPath}\"";

          string arguments = "";
          if (isImage)
          {
            //Default image decoder (image2) fails if file name contains å, ø, ö etc., so force format to image2pipe
            arguments = string.Format("-threads {0} -f image2pipe -i {1}", _analyzerMaximumThreads, fileName);
          }
          else
          {
            arguments = string.Format("-threads {0} -i {1}", _analyzerMaximumThreads, fileName);
          }

          //Use first file for parsing. The other files are expected to be of same encoding and same location
          var firstFile = helpers.First().LocalFsResourceAccessor;
          executionResult = await ParseFileAsync(helpers.First().LocalFsResourceAccessor, arguments);
          if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
          {
            //_logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
            info = new MetadataContainer();
            info.AddEdition(Editions.DEFAULT_EDITION);
            info.Metadata[Editions.DEFAULT_EDITION].Size = helpers.Sum(h => h.LocalFsResourceAccessor.Size);
            FFMpegParseFFMpegOutput.ParseFFMpegOutput(firstFile, executionResult.StandardError, ref info, _countryCodesMapping);

            // Special handling for files like OGG which will be falsely identified as videos
            if (info.Metadata[0].VideoContainerType != VideoContainer.Unknown && info.Video[0].Codec == VideoCodec.Unknown)
            {
              info.Metadata[0].VideoContainerType = VideoContainer.Unknown;
            }

            if (info.IsImage(Editions.DEFAULT_EDITION) || HasImageExtension(fileName))
            {
              info.Metadata[Editions.DEFAULT_EDITION].Mime = MimeDetector.GetFileMime(firstFile, "image/unknown");
            }
            else if (info.IsVideo(Editions.DEFAULT_EDITION) || HasVideoExtension(fileName))
            {
              info.Metadata[Editions.DEFAULT_EDITION].Mime = MimeDetector.GetFileMime(firstFile, "video/unknown");
              await _probeLock.WaitAsync();
              try
              {
                FFMpegParseH264Info.ParseH264Info(firstFile, info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
              }
              finally
              {
                _probeLock.Release();
              }
              FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(firstFile, info);
            }
            else if (info.IsAudio(Editions.DEFAULT_EDITION) || HasAudioExtension(fileName))
            {
              info.Metadata[Editions.DEFAULT_EDITION].Mime = MimeDetector.GetFileMime(firstFile, "audio/unknown");
            }
            else
            {
              return null;
            }

            return info;
          }
        }
        finally
        {
          foreach (var accessor in accessors)
            accessor?.Dispose();
          foreach (var helper in helpers)
            helper?.Dispose();
        }

        if (executionResult != null)
          _logger.Error("FFMpegMediaAnalyzer: Failed to extract media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", logFileName, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
        else
          _logger.Error("FFMpegMediaAnalyzer: Failed to extract media type information for resource '{0}', Execution result empty", logFileName);
      }
      else if (isNetwork)
      {
        //We can only read one network resource so take the first
        var urlRes = mediaResources.First() as INetworkResourceAccessor;
        string url = urlRes.URL;

        string arguments = "";
        if (url.StartsWith("rtsp://", StringComparison.InvariantCultureIgnoreCase) == true)
        {
          arguments += "-rtsp_transport tcp ";
        }
        arguments += "-analyzeduration " + _analyzerStreamTimeout + " ";

        //Resolve host first because ffprobe can hang when resolving host
        var resolvedUrl = UrlHelper.ResolveHostToIPv4Url(url);
        if (string.IsNullOrEmpty(resolvedUrl))
        {
          throw new InvalidOperationException($"FFMpegMediaAnalyzer: Failed to resolve host for resource '{url}'");
        }
        arguments += string.Format("-i \"{0}\"", resolvedUrl);

        executionResult = await ParseUrlAsync(url, arguments);
        if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
        {
          //_logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
          MetadataContainer info = new MetadataContainer();
          info.AddEdition(Editions.DEFAULT_EDITION);
          info.Metadata[Editions.DEFAULT_EDITION].Size = 0;
          FFMpegParseFFMpegOutput.ParseFFMpegOutput(urlRes, executionResult.StandardError, ref info, _countryCodesMapping);

          // Special handling for files like OGG which will be falsely identified as videos
          if (info.Metadata[Editions.DEFAULT_EDITION].VideoContainerType != VideoContainer.Unknown && info.Video[Editions.DEFAULT_EDITION].Codec == VideoCodec.Unknown)
          {
            info.Metadata[Editions.DEFAULT_EDITION].VideoContainerType = VideoContainer.Unknown;
          }

          if (info.IsImage(Editions.DEFAULT_EDITION))
          {
            info.Metadata[Editions.DEFAULT_EDITION].Mime = MimeDetector.GetUrlMime(url, "image/unknown");
          }
          else if (info.IsVideo(Editions.DEFAULT_EDITION))
          {
            info.Metadata[Editions.DEFAULT_EDITION].Mime = MimeDetector.GetUrlMime(url, "video/unknown");
            await _probeLock.WaitAsync();
            try
            {
              FFMpegParseH264Info.ParseH264Info(urlRes, info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
            }
            finally
            {
              _probeLock.Release();
            }
            FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(urlRes, info);
          }
          else if (info.IsAudio(Editions.DEFAULT_EDITION))
          {
            info.Metadata[Editions.DEFAULT_EDITION].Mime = MimeDetector.GetUrlMime(url, "audio/unknown");
          }
          else
          {
            return null;
          }
          return info;
        }

        if (executionResult != null)
          _logger.Error("FFMpegMediaAnalyzer: Failed to extract media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", url, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
        else
          _logger.Error("FFMpegMediaAnalyzer: Failed to extract media type information for resource '{0}', Execution result empty", url);
      }

      return null;
    }
  }
}
