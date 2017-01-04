#region Copyright (C) 2007-2017 Team MediaPortal

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

using System;
using System.Collections.Generic;
using System.Net;
using MediaPortal.Common.Exceptions;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Provides low-level file meta information to remote resources.
  /// </summary>
  public interface IRemoteResourceInformationService
  {
    void Startup();

    void Shutdown();

    /// <summary>
    /// Gets resource information about the resource at the system with the given <paramref name="nativeSystemId"/>
    /// with the given <paramref name="nativeResourcePath"/>.
    /// </summary>
    /// <param name="nativeSystemId">Id of the system where the resource is located.</param>
    /// <param name="nativeResourcePath">Path of the resource.</param>
    /// <param name="isFileSystemResource">Returns the information if the referenced resource is located in a virtual
    /// file system (e.g. the resource can be accessed via file system methods, for example <see cref="GetFiles"/> or
    /// <see cref="GetChildDirectories"/>).</param>
    /// <param name="isFile">Returns the information if the resource is a file.</param>
    /// <param name="resourcePathName">Returns a human readable name for the resource's path.</param>
    /// <param name="resourceName">Returns a human readable name for the resource.</param>
    /// <param name="lastChanged">Returns the last change date of the resource.</param>
    /// <param name="size">Returns the size of the resource in bytes.</param>
    /// <returns><c>true</c>, if the resource exists and its resource information could be retrieved, else <c>false</c>.</returns>
    /// <exception cref="NotConnectedException">If the system with the given <paramref name="nativeSystemId"/> is not
    /// connected.</exception>
    bool GetResourceInformation(string nativeSystemId, ResourcePath nativeResourcePath, out bool isFileSystemResource,
        out bool isFile, out string resourcePathName, out string resourceName, out DateTime lastChanged, out long size);

    /// <summary>
    /// Returns the information if the given resource exists.
    /// </summary>
    /// <param name="nativeSystemId">Id of the system where the resource to be checked is located.</param>
    /// <param name="nativeResourcePath">Native path of the resource in the system of the given
    /// <paramref name="nativeSystemId"/>.</param>
    /// <returns><c>true</c>, if the resource exists, else <c>false</c>.</returns>
    /// <exception cref="NotConnectedException">If the system with the given <paramref name="nativeSystemId"/> is not
    /// connected.</exception>
    bool ResourceExists(string nativeSystemId, ResourcePath nativeResourcePath);

    /// <summary>
    /// Concatenates the given <paramref name="nativeResourcePath"/> and the given <paramref name="relativePath"/> at
    /// the system with the given <paramref name="nativeSystemId"/> and returns the concatenated path, if it exists.
    /// </summary>
    /// <param name="nativeSystemId">Id of the system where the <paramref name="nativeResourcePath"/> is valid.</param>
    /// <param name="nativeResourcePath">Base resource path to be concatenated.</param>
    /// <param name="relativePath">Relative path to be added to the given <paramref name="nativeResourcePath"/>.</param>
    /// <returns>Concatenated path or <c>null</c>, if it doesn't exist at the system with the given
    /// <paramref name="nativeSystemId"/> or if <paramref name="nativeResourcePath"/> isn't a path in a virtual
    /// filesystem, i.e. the underlaying resource provider doesn't support concatenation of paths.</returns>
    /// <exception cref="NotConnectedException">If the given <paramref name="nativeSystemId"/> is not connected.</exception>
    ResourcePath ConcatenatePaths(string nativeSystemId, ResourcePath nativeResourcePath, string relativePath);

    /// <summary>
    /// Gets all files in the given <paramref name="nativeResourcePath"/> at the system with the given
    /// <paramref name="nativeSystemId"/>.
    /// </summary>
    /// <param name="nativeSystemId">Id of the system where the <paramref name="nativeResourcePath"/> is valid.</param>
    /// <param name="nativeResourcePath">Resource path whose files should be retrieved.</param>
    /// <returns>Collection of files resource path metadata, if the given <paramref name="nativeResourcePath"/>
    /// is a filesystem directory, else <c>null</c>.</returns>
    /// <exception cref="NotConnectedException">If the system of the given <paramref name="nativeSystemId"/> is not
    /// connected.</exception>
    ICollection<ResourcePathMetadata> GetFiles(string nativeSystemId, ResourcePath nativeResourcePath);

    /// <summary>
    /// Gets all child directories in the given <paramref name="nativeResourcePath"/> at the system with the given
    /// <paramref name="nativeSystemId"/>.
    /// </summary>
    /// <param name="nativeSystemId">Id of the system where the <paramref name="nativeResourcePath"/> is valid.</param>
    /// <param name="nativeResourcePath">Resource path whose child directories should be retrieved.</param>
    /// <returns>Collection of directories resource path metadata, if the given <paramref name="nativeResourcePath"/>
    /// is a filesystem directory, else <c>null</c>.</returns>
    /// <exception cref="NotConnectedException">If the system of the given <paramref name="nativeSystemId"/> is not
    /// connected.</exception>
    ICollection<ResourcePathMetadata> GetChildDirectories(string nativeSystemId, ResourcePath nativeResourcePath);

    /// <summary>
    /// Gets an HTTP URL pointing to the file at the system with the given <paramref name="nativeSystemId"/> at the given
    /// <paramref name="nativeResourcePath"/>.
    /// </summary>
    /// <remarks>
    /// For multi-network interface environments, it can be necessary to point the underlaying modules to the correct network
    /// interface for outgoing HTTP requests. Among the file's HTTP URL, this method also returns the appropriate local IP address
    /// which should be used as local endpoint for the HTTP request.
    /// </remarks>
    /// <param name="nativeSystemId">Id of the system where the file can be accessed.</param>
    /// <param name="nativeResourcePath">Path of the file resource at the system with the given
    /// <paramref name="nativeSystemId"/>.</param>
    /// <param name="fileHttpUrl">The HTTP URL pointing to the requested file.</param>
    /// <param name="localIpAddress">Local IP address which specifies the local network interface to send the HTTP request to.</param>
    /// <returns>HTTP URL where the file data can be retrieved or <c>null</c>, if the URL cannot be resolved.</returns>
    /// <exception cref="NotConnectedException">If the system of the given <paramref name="nativeSystemId"/> is not
    /// connected.</exception>
    bool GetFileHttpUrl(string nativeSystemId, ResourcePath nativeResourcePath, out string fileHttpUrl, out IPAddress localIpAddress);
  }
}