#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using MediaPortal.Core.General;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Encapsulates the data needed to locate a specific media item.
  /// </summary>
  /// <remarks>
  /// To locate a media item, we basically need its <see cref="NativeSystem"/> and its <see cref="NativeResourcePath"/> for
  /// its native system. This pair of data identifies a media item uniquely in an MP-II system.
  /// </remarks>
  public interface IResourceLocator
  {
    /// <summary>
    /// Gets the system where the media item is located originally.
    /// </summary>
    SystemName NativeSystem { get; }

    /// <summary>
    /// Gets the resource path in the <see cref="NativeSystem"/> of the media item. This path must be evaluated
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
    /// Creates a resource accessor which is able to provide a path in the local filesystem.
    /// This is necessary for some players to be able to play the media item content.
    /// </summary>
    /// <remarks>
    /// The returned instance implements <see cref="IDisposable"/> and
    /// must be disposed after usage.
    /// The usage of a construct like this is strongly recommended:
    /// <code>
    ///   IResourceLocator locator = ...;
    ///   using (ILocalFsResourceAccessor accessor = locator.CreateLocalFsAccessor())
    ///   {
    ///     ...
    ///   }
    /// </code>
    /// </remarks>
    /// <returns>Resource accessor to a local filesystem resource containing the contents of the media item
    /// specified by this instance.</returns>
    ILocalFsResourceAccessor CreateLocalFsAccessor();
  }
}