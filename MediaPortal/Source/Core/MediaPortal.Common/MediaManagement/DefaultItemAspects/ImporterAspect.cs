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
  /// Contains the metadata specification of the "ImportedItem" media item aspect which is assigned to all items
  /// which are updated by an importer.
  /// </summary>
  public static class ImporterAspect
  {
    /// <summary>
    /// Aspect id of the importer aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("A531385E-771B-48B3-8CE0-EE0611A84A17");

    /// <summary>
    /// Date and time of the last import of the media item.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_LAST_IMPORT_DATE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("LastImportDate", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// If set to <c>true</c>, the media item must be re-imported.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DIRTY =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Dirty", typeof(bool), Cardinality.Inline, true);

    /// <summary>
    /// Contains the date when the media item was added to the media library.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DATEADDED =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("DateAdded", typeof(DateTime), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ImportedItem", new[] {
            ATTR_LAST_IMPORT_DATE,
            ATTR_DIRTY,
            ATTR_DATEADDED,
        });
  }
}
