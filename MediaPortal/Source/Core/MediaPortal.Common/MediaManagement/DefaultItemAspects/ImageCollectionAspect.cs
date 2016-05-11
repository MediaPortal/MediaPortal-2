#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
  /// Contains the metadata specification of the "Collection" media item aspect which is assigned to image media items.
  /// </summary>
  public static class ImageCollectionAspect
  {
    // TODO: Put this somewhere else?
    public static readonly string TYPE_CITY = "city";
    public static readonly string TYPE_YEAR = "year";
    public static readonly string TYPE_DATE = "date";

    /// <summary>
    /// Media item aspect id of the image collection aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("C9031AF7-F057-4AB8-A2A6-9511B73CDB61");

    /// <summary>
    /// Collection name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COLLECTION_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("CollectionName", 200, Cardinality.Inline, true);

    /// <summary>
    /// Contains the date or year of all contained images.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COLLECTION_DATE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("CollectionDate", typeof(DateTime), Cardinality.Inline, true);

    /// <summary>
    /// Specifies the image collection type. Use <see cref="ImageCollectionType"/> to cast it to a meaningful value.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COLLECTION_TYPE =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("CollectionType", 15, Cardinality.Inline, true);

    /// <summary>
    /// Approximate latitude of all contained images.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_LATITUDE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Latitude", typeof(double), Cardinality.Inline, false);

    /// <summary>
    /// Approximate longitude of all contained images.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_LONGITUDE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Longitude", typeof(double), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "ImageCollectionItem", new[] {
            ATTR_COLLECTION_NAME,
            ATTR_COLLECTION_DATE,
            ATTR_COLLECTION_TYPE,
            ATTR_LATITUDE,
            ATTR_LONGITUDE
        });

    public static readonly Guid ROLE_IMAGE_COLLECTION = new Guid("70E45F3F-31A4-4F58-BE84-B25B25204D54");
  }
}
