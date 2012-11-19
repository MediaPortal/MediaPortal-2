#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Extensions.MediaServer.DLNA
{
  public static class DlnaProfiles
  {
    // JPEG Profiles

    /// <summary>
    /// Profile for image media class content of small resolution
    /// </summary>
    public const string JpegSmall = "JPEG_SM";

    /// <summary>
    /// Profile for image media class content of medium resolution
    /// </summary>
    public const string JpegMedium = "JPEG_MED";

    /// <summary>
    /// Profile for image media class content of large resolution
    /// </summary>
    public const string JpegLarge = "JPEG_LRG";

    /// <summary>
    /// Profile for image thumbnails
    /// </summary>
    public const string JpegThumbnail = "JPEG_TN";

    /// <summary>
    /// Profile for small icons
    /// </summary>
    public const string JpegSmallIcon = "JPEG_SM_ICO";

    /// <summary>
    /// Profile for large icons
    /// </summary>
    public const string JpegLargeIcon = "JPEG_LRG_ICO";

    // PNG Profiles

    /// <summary>
    /// Profile for image thumbnails
    /// </summary>
    public const string PngThumbnail = "PNG_TN";

    /// <summary>
    /// Profile for small icons
    /// </summary>
    public const string PngSmallIcon = "PNG_SM_ICO";

    /// <summary>
    /// Profile for large icons
    /// </summary>
    public const string PngLargeIcon = "PNG_LRG_ICO";

    /// <summary>
    /// Profile for image media class content of large resolution
    /// </summary>
    public const string PngLarge = "PNG_LRG";

    // Audio AC-3 Profiles

    /// <summary>
    /// Profile for audio media class content
    /// </summary>
    public const string Ac3 = "AC3";


    // Some missing


    // Audio MP3 Profiles

    /// <summary>
    /// Profile for audio media class content
    /// </summary>
    public const string Mp3 = "MP3";

    /// <summary>
    /// Profile for audio media class content with extensions for lower sampling rates and bitrates.
    /// </summary>
    public const string Mp3X = "MP3X";


    // Some missing


    // AV MPEG-2 Profiles


    /// <summary>
    /// Profile for NTSC-formatted AV class media
    /// </summary>
    public const string MpegPsNtsc = "MPEG_PS_NTSC";

    /// <summary>
    /// Profile for NTSC-formatted AV class media
    /// </summary>
    public const string MpegPsNtscXAc3 = "MPEG_PS_NTSC_X_AC3";

    /// <summary>
    /// Profile for PAL-formatted AV class media
    /// </summary>
    public const string MpegPsPal = "MPEG_PS_PAL";

    /// <summary>
    /// Profile for PAL-formatted AV class media
    /// </summary>
    public const string MpegPsPalXAc3 = "MPEG_PS_PAL_X_AC3";

    // Some missing

    // AV MPEG-4 Part 2 Profiles


    /// <summary>
    /// MPEG-4 Part2 Simple Profile with AAC LC audio, encapsulated in MP4.
    /// </summary>
    public const string Mpeg4P2Mp4SpAac = "MPEG4_P2_MP4_SP_AAC";

    /// <summary>
    /// MPEG-4 Part2 Simple Profile with HE AAC audio, encapsulated in MP4.
    /// </summary>
    public const string Mpeg4P2Mp4SpHeAac = "MPEG4_P2_MP4_SP_HEAAC";

    /// <summary>
    /// MPEG-4 Part2 Simple Profile with ATRAC3plus audio, encapsulated in MP4.
    /// </summary>
    public const string Mpeg4P2Mp4SpAtrac3Plus = "MPEG4_P2_MP4_SP_ATRAC3plus";

    /// <summary>
    /// MPEG-4 Part2 Simple Profile with AAC LTP audio, encapsulated in MP4.
    /// </summary>
    public const string Mpeg4P2Mp4SpAacLtp = "MPEG4_P2_MP4_SP_AAC_LTP";

    /// <summary>
    /// MPEG-4 Part2 Simple Profile Level 2 with AAC audio, encapsulated in MP4.
    /// </summary>
    public const string Mpeg4P2Mp4SpL2Aac = "MPEG4_P2_MP4_SP_L2_AAC";

    /// <summary>
    /// MPEG-4 Part2 Simple Profile Level 2 with AMR audio, encapsulated in MP4.
    /// </summary>
    public const string Mpeg4P2Mp4SpL2Amr = "MPEG4_P2_MP4_SP_L2_AMR";
  }
}