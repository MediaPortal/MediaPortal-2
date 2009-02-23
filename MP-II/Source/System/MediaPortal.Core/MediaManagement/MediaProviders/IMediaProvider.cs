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

using System.IO;

namespace MediaPortal.Core.MediaManagement.MediaProviders
{
  /// <summary>
  /// Interface to provide access to physical media files.
  /// Implementations of this interface provide a means to access additional kinds of resources.
  /// This interface provides the most common functionality each MediaProvider will have.
  /// Sub interfaces define additional functionality like enumerating available media
  /// files, a hierarchical filesystem, etc.
  /// Implementations can provide media data by accessing arbitrary sources like the local file system,
  /// a web server, a UPnP mediaserver, ...
  /// </summary>
  /// <remarks>
  /// The media provider is partitioned in its metadata part (see <see cref="Metadata"/>) and this worker class.
  /// </remarks>
  public interface IMediaProvider
  {
    /// <summary>
    /// Metadata descriptor for this media provider.
    /// </summary>
    MediaProviderMetadata Metadata { get; }

    /// <summary>
    /// Returns the information if the specified <paramref name="path"/> is a resource which can
    /// be opened to an input stream.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c>, if the specified <paramref name="path"/> denotes a resource which can be
    /// opened, else <c>false</c>.</returns>
    bool IsResource(string path);

    /// <summary>
    /// Returns a short, human readable name for the resource specified by <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <returns>A human readable name of the <paramref name="path"/>. For a filesystem media provider,
    /// this could be the file name.</returns>
    string GetResourceName(string path);

    /// <summary>
    /// Returns the full human readable path for the resource specified by <paramref name="path"/>.
    /// </summary>
    /// <remarks>
    /// The returned path will differ from the <paramref name="path"/> used as parameter. The path
    /// used for the media provider might contain additional encoded data used by this media provider,
    /// while this method will return a rectified form of the provided path.
    /// </remarks>
    /// <param name="path">The resource path.</param>
    /// <returns>A human readable name of the <paramref name="path"/>. For a filesystem media provider,
    /// this could be the file path.</returns>
    string GetResourcePath(string path);

    /// <summary>
    /// Opens the media item at the specified <paramref name="path"/> for read operations.
    /// </summary>
    /// <param name="path">Path of a media item provided by this media provider.</param>
    /// <returns>Filestream opened for read operations.</returns>
    Stream OpenRead(string path);

    /// <summary>
    /// Opens the media item at the specified <paramref name="path"/> for write operations.
    /// </summary>
    /// <param name="path">Path of a media item provided by this media provider.</param>
    /// <returns>Filestream opened for write operations.</returns>
    Stream OpenWrite(string path);
  }
}
