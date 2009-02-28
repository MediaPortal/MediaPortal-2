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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Temporary local filesystem accessor instance for a media item which might located anywhere in an MP-II system.
  /// Via this instance, the media item, which potentially is located in a remote system, can be accessed
  /// via a <see cref="LocalFileSystemPath"/>.
  /// </summary>
  /// <remarks>
  /// To get a local filesystem media item accessor, get an <see cref="IMediaItemAccessor"/> and use its
  /// <see cref="IMediaItemLocator.CreateLocalFsAccessor"/> method.
  /// The temporary local filesystem media item accessor must be disposed using its
  /// <see cref="IDisposable.Dispose"/> method when it is not needed any more.
  /// </remarks>
  public interface IMediaItemLocalFsAccessor : IDisposable
  {
    /// <summary>
    /// Gets a path in the local filesystem where the represented media item is located.
    /// </summary>
    string LocalFileSystemPath { get; }
  }
}