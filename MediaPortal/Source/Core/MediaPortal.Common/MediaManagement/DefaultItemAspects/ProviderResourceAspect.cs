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
  /// Contains the metadata specification of the "ProviderResource" media item aspect which is assigned
  /// to all resource files provided by some resource provider.
  /// </summary>
  public static class ProviderResourceAspect
  {
    /// <summary>
    /// Media item aspect id of the provider resource aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("B05EE7E4-087E-4958-B05B-E73D5B1DAACA");

    /// <summary>
    /// Contains UPnP device UUID of the system where the media item is located.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_SYSTEM_ID =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("System-Id", 100, Cardinality.Inline, true);

    /// <summary>
    /// Resource index for this resource.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_RESOURCE_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("ResourceIndex", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// If set to <c>true</c>, the resource is a primary one. A media item with only secondary resources should be deleted.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_PRIMARY =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("IsPrimary", typeof(bool), Cardinality.Inline, true);

    /// <summary>
    /// Contains the mime type of the resource.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_MIME_TYPE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("MimeType", 50, Cardinality.Inline, false);

    /// <summary>
    /// Contains a media size. For regular files this is the file size, directories might contain the total size of all content.
    /// Online resources like streams might have <c>0</c> as size.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_SIZE =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Size", typeof(long), Cardinality.Inline, true);

    /// <summary>
    /// Contains the path of the item in its provider.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_RESOURCE_ACCESSOR_PATH =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Path", 1000, Cardinality.Inline, true);

    /// <summary>
    /// Contains id of the parent directory item.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_PARENT_DIRECTORY_ID =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("ParentDirectory", typeof(Guid), Cardinality.Inline, true);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ProviderResource", new[] {
            ATTR_SYSTEM_ID,
            ATTR_RESOURCE_INDEX,
            ATTR_PRIMARY,
            ATTR_MIME_TYPE,
            ATTR_SIZE,
            ATTR_RESOURCE_ACCESSOR_PATH,
            ATTR_PARENT_DIRECTORY_ID,
        },
        new[] {
            ATTR_RESOURCE_INDEX
        }
        );
  }
}
