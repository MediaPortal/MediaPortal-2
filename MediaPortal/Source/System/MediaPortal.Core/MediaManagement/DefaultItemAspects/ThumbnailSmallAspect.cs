#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

namespace MediaPortal.Core.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification for small sized thumbnail data.
  /// </summary>
  public static class ThumbnailSmallAspect
  {
    /// <summary>
    /// Media item aspect id of the small thumbnail aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("A61846E9-0910-499D-9868-A1FABCE7CCFD");

    /// <summary>
    /// Contains a small sized (max. 96x96) thumbnail.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_THUMBNAIL =
        MediaItemAspectMetadata.CreateAttributeSpecification("Thumbnail", typeof(byte[]), Cardinality.Inline, false);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ThumbnailSmall", new[] {
            ATTR_THUMBNAIL,
        });
  }
}
