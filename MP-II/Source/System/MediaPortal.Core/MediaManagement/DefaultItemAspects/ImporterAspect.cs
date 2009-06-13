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
    public static Guid ASPECT_ID = new Guid("CC0163FE-55A5-426c-A29C-F1D64AF7E683");
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_LAST_IMPORT_TIME =
        MediaItemAspectMetadata.CreateAttributeSpecification("Last import time", typeof(DateTime), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_LAST_IMPORT_DURATION =
        MediaItemAspectMetadata.CreateAttributeSpecification("Last import duration", typeof(TimeSpan), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DIRTY =
        MediaItemAspectMetadata.CreateAttributeSpecification("Dirty", typeof(bool), Cardinality.Inline);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ImportedItem", new[] {
            ATTR_LAST_IMPORT_TIME,
            ATTR_LAST_IMPORT_DURATION,
            ATTR_DIRTY,
        });
  }
}
