﻿#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.Extensions.MediaServer.Objects
{
  /// <summary>
  /// An ‘album’ instance represents an ordered collection of ‘objects’.
  /// </summary>
  public interface IDirectoryAlbum : IDirectoryContainer
  {
    /// <summary>
    /// Indicates the type of storage medium used for the content.
    /// </summary>
    [DirectoryProperty("upnp:storageMedium", Required = false)]
    string StorageMedium { get; set; }

    /// <summary>
    /// A few lines of description of the content item (longer than DublinCore’s description element
    /// </summary>
    [DirectoryProperty("upnp:longDescription", Required = false)]
    string LongDescription { get; set; }

    /// <summary>
    /// An account of the resource.
    /// </summary>
    [DirectoryProperty("dc:description", Required = false)]
    string Description { get; set; }

    /// <summary>
    /// An entity responsible for making the resource available.
    /// </summary>
    [DirectoryProperty("dc:publisher", Required = false)]
    IList<string> Publisher { get; set; }

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
    /// A related resource.
    /// </summary>
    [DirectoryProperty("dc:relation", Required = false)]
    string Relation { get; set; }

    /// <summary>
    /// Information about rights held in and over the resource.
    /// </summary>
    [DirectoryProperty("dc:rights", Required = false)]
    IList<string> Rights { get; set; }
  }
}
