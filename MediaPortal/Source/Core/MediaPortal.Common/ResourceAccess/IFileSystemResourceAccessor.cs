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
using System.IO;
using System.Threading.Tasks;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Resource accessor interface to access a resource in a hierarchical file system structure.
  /// This interface provides methods to navigate through the structure, i.e. query available sub items and sub directories
  /// and open file streams.
  /// This resource accessor interface will be used for all hierarchical file systems - it is NOT ONLY intended to be used
  /// for the local HDD filesystem.
  /// </summary>
  /// <remarks>
  /// Implementors of this interface can provide a (maybe virtual) filesystem, starting with a root directory.
  /// The root directory is represented by "/". Directory path names are organized like unix paths.
  /// </remarks>
  public interface IFileSystemResourceAccessor : IResourceAccessor
  {
    /// <summary>
    /// Explicitly checks if the resource described by this resource accessor currently exists.
    /// </summary>
    bool Exists { get; }

    /// <summary>
    /// Returns the information if this resource is a file which can be opened to an input stream.
    /// </summary>
    /// <value><c>true</c>, if this resource denotes a file which can be opened, else <c>false</c>.</value>
    bool IsFile { get; }

    /// <summary>
    /// Gets the date and time when this resource was changed for the last time.
    /// </summary>
    DateTime LastChanged { get; }

    /// <summary>
    /// Gets the file size in bytes, if this resource represents a file. Else returns <c>-1</c>.
    /// </summary>
    long Size { get; }

    /// <summary>
    /// Returns the information if the resource at the given path exists in the resource provider of this resource.
    /// </summary>
    /// <remarks>
    /// This method is defined in interface <see cref="IFileSystemResourceAccessor"/> rather than in interface
    /// <see cref="IResourceProvider"/> because we would need two different signatures for
    /// <see cref="IBaseResourceProvider"/> and <see cref="IChainedResourceProvider"/>, which is not convenient.
    /// Furthermore, this method supports relative paths which are related to this resource.
    /// </remarks>
    /// <param name="path">Absolute or relative path to check for a resource.</param>
    /// <returns><c>true</c> if a resource at the given path exists in the <see cref="IResourceAccessor.ParentProvider"/>,
    /// else <c>false</c>.</returns>
    bool ResourceExists(string path);

    /// <summary>
    /// Returns a resource which is located in the same underlaying resource provider and which might be located relatively
    /// to this resource.
    /// </summary>
    /// <param name="path">Relative or absolute path which is valid in the underlaying resource provider.</param>
    /// <returns>Resource accessor for the desired resource, if it exists, else <c>null</c>.</returns>
    IFileSystemResourceAccessor GetResource(string path);

    /// <summary>
    /// Returns the resource accessors for all child files of this directory resource.
    /// </summary>
    /// <returns>Collection of child resource accessors of sub files or <c>null</c>, if this resource
    /// is no directory resource or if it is invalid.</returns>
    ICollection<IFileSystemResourceAccessor> GetFiles();

    /// <summary>
    /// Returns the resource accessors for all child directories of this directory resource.
    /// </summary>
    /// <returns>Collection of child resource accessors of sub directories or <c>null</c>, if
    /// this resource is no directory resource or if it is invalid in this provider.</returns>
    ICollection<IFileSystemResourceAccessor> GetChildDirectories();

    /// <summary>
    /// Prepares this resource accessor to get a stream for the resource's contents.
    /// This might take some time, so this method might block some seconds.
    /// </summary>
    /// <remarks>
    /// Resource accessors wrap resource access to different kinds of resources. Some of
    /// them might require a local file cache, for example. This method can be implemented to prepare such a cache.
    /// That avoids long latencies in the methods <see cref="OpenRead"/> and <see cref="OpenWrite"/>.
    /// </remarks>
    void PrepareStreamAccess();

    /// <summary>
    /// Opens a stream to read this resource.
    /// </summary>
    /// <returns>Stream opened for read operations, if supported. Else, <c>null</c> is returned.</returns>
    /// <exception cref="IllegalCallException">If this resource is not a file (see <see cref="IsFile"/>).</exception>
    Stream OpenRead();

    /// <summary>
    /// Opens a stream to read this resource in asynchronouns mode, if supported; otherwise returns the same as <see cref="OpenRead"/>
    /// </summary>
    /// <returns>Task of Stream opened for asynchronous read operations, if supported. Otherwise resutnrs the same as <see cref="OpenRead"/>.</returns>
    /// <exception cref="IllegalCallException">If this resource is not a file (see <see cref="IsFile"/>).</exception>
    /// <remarks>
    /// This method not only tries to open a stream to the resource in asynchronous mode, it also returns a Task of Stream (instead of a plain Stream).
    /// The reason is that - depending on the concrete implementation - a call to this method may require preparational and potentially longer lasting
    /// tasks to be done (in particular if <see cref="PrepareStreamAccess"/> was not called before).
    /// </remarks>
    Task<Stream> OpenReadAsync();

    /// <summary>
    /// Opens a stream to write this resource.
    /// </summary>
    /// <returns>Stream opened for write operations, if supported. Else, <c>null</c> is returned.</returns>
    /// <exception cref="IllegalCallException">If this resource is not a file (see <see cref="IsFile"/>).</exception>
    Stream OpenWrite();
  }
}
