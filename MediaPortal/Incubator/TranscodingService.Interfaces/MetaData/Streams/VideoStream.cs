﻿#region Copyright (C) 2007-2017 Team MediaPortal

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

using Newtonsoft.Json;
using System;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams
{
  public class VideoStream
  {
    public VideoCodec Codec { get; set; }
    public string FourCC { get; set; }
    public int StreamIndex { get; set; }
    public string Language { get; set; }
    public float? AspectRatio { get; set; }
    public float? PixelAspectRatio { get; set; }
    public PixelFormat PixelFormatType { get; set; }
    public long? Bitrate { get; set; }
    public float? Framerate { get; set; }
    public EncodingProfile ProfileType { get; set; }
    public float? HeaderLevel { get; set; }
    public float? RefLevel { get; set; }
    public Timestamp TimestampType { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }

    [JsonIgnore]
    public bool HasSquarePixels
    {
      get
      {
        if (!PixelAspectRatio.HasValue)
          return true;

        return Math.Abs(1.0 - PixelAspectRatio.Value) < 0.01;
      }
    }
  }
}
