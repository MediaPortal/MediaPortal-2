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
  /// Contains the metadata specification of the "ExternalIdentifier" media item aspect which
  /// associates media items with external sources. The source / type / id is intended to
  /// be a unique external identifier which MediaPortal can use to merge aspect data from
  /// multiple items.
  /// </summary>
  public static class ExternalIdentifierAspect
  {
    // TODO: Put this somewhere else?
    public static readonly string SOURCE_IMDB = "IMDB";
    public static readonly string SOURCE_ISRC = "ISRC";
    public static readonly string SOURCE_UPCEAN = "UPCEAN";
    public static readonly string SOURCE_MUSICBRAINZ = "MUSICBRAINZ";
    public static readonly string SOURCE_MUSICBRAINZ_GROUP = "MUSICBRAINZ_GROUP";
    public static readonly string SOURCE_TMDB = "TMDB";
    public static readonly string SOURCE_TVDB = "TVDB";
    public static readonly string SOURCE_CDDB = "CDDB";
    public static readonly string SOURCE_AUDIODB = "AUDIODB";
    public static readonly string SOURCE_TVMAZE = "TVMAZE";
    public static readonly string SOURCE_TVRAGE = "TVRAGE";
    public static readonly string SOURCE_ALLOCINE = "ALLOCINE";
    public static readonly string SOURCE_CINEPASSION = "CINEPASSION";
    public static readonly string SOURCE_AMAZON = "AMAZON";
    public static readonly string SOURCE_MUSIC_IP = "MUSIC_IP";
    public static readonly string SOURCE_MVDB = "MVDB";
    public static readonly string SOURCE_LYRIC = "LYRIC";
    public static readonly string SOURCE_ITUNES = "ITUNES";
    public static readonly string SOURCE_NAME = "NAME";

    public static readonly string TYPE_CHARACTER = "CHARACTER"; // Someone in a movie / series
    public static readonly string TYPE_COLLECTION = "COLLECTION";
    public static readonly string TYPE_EPISODE = "EPISODE";
    public static readonly string TYPE_SEASON = "SEASON";
    public static readonly string TYPE_MOVIE = "MOVIE";
    public static readonly string TYPE_PERSON = "PERSON"; // Someone in real life
    public static readonly string TYPE_SERIES = "SERIES";
    public static readonly string TYPE_TRACK = "TRACK";
    public static readonly string TYPE_ALBUM = "ALBUM";
    public static readonly string TYPE_COMPANY = "COMPANY";
    public static readonly string TYPE_NETWORK = "NETWORK";

    /// <summary>
    /// Media item aspect id of the relationship aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("242A6B8D-D75B-4C28-BDF8-4ED1BAD08038");

    /// <summary>
    /// Source of the identifier
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_SOURCE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Source", 100, Cardinality.Inline, true);

    /// <summary>
    /// The type of identifier
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_TYPE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Type", 100, Cardinality.Inline, true);

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
