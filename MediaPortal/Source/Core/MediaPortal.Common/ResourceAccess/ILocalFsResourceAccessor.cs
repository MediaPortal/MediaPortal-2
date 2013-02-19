#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Temporary local filesystem accessor instance for a resource which might located anywhere in an MP2 system.
  /// Via this instance, the resource, which potentially is located in a remote system, can be accessed
  /// via a <see cref="LocalFileSystemPath"/>.
  /// </summary>
  /// <remarks>
  /// To get a local filesystem resource accessor, get an <see cref="IResourceLocator"/> and use its
  /// <see cref="IResourceLocator.CreateLocalFsAccessor"/> method.
  /// </remarks>
  public interface ILocalFsResourceAccessor : IFileSystemResourceAccessor
  {
    /// <summary>
    /// Gets a path in the local filesystem where the represented media item is located.
    /// </summary>
    /// <value>Dos path which is valid in the local file system or <c>null</c>, if this resource accessor doesn't denote a
    /// valid file system path (i.e. it represents the root resource <c>"/"</c>).</value>
    string LocalFileSystemPath { get; }
  }
}