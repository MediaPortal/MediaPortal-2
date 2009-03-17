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
  /// Contains the metadata specification of the "Media" media item aspect which is assigned to all media items.
  /// </summary>
  public static class MediaAspect
  {
    /// <summary>
    /// Media item aspect id of the media aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("A01B7D6E-A6F2-434b-AC12-49D7D5CBD377");

    /// <summary>
    /// Contains a human readable title of the media item.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_TITLE =
        MediaItemAspectMetadata.CreateAttributeSpecification("Title", typeof(string), Cardinality.Inline);

    /// <summary>
    /// Contains the mime type of the media item.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_MIME_TYPE =
        MediaItemAspectMetadata.CreateAttributeSpecification("MimeType", typeof(string), Cardinality.Inline);

    /// <summary>
    /// Contains a rectified form of the provider resource path. This might be the DVD folder name, while
    /// the underlaying provider resource path of the media item points to the 'video_ts.ifo' file.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_RECTIFIED_PATH =
        MediaItemAspectMetadata.CreateAttributeSpecification("RectifiedPath", typeof(string), Cardinality.Inline);

    /// <summary>
    /// Contains the recording time date of the media item.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_RECORDINGTIME =
        MediaItemAspectMetadata.CreateAttributeSpecification("RecordingTime", typeof(DateTime), Cardinality.Inline);

    /// <summary>
    /// Contains a rating of the media item.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_RATING =
        MediaItemAspectMetadata.CreateAttributeSpecification("Rating", typeof(int), Cardinality.Inline);

    /// <summary>
    /// Contains a textual comment to this media item.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_COMMENT =
        MediaItemAspectMetadata.CreateAttributeSpecification("Comment", typeof(string), Cardinality.Inline);

    /// <summary>
    /// Contains the date when the media item was last played.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_LASTPLAYED =
        MediaItemAspectMetadata.CreateAttributeSpecification("LastPlayed", typeof(DateTime), Cardinality.Inline);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "MediaItem", new[] {
            ATTR_TITLE,
            ATTR_MIME_TYPE,
            ATTR_RECTIFIED_PATH,
            ATTR_RECORDINGTIME,
            ATTR_RATING,
            ATTR_COMMENT,
            ATTR_LASTPLAYED,
        });
  }
}
