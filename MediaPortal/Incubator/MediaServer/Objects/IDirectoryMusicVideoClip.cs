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

namespace MediaPortal.Plugins.MediaServer.Objects
{
  /// <summary>
  /// A ‘musicVideoClip’ instance is a discrete piece of video that should be interpreted as a clip supporting a song (as opposed to, for example, a continuus TV broadcast or a movie).
  /// </summary>
  public interface IDirectoryMusicVideoClip : IDirectoryVideoItem
  {
    /// <summary>
    /// Name of an artist
    /// </summary>
    [DirectoryProperty("upnp:artist", Required = false)]
    IList<string> Artist { get; set; }

    /// <summary>
    /// Indicates the type of storage medium used for the content.
    /// </summary>
    [DirectoryProperty("upnp:storageMedium", Required = false)]
    string StorageMedium { get; set; }

    /// <summary>
    /// Title of the album to which the item belongs.
    /// </summary>
    [DirectoryProperty("upnp:album", Required = false)]
    IList<string> Album { get; set; }

    /// <summary>
    /// ISO 8601, of the form " yyyy-mm-ddThh:mm:ss". Used to indicate the start time of a schedule program, indented for use by tuners
    /// </summary>
    [DirectoryProperty("upnp:scheduledStartTime", Required = false)]
    string ScheduledStartTime { get; set; }

    /// <summary>
    /// ISO 8601, of the form " yyyy-mm-ddThh:mm:ss". Used to indicate the end time of a scheduled program, indented for use by tuners
    /// </summary>
    [DirectoryProperty("upnp:scheduledEndTime", Required = false)]
    string ScheduledEndTime { get; set; }

    /// <summary>
    /// It is recommended that contributor includes the name of the primary content creator or owner (DublinCore ‘creator’ property)
    /// </summary>
    [DirectoryProperty("dc:contributor", Required = false)]
    IList<string> Contributor { get; set; }

    /// <summary>
    /// ISO 8601, of the form "YYYY-MM-DD",
    /// </summary>
    [DirectoryProperty("dc:date", Required = false)]
    string Date { get; set; }
  }
}