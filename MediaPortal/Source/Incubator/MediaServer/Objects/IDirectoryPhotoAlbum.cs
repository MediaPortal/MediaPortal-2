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
  public interface IDirectoryPhotoAlbum : IDirectoryAlbum
  {
    /// <summary>
    /// Name of an artist
    /// </summary>
    [DirectoryProperty("upnp:artist", Required = false)]
    IList<string> Artist { get; set; }

    /// <summary>
    /// Name of the genre to which an object belongs
    /// </summary>
    [DirectoryProperty("upnp:genre", Required = false)]
    IList<string> Genre { get; set; }

    /// <summary>
    /// Name of producer of e.g., a movie or CD
    /// </summary>
    [DirectoryProperty("dc:producer", Required = false)]
    IList<string> Producer { get; set; }

    /// <summary>
    /// Reference to album art. Values must be properly escaped URIs as described in [RFC 2396].
    /// </summary>
    [DirectoryProperty("upnp:albumArtURI", Required = false)]
    string AlbumArtUrl { get; set; }

    /// <summary>
    /// Identifier of an audio CD.
    /// </summary>
    [DirectoryProperty("upnp:toc", Required = false)]
    string Toc { get; set; }
  }
}