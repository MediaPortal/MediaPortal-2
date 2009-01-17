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

using System.Collections.Generic;

namespace MediaPortal.Core.MediaManagement.MediaProviders
{
  /// <summary>
  /// MediaProvider interface to access a hierarchical file system structure.
  /// This interface provides additional methods to navigate through the structure, i.e.
  /// query available sub items and sub directories.
  /// This provider works for all hierarchical file systems - it is NOT ONLY intended to be used
  /// for the local HDD filesystem.
  /// </summary>
  /// <remarks>
  /// Implementors of this interface can provide a (maybe virtual) filesystem, starting with
  /// a root directory.
  /// The root directory is denoted by "/". Directory path names are organized like unix paths.
  /// </remarks>
  public interface IFileSystemMediaProvider : IMediaProvider
  {
    /// <summary>
    /// Returns the paths of the media items at the specified <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path so search the media items.</param>
    /// <returns>Collection of strings containing the paths of media items.</returns>
    ICollection<string> GetFiles(string path);

    /// <summary>
    /// Returns the paths of child directories of the specified <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path to search child directories.</param>
    /// <returns>Collection of strings containing the paths of sub directories</returns>
    ICollection<string> GetChildDirectories(string path);
  }
}
