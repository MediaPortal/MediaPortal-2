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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Players.Video;

namespace MediaPortal.Media.MetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for BluRay discs.
  /// </summary>
  public class BluRayMetadataExtractor : IMetadataExtractor
  {
    #region Public constants

    /// <summary>
    /// GUID string for the BluRayMetadataExtractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "E23918BB-297F-463b-8AEB-2BDCE9998028";

    /// <summary>
    /// BluRayMetadataExtractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Protected fields and classes

    protected static IList<string> SHARE_CATEGORIES = new List<string>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static BluRayMetadataExtractor()
    {
      SHARE_CATEGORIES.Add(DefaultMediaCategory.Video.ToString());
    }

    public BluRayMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "BluRay metadata extractor", true, 
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

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        using (ILocalFsResourceAccessor fsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(mediaItemAccessor.Clone()))
          if (fsra != null && fsra.IsDirectory && fsra.ResourceExists("BDMV"))
          {
            IFileSystemResourceAccessor fsraBDMV = fsra.GetResource("BDMV");
            if (fsraBDMV != null && fsraBDMV.ResourceExists("index.bdmv"))
            {
              // BluRay
              MediaItemAspect mediaAspect;
              if (!extractedAspectData.TryGetValue(MediaAspect.ASPECT_ID, out mediaAspect))
                extractedAspectData[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
              MediaItemAspect videoAspect;
              if (!extractedAspectData.TryGetValue(VideoAspect.ASPECT_ID, out videoAspect))
                extractedAspectData[VideoAspect.ASPECT_ID] = new MediaItemAspect(VideoAspect.Metadata);

              mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, "video/bluray"); // BluRay disc

              string bdmvDirectory = fsra.LocalFileSystemPath;
              BDInfoExt bdinfo = new BDInfoExt(bdmvDirectory);
              mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, bdinfo.GetTitle() ?? mediaItemAccessor.ResourceName);
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