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
using System.Linq;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.MetadataExtractors.Settings;
using MediaPortal.Plugins.Transcoding.Service.Metadata;
using MediaPortal.Plugins.Transcoding.Service.Analyzers;

namespace MediaPortal.Plugins.Transcoding.MetadataExtractors
{
  public class TranscodeImageMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid("DFC8E367-C255-4B54-8FC9-236D4C6EBA55");

    protected static List<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory> { DefaultMediaCategories.Image };
    protected static ICollection<string> IMAGE_FILE_EXTENSIONS = new List<string>();

    static TranscodeImageMetadataExtractor()
    {
      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(TranscodeItemImageAspect.Metadata);
      TranscodeImageMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TranscodeImageMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the movie extensions for which this <see cref="TranscodeImageMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(TranscodeImageMetadataExtractorSettings settings)
    {
      IMAGE_FILE_EXTENSIONS = new List<string>(settings.ImageFileExtensions.Select(e => e.ToLowerInvariant()));
    }

    public TranscodeImageMetadataExtractor()
    {
      Metadata = new MetadataExtractorMetadata(
        MetadataExtractorId,
        "Transcode image metadata extractor",
        MetadataExtractorPriority.Core,
        true,
        MEDIA_CATEGORIES,
        new[]
          {
            MediaAspect.Metadata,
            TranscodeItemImageAspect.Metadata
          });
    }

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata { get; private set; }

    private bool HasImageExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return IMAGE_FILE_EXTENSIONS.Contains(ext);
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (mediaItemAccessor is IFileSystemResourceAccessor)
        {
          using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          {
            if (!rah.LocalFsResourceAccessor.IsFile)
              return false;
            string fileName = rah.LocalFsResourceAccessor.ResourceName;
            if (!HasImageExtension(fileName))
              return false;
            MetadataContainer metadata = MediaAnalyzer.ParseImageFile(rah.LocalFsResourceAccessor);
            if (metadata.IsImage)
            {
              ConvertMetadataToAspectData(metadata, extractedAspectData);
              return true;
            }
          }
        }
        //else if (mediaItemAccessor is INetworkResourceAccessor)
        //{
        //  using (var nra = (INetworkResourceAccessor)mediaItemAccessor.Clone())
        //  {
        //    MetadataContainer metadata = _analyzer.ParseStream(nra);
        //    if (metadata.IsImage)
        //    {
        //      ConvertMetadataToAspectData(metadata, extractedAspectData);
        //      return true;
        //    }
        //  }
        //}
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        Logger.Info("TranscodeMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    private void ConvertMetadataToAspectData(MetadataContainer info, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemImageAspect.ATTR_CONTAINER, info.Metadata.ImageContainerType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemImageAspect.ATTR_PIXEL_FORMAT, info.Image.PixelFormatType.ToString());
    }
  
    #endregion

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
