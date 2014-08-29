#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers;
using MediaPortal.Extensions.OnlineLibraries;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for Series.
  /// </summary>
  public class SeriesMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the video metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "A2D018D4-97E9-4B37-A7C3-31FD270277D0";

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    public const string MEDIA_CATEGORY_NAME_SERIES = "Series";

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static ICollection<string> VIDEO_FILE_EXTENSIONS = new List<string>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static SeriesMetadataExtractor()
    {
      MediaCategory seriesCategory;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_SERIES, out seriesCategory))
        seriesCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_SERIES, new List<MediaCategory> {DefaultMediaCategories.Video});
      MEDIA_CATEGORIES.Add(seriesCategory);
    }

    public SeriesMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Series metadata extractor", MetadataExtractorPriority.External, true,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata,
                SeriesAspect.Metadata
              });
    }

    #endregion

    #region Protected methods

    protected bool ExtractSeriesData(string localFsResourcePath, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      // VideoAspect must be present to be sure it is actually a video resource.
      if (!extractedAspectData.ContainsKey(VideoAspect.ASPECT_ID))
        return false;

      SeriesInfo seriesInfo;

      // Try to get extended information out of matroska files)
      MatroskaMatcher matroskaMatcher = new MatroskaMatcher();
      matroskaMatcher.MatchSeries(localFsResourcePath, out seriesInfo, ref extractedAspectData);

      // If no information from mkv were found, try name matching
      if (seriesInfo == null || !seriesInfo.IsCompleteMatch)
      {
        // Try to match series from folder and file namings
        SeriesMatcher seriesMatcher = new SeriesMatcher();
        seriesMatcher.MatchSeries(localFsResourcePath, out seriesInfo);
      }

      // Lookup online information (incl. fanart)
      if (seriesInfo != null && seriesInfo.IsCompleteMatch)
      {
        SeriesTvDbMatcher.Instance.FindAndUpdateSeries(seriesInfo);
        seriesInfo.SetMetadata(extractedAspectData);
      }
      return (seriesInfo != null && seriesInfo.IsCompleteMatch);
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
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
        {
          string localFsPath = rah.LocalFsResourceAccessor.LocalFileSystemPath;
          return ExtractSeriesData(localFsPath, extractedAspectData);
        }
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("SeriesMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    #endregion
  }
}