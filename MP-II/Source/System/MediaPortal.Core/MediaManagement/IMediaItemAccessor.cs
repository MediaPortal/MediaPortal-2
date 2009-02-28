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
using System.IO;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Temporary local accessor instance for a media item which might located anywhere in an MP-II system.
  /// Via this instance, the media item, which potentially is located in a remote system, can be accessed
  /// via a local media provider specified by the <see cref="LocalMediaProviderId"/>.
  /// To get a media item accessor, get an <see cref="IMediaItemAccessor"/> and use its
  /// <see cref="IMediaItemLocator.CreateAccessor"/> method.
  /// The temporary media item accessor must be disposed using its <see cref="IDisposable.Dispose"/> method
  /// when it is not needed any more.
  /// </summary>
  public interface IMediaItemAccessor : IDisposable
  {
    /// <summary>
    /// Returns the id of the local media provider which provides access to the represented media item.
    /// </summary>
    Guid LocalMediaProviderId { get; }

    /// <summary>
    /// Returns a path in the local media provider which points to the represented media item.
    /// </summary>
    string LocalMediaProviderPath { get; }

    /// <summary>
    /// Opens a stream to read the represented media item in the local provider.
    /// </summary>
    /// <returns>Stream opened for read operations.</returns>
    Stream OpenRead();

    /// <summary>
    /// Opens a stream to write to the represented media item in the local provider.
    /// </summary>
    /// <returns>Stream opened for write operations.</returns>
    Stream OpenWrite();
  }
}