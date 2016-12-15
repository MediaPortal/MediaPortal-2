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
  /// Contains the metadata specification for large sized thumbnail data.
  /// </summary>
  public static class ThumbnailLargeAspect
  {
    /// <summary>
    /// Media item aspect id of the large thumbnail aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("2E492453-269A-49EF-B3F1-FD71FE13FAB9");

    /// <summary>
    /// Contains a large sized (max. 256x256) thumbnail.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_THUMBNAIL =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Thumbnail", typeof(byte[]), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ThumbnailLarge", new[] {
            ATTR_THUMBNAIL,
        });
  }
}
