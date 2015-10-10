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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg
{
  internal class FFMpegFileProcessor
  {
    internal static void FileProcessor(ref FFMpegTranscodeData data, int transcoderTimeout)
    {
      DateTime dtStart = DateTime.Now;
      Process ffmpeg = new Process
      {
        StartInfo =
        {
          FileName = data.TranscoderBinPath,
          Arguments = data.TranscoderArguments,
          WorkingDirectory = data.WorkPath,
          CreateNoWindow = true,
          WindowStyle = ProcessWindowStyle.Hidden
        }
      };
      ffmpeg.Start();
      while (ffmpeg.HasExited == false && DateTime.Now < dtStart.AddMilliseconds(transcoderTimeout))
      {
        Thread.Sleep(5);
      }
      ffmpeg.Close();
      ffmpeg.Dispose();
    }
  }
}
