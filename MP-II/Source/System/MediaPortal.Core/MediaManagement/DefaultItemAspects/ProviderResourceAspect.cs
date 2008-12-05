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
  /// Contains the metadata specification of the "ProviderResource" media item aspect which is assigned
  /// to all resource files provided by some media provider.
  /// </summary>
  public static class ProviderResourceAspect
  {
    public static Guid ASPECT_ID = new Guid("{0A296ACD-F95B-4a28-90A2-E4FD2A4CC4ED}");

    /// <summary>
    /// Contains the source computer where the media item is located.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_SOURCE_COMPUTER =
        MediaItemAspectMetadata.CreateAttributeSpecification("Source-Computer", typeof(string), Cardinality.ManyToOne);

    /// <summary>
    /// Contains the id of the provider which provides the media item.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_PROVIDER_ID =
        MediaItemAspectMetadata.CreateAttributeSpecification("Provider-ID", typeof(string), Cardinality.ManyToOne);

    /// <summary>
    /// Contains the path of the item in its provider.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_PATH =
        MediaItemAspectMetadata.CreateAttributeSpecification("Path", typeof(string), Cardinality.Inline);

    /// <summary>
    /// Contains a collection of providers the media items depends on.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_PARENTPROVIDERS =
        MediaItemAspectMetadata.CreateAttributeSpecification("ParentProviders", typeof(string), Cardinality.ManyToMany);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ProviderResource", new[] {
            ATTR_SOURCE_COMPUTER,
            ATTR_PROVIDER_ID,
            ATTR_PATH,
            ATTR_PARENTPROVIDERS,
        });
  }
}
