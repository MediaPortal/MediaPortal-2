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
  /// Contains the metadata specification for large sized thumbnail data.
  /// </summary>
  public static class ThumbnailLargeAspect
  {
    /// <summary>
    /// Media item aspect id of the large thumbnail aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("1FDA5774-9AC5-4873-926C-E84E3C36A966");

    /// <summary>
    /// Contains a large sized (max. 256x256) thumbnail.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_THUMBNAIL =
        MediaItemAspectMetadata.CreateAttributeSpecification("Thumbnail", typeof(byte[]), Cardinality.Inline, false);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ThumbnailLarge", new[] {
            ATTR_THUMBNAIL,
        });
  }
}
