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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.Dokan
{
  /// <summary>
  /// Defines result status codes for Dokan operations.
  /// </summary>
  internal static class DokanResult
  {
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    public const NtStatus Success = NtStatus.Success;

    /// <summary>
    /// Incorrect function.
    /// </summary>
    public const NtStatus Error = NtStatus.Error;

    /// <summary>
    /// The system cannot find the file specified.
    /// </summary>
    public const NtStatus FileNotFound = NtStatus.ObjectNameNotFound;

    /// <summary>
    /// The system cannot find the path specified.
    /// </summary>
    public const NtStatus PathNotFound = NtStatus.ObjectPathNotFound;

    /// <summary>
    /// Access is denied.
    /// </summary>
    public const NtStatus AccessDenied = NtStatus.AccessDenied;

    /// <summary>
    /// The handle is invalid.
    /// </summary>
    public const NtStatus InvalidHandle = NtStatus.InvalidHandle;

    /// <summary>
    /// The device is not ready.
    /// </summary>
    public const NtStatus NotReady = NtStatus.DeviceBusy;

    /// <summary>
    /// The process cannot access the file because it is being used by another process.
    /// </summary>
    public const NtStatus SharingViolation = NtStatus.SharingViolation;

    /// <summary>
    /// The file exists.
    /// </summary>
    public const NtStatus FileExists = NtStatus.ObjectNameCollision;

    /// <summary>
    /// There is not enough space on the disk.
    /// </summary>
    public const NtStatus DiskFull = NtStatus.DiskFull;

    /// <summary>
    /// This function is not supported on this system.
    /// </summary>
    public const NtStatus NotImplemented = NtStatus.NotImplemented;

    /// <summary>
    /// The data area passed to a system call is too small.
    /// </summary>
    public const NtStatus BufferTooSmall = NtStatus.BufferTooSmall;

    /// <summary>
    /// The data area passed to a system call is too small.
    /// </summary>
    public const NtStatus BufferOverflow = NtStatus.BufferOverflow;

    /// <summary>
    /// The filename, directory name, or volume label syntax is incorrect.
    /// </summary>
    public const NtStatus InvalidName = NtStatus.ObjectNameInvalid;

    /// <summary>
    /// The directory is not empty.
    /// </summary>
    public const NtStatus DirectoryNotEmpty = NtStatus.DirectoryNotEmpty;

    /// <summary>
    /// Cannot create a file when that file already exists.
    /// </summary>
    public const NtStatus AlreadyExists = NtStatus.ObjectNameCollision;

    /// <summary>
    /// An exception occurred in the service when handling the control request.
    /// </summary>
    public const NtStatus InternalError = NtStatus.InternalError;

    /// <summary>
    /// A required privilege is not held by the client.
    /// </summary>
    public const NtStatus PrivilegeNotHeld = NtStatus.PrivilegeNotHeld;

    /// <summary>
    /// The requested operation was unsuccessful.
    /// </summary>
    public const NtStatus Unsuccessful = NtStatus.Unsuccessful;

    /// <summary>
    /// The parameter is incorrect.
    /// </summary>
    public const NtStatus InvalidParameter = NtStatus.InvalidParameter;
  }
}
