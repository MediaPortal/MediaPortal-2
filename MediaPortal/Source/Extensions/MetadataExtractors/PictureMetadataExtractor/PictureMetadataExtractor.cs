#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.ResourceAccess;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.PictureMetadataExtractor.Settings;
using MediaPortal.Utilities;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Extensions.MetadataExtractors.PictureMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for picture files. Supports several formats.
  /// </summary>
  public class PictureMetadataExtractor : IMetadataExtractor
  {
    #region Public constants

    /// <summary>
    /// GUID string for the picture metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "D39E244F-443E-4d94-9304-D1407A869209";

    /// <summary>
    /// Picture metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Protected fields and classes

    protected static IList<string> SHARE_CATEGORIES = new List<string>();
    protected static IList<string> PICTURE_EXTENSIONS = new List<string>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static PictureMetadataExtractor()
    {
      SHARE_CATEGORIES.Add(DefaultMediaCategory.Image.ToString());
      PictureMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<PictureMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the movie extensions for which this <see cref="PictureMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(PictureMetadataExtractorSettings settings)
    {
      PICTURE_EXTENSIONS = new List<string>(settings.PictureExtensions.Select(e => e.ToLowerInvariant()));
    }

    public PictureMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Picture metadata extractor", true,
          SHARE_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                PictureAspect.Metadata
              });
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Returns the information if the specified file name (or path) has a file extension which is
    /// supposed to be supported by this metadata extractor.
    /// </summary>
    /// <param name="fileName">Relative or absolute file path to check.</param>
    /// <returns><c>true</c>, if the file's extension is supposed to be supported, else <c>false</c>.</returns>
    protected static bool HasImageExtension(string fileName)
    {
      string ext = PathHelper.GetExtension(fileName).ToLowerInvariant();
      return PICTURE_EXTENSIONS.Contains(ext);
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      if (!HasImageExtension(mediaItemAccessor.ResourcePathName))
        return false;

      // TODO: The creation of new media item aspects could be moved to a general method
      MediaItemAspect mediaAspect;
      if (!extractedAspectData.TryGetValue(MediaAspect.ASPECT_ID, out mediaAspect))
        extractedAspectData[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
      MediaItemAspect pictureAspect;
      if (!extractedAspectData.TryGetValue(PictureAspect.ASPECT_ID, out pictureAspect))
        extractedAspectData[PictureAspect.ASPECT_ID] = pictureAspect = new MediaItemAspect(PictureAspect.Metadata);
      MediaItemAspect thumbnailSmallAspect;
      if (!extractedAspectData.TryGetValue(ThumbnailSmallAspect.ASPECT_ID, out thumbnailSmallAspect))
        extractedAspectData[ThumbnailSmallAspect.ASPECT_ID] = thumbnailSmallAspect = new MediaItemAspect(ThumbnailSmallAspect.Metadata);
      MediaItemAspect thumbnailLargeAspect;
      if (!extractedAspectData.TryGetValue(ThumbnailLargeAspect.ASPECT_ID, out thumbnailLargeAspect))
        extractedAspectData[ThumbnailLargeAspect.ASPECT_ID] = thumbnailLargeAspect = new MediaItemAspect(ThumbnailLargeAspect.Metadata);

      try
      {
        // Open a stream for media item to detect mimeType.
        using (Stream mediaStream = mediaItemAccessor.OpenRead())
          mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, MimeTypeDetector.GetMimeType(mediaStream));
        // Extract EXIF information from media item.
        using (ExifMetaInfo.ExifMetaInfo exif = new ExifMetaInfo.ExifMetaInfo(mediaItemAccessor))
        {
          mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, PathHelper.GetFileNameWithoutExtension(mediaItemAccessor.ResourcePathName));
          mediaAspect.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, exif.OriginalDate != DateTime.MinValue ? exif.OriginalDate : mediaItemAccessor.LastChanged);
          mediaAspect.SetAttribute(MediaAspect.ATTR_COMMENT, StringUtils.TrimToNull(exif.ImageDescription));

          pictureAspect.SetAttribute(PictureAspect.ATTR_WIDTH, (Int32) exif.PixXDim);
          pictureAspect.SetAttribute(PictureAspect.ATTR_HEIGHT, (Int32) exif.PixYDim);
          pictureAspect.SetAttribute(PictureAspect.ATTR_MAKE, StringUtils.TrimToNull(exif.EquipMake));
          pictureAspect.SetAttribute(PictureAspect.ATTR_MODEL, StringUtils.TrimToNull(exif.EquipModel));
          pictureAspect.SetAttribute(PictureAspect.ATTR_EXPOSURE_BIAS, ((double) exif.ExposureBias).ToString());
          pictureAspect.SetAttribute(PictureAspect.ATTR_EXPOSURE_TIME, exif.ExposureTime.ToString());
          pictureAspect.SetAttribute(PictureAspect.ATTR_FLASH_MODE, StringUtils.TrimToNull(exif.FlashMode));
          pictureAspect.SetAttribute(PictureAspect.ATTR_FNUMBER, string.Format("F {0}", (double) exif.FNumber));
          pictureAspect.SetAttribute(PictureAspect.ATTR_ISO_SPEED, StringUtils.TrimToNull(exif.ISOSpeed));
          pictureAspect.SetAttribute(PictureAspect.ATTR_ORIENTATION, (Int32) exif.Orientation);
          pictureAspect.SetAttribute(PictureAspect.ATTR_METERING_MODE, exif.MeteringMode.ToString());

          ILocalFsResourceAccessor lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(mediaItemAccessor);
          string localFsResourcePath = lfsra.LocalFileSystemPath;
          if (localFsResourcePath != null)
          {
            // In quick mode only allow thumbs taken from cache.
            bool cachedOnly = forceQuickMode;

            // Thumbnail extraction
            IThumbnailGenerator generator = ServiceRegistration.Get<IThumbnailGenerator>();
            byte[] thumbData;
            ImageType imageType;
            if (generator.GetThumbnail(localFsResourcePath, 96, 96, cachedOnly, out thumbData, out imageType))
              thumbnailSmallAspect.SetAttribute(ThumbnailSmallAspect.ATTR_THUMBNAIL, thumbData);
            if (generator.GetThumbnail(localFsResourcePath, 256, 256, cachedOnly, out thumbData, out imageType))
              thumbnailLargeAspect.SetAttribute(ThumbnailLargeAspect.ATTR_THUMBNAIL, thumbData);
          }
        }
        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here
        ServiceRegistration.Get<ILogger>().Info("PictureMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.LocalResourcePath, e.Message);
      }
      return false;
    }

    #endregion
  }
}
