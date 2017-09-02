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
  public interface IDirectoryPlaylist : IDirectoryContainer
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
    /// A few lines of description of the content item (longer than DublinCore’s description element
    /// </summary>
    [DirectoryProperty("upnp:longDescription", Required = false)]
    string LongDescription { get; set; }

    /// <summary>
    /// Name of producer of e.g., a movie or CD
    /// </summary>
    [DirectoryProperty("upnp:producer", Required = false)]
    IList<string> Producer { get; set; }

    /// <summary>
    /// Indicates the type of storage medium used for the content.
    /// </summary>
    [DirectoryProperty("upnp:storageMedium", Required = false)]
    string StorageMedium { get; set; }

    /// <summary>
    /// An account of the resource.
    /// </summary>
    [DirectoryProperty("dc:description", Required = false)]
    string Description { get; set; }

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

    /// <summary>
    /// A language of the resource.
    /// </summary>
    [DirectoryProperty("dc:language", Required = false)]
    string Language { get; set; }

    /// <summary>
    /// Information about rights held in and over the resource.
    /// </summary>
    [DirectoryProperty("dc:rights", Required = false)]
    IList<string> Rights { get; set; }
  }
}