#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
  public enum PictureRotation
  {
    Rot_0,
    Rot_90,
    Rot_180,
    Rot_270
  }

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
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ISO_SPEED =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("ISOSpeedRating", 10, Cardinality.Inline, false);

    /// <summary>
    /// Contains the EXIF orientation info. Use <see cref="OrientationToDegrees"/> to get a translated angle in degree.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ORIENTATION =
        MediaItemAspectMetadata.CreateAttributeSpecification("Orientation", typeof(int), Cardinality.Inline, false);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_METERING_MODE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("MeteringMode", 50, Cardinality.Inline, false);

    public static bool OrientationToRotation(int orientationInfo, out PictureRotation rotation)
    {
      switch (orientationInfo)
      {
        case 1:
        case 2:
          rotation = PictureRotation.Rot_0;
          break;
        case 3:
        case 4:
          rotation = PictureRotation.Rot_180;
          break;
        case 6:
        case 7:
          rotation = PictureRotation.Rot_90;
          break;
        case 5:
        case 8:
          rotation = PictureRotation.Rot_270;
          break;
        default:
          rotation = PictureRotation.Rot_0;
          return false;
      }
      return true;
    }

    /// <summary>
    /// Translates the EXIF orientation info to an angle in degrees. The value should be used to apply a rotation
    /// to show a picture correctly oriented.
    /// </summary>
    /// <param name="orientationInfo">Orientation info, stored in attribute <see cref="ATTR_ORIENTATION"/>.</param>
    /// <param name="degrees">Returns the number degrees the picture has to be rotated in clockwise direction.</param>
    /// <returns><c>true</c>, if the rotation could successfully be decoded from the given <paramref name="orientationInfo"/>,
    /// else <c>false</c>.</returns>
    public static bool OrientationToDegrees(int orientationInfo, out int degrees)
    {
      degrees = 0;
      PictureRotation rotation;
      if (!OrientationToRotation(orientationInfo, out rotation))
        return false;
      switch (rotation)
      {
        case PictureRotation.Rot_0:
          degrees = 0;
          break;
        case PictureRotation.Rot_90:
          degrees = 90;
          break;
        case PictureRotation.Rot_180:
          degrees = 180;
          break;
        case PictureRotation.Rot_270:
          degrees = 270;
          break;
      }
      return true;
    }

    /// <summary>
    /// Translates the EXIF orientation info to the information if the picture must be flipped in X or Y direction.
    /// The value should be used to flip the picture horizontally or vertically, if <paramref name="flipX"/> or <paramref name="flipY"/>
    /// are set.
    /// </summary>
    /// <param name="orientationInfo">Orientation info, stored in attribute <see cref="ATTR_ORIENTATION"/>.</param>
    /// <param name="flipX">If set to <c>true</c>, the picture should be horizontally flipped.</param>
    /// <param name="flipY">If set to <c>true</c>, the picture should be vertically flipped.</param>
    /// <returns><c>true</c>, if the flipping could be successfully be decoded from the given <paramref name="orientationInfo"/>,
    /// else <c>false</c>.</returns>
    public static bool OrientationToFlip(int orientationInfo, out bool flipX, out bool flipY)
    {
      flipX = false;
      flipY = false;
      switch (orientationInfo)
      {
        case 1:
        case 3:
        case 6:
        case 8:
          break;
        case 2:
        case 4:
          flipX = true;
          break;
        case 5:
        case 7:
          flipY = true;
          break;
        default:
          return false;
      }
      return true;
    }

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
            ATTR_ISO_SPEED,
            ATTR_ORIENTATION,
            ATTR_METERING_MODE,
        });
  }
}
