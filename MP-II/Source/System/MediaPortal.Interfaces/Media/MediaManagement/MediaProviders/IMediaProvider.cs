#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;

namespace MediaPortal.Media.MediaManagement
{
  /// <summary>
  /// Interface to provide access to physical media files.
  /// Implementations of this interface provide a means to access additional kinds of resources.
  /// This interface provides the most common functionality each MediaProvider will have.
  /// Sub interfaces define additional functionality like enumerating available media
  /// files, a hierarchical filesystem, etc.
  /// Implementations can provide media data by accessing the local file system, a web server,
  /// an UPnP mediaserver, ...
  /// </summary>
  public interface IMediaProvider
  {
    /// <summary>
    /// GUID which uniquely identifies this media provider.
    /// </summary>
    Guid GUID { get; }

    /// <summary>
    /// Returns the name of this media provider. The name should be unique among all
    /// media providers.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Opens the media item at the specified <paramref name="path"/> for read operations.
    /// </summary>
    /// <param name="path">Path of a media item provided by this media provider.</param>
    /// <returns>Filestream opened for read operations.</returns>
    FileStream OpenRead(string path);
  }
}
