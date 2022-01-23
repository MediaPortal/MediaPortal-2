#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseInputLine
  {
    internal static void ParseInputLine(IResourceAccessor file, string inputLine, ref MetadataContainer info)
    {
      inputLine = inputLine.Trim();
      int inputPos = inputLine.IndexOf("Input #0", StringComparison.InvariantCultureIgnoreCase);
      string ffmContainer = inputLine.Substring(inputPos + 10, inputLine.IndexOf(",", inputPos + 11) - 10).Trim();
      if (info.IsAudio(Editions.DEFAULT_EDITION))
      {
        info.Metadata[Editions.DEFAULT_EDITION].AudioContainerType = FFMpegParseAudioContainer.ParseAudioContainer(ffmContainer, file);
      }
      else if (info.IsVideo(Editions.DEFAULT_EDITION))
      {
        info.Metadata[Editions.DEFAULT_EDITION].VideoContainerType = FFMpegParseVideoContainer.ParseVideoContainer(ffmContainer, file);
      }
      else if (info.IsImage(Editions.DEFAULT_EDITION))
      {
        info.Metadata[Editions.DEFAULT_EDITION].ImageContainerType = FFMpegParseImageContainer.ParseImageContainer(ffmContainer, file);
      }
      else
      {
        info.Metadata[Editions.DEFAULT_EDITION].VideoContainerType = FFMpegParseVideoContainer.ParseVideoContainer(ffmContainer, file);
        info.Metadata[Editions.DEFAULT_EDITION].AudioContainerType = FFMpegParseAudioContainer.ParseAudioContainer(ffmContainer, file);
        info.Metadata[Editions.DEFAULT_EDITION].ImageContainerType = FFMpegParseImageContainer.ParseImageContainer(ffmContainer, file);
      }
    }
  }
}
