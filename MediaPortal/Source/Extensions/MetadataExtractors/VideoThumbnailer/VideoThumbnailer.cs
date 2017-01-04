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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Utilities.Process;
using System.Threading;

namespace MediaPortal.Extensions.MetadataExtractors.VideoThumbnailer
{
  /// <summary>
  /// MediaPortal 2 metadata extractor to exctract thumbnails from videos.
  /// </summary>
  public class VideoThumbnailer : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the VideoThumbnailer metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "FB0AA0ED-97B2-4721-BE74-AC67E77A17B2";

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// Maximum duration for creating a single video thumbnail.
    /// </summary>
    protected const int PROCESS_TIMEOUT_MS = 30000;
    protected const int MAX_CONCURRENT_FFMPEG = 5;

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static readonly SemaphoreSlim FFMPEG_THROTTLE_LOCK = new SemaphoreSlim(MAX_CONCURRENT_FFMPEG, MAX_CONCURRENT_FFMPEG);
    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static VideoThumbnailer()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);
    }

    public VideoThumbnailer()
    {
      // Creating thumbs with this MetadataExtractor takes much longer than downloading them from the internet.
      // This MetadataExtractor only creates thumbs if the ThumbnailLargeAspect has not been filled before.
      // ToDo: Correct this once we have a better priority system
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Video thumbnail extractor", MetadataExtractorPriority.FallBack, true,
          MEDIA_CATEGORIES, new[]
              {
                ThumbnailLargeAspect.Metadata
              });
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
    {
      try
      {
        if (importOnly)
          return false;

        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          return ExtractThumbnail(rah.LocalFsResourceAccessor, extractedAspectData);
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Error("VideoThumbnailer: Exception reading resource '{0}' (Text: '{1}')", e, mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    private bool ExtractThumbnail(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // We can only work on files and make sure this file was detected by a lower MDE before (title is set then).
      // VideoAspect must be present to be sure it is actually a video resource.
      if (!lfsra.IsFile || !extractedAspectData.ContainsKey(VideoStreamAspect.ASPECT_ID))
        return false;

      byte[] thumb;
      // We only want to create missing thumbnails here, so check for existing ones first
      if (MediaItemAspect.TryGetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out thumb) && thumb != null)
        return false;

      //ServiceRegistration.Get<ILogger>().Info("VideoThumbnailer: Evaluate {0}", lfsra.ResourceName);

      bool isPrimaryResource = false;
      IList<MultipleMediaItemAspect> resourceAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out resourceAspects))
      {
        foreach(MultipleMediaItemAspect pra in resourceAspects)
        {
          string accessorPath = (string)pra.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
          ResourcePath resourcePath = ResourcePath.Deserialize(accessorPath);
          if (resourcePath.Equals(lfsra.CanonicalLocalResourcePath))
          {
            if(pra.GetAttributeValue<bool?>(ProviderResourceAspect.ATTR_PRIMARY) == true)
            {
              isPrimaryResource = true;
              break;
            }
          }
        }
      }

      if (!isPrimaryResource) //Ignore subtitles
        return false;

      // Check for a reasonable time offset
      long defaultVideoOffset = 720;
      long videoDuration;
      string downscale = ",scale=iw/2:-1"; // Reduces the video frame size to a half of original
      IList<MultipleMediaItemAspect> videoAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspectData, VideoStreamAspect.Metadata, out videoAspects))
      {
        if ((videoDuration = videoAspects[0].GetAttributeValue<long>(VideoStreamAspect.ATTR_DURATION)) > 0)
        {
          if (defaultVideoOffset > videoDuration * 1 / 3)
            defaultVideoOffset = videoDuration * 1 / 3;
        }

        int videoWidth = videoAspects[0].GetAttributeValue<int>(VideoStreamAspect.ATTR_WIDTH);
        // Don't downscale SD video frames, quality is already quite low.
        if (videoWidth > 0 && videoWidth <= 720)
          downscale = "";
      }

      // ToDo: Move creation of temp file names to FileUtils class
      string tempFileName = Path.GetTempPath() + Guid.NewGuid() + ".jpg";
      string executable = FileUtils.BuildAssemblyRelativePath("ffmpeg.exe");
      string arguments = string.Format("-ss {0} -i \"{1}\" -vframes 1 -an -dn -vf \"yadif='mode=send_frame:parity=auto:deint=all',scale=iw*sar:ih,setsar=1/1{3}\" -y \"{2}\"",
        defaultVideoOffset,
        // Calling EnsureLocalFileSystemAccess not necessary; access for external process ensured by ExecuteWithResourceAccess
        lfsra.LocalFileSystemPath,
        tempFileName,
        downscale);

      //ServiceRegistration.Get<ILogger>().Info("VideoThumbnailer: FFMpeg {0} {1}", executable, arguments);

      try
      {
        Task<ProcessExecutionResult> executionResult = null;
        FFMPEG_THROTTLE_LOCK.Wait();
        executionResult = FFMpegBinary.FFMpegExecuteWithResourceAccessAsync(lfsra, arguments, ProcessPriorityClass.BelowNormal, PROCESS_TIMEOUT_MS);
        if (executionResult.Result.Success && File.Exists(tempFileName))
        {
          var binary = FileUtils.ReadFile(tempFileName);
          MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, binary);
          // Calling EnsureLocalFileSystemAccess not necessary; only string operation
          ServiceRegistration.Get<ILogger>().Info("VideoThumbnailer: Successfully created thumbnail for resource '{0}'", lfsra.LocalFileSystemPath);
        }
        else
        {
          // Calling EnsureLocalFileSystemAccess not necessary; only string operation
          ServiceRegistration.Get<ILogger>().Warn("VideoThumbnailer: Failed to create thumbnail for resource '{0}'", lfsra.LocalFileSystemPath);
          ServiceRegistration.Get<ILogger>().Debug("VideoThumbnailer: FFMpeg failure {0} dump:\n{1}", executionResult.Result.ExitCode, executionResult.Result.StandardError);
        }
      }
      catch (AggregateException ae)
      {
        ae.Handle(e =>
        {
          if (e is TaskCanceledException)
          {
            ServiceRegistration.Get<ILogger>().Warn("VideoThumbnailer.ExtractThumbnail: External process aborted due to timeout: Executable='{0}', Arguments='{1}', Timeout='{2}'", executable, arguments, PROCESS_TIMEOUT_MS);
            return true;
          }
          return false;
        });
      }
      finally
      {
        FFMPEG_THROTTLE_LOCK.Release();

        try
        {
          if (File.Exists(tempFileName))
            File.Delete(tempFileName);
        }
        catch { }
      }
      return true;
    }

    #endregion
  }
}
