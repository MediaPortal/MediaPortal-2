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
  /// A ‘videoBroadcast’ instance is a continuus stream of video that should be interpreted as a broadcast (e.g., a convential TV channel or a Webcast).
  /// A tvStation represents an (Internet or conventional) tv station, and is derived from the cdsItemContainer base class. A tv channel can contain other items representing the broadcast schedule of the channel, or it can be present as an atomatic item, for example when no schedule information is known.
  /// </summary>
  public interface IDirectoryVideoBroadcast : IDirectoryItem
  {
    /// <summary>
    /// Some icon that a control point can use in its UI to display the content, e.g. a CNN logo for a Tuner channel. Recommend same format as the icon element in the UPnP device description document schema. (PNG). Values must be properly escaped URIs as described in [RFC 2396].
    /// </summary>
    [DirectoryProperty("upnp:icon", Required = false)]
    string Icon { get; set; }

    /// <summary>
    /// Some identification of the region, associated with the ‘source’ of the object, e.g. “US”, “Latin America”, “Seattlle”.
    /// </summary>
    [DirectoryProperty("upnp:region", Required = false)]
    string Region { get; set; }

    /// <summary>
    /// Used for identification of tuner channels themselves, or information associated with a piece of recorded content
    /// </summary>
    [DirectoryProperty("upnp:channelNr", Required = false)]
    int ChannelNr { get; set; }

    /// <summary>
    /// Used to identify the channel, not the program content.
    /// </summary>
    [DirectoryProperty("upnp:channelName", Required = false)]
    string ChannelName { get; set; }
  }
}