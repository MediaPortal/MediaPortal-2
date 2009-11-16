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

namespace MediaPortal.Core.MediaManagement.DefaultItemAspects
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
    public static Guid ASPECT_ID = new Guid("CC0163FE-55A5-426c-A29C-F1D64AF7E683");

    /// <summary>
    /// Date and time of the last import of the media item.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_LAST_IMPORT_DATE =
        MediaItemAspectMetadata.CreateAttributeSpecification("LastImportDate", typeof(DateTime), Cardinality.Inline);

    /// <summary>
    /// If set to <c>true</c>, the media item must be re-imported.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DIRTY =
        MediaItemAspectMetadata.CreateAttributeSpecification("Dirty", typeof(bool), Cardinality.Inline);

    /// <summary>
    /// Contains the date when the media item was added to the media library.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DATEADDED =
        MediaItemAspectMetadata.CreateAttributeSpecification("DateAdded", typeof(DateTime), Cardinality.Inline);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ImportedItem", new[] {
            ATTR_LAST_IMPORT_DATE,
            ATTR_DIRTY,
            ATTR_DATEADDED,
        });
  }
}
