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
  /// Contains the metadata specification of the "Media" media item aspect which is assigned to all media items.
  /// </summary>
  public static class MediaAspect
  {
    /// <summary>
    /// Media item aspect id of the media aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("29146287-00C3-417B-AC10-BED1A84DB1A9");

    /// <summary>
    /// Contains a human readable title of the media item.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_TITLE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Title", 1000, Cardinality.Inline, true);

    /// <summary>
    /// Contains the mime type of the media item.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_MIME_TYPE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("MimeType", 50, Cardinality.Inline, false);

    /// <summary>
    /// Contains a media size. For regular files this is the file size, directories might contain the total size of all content.
    /// Online resources like streams might have <c>0</c> as size.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_SIZE =
        MediaItemAspectMetadata.CreateAttributeSpecification("Size", typeof(long), Cardinality.Inline, true);

    /// <summary>
    /// Contains the recording time and date of the media item. Can be used for an exact recording time
    /// (e.g. for images) as well as for only storing a recording year (e.g. for movies).
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_RECORDINGTIME =
        MediaItemAspectMetadata.CreateAttributeSpecification("RecordingTime", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// Contains the user's rating of the media item. Value ranges from 0 (very bad) to 10 (very good).
    /// TODO: once we have user dependent aspects, move this attribute there.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_RATING =
        MediaItemAspectMetadata.CreateAttributeSpecification("Rating", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// Contains a textual comment to this media item.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_COMMENT =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Comment", 1000, Cardinality.Inline, false);

    /// <summary>
    /// Number of times played.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_PLAYCOUNT =
        MediaItemAspectMetadata.CreateAttributeSpecification("PlayCount", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the date when the media item was last played.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_LASTPLAYED =
        MediaItemAspectMetadata.CreateAttributeSpecification("LastPlayed", typeof(DateTime), Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "MediaItem", new[] {
            ATTR_TITLE,
            ATTR_MIME_TYPE,
            ATTR_SIZE,
            ATTR_RECORDINGTIME,
            ATTR_RATING,
            ATTR_COMMENT,
            ATTR_PLAYCOUNT,
            ATTR_LASTPLAYED,
        });
  }
}
