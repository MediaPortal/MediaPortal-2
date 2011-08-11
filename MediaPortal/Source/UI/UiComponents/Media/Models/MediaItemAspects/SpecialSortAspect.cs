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
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.UiComponents.Media.Models.MediaItemAspects
{
  /// <summary>
  /// Fake media item aspect which contains the metadata specification of media items which must be sorted by a special attribute.
  /// </summary>
  public static class SpecialSortAspect
  {
    /// <summary>
    /// Media item aspect id of the audio aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("49D1A110-F1A1-450A-8817-CF02ECBF5E22");

    /// <summary>
    /// Special sort string.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_SORT_STRING =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("SortString", 100, Cardinality.Inline, true);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "SpecialSortAspect", new[] {
            ATTR_SORT_STRING,
        });
  }
}
