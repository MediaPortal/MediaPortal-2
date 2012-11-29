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
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Extensions.MetadataExtractors.MovieThumbnailer
{
  /// <summary>
  /// MediaPortal 2 metadata extractor to exctract thumbnails from videos.
  /// </summary>
  public class MovieThumbnailer : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the MovieThumbnailer metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "FB0AA0ED-97B2-4721-BE74-AC67E77A17B2";

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// Maximum duration for creating a single movie thumbnail.
    /// </summary>
    protected const int PROCESS_TIMEOUT_MS = 2500;

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static MovieThumbnailer()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);
    }

    public MovieThumbnailer()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Movies thumbnail extractor", MetadataExtractorPriority.Extended, true,
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

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (forceQuickMode)
          return false;

        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;
        using (IFileSystemResourceAccessor fsra = (IFileSystemResourceAccessor) mediaItemAccessor.Clone())
        using (ILocalFsResourceAccessor lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
          return ExtractThumbnail(lfsra, extractedAspectData);
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("MovieThumbnailer: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    private bool ExtractThumbnail(ILocalFsResourceAccessor lfsra, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      // We can only work on files and make sure this file was detected by a lower MDE before (title is set then).
      string title;
      if (!lfsra.IsFile || !MediaItemAspect.TryGetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, out title))
        return false;

      byte[] thumb;
      // We only want to create missing thumbnails here, so check for existing ones first
      if (MediaItemAspect.TryGetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out thumb) && thumb != null)
        return true;

      // Check for a reasonable time offset
      long defaultVideoOffset = 720;
      long videoDuration;
      if (MediaItemAspect.TryGetAttribute(extractedAspectData, VideoAspect.ATTR_DURATION, out videoDuration))
      {
        if (defaultVideoOffset > videoDuration * 1 / 3)
          defaultVideoOffset = videoDuration * 1 / 3;
      }

      string tempFileName = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");
      string executable = FileUtils.BuildAssemblyRelativePath("ffmpeg.exe");
      string arguments = string.Format("-ss {0} -itsoffset -5 -i \"{1}\" -vframes 1 -vf \"yadif=0:-1:0,scale=iw*sar:ih,setsar=1:1,scale=iw/2:-1\" -y \"{2}\"",
        defaultVideoOffset,
        lfsra.LocalFileSystemPath,
        tempFileName);

      try
      {
        if (ProcessUtils.TryExecute(executable, arguments, ProcessPriorityClass.BelowNormal, PROCESS_TIMEOUT_MS) && File.Exists(tempFileName))
        {
          var binary = FileUtils.ReadFile(tempFileName);
          MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, binary);
          ServiceRegistration.Get<ILogger>().Info("MovieThumbnailer: Successfully created thumbnail for resource '{0}'", lfsra.LocalFileSystemPath);
        }
        else
          ServiceRegistration.Get<ILogger>().Warn("MovieThumbnailer: Failed to create thumbnail for resource '{0}'", lfsra.LocalFileSystemPath);
      }
      finally
      {
        if (File.Exists(tempFileName))
          File.Delete(tempFileName);
      }
      return true;
    }

    #endregion
  }
}