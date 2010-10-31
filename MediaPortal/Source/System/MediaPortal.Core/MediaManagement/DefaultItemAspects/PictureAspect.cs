#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

namespace MediaPortal.Core.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Picture" media item aspect which is assigned to all images.
  /// </summary>
  public static class PictureAspect
  {
    /// <summary>
    /// Media item aspect id of the picture aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("8B195D23-5028-4322-98B9-3DEF2BFDD510");

    public static MediaItemAspectMetadata.AttributeSpecification ATTR_WIDTH =
        MediaItemAspectMetadata.CreateAttributeSpecification("Width", typeof(int), Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_HEIGHT =
        MediaItemAspectMetadata.CreateAttributeSpecification("Height", typeof(int), Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_MAKE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EquipmentMake", 100, Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_MODEL =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EquipmentModel", 100, Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_EXPOSURE_BIAS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("ExposureBias", 20, Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_EXPOSURE_TIME =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("ExposureTime", 20, Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_FLASH_MODE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("FlashMode", 50, Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_FNUMBER =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("FNumber", 10, Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DIMENSIONS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Dimensions", 10, Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ISO_SPEED =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("ISOSpeedRating", 10, Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ORIENTATION =
        MediaItemAspectMetadata.CreateAttributeSpecification("Orientation", typeof(int), Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_METERING_MODE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("MeteringMode", 50, Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_SHUTTER_SPEED =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("ShutterSpeed", 50, Cardinality.Inline, false);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "PictureItem", new[] {
            ATTR_WIDTH,
            ATTR_HEIGHT,
            ATTR_MAKE,
            ATTR_MODEL,
            ATTR_EXPOSURE_BIAS,
            ATTR_EXPOSURE_TIME,
            ATTR_FLASH_MODE,
            ATTR_FNUMBER,
            ATTR_DIMENSIONS,
            ATTR_ISO_SPEED,
            ATTR_ORIENTATION,
            ATTR_METERING_MODE,
            ATTR_SHUTTER_SPEED,
        });
  }
}
