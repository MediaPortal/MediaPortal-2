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
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using MediaPortal.Utilities.Exceptions;
using Microsoft.Win32.SafeHandles;

namespace MediaPortal.Utilities.Security
{
  /// <summary>
  /// Helper class to safely store a thread handle and close it if this class is diposed
  /// </summary>
  public sealed class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
  {
    public SafeThreadHandle()
      : base(true)
    {
    }

    public void InitialSetHandle(IntPtr h)
    {
      if (!IsInvalid)
        throw new IllegalCallException("Safe handle can only be set once");
      handle = h;
    }

    [DllImport("kernel32.dll")]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [SuppressUnmanagedCodeSecurity]
    private static extern bool CloseHandle(IntPtr handle);

    override protected bool ReleaseHandle()
    {
      return CloseHandle(handle);
    }
  }
}
