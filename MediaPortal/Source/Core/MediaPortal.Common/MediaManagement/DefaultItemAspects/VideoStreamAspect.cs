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
  /// Contains the metadata specification of the "Video" media item aspect which is assigned to all video media items.
  /// </summary>
  public static class VideoStreamAspect
  {
    // TODO: Put this somewhere else?
    public static readonly string TYPE_SD = "SD";
    public static readonly string TYPE_HD = "HD";
    public static readonly string TYPE_UHD = "UHD";
    public static readonly string TYPE_HSBS = "HSBS";
    public static readonly string TYPE_SBS = "SBS";
    public static readonly string TYPE_HTAB = "HTAB";
    public static readonly string TYPE_TAB = "TAB";
    public static readonly string TYPE_MVC = "MVC";
    public static readonly string TYPE_ANAGLYPH = "ANAGLYPH";

    /// <summary>
    /// Media item aspect id of the video aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("3730566F-39AA-4597-9308-39F361C7375D");

    /// <summary>
    /// Resource index for this video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_RESOURCE_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("ResourceIndex", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// The index of the stream inside the container.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_STREAM_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("StreamIndex", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the type of the video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_VIDEO_TYPE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("VideoType", 50, Cardinality.Inline, true);

    /// <summary>
    /// The part number if part of a multiple parts. Use -1 if not a part.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_VIDEO_PART =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("PartNum", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// The part set number if part of a a set of multiple parts. Use -1 if not a part.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_VIDEO_PART_SET =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("PartSetNum", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// Contains the name of the set if any.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_VIDEO_PART_SET_NAME =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("PartSetName", 100, Cardinality.Inline, false);

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_DURATION =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Duration", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Number of audio streams for this video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_AUDIOSTREAMCOUNT =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("AudioStreamCount", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Encoding of the video. TODO: Describe format.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_VIDEOENCODING =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("VideoEncoding", 50, Cardinality.Inline, false);

    /// <summary>
    /// Bitrate of the video in kbits/second.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_VIDEOBITRATE =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("VideoBitRate", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Width of the video in pixels.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_WIDTH =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Width", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Height of the video in pixels.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_HEIGHT =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Height", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Aspect ratio of the resulting video in width/height. Might differ from the quotient width/height of the properties
    /// <see cref="ATTR_WIDTH"/> and <see cref="ATTR_HEIGHT"/> if the resulting video must be stretched.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_ASPECTRATIO =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("AspectRatio", typeof(float), Cardinality.Inline, false);

    /// <summary>
    /// Frames/second of the video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_FPS =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("FPS", typeof(float), Cardinality.Inline, false);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "VideoStreamItem", new[] {
            ATTR_RESOURCE_INDEX,
            ATTR_STREAM_INDEX,
            ATTR_VIDEO_TYPE,
            ATTR_VIDEO_PART,
            ATTR_VIDEO_PART_SET,
            ATTR_VIDEO_PART_SET_NAME,
            ATTR_DURATION,
            ATTR_AUDIOSTREAMCOUNT,
            ATTR_VIDEOENCODING,
            ATTR_VIDEOBITRATE,
            ATTR_WIDTH,
            ATTR_HEIGHT,
            ATTR_ASPECTRATIO,
            ATTR_FPS,
        },
        new[] {
            ATTR_RESOURCE_INDEX,
            ATTR_STREAM_INDEX
        },
        ProviderResourceAspect.ASPECT_ID,
        new[] {
            ATTR_RESOURCE_INDEX
        }
        );

    public static string GetVideoType(string stereoscopicMode, string stereoscopicType, double? videoHeight, double? videoWidth)
    {
      if (!string.IsNullOrWhiteSpace(stereoscopicMode) && !string.IsNullOrWhiteSpace(stereoscopicType))
      {
        if (stereoscopicMode.StartsWith("H", StringComparison.InvariantCultureIgnoreCase))
        {
          if (stereoscopicType.EndsWith("SBS", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_HSBS;
          }
          else if (stereoscopicType.EndsWith("TAB", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_HTAB;
          }
          else if (stereoscopicType.EndsWith("OU", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_HTAB;
          }
        }
        else
        {
          if (stereoscopicType.EndsWith("SBS", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_SBS;
          }
          else if (stereoscopicType.EndsWith("TAB", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_TAB;
          }
          else if (stereoscopicType.EndsWith("OU", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_TAB;
          }
        }
      }
      else if (!string.IsNullOrWhiteSpace(stereoscopicType))
      {
        if (stereoscopicType.StartsWith("H", StringComparison.InvariantCultureIgnoreCase))
        {
          if (stereoscopicType.EndsWith("SBS", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_HSBS;
          }
          else if (stereoscopicType.EndsWith("TAB", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_HTAB;
          }
          else if (stereoscopicType.EndsWith("OU", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_HTAB;
          }
        }
        else
        {
          if (stereoscopicType.EndsWith("SBS", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_SBS;
          }
          else if (stereoscopicType.EndsWith("TAB", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_TAB;
          }
          else if (stereoscopicType.EndsWith("OU", StringComparison.InvariantCultureIgnoreCase))
          {
            stereoscopicType = TYPE_TAB;
          }
        }
      }
      if (!string.IsNullOrWhiteSpace(stereoscopicType))
      {
        if (stereoscopicType.EndsWith("MVC", StringComparison.InvariantCultureIgnoreCase))
        {
          stereoscopicType = TYPE_MVC;
        }
        else if (stereoscopicType.EndsWith("ANAGLYPH", StringComparison.InvariantCultureIgnoreCase))
        {
          stereoscopicType = TYPE_ANAGLYPH;
        }
      }

      int full3DTABMinHeight = 720 * 2;
      int full3DSBSMinWidth = 1280 * 2;
      string videoType = null;
      if (videoHeight.HasValue && videoWidth.HasValue)
      {
        if ((videoWidth / videoHeight >= 2.5) && (videoWidth >= full3DSBSMinWidth)) // we have Full HD SBS 
        {
          videoType = TYPE_SBS;
          videoWidth = videoWidth / 2;
        }
        else if ((videoWidth / videoHeight <= 1.5) && (videoHeight >= full3DTABMinHeight)) // we have Full HD TAB
        {
          videoType = TYPE_TAB;
          videoHeight = videoHeight / 2;
        }
        else if (stereoscopicType == TYPE_SBS || stereoscopicType == TYPE_HSBS)
        {
          //Cannot be full SBS because of resolution
          videoType = TYPE_HSBS;
          videoWidth = videoWidth / 2;
        }
        else if (stereoscopicType == TYPE_TAB || stereoscopicType == TYPE_HTAB)
        {
          //Cannot be full TAB because of resolution
          videoType = TYPE_HTAB;
          videoHeight = videoHeight / 2;
        }
        else if (!string.IsNullOrWhiteSpace(stereoscopicType))
        {
          videoType = stereoscopicType;
        }
        else if (videoWidth > 3800 && videoHeight > 1500)
        {
          videoType = TYPE_UHD;
        }
        else if (videoWidth > 1250 && videoHeight > 500)
        {
          videoType = TYPE_HD;
        }
        else
        {
          videoType = TYPE_SD;
        }
      }
      else if (!string.IsNullOrWhiteSpace(stereoscopicType))
      {
        videoType = stereoscopicType;
      }
      else if (videoWidth.HasValue)
      {
        if (videoWidth > 3800)
        {
          videoType = TYPE_UHD;
        }
        else if (videoWidth > 1250)
        {
          videoType = TYPE_HD;
        }
        else
        {
          videoType = TYPE_SD;
        }
      }
      else if (videoHeight.HasValue)
      {
        if (videoHeight > 1570)
        {
          videoType = TYPE_UHD;
        }
        else if (videoHeight > 580)
        {
          videoType = TYPE_HD;
        }
        else
        {
          videoType = TYPE_SD;
        }
      }
      return videoType;
    }
  }
}
