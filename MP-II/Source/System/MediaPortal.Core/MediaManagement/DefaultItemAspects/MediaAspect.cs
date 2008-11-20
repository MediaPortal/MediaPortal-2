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
    public static Guid ASPECT_ID = new Guid("{A01B7D6E-A6F2-434b-AC12-49D7D5CBD377}");

    public static MediaItemAspectMetadata.AttributeSpecification ATTR_TITLE =
        MediaItemAspectMetadata.CreateAttributeSpecification("Title", typeof(string), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ENCODING =
        MediaItemAspectMetadata.CreateAttributeSpecification("Encoding", typeof(string), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DATE =
        MediaItemAspectMetadata.CreateAttributeSpecification("Recording time", typeof(DateTime), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_RATING =
        MediaItemAspectMetadata.CreateAttributeSpecification("Rating", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_COMMENT =
        MediaItemAspectMetadata.CreateAttributeSpecification("Comment", typeof(string), Cardinality.Inline);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "MediaItem", new[] {
            ATTR_TITLE,
            ATTR_ENCODING,
            ATTR_DATE,
            ATTR_RATING,
            ATTR_COMMENT,
        });
  }
}
