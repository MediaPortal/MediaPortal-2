#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Core.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Music" media item aspect which is assigned to all song items.
  /// </summary>
  public static class MusicAspect
  {
    /// <summary>
    /// Media item aspect id of the media aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("9BA3C559-41F7-4a5f-917C-E3EF65516D14");

    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ARTISTS =
        MediaItemAspectMetadata.CreateAttributeSpecification("Artists", typeof(string), Cardinality.ManyToMany);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ALBUM =
        MediaItemAspectMetadata.CreateAttributeSpecification("Album", typeof(string), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_GENRES =
        MediaItemAspectMetadata.CreateAttributeSpecification("Genres", typeof(string), Cardinality.ManyToMany);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DURATION =
        MediaItemAspectMetadata.CreateAttributeSpecification("Duration", typeof(long), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_TRACK =
        MediaItemAspectMetadata.CreateAttributeSpecification("Track", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_NUMTRACKS =
        MediaItemAspectMetadata.CreateAttributeSpecification("NumTracks", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_PLAYCOUNT =
        MediaItemAspectMetadata.CreateAttributeSpecification("PlayCount", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ALBUMARTISTS =
        MediaItemAspectMetadata.CreateAttributeSpecification("AlbumArtists", typeof(string), Cardinality.ManyToMany);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_COMPOSERS =
        MediaItemAspectMetadata.CreateAttributeSpecification("Composers", typeof(string), Cardinality.ManyToMany);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ENCODING =
        MediaItemAspectMetadata.CreateAttributeSpecification("Encoding", typeof(string), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_BITRATE =
        MediaItemAspectMetadata.CreateAttributeSpecification("BitRate", typeof(int), Cardinality.ManyToOne);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DISCID =
        MediaItemAspectMetadata.CreateAttributeSpecification("DiscId", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_NUMDISCS =
        MediaItemAspectMetadata.CreateAttributeSpecification("NumDiscs", typeof(int), Cardinality.Inline);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "MusicItem", new[] {
            ATTR_ARTISTS,
            ATTR_ALBUM,
            ATTR_GENRES,
            ATTR_DURATION,
            ATTR_TRACK,
            ATTR_NUMTRACKS,
            ATTR_PLAYCOUNT,
            ATTR_ALBUMARTISTS,
            ATTR_COMPOSERS,
            ATTR_ENCODING,
            ATTR_BITRATE,
            ATTR_DISCID,
            ATTR_NUMDISCS,
        });
  }
}
