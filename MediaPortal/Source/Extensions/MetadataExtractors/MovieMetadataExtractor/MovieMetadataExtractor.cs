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
using System.Linq;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Matchers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Extensions.OnlineLibraries.TheMovieDB;

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

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    protected MetadataExtractorMetadata _metadata;

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
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata,
                MovieAspect.Metadata
              });
    }

    #endregion

    #region Private methods

    private static bool ExtractMovieData(ILocalFsResourceAccessor lfsra, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      string[] pathsToTest = new[] { lfsra.LocalFileSystemPath, lfsra.CanonicalLocalResourcePath.ToString() };
      string title;
      // VideoAspect must be present to be sure it is actually a video resource.
      if (!extractedAspectData.ContainsKey(VideoAspect.ASPECT_ID))
        return false;

      if (!MediaItemAspect.TryGetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, out title) || string.IsNullOrEmpty(title))
        return false;

      MovieInfo movieInfo = new MovieInfo
        {
          MovieName = title,
        };

      // Allow the online lookup to choose best matching language for metadata
      ICollection<string> movieLanguages;
      if (MediaItemAspect.TryGetAttribute(extractedAspectData, VideoAspect.ATTR_AUDIOLANGUAGES, out movieLanguages) && movieLanguages.Count > 0)
        movieInfo.Languages.AddRange(movieLanguages);

      // Try to use an existing IMDB id for exact mapping
      string imdbId;
      if (MediaItemAspect.TryGetAttribute(extractedAspectData, MovieAspect.ATTR_IMDB_ID, out imdbId) ||
          pathsToTest.Any(path => ImdbIdMatcher.TryMatchImdbId(path, out imdbId)) ||
          NfoReader.TryMatchImdbId(lfsra, out imdbId) ||
          MatroskaMatcher.TryMatchImdbId(lfsra.LocalFileSystemPath, out imdbId))
        movieInfo.ImdbId = imdbId;

      // Also test the full path year, using a dummy. This is useful if the path contains the real name and year.
      foreach (string path in pathsToTest)
      {
        MovieInfo dummy = new MovieInfo { MovieName = path };
        if (NamePreprocessor.MatchTitleYear(dummy))
        {
          movieInfo.MovieName = dummy.MovieName;
          movieInfo.Year = dummy.Year;
          break;
        }
      }

      // When searching movie title, the year can be relevant for multiple titles with same name but different years
      DateTime recordingDate;
      if (MediaItemAspect.TryGetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, out recordingDate))
        movieInfo.Year = recordingDate.Year;

      if (MovieTheMovieDbMatcher.Instance.FindAndUpdateMovie(movieInfo))
      {
        movieInfo.SetMetadata(extractedAspectData);
        return true;
      }
      return false;
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
          return ExtractMovieData(rah.LocalFsResourceAccessor, extractedAspectData);
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