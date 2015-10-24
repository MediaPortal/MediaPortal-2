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
  /// A ‘musicTrack’ instance is a discrete piece of audio that should be interpreted as music (as opposed to, for example, a news broadcast or an audio book).
  /// </summary>
  public interface IDirectoryMusicTrack : IDirectoryAudioItem
  {
    /// <summary>
    /// Name of an artist
    /// </summary>
    [DirectoryProperty("upnp:artist", Required = false)]
    IList<string> Artist { get; set; }

    /// <summary>
    /// Title of the album to which the item belongs.
    /// </summary>
    [DirectoryProperty("upnp:album", Required = false)]
    IList<string> Album { get; set; }

    /// <summary>
    /// Original track number on an audio CD or other medium
    /// </summary>
    [DirectoryProperty("upnp:originalTrackNumber", Required = false)]
    int OriginalTrackNumber { get; set; }

    /// <summary>
    /// Name of a playlist to which the item belongs
    /// </summary>
    [DirectoryProperty("upnp:playlist", Required = false)]
    IList<string> Playlist { get; set; }

    /// <summary>
    /// Indicates the type of storage medium used for the content.
    /// </summary>
    [DirectoryProperty("upnp:storageMedium", Required = false)]
    string StorageMedium { get; set; }

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