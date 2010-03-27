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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Temporary local filesystem accessor instance for a resource which might located anywhere in an MP-II system.
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
    string LocalFileSystemPath { get; }
  }
}