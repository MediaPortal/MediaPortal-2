#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.Common.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Movie" media item aspect which is assigned to movie media items. It is a specialized type of the general
  /// <see cref="VideoStreamAspect"/> for movies, that can be looked up by online libraries like TMDB, IMDB, OFDB...
  /// </summary>
  /// TODO: Improve documentation for attributes. Is it related to some internet service? What is the meaning of the value? Where is the source of the values?
  public static class MovieAspect
  {
    /// <summary>
    /// Media item aspect id of the series aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("C5C21762-FA8A-4C51-8C5B-6B0BF7FB162A");

    /// <summary>
    /// Contains the localized name of the movie.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_MOVIE_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("MovieName", 100, Cardinality.Inline, true);

    /// <summary>
    /// Contains the original name of the movie.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ORIG_MOVIE_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("OrigName", 100, Cardinality.Inline, false);

    /// <summary>
    /// Contains the name of the movie collection.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COLLECTION_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("CollectionName", 100, Cardinality.Inline, true);

    /// <summary>
    /// Contains the official runtime in minutes. This value must not necessary match the exact video runtime (i.e. tv recordings will be longer because
    /// of advertisements).
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RUNTIME_M =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Runtime", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the certification.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CERTIFICATION =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Certification", 20, Cardinality.Inline, true);

    /// <summary>
    /// Contains list of awards.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AWARDS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Awards", 20, Cardinality.ManyToMany, true);

    /// <summary>
    /// Contains a short comment for the movie.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TAGLINE =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Tagline", 255, Cardinality.Inline, false);

    /// <summary>
    /// List of production company's involved in making the movie.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COMPANIES =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Companies", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Contains a popularity of movies, based on user votings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_POPULARITY =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Popularity", typeof(float), Cardinality.Inline, false);

    /// <summary>
    /// Contains the budget for producing the movie.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_BUDGET =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Budget", typeof(long), Cardinality.Inline, false);
    
    /// <summary>
    /// Contains the revenue of the movie.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_REVENUE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Revenue", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Contains the score of the movie.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SCORE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Score", typeof(float), Cardinality.Inline, false);

    /// <summary>
    /// Contains the overall rating of the movie. Value ranges from 0 (very bad) to 10 (very good).
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TOTAL_RATING =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("TotalRating", typeof(double), Cardinality.Inline, true);

    /// <summary>
    /// Contains the overall number ratings of the movie.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RATING_COUNT =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("RatingCount", typeof(int), Cardinality.Inline, true);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "MovieItem", new[] {
            ATTR_MOVIE_NAME,
            ATTR_ORIG_MOVIE_NAME,
            ATTR_COLLECTION_NAME,
            ATTR_RUNTIME_M,
            ATTR_CERTIFICATION,
            ATTR_TAGLINE,
            ATTR_AWARDS,
            ATTR_COMPANIES,
            ATTR_POPULARITY,
            ATTR_BUDGET,
            ATTR_REVENUE,
            ATTR_SCORE,
            ATTR_TOTAL_RATING,
            ATTR_RATING_COUNT,
        });

    public static readonly Guid ROLE_MOVIE = new Guid("B8B58098-6577-442E-BCB9-DEE3D114EAEF");
  }
}
