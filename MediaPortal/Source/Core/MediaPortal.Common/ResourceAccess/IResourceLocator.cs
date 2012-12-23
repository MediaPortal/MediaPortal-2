#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
  /// Encapsulates the data needed to locate a specific media item in a MediaPortal 2 network.
  /// </summary>
  /// <remarks>
  /// To locate a media item, we basically need its <see cref="NativeSystemId"/> and its <see cref="NativeResourcePath"/> for
  /// its native system. This pair of data identifies a media item uniquely in an MP 2 system.
  /// </remarks>
  public interface IResourceLocator
  {
    /// <summary>
    /// Gets the system Id of the system where the media item is located originally.
    /// </summary>
    string NativeSystemId { get; }

    /// <summary>
    /// Gets the resource path in the <see cref="NativeSystemId"/> of the media item. This path must be evaluated
    /// at the media item's native system.
    /// </summary>
    ResourcePath NativeResourcePath { get; }

    /// <summary>
    /// Creates a temporary resource accessor.
    /// </summary>
    /// <remarks>
    /// The returned instance implements <see cref="IDisposable"/> and
    /// must be disposed after usage.
    /// The usage of a construct like this is strongly recommended:
    /// <code>
    ///   IResourceLocator locator = ...;
    ///   using (IResourceAccessor accessor = locator.CreateAccessor())
    ///   {
    ///     ...
    ///   }
    /// </code>
    /// </remarks>
    /// <returns>Resource accessor to the media item specified by this instance.</returns>
    IResourceAccessor CreateAccessor();

    /// <summary>
    /// Tries to create a resource accessor which is able to provide a path in the local filesystem.
    /// This only works if the underlaying <see cref="NativeResourcePath"/> is a network path
    /// (See also <see cref="ResourcePath.IsNetworkPath"/>).
    /// </summary>
    /// <remarks>
    /// The returned instance implements <see cref="IDisposable"/> and
    /// must be disposed after usage.
    /// The usage of a construct like this is strongly recommended:
    /// <code>
    ///   IResourceLocator locator = ...;
    ///   ILocalFsResourceAccessor accessor;
    ///   if (locator.TryCreateLocalFsAccessor(out accessor))
    ///     using (accessor)
    ///     {
    ///       ...
    ///     }
    /// </code>
    /// </remarks>
    /// <param name="localFsResourceAccessor">Resource accessor to a local filesystem resource containing
    /// the contents of the resource specified by this instance.</param>
    /// <returns><c>true</c>, if this resource locator points to a file system resource which either is
    /// located in the local file system or which can be bridged to the local file system.</returns>
    bool TryCreateLocalFsAccessor(out ILocalFsResourceAccessor localFsResourceAccessor);
  }
}