#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.UI.Players.Video;

namespace MediaPortal.Media.MetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for BluRay discs.
  /// </summary>
  public class BluRayMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the BluRayMetadataExtractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "E23918BB-297F-463b-8AEB-2BDCE9998028";

    /// <summary>
    /// BluRayMetadataExtractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    protected const string MEDIA_CATEGORY_NAME_MOVIE = "Movie";

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static BluRayMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);

      MediaCategory movieCategory;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_MOVIE, out movieCategory))
        movieCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_MOVIE, new List<MediaCategory> {DefaultMediaCategories.Video});
      MEDIA_CATEGORIES.Add(movieCategory);
    }

    public BluRayMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "BluRay metadata extractor", MetadataExtractorPriority.Core, false, 
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata
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
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;
        using (IFileSystemResourceAccessor fsra = (IFileSystemResourceAccessor) mediaItemAccessor.Clone())
        using (ILocalFsResourceAccessor lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
          if (!lfsra.IsFile && lfsra.ResourceExists("BDMV"))
          {
            IFileSystemResourceAccessor fsraBDMV = lfsra.GetResource("BDMV");
            if (fsraBDMV != null && fsraBDMV.ResourceExists("index.bdmv"))
            {
              // This line is important to keep in, if no VideoAspect is created here, the MediaItems is not detected as Video! 
              MediaItemAspect.GetOrCreateAspect(extractedAspectData, VideoAspect.Metadata);
              MediaItemAspect mediaAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, MediaAspect.Metadata);

              mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, "video/bluray"); // BluRay disc

              string bdmvDirectory = lfsra.LocalFileSystemPath;
              BDInfoExt bdinfo = new BDInfoExt(bdmvDirectory);
              string title = bdinfo.GetTitle();
              mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, title ?? mediaItemAccessor.ResourceName);

              // Check for BD disc thumbs
              FileInfo thumbnail = bdinfo.GetBiggestThumb();
              if (thumbnail != null)
              {
                byte[] binary = new byte[thumbnail.Length];
                using (FileStream fileStream = new FileStream(thumbnail.FullName, FileMode.Open, FileAccess.Read))
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                  binaryReader.Read(binary, 0, binary.Length);

                MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, binary);
              }
              return true;
            }
          }
        return false;
      }
      catch
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here
        if (mediaItemAccessor != null)
          ServiceRegistration.Get<ILogger>().Info("BluRayMetadataExtractor: Exception reading source '{0}'", mediaItemAccessor.ResourcePathName);
        return false;
      }
    }

    #endregion
  }
}