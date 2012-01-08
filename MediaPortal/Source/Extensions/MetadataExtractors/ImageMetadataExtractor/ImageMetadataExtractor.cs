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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.ImageMetadataExtractor.Settings;
using MediaPortal.Utilities;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Extensions.MetadataExtractors.ImageMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for image files. Supports several formats.
  /// </summary>
  public class ImageMetadataExtractor : IMetadataExtractor
  {
    #region Public constants

    /// <summary>
    /// GUID string for the image metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "426AC13A-AC78-4224-A1B1-65CD60AC0B88";

    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Protected fields and classes

    protected static IList<string> SHARE_CATEGORIES = new List<string>();
    protected static IList<string> IMAGE_FILE_EXTENSIONS = new List<string>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static ImageMetadataExtractor()
    {
      SHARE_CATEGORIES.Add(DefaultMediaCategory.Image.ToString());
      ImageMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ImageMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the movie extensions for which this <see cref="ImageMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(ImageMetadataExtractorSettings settings)
    {
      IMAGE_FILE_EXTENSIONS = new List<string>(settings.ImageFileExtensions.Select(e => e.ToLowerInvariant()));
    }

    public ImageMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Image metadata extractor", true,
          SHARE_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                ImageAspect.Metadata
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
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return IMAGE_FILE_EXTENSIONS.Contains(ext);
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      string fileName = mediaItemAccessor.ResourceName;
      if (!HasImageExtension(fileName))
        return false;

      // TODO: The creation of new media item aspects could be moved to a general method
      MediaItemAspect mediaAspect;
      if (!extractedAspectData.TryGetValue(MediaAspect.ASPECT_ID, out mediaAspect))
        extractedAspectData[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
      MediaItemAspect imageAspect;
      if (!extractedAspectData.TryGetValue(ImageAspect.ASPECT_ID, out imageAspect))
        extractedAspectData[ImageAspect.ASPECT_ID] = imageAspect = new MediaItemAspect(ImageAspect.Metadata);
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
        {
          string mimeType = MimeTypeDetector.GetMimeType(mediaStream);
          if (mimeType != null)
            mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, mimeType);
        }
        // Extract EXIF information from media item.
        using (ExifMetaInfo.ExifMetaInfo exif = new ExifMetaInfo.ExifMetaInfo(mediaItemAccessor))
        {
          mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, ProviderPathHelper.GetFileNameWithoutExtension(fileName));
          mediaAspect.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, exif.OriginalDate != DateTime.MinValue ? exif.OriginalDate : mediaItemAccessor.LastChanged);
          mediaAspect.SetAttribute(MediaAspect.ATTR_COMMENT, StringUtils.TrimToNull(exif.ImageDescription));

          imageAspect.SetAttribute(ImageAspect.ATTR_WIDTH, (int) exif.PixXDim);
          imageAspect.SetAttribute(ImageAspect.ATTR_HEIGHT, (int) exif.PixYDim);
          imageAspect.SetAttribute(ImageAspect.ATTR_MAKE, StringUtils.TrimToNull(exif.EquipMake));
          imageAspect.SetAttribute(ImageAspect.ATTR_MODEL, StringUtils.TrimToNull(exif.EquipModel));
          imageAspect.SetAttribute(ImageAspect.ATTR_EXPOSURE_BIAS, ((double) exif.ExposureBias).ToString());
          imageAspect.SetAttribute(ImageAspect.ATTR_EXPOSURE_TIME, exif.ExposureTime.ToString());
          imageAspect.SetAttribute(ImageAspect.ATTR_FLASH_MODE, StringUtils.TrimToNull(exif.FlashMode));
          imageAspect.SetAttribute(ImageAspect.ATTR_FNUMBER, string.Format("F {0}", (double) exif.FNumber));
          imageAspect.SetAttribute(ImageAspect.ATTR_ISO_SPEED, StringUtils.TrimToNull(exif.ISOSpeed));
          imageAspect.SetAttribute(ImageAspect.ATTR_ORIENTATION, (Int32) exif.Orientation);
          imageAspect.SetAttribute(ImageAspect.ATTR_METERING_MODE, exif.MeteringMode.ToString());

          IResourceAccessor ra = mediaItemAccessor.Clone();
          try
          {
            using (ILocalFsResourceAccessor lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(ra))
            {
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
          }
          catch
          {
            ra.Dispose();
            throw;
          }
        }
        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("ImageMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    #endregion
  }
}
