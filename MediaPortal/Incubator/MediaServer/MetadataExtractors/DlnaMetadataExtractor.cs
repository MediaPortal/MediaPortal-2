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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MediaServer.Aspects;

namespace MediaPortal.Extensions.MediaServer.MetadataExtractors
{
  public class DlnaMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid(MediaServerPlugin.DEVICE_UUID);

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
        var fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra == null || !fsra.IsFile)
        {
          return false;
        }

        MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAspect.ATTR_MIME_TYPE, "audio/mpeg");

        MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAspect.ATTR_PROFILE, "MP3");
        
        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("DlnaMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }

      return false;
    }

    #endregion
  }
}