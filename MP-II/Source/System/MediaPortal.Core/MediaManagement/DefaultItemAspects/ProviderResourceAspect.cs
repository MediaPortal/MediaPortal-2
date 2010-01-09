#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
  /// Contains the metadata specification of the "ProviderResource" media item aspect which is assigned
  /// to all resource files provided by some media provider.
  /// </summary>
  public static class ProviderResourceAspect
  {
    /// <summary>
    /// Media item aspect id of the provider resource aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("0A296ACD-F95B-4a28-90A2-E4FD2A4CC4ED");

    /// <summary>
    /// Contains UPnP device UUID of the system where the media item is located.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_SYSTEM_ID =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("System-Id", 100, Cardinality.Inline, true);

    /// <summary>
    /// Contains the path of the item in its provider.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_RESOURCE_ACCESSOR_PATH =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Path", 1000, Cardinality.Inline, true);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ProviderResource", new[] {
            ATTR_SYSTEM_ID,
            ATTR_RESOURCE_ACCESSOR_PATH,
        });
  }
}
