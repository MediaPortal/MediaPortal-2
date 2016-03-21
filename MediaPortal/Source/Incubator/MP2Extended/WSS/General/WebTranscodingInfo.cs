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
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;

namespace MediaPortal.Plugins.MP2Extended.WSS.General
{
  public class WebTranscodingInfo
  {
    public WebTranscodingInfo(TranscodeContext context)
    {
      if (context != null)
      {
        Supported = context.Running;
        TranscodedTime = Convert.ToInt64(context.CurrentDuration.TotalSeconds);
        TranscodedFrames = context.CurrentFrames;
        TranscodingPosition = context.CurrentThroughput;
        TranscodingFPS = context.CurrentFPS;
        OutputBitrate = context.CurrentBitrate;
        Finished = !context.Running;
        Failed = context.Failed;
      }
      else
      {
        Supported = false;
        TranscodedTime = 0;
        TranscodedFrames = 0;
        TranscodingPosition = 0;
        TranscodingFPS = 0;
        OutputBitrate = 0;
        Finished = true;
        Failed = false;
      }
    }

    /// <summary>
    /// The amount of video that has already been transcoded (0 is the start of playback position).
    /// </summary>
    public long TranscodedTime { get; set; }

    /// <summary>
    /// The number of frames already transcoded.
    /// </summary>
    public long TranscodedFrames { get; set; }

    /// <summary>
    /// The position in the file at which the transcoder currently is (0 is the start of the file).
    /// </summary>
    public long TranscodingPosition { get; set; }

    /// <summary>
    /// The framerate at which the transcoder is currently transcoding in frames per second. Doesn't have to be the framerate of the output stream.
    /// </summary>
    public long TranscodingFPS { get; set; }

    /// <summary>
    /// The current bitrate of the output stream in kbit/s.
    /// </summary>
    public long OutputBitrate { get; set; }

    // whether or not getting the transcoding info is supported
    public bool Supported { get; set; }
    // is the transcoding finished?
    public bool Finished { get; set; }
    // did the transcoding fail?
    public bool Failed { get; set; }
  }
}
