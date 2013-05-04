#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace MediaPortal.Utilities.Process
{
  public class ProcessUtils
  {
    #region Imports and consts

    // ReSharper disable InconsistentNaming
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
      public IntPtr hProcess;
      public IntPtr hThread;
      public uint dwProcessId;
      public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
      public uint nLength;
      public IntPtr lpSecurityDescriptor;
      public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STARTUPINFO
    {
      public uint cb;
      public string lpReserved;
      public string lpDesktop;
      public string lpTitle;
      public uint dwX;
      public uint dwY;
      public uint dwXSize;
      public uint dwYSize;
      public uint dwXCountChars;
      public uint dwYCountChars;
      public uint dwFillAttribute;
      public uint dwFlags;
      public short wShowWindow;
      public short cbReserved2;
      public IntPtr lpReserved2;
      public IntPtr hStdInput;
      public IntPtr hStdOutput;
      public IntPtr hStdError;
    }

    internal enum SECURITY_IMPERSONATION_LEVEL
    {
      SecurityAnonymous,
      SecurityIdentification,
      SecurityImpersonation,
      SecurityDelegation
    }

    internal enum TOKEN_TYPE
    {
      TokenPrimary = 1,
      TokenImpersonation
    }


    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool CreateProcessAsUser(
        IntPtr hToken,
        string lpApplicationName,
        string lpCommandLine,
        ref SECURITY_ATTRIBUTES lpProcessAttributes,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    private const int INFINITE = -1;

    private const short SW_HIDE = 0;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const uint CREATE_NO_WINDOW = 0x08000000;

    // ReSharper restore InconsistentNaming

    #endregion

    #region Internal classes

    class ProcessWaitHandle : WaitHandle
    {
      public ProcessWaitHandle(IntPtr processHandle)
      {
        SafeWaitHandle = new SafeWaitHandle(processHandle, false);
      }
    }

    #endregion

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion. If the process doesn't end in 
    /// this time, it gets aborted.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns><c>true</c> if process was executed and finished correctly</returns>
    public static bool TryExecute(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = 1000)
    {
      using (System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true } })
      {
        process.Start();
        process.PriorityClass = priorityClass;
        if (process.WaitForExit(maxWaitMs))
          return process.ExitCode == 0;
        if (!process.HasExited)
          process.Kill();
      }
      return false;
    }

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion. If the process doesn't end in 
    /// this time, it gets aborted. This helper method automatically decides if an impersonation should be done, depending on the current identity's 
    /// <see cref="TokenImpersonationLevel"/>.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns><c>true</c> if process was executed and finished correctly</returns>
    public static bool TryExecute_AutoImpersonate(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = 1000)
    {
      return WindowsIdentity.GetCurrent().ImpersonationLevel == TokenImpersonationLevel.Impersonation ?
        TryExecute_Impersonated(executable, arguments, priorityClass, maxWaitMs) :
        TryExecute(executable, arguments, priorityClass, maxWaitMs);
    }

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion and returns the contents of
    /// <see cref="Process.StandardOutput"/>. If the process doesn't end in this time, it gets aborted.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="result">Returns the contents of standard output</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns></returns>
    public static bool TryExecuteReadString(string executable, string arguments, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = 1000)
    {
      using (System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true } })
      {
        process.Start();
        process.PriorityClass = priorityClass;
        using (process.StandardOutput)
        {
          result = process.StandardOutput.ReadToEnd();
          if (process.WaitForExit(maxWaitMs))
            return process.ExitCode == 0;
        }
        if (!process.HasExited)
          process.Kill();
      }
      return false;
    }

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion. If the process doesn't end in 
    /// this time, it gets aborted. This method tries to impersonate the interactive user and run the process under its identity.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns><c>true</c> if process was executed and finished correctly</returns>
    public static bool TryExecute_Impersonated(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = INFINITE)
    {
      IntPtr userToken;
      if (!ImpersonationHelper.GetTokenByProcess("explorer", out userToken))
        return false;

      return TryExecute_Impersonated(executable, arguments, userToken, priorityClass, maxWaitMs);
    }

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion. If the process doesn't end in 
    /// this time, it gets aborted. This method tries to impersonate the interactive user and run the process under its identity.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="token">User token to run process</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns><c>true</c> if process was executed and finished correctly</returns>
    public static bool TryExecute_Impersonated(string executable, string arguments, IntPtr token, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = INFINITE)
    {
      string appCmdLine = executable + (!string.IsNullOrWhiteSpace(arguments) ? " " + arguments : string.Empty);
      try
      {
        PROCESS_INFORMATION pi;
        if (!TryExecute_Impersonated(appCmdLine, token, out pi))
          throw new InvalidOperationException("Failed to start process!");

        SetProcessPriority(pi.hProcess, priorityClass);

        ProcessWaitHandle waitable = new ProcessWaitHandle(pi.hProcess);
        if (waitable.WaitOne(maxWaitMs))
        {
          uint exitCode;
          return GetExitCodeProcess(pi.hProcess, out exitCode) && exitCode == 0;
        }
        else
        {
          TerminateProcess(pi.hProcess, 255);
          return false;
        }
      }
      finally
      {
        ImpersonationHelper.CloseHandle(token);
      }
    }

    #region Private methods

    private static bool TryExecute_Impersonated(string cmdLine, IntPtr token, out PROCESS_INFORMATION pi)
    {
      SECURITY_ATTRIBUTES saProcess = new SECURITY_ATTRIBUTES();
      SECURITY_ATTRIBUTES saThread = new SECURITY_ATTRIBUTES();
      saProcess.nLength = (uint) Marshal.SizeOf(saProcess);
      saThread.nLength = (uint) Marshal.SizeOf(saThread);

      STARTUPINFO si = new STARTUPINFO();
      si.cb = (uint) Marshal.SizeOf(si);

      si.lpDesktop = @"WinSta0\Default"; // Modify as needed
      si.wShowWindow = SW_HIDE;

      return CreateProcessAsUser(
        token,
        null,
        cmdLine,
        ref saProcess,
        ref saThread,
        false,
        CREATE_UNICODE_ENVIRONMENT | CREATE_NO_WINDOW,
        IntPtr.Zero,
        null,
        ref si,
        out pi);
    }

    private static bool SetProcessPriority(IntPtr processHandle, ProcessPriorityClass priority)
    {
      return SetPriorityClass(processHandle, (uint) priority); // Note: Enum values are equal to unmanaged constants.
    }

    #endregion
  }
}
