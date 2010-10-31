#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.Exceptions;
using MediaPortal.Core.General;

namespace MediaPortal.Core.MediaManagement.ResourceAccess
{
  /// <summary>
  /// Provides low-level file meta information to remote resources.
  /// </summary>
  public interface IRemoteResourceInformationService
  {
    void Startup();

    void Shutdown();

    /// <summary>
    /// Gets resource information about the resource at the given <paramref name="nativeSystem"/> with the given
    /// <paramref name="nativeResourcePath"/>.
    /// </summary>
    /// <param name="nativeSystem">System where the resource is located.</param>
    /// <param name="nativeResourcePath">Path of the resource.</param>
    /// <param name="isFileSystemResource">Returns the information if that resource is part of a file system (e.g. the
    /// resource can be accessed via file system methods, for example <see cref="GetFiles"/> or
    /// <see cref="GetChildDirectories"/>).</param>
    /// <param name="isFile">Returns the information if the resource is a file.</param>
    /// <param name="resourcePathName">Returns a human readable name for the resource's path.</param>
    /// <param name="resourceName">Returns a human readable name for the resource.</param>
    /// <param name="lastChanged">Returns the last change date of the resource.</param>
    /// <param name="size">Returns the size of the resource in bytes.</param>
    /// <returns><c>true</c>, if the resource exists and its resource information could be retrieved, else <c>false</c>.</returns>
    /// <exception cref="NotConnectedException">If the given <paramref name="nativeSystem"/> is not connected.</exception>
    bool GetResourceInformation(SystemName nativeSystem, ResourcePath nativeResourcePath, out bool isFileSystemResource,
        out bool isFile, out string resourcePathName, out string resourceName, out DateTime lastChanged, out long size);

    /// <summary>
    /// Returns the information if the given resource exists.
    /// </summary>
    /// <param name="nativeSystem">System where the resource to be checked is located.</param>
    /// <param name="nativeResourcePath">Native path of the resource in the given <paramref name="nativeSystem"/>.</param>
    /// <returns><c>true</c>, if the resource exists, else <c>false</c>.</returns>
    /// <exception cref="NotConnectedException">If the given <paramref name="nativeSystem"/> is not connected.</exception>
    bool ResourceExists(SystemName nativeSystem, ResourcePath nativeResourcePath);

    /// <summary>
    /// Concatenates the given <paramref name="nativeResourcePath"/> and the given <paramref name="relativePath"/> at
    /// the given <paramref name="nativeSystem"/> and returns the concatenated path, if it exists.
    /// </summary>
    /// <param name="nativeSystem">System where the <paramref name="nativeResourcePath"/> is valid.</param>
    /// <param name="nativeResourcePath">Base resource path to be concatenated.</param>
    /// <param name="relativePath">Relative path to be added to the given <paramref name="nativeResourcePath"/>.</param>
    /// <returns>Concatenated path or <c>null</c>, if it doesn't exist at the given <paramref name="nativeSystem"/>
    /// or if <paramref name="nativeResourcePath"/> isn't a file system path.</returns>
    /// <exception cref="NotConnectedException">If the given <paramref name="nativeSystem"/> is not connected.</exception>
    ResourcePath ConcatenatePaths(SystemName nativeSystem, ResourcePath nativeResourcePath, string relativePath);

    /// <summary>
    /// Gets all files in the given <paramref name="nativeResourcePath"/> at the given <paramref name="nativeSystem"/>.
    /// </summary>
    /// <param name="nativeSystem">System where the <paramref name="nativeResourcePath"/> is valid.</param>
    /// <param name="nativeResourcePath">Resource path whose files should be retrieved.</param>
    /// <returns>Collection of files resource path metadata, if the given <paramref name="nativeResourcePath"/>
    /// is a filesystem directory, else <c>null</c>.</returns>
    /// <exception cref="NotConnectedException">If the given <paramref name="nativeSystem"/> is not connected.</exception>
    ICollection<ResourcePathMetadata> GetFiles(SystemName nativeSystem, ResourcePath nativeResourcePath);

    /// <summary>
    /// Gets all child directories in the given <paramref name="nativeResourcePath"/> at the given <paramref name="nativeSystem"/>.
    /// </summary>
    /// <param name="nativeSystem">System where the <paramref name="nativeResourcePath"/> is valid.</param>
    /// <param name="nativeResourcePath">Resource path whose child directories should be retrieved.</param>
    /// <returns>Collection of directories resource path metadata, if the given <paramref name="nativeResourcePath"/>
    /// is a filesystem directory, else <c>null</c>.</returns>
    /// <exception cref="NotConnectedException">If the given <paramref name="nativeSystem"/> is not connected.</exception>
    ICollection<ResourcePathMetadata> GetChildDirectories(SystemName nativeSystem, ResourcePath nativeResourcePath);

    /// <summary>
    /// Gets an HTTP URL pointing to the file at the given <paramref name="nativeSystem"/> at the given
    /// <paramref name="nativeResourcePath"/>.
    /// </summary>
    /// <param name="nativeSystem">System where the file can be accessed.</param>
    /// <param name="nativeResourcePath">Path of the file resource at the given <paramref name="nativeSystem"/>.</param>
    /// <returns>HTTP URL where the file data can be retrieved or <c>null</c>, if the URL cannot be resolved.</returns>
    string GetFileHttpUrl(SystemName nativeSystem, ResourcePath nativeResourcePath);
  }
}