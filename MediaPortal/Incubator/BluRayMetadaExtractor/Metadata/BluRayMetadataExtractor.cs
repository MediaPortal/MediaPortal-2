#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
    #region Public constants

    /// <summary>
    /// GUID string for the BluRayMetadataExtractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "E23918BB-297F-463b-8AEB-2BDCE9998028";

    /// <summary>
    /// BluRayMetadataExtractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    public const string MIMETYPE_BLURAY = "video/bluray";
    public const string MIMETYPE_AVCHD = "video/avchd";
    public const string INDEX_BLURAY = "index.bdmv";
    public const string INDEX_AVCHD = "INDEX.BDM";
    public const string BASEFOLDER_BLURAY = "BDMV";
    public const string BASEFOLDER_AVCHD = "PRIVATE\\AVCHD\\BDMV";

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
      return DetectAvchdBluRay(mediaItemAccessor, extractedAspectData, BASEFOLDER_BLURAY, INDEX_BLURAY, MIMETYPE_BLURAY) ||
             DetectAvchdBluRay(mediaItemAccessor, extractedAspectData, BASEFOLDER_AVCHD, INDEX_AVCHD, MIMETYPE_AVCHD);
    }

    private static bool DetectAvchdBluRay(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, string bdmvFolder, string indexName, string mimeType)
    {
      try
      {
        IResourceAccessor ra = mediaItemAccessor.Clone();
        try
        {
          using (ILocalFsResourceAccessor fsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(ra))
            if (fsra != null && fsra.IsDirectory && fsra.ResourceExists(bdmvFolder))
            {
              IFileSystemResourceAccessor fsraBDMV = fsra.GetResource(bdmvFolder);
              if (fsraBDMV != null && fsraBDMV.ResourceExists(indexName))
              {
                // This line is important to keep in, if no VideoAspect is created here, the MediaItems is not detected as Video! 
                MediaItemAspect.GetOrCreateAspect(extractedAspectData, VideoAspect.Metadata);
                MediaItemAspect mediaAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, MediaAspect.Metadata);

                mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, mimeType); // BluRay disc

                string bdmvDirectory = Path.Combine(fsra.LocalFileSystemPath, bdmvFolder);
                BDInfoExt bdinfo = new BDInfoExt(bdmvDirectory, false);
                mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, bdinfo.GetTitle() ?? mediaItemAccessor.ResourceName);
                return true;
              }
            }
        }
        catch
        {
          ra.Dispose();
          throw;
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