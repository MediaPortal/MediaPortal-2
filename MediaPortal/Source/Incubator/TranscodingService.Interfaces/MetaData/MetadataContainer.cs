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

using System.Collections.Generic;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Metadata
{
  public class MetadataContainer
  {
    public MetadataStream Metadata = new MetadataStream();
    public ImageStream Image = new ImageStream();
    public VideoStream Video = new VideoStream();
    public List<AudioStream> Audio = new List<AudioStream>();
    public List<SubtitleStream> Subtitles = new List<SubtitleStream>();

    public bool IsImage
    {
      get
      {
        if (Audio.Count > 0)
        {
          return false;
        }
        if (Metadata.Mime != null && Metadata.Mime.StartsWith("Image/", System.StringComparison.InvariantCultureIgnoreCase))
        {
          return true;
        }
        if (Metadata.ImageContainerType != ImageContainer.Unknown)
        {
          return true;
        }
        return false;
      }
    }

    public bool IsAudio
    {
      get
      {
        if (IsVideo == true)
        {
          return false;
        }
        if (Audio.Count > 0)
        {
          return true;
        }
        if (Metadata.Mime != null && Metadata.Mime.StartsWith("Audio/", System.StringComparison.InvariantCultureIgnoreCase))
        {
          return true;
        }
        if (Metadata.AudioContainerType != AudioContainer.Unknown)
        {
          return true;
        }
        return false;
      }
    }

    public bool IsVideo
    {
      get
      {
        if (Audio.Count > 0 && Video.Codec != VideoCodec.Unknown)
        {
          return true;
        }
        if (Metadata.Mime != null && Metadata.Mime.StartsWith("Video/", System.StringComparison.InvariantCultureIgnoreCase))
        {
          return true;
        }
        if (Metadata.VideoContainerType != VideoContainer.Unknown)
        {
          return true;
        }
        return false;
      }
    }
  }
}
