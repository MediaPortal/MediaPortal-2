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

using System;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams
{
  public class VideoStream
  {
    public VideoCodec Codec;
    public string FourCC;
    public int StreamIndex;
    public string Language;
    public float AspectRatio;
    public float PixelAspectRatio;
    public PixelFormat PixelFormatType;
    public long Bitrate;
    public float Framerate;
    public EncodingProfile ProfileType;
    public float HeaderLevel;
    public float RefLevel;
    public Timestamp TimestampType;
    public int Width;
    public int Height;

    public bool HasSquarePixels
    {
      get
      {
        if (PixelAspectRatio == 0)
        {
          return true;
        }
        return Math.Abs(1.0 - PixelAspectRatio) < 0.01;
      }
    }
  }
}
