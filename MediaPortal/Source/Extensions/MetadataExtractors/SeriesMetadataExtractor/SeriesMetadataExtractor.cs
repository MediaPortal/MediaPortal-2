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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Utilities;

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
                SeriesAspect.Metadata
              });
    }

    #endregion

    #region Protected methods

    protected SeriesInfo GetSeriesFromTags(IDictionary<string, IList<string>> extractedTags)
    {
      SeriesInfo seriesInfo = new SeriesInfo();
      if (extractedTags[MatroskaConsts.TAG_EPISODE_TITLE] != null)
        seriesInfo.Episode = extractedTags[MatroskaConsts.TAG_EPISODE_TITLE].FirstOrDefault();

      if (extractedTags[MatroskaConsts.TAG_SERIES_TITLE] != null)
        seriesInfo.Series = extractedTags[MatroskaConsts.TAG_SERIES_TITLE].FirstOrDefault();

      if (extractedTags[MatroskaConsts.TAG_SEASON_NUMBER] != null)
        int.TryParse(extractedTags[MatroskaConsts.TAG_SEASON_NUMBER].FirstOrDefault(), out seriesInfo.SeasonNumber);

      if (extractedTags[MatroskaConsts.TAG_EPISODE_NUMBER] != null)
      {
        int episodeNum;
        if (int.TryParse(extractedTags[MatroskaConsts.TAG_EPISODE_NUMBER].FirstOrDefault(), out episodeNum))
          seriesInfo.EpisodeNumbers.Add(episodeNum);
      }
      return seriesInfo;
    }

    protected bool ExtractSeriesData(string localFsResourcePath, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      SeriesInfo seriesInfo = null;

      string extensionUpper = StringUtils.TrimToEmpty(Path.GetExtension(localFsResourcePath)).ToUpper();

      // Try to get extended information out of matroska files)
      if (extensionUpper == ".MKV" || extensionUpper == ".MK3D")
      {
        MatroskaInfoReader mkvReader = new MatroskaInfoReader(localFsResourcePath);
        // Add keys to be extracted to tags dictionary, matching results will returned as value
        Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultTags;
        mkvReader.ReadTags(tagsToExtract);

        string title = string.Empty;
        IList<string> tags = tagsToExtract[MatroskaConsts.TAG_SIMPLE_TITLE];
        if (tags != null)
          title = tags.FirstOrDefault();

        if (!string.IsNullOrEmpty(title))
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);

        string yearCandidate = null;
        tags = tagsToExtract[MatroskaConsts.TAG_EPISODE_YEAR] ?? tagsToExtract[MatroskaConsts.TAG_SEASON_YEAR];
        if (tags != null)
          yearCandidate = (tags.FirstOrDefault() ?? string.Empty).Substring(0, 4);

        int year;
        if (int.TryParse(yearCandidate, out year))
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 1, 1));

        tags = tagsToExtract[MatroskaConsts.TAG_EPISODE_SUMMARY];
        string plot = tags != null ? tags.FirstOrDefault() : string.Empty;
        if (!string.IsNullOrEmpty(plot))
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, plot);

        // Series and episode handling. Prefer information from tags.
        seriesInfo = GetSeriesFromTags(tagsToExtract);
      }

      // If now information from mkv were found, try name matching
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
        using (IFileSystemResourceAccessor fsra = (IFileSystemResourceAccessor) mediaItemAccessor.Clone())
        using (ILocalFsResourceAccessor lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
        {
          string localFsPath = lfsra.LocalFileSystemPath;
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