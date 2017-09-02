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

namespace MediaPortal.Plugins.MediaServer.Objects
{
  /// <summary>
  /// A ‘movie’ instance is a discrete piece of video that should be interpreted as a movie (as opposed to, for example, a continuus TV broadcast or a music video clip).
  /// </summary>
  public interface IDirectoryMovie : IDirectoryVideoItem
  {
    /// <summary>
    /// Indicates the type of storage medium used for the content. Potentially useful for user-interface purposes.
    /// </summary>
    [DirectoryProperty("upnp.storageMedium", Required = false)]
    string StorageMedium { get; set; }

    /// <summary>
    /// Region code of the DVD disc
    /// </summary>
    [DirectoryProperty("upnp:DVDRegionCode", Required = false)]
    int DvdRegionCode { get; set; }

    /// <summary>
    /// Used for identification of channels themselves, or information associated with a piece of recorded content
    /// </summary>
    [DirectoryProperty("upnp:channelName", Required = false)]
    string ChannelName { get; set; }

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
  }
}