#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store inforation about the streams contained in a media file
  /// </summary>
  public class StreamDetailsStub
  {
    /// <summary>
    /// Extension of the container file
    /// </summary>
    /// <example>".avi"</example>
    public string Container { get; set; }

    /// <summary>
    /// Details of the video stream(s)
    /// </summary>
    public HashSet<VideoStreamDetailsStub> VideoStreams { get; set; }

    /// <summary>
    /// Details of the audio stream(s)
    /// </summary>
    public HashSet<AudioStreamDetailsStub> AudioStreams { get; set; }

    /// <summary>
    /// Details of the subtitle stream(s)
    /// </summary>
    public HashSet<SubtitleStreamDetailsStub> SubtitleStreams { get; set; }
  }
}
