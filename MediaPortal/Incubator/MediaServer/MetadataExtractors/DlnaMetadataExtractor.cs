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
using System.IO;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Extensions.MediaServer.Aspects;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Extensions.MediaServer.MetadataExtractors
{
  public class DlnaMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid(MediaServerPlugin.DEVICE_UUID);

    /// <summary>
    /// Maximum duration for creating a single movie thumbnail.
    /// </summary>
    protected const int PROCESS_TIMEOUT_MS = 2500;

    protected static List<MediaCategory> MediaCategories = new List<MediaCategory>
      { DefaultMediaCategories.Audio, DefaultMediaCategories.Image, DefaultMediaCategories.Video };

    static DlnaMetadataExtractor()
    {
      //ImageMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ImageMetadataExtractorSettings>();
      //InitializeExtensions(settings);

      
    }

    public DlnaMetadataExtractor()
    {
      Metadata = new MetadataExtractorMetadata(
        MetadataExtractorId,
        "DLNA metadata extractor",
        MetadataExtractorPriority.Core,
        true,
        MediaCategories,
        new[]
          {
            MediaAspect.Metadata,
            DlnaItemAspect.Metadata
          });
    }

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata { get; private set; }

    public bool TryExtractMetadata(
      IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;
        using (var fsra = (IFileSystemResourceAccessor)mediaItemAccessor.Clone())
        {
          if (!fsra.IsFile)
            return false;
          using (var lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
          {
            var info = ExtractFFMpegInfo(lfsra);
            ConvertFFMPEGInfoToAspectData(extractedAspectData, info);
          }
        }       
        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("DlnaMediaServer: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }

      return false;
    }

    private FFMPEGInfo ExtractFFMpegInfo(ILocalFsResourceAccessor lfsra)
    {
      var executable = FileUtils.BuildAssemblyRelativePath("..\\VideoThumbnailer\\ffmpeg.exe");
      var arguments = string.Format("-i \"{0}\"",
        lfsra.LocalFileSystemPath);

      string result;
      if (TryExecuteReadString(executable, arguments, out result, ProcessPriorityClass.BelowNormal, PROCESS_TIMEOUT_MS))
      {        
        ServiceRegistration.Get<ILogger>().Info("DlnaMediaServer: Successfully ran ffmpeg:\n {0}", result);
        return ParseFFMpegOutput(result);
      }
      ServiceRegistration.Get<ILogger>().Warn("DlnaMediaServer: Failed to extract media type information for resource '{0}'", lfsra.LocalFileSystemPath);
      return null;
    }

    public static bool TryExecuteReadString(string executable, string arguments, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = 1000)
    {
      using (Process process = new Process { StartInfo = new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true } })
      {
        process.Start();
        //process.PriorityClass = priorityClass;
        using (process.StandardError)
        {
          result = process.StandardError.ReadToEnd();
          if (process.WaitForExit(maxWaitMs))
            return process.ExitCode == 1;
        }
        if (!process.HasExited)
          process.Close();
      }
      return false;
    }

    private FFMPEGInfo ParseFFMpegOutput(string ffmpeg)
    {
      var input = ffmpeg.Split('\n');
      if (!input[0].StartsWith("ffmpeg version"))
        return null;
      for(var i = 0; i < input.Length; i++)
      {
        if (input[i].StartsWith("Input"))
        {
          return ParseFFMpegInputBlock(input, i);
        }
      }
      return null;            
    }

    private FFMPEGInfo ParseFFMpegInputBlock(string[] input, int i)
    {
      var result = new FFMPEGInfo();     
      var match = Regex.Match(input[i++], @"Input #(\d), (\w*), from");
      if (!match.Success)
        return null;

      result.ContainerType = match.Groups[2].Value;

      while (i < input.Length)
      {
        match = Regex.Match(input[i++], @"  Duration: ([\d\.:]+), start: [\d\.]+, bitrate: (\d+) kb/s");
        if (match.Success)
        {
          result.ContainerBitRate = match.Groups[2].Value;
          return result;
        }
      }
      return null;
    }

    private static bool ConvertFFMPEGInfoToAspectData(IDictionary<Guid, MediaItemAspect> extractedAspectData, FFMPEGInfo info)
    {
      switch (info.ContainerType)
      {
        case "mp3":
          MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAspect.ATTR_MIME_TYPE, "audio/mpeg");
          MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAspect.ATTR_PROFILE, "MP3");
          break;
        case "jpg":
          MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAspect.ATTR_MIME_TYPE, "image/jpeg");
          MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAspect.ATTR_PROFILE, "JPEG_LRG");
          break;
        default:
          return false;
      }
      return true;
    }

    private class FFMPEGInfo
    {
      public string ContainerType;
      public string ContainerBitRate;

      public string ImageType;
      public string ImageResolutionX;
      public string ImageResolutionY;
      public string ImageBitRate;

      public string AudioType;
      public string AudioFrequency;
      public string AudioChannels;
      public string AudioBits;
      public string AudioBitRate;
    }

    #endregion
  }
}