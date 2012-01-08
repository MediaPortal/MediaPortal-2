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
  public enum ImageRotation
  {
    Rot_0,
    Rot_90,
    Rot_180,
    Rot_270
  }

  /// <summary>
  /// Contains the metadata specification of the "Image" media item aspect which is assigned to all images.
  /// </summary>
  public static class ImageAspect
  {
    /// <summary>
    /// Media item aspect id of the image aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("7868A212-F866-47F6-9F5D-4DFB3868F763");

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_WIDTH =
        MediaItemAspectMetadata.CreateAttributeSpecification("Width", typeof(int), Cardinality.Inline, false);
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_HEIGHT =
        MediaItemAspectMetadata.CreateAttributeSpecification("Height", typeof(int), Cardinality.Inline, false);
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_MAKE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EquipmentMake", 100, Cardinality.Inline, false);
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_MODEL =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EquipmentModel", 100, Cardinality.Inline, false);
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EXPOSURE_BIAS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("ExposureBias", 20, Cardinality.Inline, false);
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EXPOSURE_TIME =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("ExposureTime", 20, Cardinality.Inline, false);
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_FLASH_MODE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("FlashMode", 50, Cardinality.Inline, false);
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_FNUMBER =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("FNumber", 10, Cardinality.Inline, false);
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_ISO_SPEED =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("ISOSpeedRating", 10, Cardinality.Inline, false);

    /// <summary>
    /// Contains the EXIF orientation info. Use <see cref="OrientationToRotation"/>, <see cref="OrientationToFlip"/>
    /// or <see cref="GetOrientationMetadata"/> to translate the orientation information into degrees and flipX/flipY values.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_ORIENTATION =
        MediaItemAspectMetadata.CreateAttributeSpecification("Orientation", typeof(int), Cardinality.Inline, false);
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_METERING_MODE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("MeteringMode", 50, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ImageItem", new[] {
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

    /// <summary>
    /// Translates the EXIF orientation info to a rotation. The value should be used to apply a rotation
    /// to show a image correctly oriented.
    /// </summary>
    /// <param name="orientationInfo">Orientation info, stored in attribute <see cref="ATTR_ORIENTATION"/>.</param>
    /// <param name="rotation">Returns the rotation in clockwise direction.</param>
    /// <returns><c>true</c>, if the rotation could successfully be decoded from the given <paramref name="orientationInfo"/>,
    /// else <c>false</c>.</returns>
    public static bool OrientationToRotation(int orientationInfo, out ImageRotation rotation)
    {
      switch (orientationInfo)
      {
        case 1:
        case 2:
          rotation = ImageRotation.Rot_0;
          break;
        case 3:
        case 4:
          rotation = ImageRotation.Rot_180;
          break;
        case 6:
        case 7:
          rotation = ImageRotation.Rot_90;
          break;
        case 5:
        case 8:
          rotation = ImageRotation.Rot_270;
          break;
        default:
          rotation = ImageRotation.Rot_0;
          return false;
      }
      return true;
    }

    /// <summary>
    /// Translates the given <paramref name="rotation"/> to an angle in degrees.
    /// </summary>
    /// <param name="rotation">Rotation to be translated.</param>
    /// <returns>Number degrees in clockwise direction, corresponding to the given <paramref name="rotation"/>.</returns>
    public static int RotationToDegrees(ImageRotation rotation)
    {
      switch (rotation)
      {
        case ImageRotation.Rot_0:
          return 0;
        case ImageRotation.Rot_90:
          return 90;
        case ImageRotation.Rot_180:
          return 180;
        case ImageRotation.Rot_270:
          return 270;
        default:
          return 0;
      }
    }

    /// <summary>
    /// Translates the EXIF orientation info to the information if the image must be flipped in X or Y direction.
    /// The value should be used to flip the image horizontally or vertically, if <paramref name="flipX"/> or <paramref name="flipY"/>
    /// are set.
    /// </summary>
    /// <param name="orientationInfo">Orientation info, stored in attribute <see cref="ATTR_ORIENTATION"/>.</param>
    /// <param name="flipX">If set to <c>true</c>, the image should be horizontally flipped.</param>
    /// <param name="flipY">If set to <c>true</c>, the image should be vertically flipped.</param>
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

    public static bool GetOrientationMetadata(MediaItem mediaItem, out ImageRotation rotation, out bool flipX, out bool flipY)
    {
      rotation = ImageRotation.Rot_0;
      flipX = false;
      flipY = false;
      MediaItemAspect imageAspect;
      if (mediaItem != null && mediaItem.Aspects.TryGetValue(ASPECT_ID, out imageAspect))
      {
        int orientationInfo = (int) imageAspect[ATTR_ORIENTATION];
        return (OrientationToRotation(orientationInfo, out rotation) &&
            OrientationToFlip(orientationInfo, out flipX, out flipY));
      }
      return false;
    }
  }
}
