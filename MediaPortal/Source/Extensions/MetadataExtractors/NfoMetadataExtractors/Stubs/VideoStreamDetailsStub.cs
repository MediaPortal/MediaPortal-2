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

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store inforation about a video stream
  /// </summary>
  public class VideoStreamDetailsStub
  {
    /// <summary>
    /// Videocodec
    /// </summary>
    /// <example>"x264"</example>
    public string Codec { get; set; }

    /// <summary>
    /// Aspect ratio
    /// </summary>
    /// <example>1.330000</example>
    public decimal? Aspect { get; set; }

    /// <summary>
    /// Width in pixels
    /// </summary>
    /// <example>960</example>
    public int? Width { get; set; }

    /// <summary>
    /// Height in pixels
    /// </summary>
    /// <example>720</example>
    public int? Height { get; set; }

    /// <summary>
    /// Duration of this particular stream
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Stereomode of this particular stream
    /// </summary>
    public string Stereomode { get; set; }
  }
}
