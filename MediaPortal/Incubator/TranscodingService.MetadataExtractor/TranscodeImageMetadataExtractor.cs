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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor
{
  public class TranscodeImageMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid("DFC8E367-C255-4B54-8FC9-236D4C6EBA55");

    protected static List<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory> { DefaultMediaCategories.Image };

    public TranscodeImageMetadataExtractor()
    {
      Metadata = new MetadataExtractorMetadata(
        MetadataExtractorId,
        "Transcode image metadata extractor",
        MetadataExtractorPriority.Extended,
        true,
        MEDIA_CATEGORIES,
        new[]
          {
            MediaAspect.Metadata,
          });
    }

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata { get; private set; }

    public async Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (forceQuickMode)
          return false;

        if (!extractedAspectData.ContainsKey(ImageAspect.ASPECT_ID))
          return false;

        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;

        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
        {
          if (!rah.LocalFsResourceAccessor.IsFile)
            return false;
        }

        MetadataContainer metadata = await MediaAnalyzer.ParseMediaStreamAsync(mediaItemAccessor);
        if (metadata == null)
        {
          Logger.Info("TranscodeImageMetadataExtractor: Error analyzing stream '{0}'", mediaItemAccessor.CanonicalLocalResourcePath);
        }
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        Logger.Error("TranscodeImageMetadataExtractor: Exception reading resource '{0}'", e, mediaItemAccessor.CanonicalLocalResourcePath);
      }
      return false;
    }

    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
    }

    #endregion

    private static IMediaAnalyzer MediaAnalyzer
    {
      get { return ServiceRegistration.Get<IMediaAnalyzer>(); }
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
