#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  /// Contains the metadata specification of the "ReimportItem" media item aspect which is assigned to all items
  /// which are reimported by a user.
  /// </summary>
  public static class ReimportAspect
  {
    /// <summary>
    /// Aspect id of the reimport aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("DC260473-3098-4458-B485-0ED9EFB22972");

    /// <summary>
    /// Contains the search parameter for the reimport.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SEARCH =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Search", 300, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ReimportItem", new[] {
            ATTR_SEARCH,
        });
  }
}
