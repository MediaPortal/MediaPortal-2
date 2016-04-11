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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="MovieInfo"/> contains information about a movie. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class MovieInfo
  {
    public int MovieDbId = 0;
    public string ImDbId = null;

    public string MovieName = null;
    public string OriginalName = null;
    public DateTime? ReleaseDate = null;
    public int Runtime = 0;
    public string Certification = null;
    public string Tagline = null;
    public string Summary = null;

    public string CollectionName = null;
    public int CollectionMovieDbId = 0;

    public float Popularity = 0;
    public long Budget = 0;
    public long Revenue = 0;
    public double Score = 0;
    public double TotalRating = 0;
    public int RatingCount = 0;

    /// <summary>
    /// Contains a list of <see cref="CultureInfo.TwoLetterISOLanguageName"/> of the medium. This can be used
    /// to do an online lookup in the best matching language.
    /// </summary>
    public List<string> Languages = new List<string>();
    public List<PersonInfo> Actors = new List<PersonInfo>();
    public List<PersonInfo> Directors = new List<PersonInfo>();
    public List<PersonInfo> Writers = new List<PersonInfo>();
    public List<CharacterInfo> Characters = new List<CharacterInfo>();
    public List<CompanyInfo> ProductionCompanys = new List<CompanyInfo>();
    public List<string> Genres = new List<string>();
    public List<string> Awards = new List<string>();

    #region Members

    /// <summary>
    /// Copies the contained movie information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(MovieName)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, MovieName);
      MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_MOVIE_NAME, MovieName);
      if (ReleaseDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate.Value);
      if (!string.IsNullOrEmpty(Summary)) MediaItemAspect.SetAttribute(aspectData, VideoAspect.ATTR_STORYPLOT, Summary);
      if (!string.IsNullOrEmpty(Tagline)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_TAGLINE, Tagline);
      if (!string.IsNullOrEmpty(CollectionName)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_COLLECTION_NAME, CollectionName);
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_CERTIFICATION, Certification);

      if (!string.IsNullOrEmpty(ImDbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, ImDbId);
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, MovieDbId.ToString());
      if (CollectionMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, CollectionMovieDbId.ToString());

      if (Runtime > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_RUNTIME_M, Runtime);
      if (Budget > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_BUDGET, Budget);
      if (Revenue > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_REVENUE, Revenue);

      if (Popularity > 0f) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_POPULARITY, Popularity);
      if (Score > 0d) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_SCORE, Score);
      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_RATING_COUNT, RatingCount);
      
      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_ACTORS, Actors);
      if (Directors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_DIRECTORS, Directors);
      if (Writers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_WRITERS, Writers);

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_GENRES, Genres);

      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_ACTORS, Actors.Select(p => p.Name).ToList());
      if (Directors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_DIRECTORS, Directors.Select(p => p.Name).ToList());
      if (Writers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_WRITERS, Writers.Select(p => p.Name).ToList());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_CHARACTERS, Characters.Select(p => p.Name).ToList());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_GENRES, Genres);
      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_AWARDS, Awards);

      if (ProductionCompanys.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_COMPANYS, ProductionCompanys.Select(c => c.Name).ToList());
      return true;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return MovieName;
    }

    #endregion
  }
}
