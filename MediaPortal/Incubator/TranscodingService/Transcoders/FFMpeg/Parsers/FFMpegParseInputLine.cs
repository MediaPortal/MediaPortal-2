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
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseInputLine
  {
    internal static void ParseInputLine(string inputLine, ref MetadataContainer info)
    {
      inputLine = inputLine.Trim();
      int inputPos = inputLine.IndexOf("Input #0", StringComparison.InvariantCultureIgnoreCase);
      string ffmContainer = inputLine.Substring(inputPos + 10, inputLine.IndexOf(",", inputPos + 11) - 10).Trim();
      if (info.IsAudio)
      {
        info.Metadata.AudioContainerType = FFMpegParseAudioContainer.ParseAudioContainer(ffmContainer);
      }
      else if (info.IsVideo)
      {
        info.Metadata.VideoContainerType = FFMpegParseVideoContainer.ParseVideoContainer(ffmContainer, (ILocalFsResourceAccessor)info.Metadata.Source);
      }
      else if (info.IsImage)
      {
        info.Metadata.ImageContainerType = FFMpegParseImageContainer.ParseImageContainer(ffmContainer);
      }
      else
      {
        info.Metadata.VideoContainerType = FFMpegParseVideoContainer.ParseVideoContainer(ffmContainer, (ILocalFsResourceAccessor)info.Metadata.Source);
        info.Metadata.AudioContainerType = FFMpegParseAudioContainer.ParseAudioContainer(ffmContainer);
        info.Metadata.ImageContainerType = FFMpegParseImageContainer.ParseImageContainer(ffmContainer);
      }
    }
  }
}
