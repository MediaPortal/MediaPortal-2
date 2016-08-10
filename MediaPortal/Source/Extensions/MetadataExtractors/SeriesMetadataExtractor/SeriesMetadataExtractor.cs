#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
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
    public const double MINIMUM_HOUR_AGE_BEFORE_UPDATE = 1;

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static ICollection<string> VIDEO_FILE_EXTENSIONS = new List<string>();
    protected MetadataExtractorMetadata _metadata;
    protected bool _onlyFanArt;

    #endregion

    #region Ctor

    static SeriesMetadataExtractor()
    {
      MediaCategory seriesCategory;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_SERIES, out seriesCategory))
        seriesCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_SERIES, new List<MediaCategory> { DefaultMediaCategories.Video });
      MEDIA_CATEGORIES.Add(seriesCategory);
    }

    public SeriesMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Series metadata extractor", MetadataExtractorPriority.External, true,
          MEDIA_CATEGORIES, new MediaItemAspectMetadata[]
              {
                MediaAspect.Metadata,
                EpisodeAspect.Metadata
              });
      _onlyFanArt = ServiceRegistration.Get<ISettingsManager>().Load<SeriesMetadataExtractorSettings>().OnlyFanArt;
    }

    #endregion

    #region Protected methods

    protected bool ExtractSeriesData(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      // VideoAspect must be present to be sure it is actually a video resource.
      if (!extractedAspectData.ContainsKey(VideoStreamAspect.ASPECT_ID) && !extractedAspectData.ContainsKey(SubtitleAspect.ASPECT_ID))
        return false;

      EpisodeInfo episodeInfo = new EpisodeInfo();
      if (extractedAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        episodeInfo.FromMetadata(extractedAspectData);
      }
      if(!episodeInfo.IsBaseInfoPresent)
      {
        string title = null;
        int seasonNumber;
        SingleMediaItemAspect episodeAspect;
        MediaItemAspect.TryGetAspect(extractedAspectData, EpisodeAspect.Metadata, out episodeAspect);
        IEnumerable<int> episodeNumbers;
        if (MediaItemAspect.TryGetAttribute(extractedAspectData, EpisodeAspect.ATTR_SERIES_NAME, out title) &&
            MediaItemAspect.TryGetAttribute(extractedAspectData, EpisodeAspect.ATTR_SEASON, out seasonNumber) &&
            (episodeNumbers = episodeAspect.GetCollectionAttribute<object>(EpisodeAspect.ATTR_EPISODE).Cast<int>()) != null)
        {
          episodeInfo.SeriesName = title;
          episodeInfo.SeasonNumber = seasonNumber;
          episodeInfo.EpisodeNumbers.Clear();
          episodeNumbers.ToList().ForEach(n => episodeInfo.EpisodeNumbers.Add(n));
        }

        // If there was no complete match, yet, try to get extended information out of matroska files)
        if (!episodeInfo.IsBaseInfoPresent)
        {
          MatroskaMatcher matroskaMatcher = new MatroskaMatcher();
          if (matroskaMatcher.MatchSeries(lfsra, episodeInfo))
          {
            ServiceRegistration.Get<ILogger>().Debug("ExtractSeriesData: Found EpisodeInfo by MatroskaMatcher for {0}, IMDB {1}, TVDB {2}, TMDB {3}, AreReqiredFieldsFilled {4}",
              episodeInfo.SeriesName, episodeInfo.SeriesImdbId, episodeInfo.SeriesTvdbId, episodeInfo.SeriesMovieDbId, episodeInfo.IsBaseInfoPresent);
          }
        }

        // If no information was found before, try name matching
        if (!episodeInfo.IsBaseInfoPresent)
        {
          // Try to match series from folder and file naming
          SeriesMatcher seriesMatcher = new SeriesMatcher();
          seriesMatcher.MatchSeries(lfsra, episodeInfo);
        }
        else if (episodeInfo.SeriesFirstAired == null)
        {
          EpisodeInfo tempEpisodeInfo = new EpisodeInfo();
          SeriesMatcher seriesMatcher = new SeriesMatcher();
          seriesMatcher.MatchSeries(lfsra, tempEpisodeInfo);
          if (tempEpisodeInfo.SeriesFirstAired.HasValue)
            episodeInfo.SeriesFirstAired = tempEpisodeInfo.SeriesFirstAired;
        }
      }

      // Lookup online information (incl. fanart)
      IList<MultipleMediaItemAspect> audioAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspectData, VideoAudioStreamAspect.Metadata, out audioAspects))
      {
        foreach (MultipleMediaItemAspect aspect in audioAspects)
        {
          string language = (string)aspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE);
          if (!string.IsNullOrEmpty(language))
            episodeInfo.Languages.Add(language);
        }
      }

      if (!episodeInfo.IsBaseInfoPresent || !episodeInfo.HasExternalId)
      {
        //Reset string to prefer online texts
        episodeInfo.EpisodeName.DefaultLanguage = true;
        episodeInfo.SeriesName.DefaultLanguage = true;
        episodeInfo.Summary.DefaultLanguage = true;
      }

      OnlineMatcherService.FindAndUpdateEpisode(episodeInfo, forceQuickMode);

      if (!_onlyFanArt)
        episodeInfo.SetMetadata(extractedAspectData);

      return episodeInfo.IsBaseInfoPresent;
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;

        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          return ExtractSeriesData(rah.LocalFsResourceAccessor, extractedAspectData, forceQuickMode);
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
