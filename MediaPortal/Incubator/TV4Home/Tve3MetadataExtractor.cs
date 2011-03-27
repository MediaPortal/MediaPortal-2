#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Media.MetadataExtractors.Tve3MetadataExtractor
{
  /// <summary>
  /// MediaPortal-II metadata extractor implementation for movie files. Supports several formats.
  /// </summary>
  public class Tve3MetadataExtractor : IMetadataExtractor
  {
    #region Public constants

    /// <summary>
    /// GUID string for the tve3 metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "C951B2D4-1C5E-499d-912C-756603EF002C";

    /// <summary>
    /// Tve3 metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Protected fields and classes

    protected static IList<string> SHARE_CATEGORIES = new List<string>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static Tve3MetadataExtractor()
    {
      SHARE_CATEGORIES.Add(DefaultMediaCategory.Video.ToString());
    }

    public Tve3MetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Tve3 metadata extractor", false,
          SHARE_CATEGORIES, new[]
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

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      try
      {
        // All media provided by Tve3ResourceAccessor are valid ones.
        if (mediaItemAccessor != null && mediaItemAccessor.GetType().ToString().EndsWith("Tve3ResourceAccessor"))
        {
          MediaItemAspect mediaAspect;
          if (!extractedAspectData.TryGetValue(MediaAspect.ASPECT_ID, out mediaAspect))
            extractedAspectData[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
          MediaItemAspect movieAspect;
          if (!extractedAspectData.TryGetValue(VideoAspect.ASPECT_ID, out movieAspect))
            extractedAspectData[VideoAspect.ASPECT_ID] = movieAspect = new MediaItemAspect(VideoAspect.Metadata);

          mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, mediaItemAccessor.ResourceName);
          mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, "video/mp2t"); // MPEG 2 Transport Stream
          movieAspect.SetAttribute(VideoAspect.ATTR_ISDVD, false);
          return true;
        }
        return false;
      }
      catch
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here
        ServiceRegistration.Get<ILogger>().Info("Tve3MetadataExtractor: Exception reading source '{0}'", mediaItemAccessor.ResourcePathName);
        return false;
      }
    }

    #endregion
  }
}