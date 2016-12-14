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

    /// <summary>
    /// Ensures that <see cref="LocalFileSystemPath"/> can be accessed by this process until the returned <see cref="IDisposable"/> is disposed.
    /// </summary>
    /// <returns><see cref="IDisposable"/> that must be disposed after accessing the resource via <see cref="LocalFileSystemPath"/></returns>
    /// <remarks>
    /// In general, a <see cref="ILocalFsResourceAccessor"/> should ensure access to the respective resource through all of its methods
    /// as of its instantiation. Access through <see cref="LocalFileSystemPath"/>, however, is particular in so far as the actuall access
    /// to the resource does not happen within the <see cref="ILocalFsResourceAccessor"/>, but other code first gets the <see cref="LocalFileSystemPath"/>
    /// and then accesses the resource outside of the <see cref="ILocalFsResourceAccessor"/>. Additionally, there are situations, in
    /// which the <see cref="ILocalFsResourceAccessor"/> cannot ensure that between accessing the <see cref="LocalFileSystemPath"/> and
    /// accessing the resource behind it, it is still possible to access that resource through the <see cref="LocalFileSystemPath"/>. This
    /// is e.g. the case for the NetworkNeighborhoodResourceAccessor, which requires impersonation. Impersonation is thread-affin. If the
    /// <see cref="LocalFileSystemPath"/> was read in one thread, but the access to the resource happens in another thread, the
    /// access to the resource would fail.
    /// Accessing and using the <see cref="LocalFileSystemPath"/> is therefore generally only allowed within a:
    /// <example>
    /// using(lfsra.EnsureLocalFileSystemAccess()
    /// {
    ///   [Access and use <see cref="LocalFileSystemPath"/> here]
    /// }
    /// </example>
    /// There are two exceptions to this general rule:
    /// - if the <see cref="LocalFileSystemPath"/> is only used for string operations and the resource behind it is actually not accessed
    ///   (such as e.g. if the string is parsed for a folder name to match a movie name); and
    /// - if the actual access to the resource happens in another (external) process
    ///   (in which case the external process must be started via <see cref="IImpersonationService.ExecuteWithResourceAccessAsync"/>).
    /// In both cases please add a comment why calling <see cref="EnsureLocalFileSystemAccess"/> is not necessary.
    /// This method may return <c>null</c>. Make sure you check for <c>null</c> when calling Dispose() manually or better use it
    /// in a using-block, which automatically checks for <c>null</c>
    /// </remarks>
    IDisposable EnsureLocalFileSystemAccess();
  }
}