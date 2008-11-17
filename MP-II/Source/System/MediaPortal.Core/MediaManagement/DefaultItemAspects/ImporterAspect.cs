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
  /// Contains the metadata of the "ImportedItem" media item aspect which is assigned to all items
  /// which are updated by an importer.
  /// </summary>
  public static class ImporterAspect
  {
    public static Guid ASPECT_ID = new Guid("{CC0163FE-55A5-426c-A29C-F1D64AF7E683}");
    public static string ATTR_LAST_IMPORT_TIME = "Last import time";
    public static string ATTR_LAST_IMPORT_DURATION = "Last import duration";
    public static string ATTR_DIRTY = "Dirty";

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ImportedItem", new[] {
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_LAST_IMPORT_TIME, typeof(DateTime), false),
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_LAST_IMPORT_DURATION, typeof(TimeSpan), false),
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_DIRTY, typeof(bool), false),
  });
}

}
