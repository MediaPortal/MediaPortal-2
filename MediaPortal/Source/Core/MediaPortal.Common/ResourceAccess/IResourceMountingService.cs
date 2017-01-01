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

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// System service which provides a configurable directory structure on a virtual drive.
  /// </summary>
  public interface IResourceMountingService
  {
    /// <summary>
    /// Gets the root path in the local filesystem where remote resources are mounted or <c>null</c> if the resource mounting service was not started
    /// or if it had a problem mounting its configuread drive.
    /// </summary>
    ResourcePath RootPath { get; }

    /// <summary>
    /// Gets all configured virtual root directories.
    /// </summary>
    ICollection<string> RootDirectories { get; }

    /// <summary>
    /// Starts the resource mounting service.
    /// </summary>
    void Startup();

    /// <summary>
    /// Shuts the resource mounting service down.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Returns the information if the given resource path is located inside a virtual mount of this service.
    /// </summary>
    /// <param name="rp">Resource path to check.</param>
    /// <returns><c>true</c>, if the given resource accessor is located inside a virtual mount of this service, else <c>false</c>.</returns>
    bool IsVirtualResource(ResourcePath rp);

    /// <summary>
    /// Creates a new virtual root directory with the given <paramref name="rootDirectoryName"/> under the <see cref="RootPath"/>.
    /// </summary>
    /// <param name="rootDirectoryName">Name of the new root directory.</param>
    /// <returns>Local filesystem path for the new root directory or <c>null</c> if the resource mounting service
    /// cannot mount resources.</returns>
    string CreateRootDirectory(string rootDirectoryName);

    /// <summary>
    /// Deletes the root directory with the given <paramref name="rootDirectoryName"/>. This method also disposes all
    /// resources inside the directory.
    /// </summary>
    /// <param name="rootDirectoryName">Name of the root directory to remove.</param>
    void DisposeRootDirectory(string rootDirectoryName);

    /// <summary>
    /// Gets all mounted resources in the root directory of the given <paramref name="rootDirectoryName"/>.
    /// </summary>
    /// <param name="rootDirectoryName">Name of the root directory whose resources should be returned.</param>
    /// <returns>Collection of resource accessors inside the given <paramref name="rootDirectoryName"/> or
    /// <c>null</c>, if there is no root directory with the given <paramref name="rootDirectoryName"/>.</returns>
    ICollection<IFileSystemResourceAccessor> GetResources(string rootDirectoryName);

    /// <summary>
    /// Adds a resource to the given <paramref name="rootDirectoryName"/>.
    /// </summary>
    /// <param name="rootDirectoryName">Name of the root directory which must have been created before by
    /// calling <see cref="CreateRootDirectory"/>.</param>
    /// <param name="resourceAccessor">Resource accessor of the resource to add. This service will take over the
    /// ownership for this resource accessor, i.e. it will call <see cref="IDisposable.Dispose"/> when
    /// <see cref="RemoveResource"/> is called for the given <paramref name="rootDirectoryName"/>.</param>
    /// <returns>Local filesystem path for the new resource, if the root directory with the given
    /// <paramref name="rootDirectoryName"/> exists and the resource could successfully be added, else <c>null</c>.</returns>
    string AddResource(string rootDirectoryName, IFileSystemResourceAccessor resourceAccessor);

    /// <summary>
    /// Removes the given <paramref name="resourceAccessor"/> from the root directory of the given
    /// <paramref name="rootDirectoryName"/>.
    /// </summary>
    /// <remarks>
    /// The resource's resource accessor will be automatically disposed.
    /// </remarks>
    /// <param name="rootDirectoryName">Root directory name which contains the given resource.</param>
    /// <param name="resourceAccessor">Resource to remove.</param>
    void RemoveResource(string rootDirectoryName, IFileSystemResourceAccessor resourceAccessor);
  }
}