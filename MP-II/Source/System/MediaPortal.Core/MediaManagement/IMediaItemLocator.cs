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

using System;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement.MediaProviders;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Encapsulates the data needed to locate a specific media item.
  /// To locate a media item, we basically need its <see cref="SystemName"/>, the <see cref="Guid"/> of its
  /// <see cref="IMediaProvider"/> and its path into the media provider. This triple of data identifies a media item
  /// uniquely in an MP-II system.
  /// But to access a media item, a local endpoint is needed, so for accessing it, an <see cref="IMediaItemAccessor"/> can
  /// be built from this item locator.
  /// </summary>
  public interface IMediaItemLocator
  {
    /// <summary>
    /// Gets the system where the media item is located originally.
    /// </summary>
    SystemName SystemName { get; }

    /// <summary>
    /// Gets the id of the media provider which provides access to the original media item. The media provider specified
    /// by this id is installed in the media portal instance which is specified by the <see cref="SystemName"/>.
    /// </summary>
    Guid MediaProviderId { get; }

    /// <summary>
    /// Gets the path to the media provider to get the original media item.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Creates a temporary media item accessor.
    /// </summary>
    /// <remarks>
    /// The returned instance implements <see cref="IDisposable"/> and
    /// must be disposed after usage.
    /// The usage of a construct like this is strongly recommended:
    /// <code>
    ///   IMediaItemLocator locator = ...;
    ///   using (IMediaItemAccessor accessor = locator.CreateAccessor())
    ///   {
    ///     ...
    ///   }
    /// </code>
    /// </remarks>
    /// <returns>Media item accessor to the media item specified by this instance.</returns>
    IMediaItemAccessor CreateAccessor();

    /// <summary>
    /// Creates a media item accessor which is able to provide a path in the local filesystem.
    /// This is necessary for some players to be able to play the media item content.
    /// </summary>
    /// <remarks>
    /// The returned instance implements <see cref="IDisposable"/> and
    /// must be disposed after usage.
    /// The usage of a construct like this is strongly recommended:
    /// <code>
    ///   IMediaItemLocator locator = ...;
    ///   using (IMediaItemLocalFsAccessor accessor = locator.CreateLocalFsAccessor())
    ///   {
    ///     ...
    ///   }
    /// </code>
    /// </remarks>
    /// <returns>Media item accessor to a local filesystem resource containing the contents of the media item
    /// specified by this instance.</returns>
    IMediaItemLocalFsAccessor CreateLocalFsAccessor();
  }
}