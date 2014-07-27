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
using System.Globalization;
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
    public bool Matched { get; set; }

    public int MovieDbId { get; set; }
    public string ImdbId { get; set; }

    public string MovieName { get; set; }
    public string OriginalName { get; set; }
    public int Year { get; set; }
    public int Runtime { get; set; }
    public string Certification { get; set; }
    public string Tagline { get; set; }
    public string Summary { get; set; }

    public string CollectionName { get; set; }
    public int CollectionMovieDbId { get; set; }

    public float Popularity { get; set; }
    public long Budget { get; set; }
    public long Revenue { get; set; }
    public double Score { get; set; }
    public double TotalRating { get; set; }
    public int RatingCount { get; set; }

    /// <summary>
    /// Contains a list of <see cref="CultureInfo.TwoLetterISOLanguageName"/> of the medium. This can be used
    /// to do an online lookup in the best matching language.
    /// </summary>
    public List<string> Languages { get; internal set; }
    public List<string> Actors { get; internal set; }
    public List<string> Directors { get; internal set; }
    public List<string> Writers { get; internal set; }
    public List<string> Genres { get; internal set; }

    public MovieInfo ()
    {
      Languages = new List<string>();
      Actors = new List<string>();
      Directors = new List<string>();
      Writers = new List<string>();
      Genres = new List<string>();
    }

    /// <summary>
    /// Copies the contained movie information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, MediaItemAspect> aspectData)
    {
      if (!string.IsNullOrEmpty(MovieName))
      {
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, MovieName);
        MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_MOVIE_NAME, MovieName);
      }
      if (!string.IsNullOrEmpty(Summary)) MediaItemAspect.SetAttribute(aspectData, VideoAspect.ATTR_STORYPLOT, Summary);
      if (!string.IsNullOrEmpty(Tagline)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_TAGLINE, Tagline);
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_CERTIFICATION, Certification);
      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_IMDB_ID, ImdbId);
      if (MovieDbId > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_TMDB_ID, MovieDbId);
      if (Runtime > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_RUNTIME_M, Runtime);
      if (Popularity > 0f) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_POPULARITY, Popularity);
      if (Budget > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_BUDGET, Budget);
      if (Revenue > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_REVENUE, Revenue);
      if (Score > 0d) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_SCORE, Score);
      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_RATING_COUNT, RatingCount);

      if (!string.IsNullOrEmpty(CollectionName)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_COLLECTION_NAME, CollectionName);
      if (CollectionMovieDbId > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_COLLECTION_ID, CollectionMovieDbId);

      if (Year > 0)
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(Year, 1, 1));

      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_ACTORS, Actors);
      if (Directors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_DIRECTORS, Directors);
      if (Writers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_WRITERS, Writers);
      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_GENRES, Genres);
      return true;
    }
  }
}
