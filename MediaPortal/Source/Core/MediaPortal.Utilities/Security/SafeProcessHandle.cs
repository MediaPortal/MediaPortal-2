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
  /// Helper class to safely open and store a process handle and close it if this class is diposed
  /// </summary>
  public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
  {
    [Flags]
    public enum ProcessAccessFlags : uint
    {
      Terminate = 0x00000001,
      CreateThread = 0x00000002,
      VirtualMemoryOperation = 0x00000008,
      VirtualMemoryRead = 0x00000010,
      VirtualMemoryWrite = 0x00000020,
      DuplicateHandle = 0x00000040,
      CreateProcess = 0x00000080,
      SetQuota = 0x00000100,
      SetInformation = 0x00000200,
      QueryInformation = 0x00000400,
      QueryLimitedInformation = 0x00001000,
      Synchronize = 0x00100000,
      All = Terminate |
            CreateThread |
            VirtualMemoryOperation |
            VirtualMemoryRead |
            VirtualMemoryWrite |
            DuplicateHandle |
            CreateProcess |
            SetQuota |
            SetInformation |
            QueryInformation |
            QueryLimitedInformation |
            Synchronize,
    }

    public SafeProcessHandle()
      : base(true)
    {
    }

    public void InitialSetHandle(IntPtr h)
    {
      if(!IsInvalid)
        throw new IllegalCallException("Safe handle can only be set once");
      handle = h;
    }
    
    /// <summary>
    /// Opens the handle of a process
    /// </summary>
    /// <param name="dwDesiredAccess">Requested access flag(s)</param>
    /// <param name="bInheritHandle">If <c>true</c>, processes created by this process inherit the handle; otherwise they don't</param>
    /// <param name="dwProcessId">Process ID</param>
    /// <returns>Handle of the process</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern SafeProcessHandle OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

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
