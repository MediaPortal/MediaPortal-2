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

namespace MediaPortal.Common.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "ExternalIdentifier" media item aspect which
  /// associates media items with external sources. The source / type / id is intended to
  /// be a unique external identifier which MediaPortal can use to merge aspect data from
  /// multiple items.
  /// </summary>
  public static class ExternalIdentifierAspect
  {
    // TODO: Put this somewhere else?
    public static readonly string SOURCE_IMDB = "imdb";
    public static readonly string SOURCE_MUSICBRAINZ = "musicbrainz";
    public static readonly string SOURCE_TMDB = "tmdb";
    public static readonly string SOURCE_TVDB = "tvdb";
    public static readonly string SOURCE_CDDB = "cddb";
    public static readonly string SOURCE_AUDIODB = "audiodb";
    public static readonly string SOURCE_TVMAZE = "tvmaze";
    public static readonly string SOURCE_TVRAGE = "tvrage";
    public static readonly string SOURCE_YEAR = "year";
    public static readonly string SOURCE_DATE = "date";
    public static readonly string SOURCE_NAME = "name";

    public static readonly string TYPE_CHARACTER = "character"; // Someone in a movie / series
    public static readonly string TYPE_COLLECTION = "collection";
    public static readonly string TYPE_EPISODE = "episode";
    public static readonly string TYPE_SEASON = "season";
    public static readonly string TYPE_MOVIE = "movie";
    public static readonly string TYPE_PERSON = "person"; // Someone in real life
    public static readonly string TYPE_SERIES = "series";
    public static readonly string TYPE_TRACK = "track";
    public static readonly string TYPE_ALBUM = "album";
    public static readonly string TYPE_COMAPANY = "company";
    public static readonly string TYPE_NETWORK = "network";

    /// <summary>
    /// Media item aspect id of the relationship aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("4C43FFDC-8A43-42F0-A6EF-3A0ECA46F9AA");

    /// <summary>
    /// Source of the identifier
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_SOURCE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Source", 100, Cardinality.Inline, false);

    /// <summary>
    /// The type of identifier
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_TYPE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Type", 100, Cardinality.Inline, false);

    /// <summary>
    /// Source type's unique id
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_ID =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Id", 100, Cardinality.Inline, false);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
      // TODO: Localize name
      ASPECT_ID, "ExternalIdentifier",
        new[] {
          ATTR_SOURCE,
          ATTR_TYPE,
          ATTR_ID,
        },
        new[] {
          ATTR_SOURCE,
          ATTR_TYPE,
        }
      );
  }
}
