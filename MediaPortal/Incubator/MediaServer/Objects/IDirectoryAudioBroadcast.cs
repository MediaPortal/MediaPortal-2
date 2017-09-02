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
  /// An ‘audioBroadcast’ instance is a continuus stream of audio that should be interpreted as an audio broadcast (as opposed to, for example, a song or an audio book).
  /// </summary>
  public interface IDirectoryAudioBroadcast : IDirectoryItem
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
    /// Radio station call sign, e.g. “KSJO”
    /// </summary>
    [DirectoryProperty("upnp:radioCallSign", Required = false)]
    string RadioCallSign { get; set; }

    /// <summary>
    /// Some identification, e.g. “107.7”, broadcast frequency of the radio station
    /// </summary>
    [DirectoryProperty("upnp:radioStationID", Required = false)]
    string RadioStationId { get; set; }

    /// <summary>
    /// Radio station frequency band. Recommended values are “AM”, “FM”, “Shortwave“, “Internet”, “Satellite”. Vendor’s may extend this list.
    /// </summary>
    [DirectoryProperty("upnp:radioBand", Required = false)]
    string RadioBand { get; set; }

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