#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

namespace MediaPortal.Core.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Audio" media item aspect which is assigned to all audio media items.
  /// </summary>
  public static class AudioAspect
  {
    /// <summary>
    /// Media item aspect id of the audio aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("9BA3C559-41F7-4a5f-917C-E3EF65516D14");

    /// <summary>
    /// Enumeration of artist name strings.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ARTISTS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Artists", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Album string.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ALBUM =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Album", 100, Cardinality.Inline, true);

    /// <summary>
    /// Enumeration of genre strings.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_GENRES =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Genres", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DURATION =
        MediaItemAspectMetadata.CreateAttributeSpecification("Duration", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Track number.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_TRACK =
        MediaItemAspectMetadata.CreateAttributeSpecification("Track", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Number of tracks on the CD.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_NUMTRACKS =
        MediaItemAspectMetadata.CreateAttributeSpecification("NumTracks", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Number of times played.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_PLAYCOUNT =
        MediaItemAspectMetadata.CreateAttributeSpecification("PlayCount", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Enumeration of album artist name strings.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ALBUMARTISTS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("AlbumArtists", 100, Cardinality.ManyToMany, false);

    /// <summary>
    /// Enumeration of composer name strings.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_COMPOSERS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Composers", 100, Cardinality.ManyToMany, false);

    /// <summary>
    /// Encoding as string. TODO: Describe format.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ENCODING =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Encoding", 50, Cardinality.Inline, false);

    /// <summary>
    /// Bitrate in bits/second.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_BITRATE =
        MediaItemAspectMetadata.CreateAttributeSpecification("BitRate", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// ID of the disc. TODO: Specification.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DISCID =
        MediaItemAspectMetadata.CreateAttributeSpecification("DiscId", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Number of discs in the collection.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_NUMDISCS =
        MediaItemAspectMetadata.CreateAttributeSpecification("NumDiscs", typeof(int), Cardinality.Inline, false);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "AudioItem", new[] {
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
