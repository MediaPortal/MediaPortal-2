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
using System.Linq;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Matchers;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using MediaPortal.Extensions.OnlineLibraries;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for Movies.
  /// </summary>
  public class MovieMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the video metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "C2800928-8A57-4979-A95F-3CE6F3EBAB92";

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    protected const string MEDIA_CATEGORY_NAME_MOVIE = "Movie";
    public const  double MINIMUM_HOUR_AGE_BEFORE_UPDATE = 1;

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;
    protected bool _onlyFanArt;

    #endregion

    #region Ctor

    static MovieMetadataExtractor()
    {
      MediaCategory movieCategory;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_MOVIE, out movieCategory))
        movieCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_MOVIE, new List<MediaCategory> { DefaultMediaCategories.Video });
      MEDIA_CATEGORIES.Add(movieCategory);
    }

    public MovieMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Movies metadata extractor", MetadataExtractorPriority.External, true,
          MEDIA_CATEGORIES, new MediaItemAspectMetadata[]
              {
                MediaAspect.Metadata,
                MovieAspect.Metadata
              });
      _onlyFanArt = ServiceRegistration.Get<ISettingsManager>().Load<MovieMetadataExtractorSettings>().OnlyFanArt;
    }

    #endregion

    #region Private methods

    private bool ExtractMovieData(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string[] pathsToTest = new[] { lfsra.LocalFileSystemPath, lfsra.CanonicalLocalResourcePath.ToString() };
      string title;
      // VideoAspect must be present to be sure it is actually a video resource.
      if (!extractedAspectData.ContainsKey(VideoStreamAspect.ASPECT_ID) && !extractedAspectData.ContainsKey(SubtitleAspect.ASPECT_ID))
        return false;

      MovieInfo movieInfo = new MovieInfo();
      if (extractedAspectData.ContainsKey(MovieAspect.ASPECT_ID))
      {
        movieInfo.FromMetadata(extractedAspectData);
      }
      if(!movieInfo.IsBaseInfoPresent)
      {
        //Try to get title
        if (MediaItemAspect.TryGetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, out title) && 
          !string.IsNullOrEmpty(title) && !lfsra.ResourceName.StartsWith(title, StringComparison.InvariantCultureIgnoreCase))
          movieInfo.MovieName = title;

        // Try to use an existing TMDB id for exact mapping
        string tmdbId;
        if (MediaItemAspect.TryGetExternalAttribute(extractedAspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out tmdbId) ||
            MatroskaMatcher.TryMatchTmdbId(lfsra, out tmdbId))
          movieInfo.MovieDbId = Convert.ToInt32(tmdbId);

        // Try to use an existing IMDB id for exact mapping
        string imdbId;
        if (MediaItemAspect.TryGetExternalAttribute(extractedAspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out imdbId) ||
            pathsToTest.Any(path => ImdbIdMatcher.TryMatchImdbId(path, out imdbId)) ||
            MatroskaMatcher.TryMatchImdbId(lfsra, out imdbId))
          movieInfo.ImdbId = imdbId;

        if (!movieInfo.IsBaseInfoPresent)
        {
          // Also test the full path year. This is useful if the path contains the real name and year.
          foreach (string path in pathsToTest)
          {
            if (MovieNameMatcher.MatchTitleYear(path, movieInfo))
              break;
          }
          //Fall back to MediaAspect.ATTR_TITLE
          if (movieInfo.MovieName.IsEmpty && !string.IsNullOrEmpty(title))
            movieInfo.MovieName = title;
        }

        if (!movieInfo.ReleaseDate.HasValue)
        {
          // When searching movie title, the year can be relevant for multiple titles with same name but different years
          DateTime recordingDate;
          if (MediaItemAspect.TryGetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, out recordingDate))
            movieInfo.ReleaseDate = recordingDate;
        }

        /* Clear the names from unwanted strings */
        MovieNameMatcher.CleanupTitle(movieInfo);
      }

      // Allow the online lookup to choose best matching language for metadata
      IList<MultipleMediaItemAspect> audioAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspectData, VideoAudioStreamAspect.Metadata, out audioAspects))
      {
        foreach (MultipleMediaItemAspect aspect in audioAspects)
        {
          string language = (string)aspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE);
          if (!string.IsNullOrEmpty(language))
            movieInfo.Languages.Add(language);
        }
      }

      if (!movieInfo.IsBaseInfoPresent || !movieInfo.HasExternalId)
      {
        //Reset string to prefer online texts
        movieInfo.CollectionName.DefaultLanguage = true;
        movieInfo.MovieName.DefaultLanguage = true;
        movieInfo.Summary.DefaultLanguage = true;
      }

      MatroskaMatcher.ExtractFromTags(lfsra, movieInfo);
      MP4Matcher.ExtractFromTags(lfsra, movieInfo);

      OnlineMatcherService.FindAndUpdateMovie(movieInfo, forceQuickMode);

      if (!_onlyFanArt)
        movieInfo.SetMetadata(extractedAspectData);

      return movieInfo.IsBaseInfoPresent;
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
          return ExtractMovieData(rah.LocalFsResourceAccessor, extractedAspectData, forceQuickMode);
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("MoviesMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    #endregion
  }
}
