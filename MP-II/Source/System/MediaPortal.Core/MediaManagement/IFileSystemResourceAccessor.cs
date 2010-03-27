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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Resource accessor interface to access a resource in a hierarchical file system structure.
  /// This interface provides additional methods to navigate through the structure, i.e.
  /// query available sub items and sub directories.
  /// This resource accessor interface will be used for all hierarchical file systems - it is NOT ONLY intended to be used
  /// for the local HDD filesystem.
  /// </summary>
  /// <remarks>
  /// Implementors of this interface can provide a (maybe virtual) filesystem, starting with
  /// a root directory.
  /// The root directory is represented by "/". Directory path names are organized like unix paths.
  /// </remarks>
  public interface IFileSystemResourceAccessor : IResourceAccessor
  {
    /// <summary>
    /// Returns the information if this resource is a directory which might contain files and sub directories.
    /// </summary>
    /// <value><c>true</c>, if this resource denotes a directory.</value>
    bool IsDirectory { get; }

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
  }
}
