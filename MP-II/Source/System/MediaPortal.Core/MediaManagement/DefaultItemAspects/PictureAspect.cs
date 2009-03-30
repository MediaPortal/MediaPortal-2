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
  /// Contains the metadata specification of the "Picture" media item aspect which is assigned to all images.
  /// </summary>
  public static class PictureAspect
  {
    /// <summary>
    /// Media item aspect id of the picture aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("8B195D23-5028-4322-98B9-3DEF2BFDD510");

    public static MediaItemAspectMetadata.AttributeSpecification ATTR_WIDTH =
        MediaItemAspectMetadata.CreateAttributeSpecification("Width", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_HEIGHT =
        MediaItemAspectMetadata.CreateAttributeSpecification("Height", typeof(int), Cardinality.Inline);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "PictureItem", new[] {
            ATTR_WIDTH,
            ATTR_HEIGHT,
        });
  }
}
