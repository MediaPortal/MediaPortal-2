#region Copyright (C) 2012-2013 MPExtended modified by Team MediaPortal
// Copyright (C) 2012-2013 MPExtended Developers, http://www.mpextended.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using MediaPortal.Common.Logging;
using MediaPortal.Utilities.Process;
using MediaPortal.Utilities.Security;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MediaPortal.Common.Services.ResourceAccess.ImpersonationService
{
  /// <summary>
  /// Represents a process that can be run asynchronously and impersonated
  /// </summary>
  /// <remarks>
  /// This class derives from the <see cref="Process"/> class. It starts a new process with <see cref="CreateProcessAsUserW"/>
  /// and "smuggles" the <see cref="_processHandle"/> and the <see cref="_processId"/> of the so created process into the
  /// respective fields of the <see cref="Process"/> base class. Some methods of the base class are hidden by new methods.
  /// It is therefore not recommended to cast this class to <see cref="Process"/> because then the respective base class
  /// methods are called instead of the new ones.
  /// </remarks>
  internal class AsyncImpersonationProcess : Process, IProcess
  {
    #region External methods and related consts, structs, classes and enums

    #region TerminateProcess / GetExitCodeProcess

    private const uint EXITCODE_OK = 0;
    private const uint EXITCODE_STILL_ACTIVE = 259;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(SafeProcessHandle hProcess, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(SafeProcessHandle hProcess, out uint lpExitCode);

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

    /// <summary>
    /// Opens the handle of a process
    /// </summary>
    /// <param name="dwDesiredAccess">Requested access flag(s)</param>
    /// <param name="bInheritHandle">If <c>true</c>, processes created by this process inherit the handle; otherwise they don't</param>
    /// <param name="dwProcessId">Process ID</param>
    /// <returns>Handle of the process</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern SafeProcessHandle OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    #endregion

    #region GetPriorityClass / SetPriorityClass

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ProcessPriorityClass GetPriorityClass(SafeProcessHandle handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetPriorityClass(SafeProcessHandle handle, ProcessPriorityClass priorityClass);

    #endregion

    #region CreateProcessAsUserW

    [Flags]
    protected enum CreateProcessFlags : uint
    {
      None = 0,
      CreateBreakawayFromJob = 0x01000000,
      CreateDefaultErrorMode = 0x04000000,
      CreateNewConsole = 0x00000010,
      CreateNewProcessGroup = 0x00000200,
      CreateNoWindow = 0x08000000,
      CreateProtectedProcess = 0x00040000,
      CreatePreserveCodeAuthzLevel = 0x02000000,
      CreateSeparateWowVdm = 0x00000800,
      CreateSharedWowVdm = 0x00001000,
      CreateSuspended = 0x00000004,
      CreateUnicodeEnvironment = 0x00000400,
      DebugOnlyThisProcess = 0x00000002,
      DebugProcess = 0x00000001,
      DetachedProcess = 0x00000008,
      ExtendedStartupInfoPresent = 0x00080000,
      InheritParentAffinity = 0x00010000
    }

    [StructLayout(LayoutKind.Sequential)]
    protected class StartupInfo
    {
      public int cb;
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
      public StartFlags dwFlags;
      public ShowWindow wShowWindow;
      public ushort cbReserved2;
      public IntPtr lpReserved2;
      public SafeFileHandle hStdInput;
      public SafeFileHandle hStdOutput;
      public SafeFileHandle hStdError;

      public StartupInfo()
      {
        cb = Marshal.SizeOf(typeof(StartupInfo));
        hStdInput = new SafeFileHandle(IntPtr.Zero, false);
        hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
        hStdError = new SafeFileHandle(IntPtr.Zero, false);
      }
    }

    [Flags]
    protected enum StartFlags : uint
    {
      ForceOnFeedback = 0x00000040,
      ForceOffFeedback = 0x00000080,
      PreventPinning = 0x00002000,
      RunFullscreen = 0x00000020,
      TitleIsAppId = 0x00001000,
      TitleIsLinkName = 0x00000800,
      UseCountChars = 0x00000008,
      UseFillAttribute = 0x00000010,
      UseHotKey = 0x00000200,
      UsePosition = 0x00000004,
      UseShowWindow = 0x00000001,
      UseSize = 0x00000002,
      UseStdHandles = 0x00000100
    }

    protected enum ShowWindow : ushort
    {
      Hide = 0,
      Maximize = 3,
      Show = 5,
      Minimize = 6
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct ProcessInformation
    {
      public IntPtr hProcess;
      public IntPtr hThread;
      public int dwProcessId;
      public int dwThreadId;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcessAsUserW(IntPtr token, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, CreateProcessFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] StartupInfo lpStartupInfo, out ProcessInformation lpProcessInformation);

    #endregion

    #region CreatePipe

    protected const int DEFAULT_PIPE_BUFFER_SIZE = 4096;

    [StructLayout(LayoutKind.Sequential)]
    public class SecurityAttributes
    {
      public uint nLength;
      public IntPtr lpSecurityDescriptor;
      public bool bInheritHandle;
      public SecurityAttributes()
      {
        nLength = (uint)Marshal.SizeOf(typeof(SecurityAttributes));
        lpSecurityDescriptor = IntPtr.Zero;
      }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, SecurityAttributes lpPipeAttributes, int nSize);

    #endregion

    #region SetHandleInformation

    [Flags]
    protected enum HandleFlags : uint
    {
      None = 0,
      Inherit = 1,
      ProtectFromClose = 2
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetHandleInformation(SafeFileHandle hObject, HandleFlags dwMask, HandleFlags dwFlags);

    #endregion

    #region GetStdHandle

    protected enum StdHandle
    {
      Input = -10,
      Output = -11,
      Error = -12
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle GetStdHandle(StdHandle nStdHandle);

    #endregion

    #endregion

    #region Private fields

    private SafeFileHandle _stdinReadHandle;
    private SafeFileHandle _stdinWriteHandle;
    private SafeFileHandle _stdoutWriteHandle;
    private SafeFileHandle _stderrWriteHandle;
    private SafeFileHandle _stdoutReadHandle;
    private SafeFileHandle _stderrReadHandle;
    private SafeProcessHandle _processHandle;
    private int _processId;
    private readonly WindowsIdentityWrapper _idWrapper;
    private readonly ILogger _debugLogger;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new instance of this class
    /// </summary>    /// 
    /// <param name="idWrapper">The idenity to use when starting the process.</param>
    /// <param name="debugLogger">Debug logger used for debug output</param>
    private AsyncImpersonationProcess(WindowsIdentityWrapper idWrapper, ILogger debugLogger)
    {
      _idWrapper = idWrapper;
      _debugLogger = debugLogger;
    }

    #endregion

    #region Internal static methods

    /// <summary>
    /// Creates an instance of <see cref="IProcess"/> that uses a <see cref="AsyncImpersonationProcess"/> as the underlying type
    /// and that will start under the specified windows identity. 
    /// </summary>
    /// <remarks>
    /// This method will only create the process class, and will not start or otherwise modify the process.
    /// </remarks>
    /// <param name="startInfo"><see cref="ProcessStartInfo"/> to create the process with.</param>
    /// <param name="idWrapper">The idenity to use when starting the process.</param>
    /// <param name="debugLogger">Debug logger used for debug output</param>
    /// <returns>Implementation of <see cref="IProcess"/> that can be started and managed by the caller.</returns>
    internal static IProcess Create(ProcessStartInfo startInfo, WindowsIdentityWrapper idWrapper, ILogger debugLogger)
    {
      return new AsyncImpersonationProcess(idWrapper, debugLogger)
      {
        StartInfo = startInfo
      };
    }

    #endregion

    #region Base hides and overrides

    public new bool Start()
    {
      using (var tokenWrapper = _idWrapper.TokenWrapper)
        return StartAsUser(tokenWrapper.Token);
    }

    public new void Kill()
    {
      using (var hProcess = OpenProcess(AsyncImpersonationProcess.ProcessAccessFlags.Terminate, false, Id))
      {
        if (hProcess.IsInvalid)
        {
          var error = Marshal.GetLastWin32Error();
          _debugLogger.Error("AsyncImpersonationProcess ({0}): Cannot kill proces; OpenProcess failed. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
          return;
        }
        if (!TerminateProcess(hProcess, EXITCODE_OK))
        {
          var error = Marshal.GetLastWin32Error();
          _debugLogger.Error("AsyncImpersonationProcess ({0}): Cannot kill proces; TerminateProcess failed. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
        }
      }
    }

    public new ProcessPriorityClass PriorityClass
    {
      get
      {
        var result = GetPriorityClass(_processHandle);
        if (result != 0)
        {
          var error = Marshal.GetLastWin32Error();
          _debugLogger.Error("AsyncImpersonationProcess ({0}): GetPriorityClass failed. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
        }
        return result;
      }
      set
      {
        if (!SetPriorityClass(_processHandle, value))
        {
          var error = Marshal.GetLastWin32Error();
          _debugLogger.Error("AsyncImpersonationProcess ({0}): SetPriorityClass failed. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
        }
      }
    }

    public new int ExitCode
    {
      get
      {
        uint exitCode;
        if (GetExitCodeProcess(_processHandle, out exitCode))
          return (int) exitCode;
        var error = Marshal.GetLastWin32Error();
        _debugLogger.Error("AsyncImpersonationProcess ({0}): GetExitCodeProcess failed. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
        return -1;
      }
    }

    public new bool HasExited
    {
      get
      {
        if (_processHandle.IsClosed)
          return true;

        uint exitCode;
        if (!GetExitCodeProcess(_processHandle, out exitCode))
        {
          var error = Marshal.GetLastWin32Error();
          _debugLogger.Error("AsyncImpersonationProcess ({0}): GetExitCodeProcess failed in HasExited. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
          return false;
        }
        return exitCode != EXITCODE_STILL_ACTIVE;
      }
    }

    protected override void Dispose(bool disposing)
    {
      _stdinReadHandle.Dispose();
      _stdinWriteHandle.Dispose();
      _stdoutReadHandle.Dispose();
      _stdoutWriteHandle.Dispose();
      _stderrReadHandle.Dispose();
      _stderrWriteHandle.Dispose();
      if (_processHandle != null)
        _processHandle.Dispose();
      base.Dispose(disposing);
    }

    #endregion

    #region Private methods

    private bool StartAsUser(IntPtr userToken)
    {
      var startupInfo = new StartupInfo();
      switch (StartInfo.WindowStyle)
      {
        case ProcessWindowStyle.Hidden:
          startupInfo.wShowWindow = ShowWindow.Hide;
          break;
        case ProcessWindowStyle.Maximized:
          startupInfo.wShowWindow = ShowWindow.Maximize;
          break;
        case ProcessWindowStyle.Minimized:
          startupInfo.wShowWindow = ShowWindow.Minimize;
          break;
        case ProcessWindowStyle.Normal:
          startupInfo.wShowWindow = ShowWindow.Show;
          break;
      }

      CreateStandardPipe(out _stdinReadHandle, out _stdinWriteHandle, StdHandle.Input, true, StartInfo.RedirectStandardInput);
      CreateStandardPipe(out _stdoutReadHandle, out _stdoutWriteHandle, StdHandle.Output, false, StartInfo.RedirectStandardOutput);
      CreateStandardPipe(out _stderrReadHandle, out _stderrWriteHandle, StdHandle.Error, false, StartInfo.RedirectStandardError);

      startupInfo.dwFlags = StartFlags.UseStdHandles | StartFlags.UseShowWindow;
      startupInfo.hStdInput = _stdinReadHandle;
      startupInfo.hStdOutput = _stdoutWriteHandle;
      startupInfo.hStdError = _stderrWriteHandle;

      var createFlags = CreateProcessFlags.CreateNewConsole | CreateProcessFlags.CreateNewProcessGroup | CreateProcessFlags.CreateDefaultErrorMode;
      if (StartInfo.CreateNoWindow)
      {
        startupInfo.wShowWindow = ShowWindow.Hide;
        createFlags |= CreateProcessFlags.CreateNoWindow;
      }

      if (!SafeCreateProcessAsUserW(userToken, createFlags, startupInfo))
        return false;

      if (StartInfo.RedirectStandardInput)
      {
        _stdinReadHandle.Dispose();
        var standardInput = new StreamWriter(new FileStream(_stdinWriteHandle, FileAccess.Write, DEFAULT_PIPE_BUFFER_SIZE), Console.Out.Encoding) { AutoFlush = true };
#if NET5_0_OR_GREATER
        SetField("_standardInput", standardInput);
#else
        SetField("standardInput", standardInput);
#endif
      }

      if (StartInfo.RedirectStandardOutput)
      {
        _stdoutWriteHandle.Dispose();
        var standardOutput = new StreamReader(new FileStream(_stdoutReadHandle, FileAccess.Read, DEFAULT_PIPE_BUFFER_SIZE), StartInfo.StandardOutputEncoding);
#if NET5_0_OR_GREATER
        SetField("_standardOutput", standardOutput);
#else
        SetField("standardOutput", standardOutput);
#endif
      }

      if (StartInfo.RedirectStandardError)
      {
        _stderrWriteHandle.Dispose();
        var standardError = new StreamReader(new FileStream(_stderrReadHandle, FileAccess.Read, DEFAULT_PIPE_BUFFER_SIZE), StartInfo.StandardErrorEncoding);
#if NET5_0_OR_GREATER
        SetField("_standardError", standardError);
#else
        SetField("standardError", standardError);
#endif
      }

      // Workaround to get process handle as Microsoft.Win32.SafeHandles.SafeProcessHandle.
      // This class is internal and therefore not accessible for us (and it is different
      // from MediaPortal.Utilities.Security.SafeProcessHandle), which is why we need to
      // use reflection to obtain it as an object. This object is then stored in the base class.
      var processAssembly = typeof(Process).Assembly;
      var processManager = processAssembly.GetType("System.Diagnostics.ProcessManager");
      var safeProcessHandle = processManager.InvokeMember("OpenProcess", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, this, new object[] { _processId, 0x100000, false });
      InvokeMethod("SetProcessHandle", safeProcessHandle);
      InvokeMethod("SetProcessId", _processId);
      return true;
    }

    /// <summary>
    /// Calls <see cref="CreateProcessAsUserW"/> and safely stores the obtained handles.
    /// </summary>
    /// <param name="userToken">Token to impersonate the external process</param>
    /// <param name="createFlags">Flags used to create the external process</param>
    /// <param name="startupInfo">Startup information used to create the external process</param>
    /// <returns><c>true</c> if the call to <see cref="CreateProcessAsUserW"/> was successful; otherwise <c>false</c></returns>
    private bool SafeCreateProcessAsUserW(IntPtr userToken, CreateProcessFlags createFlags, StartupInfo startupInfo)
    {
      var threadHandle = new SafeThreadHandle();
      bool success;

      // The following is necessary to make sure that processInformation.hProcess and processInformation.hThread
      // are safely stored in the respective SafeHandle classes. It is, unfortunately, not possible to define
      // processInformation.hProcess and processInformation.hThread as SafeHandles and use processInformation
      // as an out parameter, because the unmanaged code is not able to create these objects. We therefore use
      // IntPtr and ensure in the following that the IntPtrs are stored in SafeHandle objects immediately after
      // they have been obtained.
      // For details see here: https://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.runtimehelpers.prepareconstrainedregions(v=vs.110).aspx
      RuntimeHelpers.PrepareConstrainedRegions();
      try { }
      finally
      {
        ProcessInformation processInformation;
        success = CreateProcessAsUserW(userToken, null, GetCommandLine(), IntPtr.Zero, IntPtr.Zero, true, createFlags, IntPtr.Zero, null, startupInfo, out processInformation);
        if (success)
        {
          _processHandle = new SafeProcessHandle(processInformation.hProcess, true);
          threadHandle.InitialSetHandle(processInformation.hThread);
          _processId = processInformation.dwProcessId;
        }
      }
      
      // We don't need the threadHandle and therefore immediately dispose it.
      threadHandle.Dispose();

      if (success)
        return true;

      if (_processHandle != null)
        _processHandle.Dispose();
      var error = Marshal.GetLastWin32Error();
      _debugLogger.Error("AsyncImpersonationProcess ({0}): Cannot start process. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
      return false;
    }

    private void CreateStandardPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle, StdHandle standardHandle, bool isInput, bool redirect)
    {
      if (redirect)
      {
        var security = new SecurityAttributes { bInheritHandle = true };
        if (!CreatePipe(out readHandle, out writeHandle, security, DEFAULT_PIPE_BUFFER_SIZE))
        {
          var error = Marshal.GetLastWin32Error();
          _debugLogger.Error("AsyncImpersonationProcess ({0}): CreatePipe failed. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
          return;
        }
        if (!SetHandleInformation(isInput ? writeHandle : readHandle, HandleFlags.Inherit, HandleFlags.None))
        {
          var error = Marshal.GetLastWin32Error();
          _debugLogger.Error("AsyncImpersonationProcess ({0}): SetHandleInformation failed. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
        }
      }
      else
      {
        if (isInput)
        {
          writeHandle = new SafeFileHandle(IntPtr.Zero, false);
          readHandle = GetStdHandle(standardHandle);
          var error = Marshal.GetLastWin32Error();
          // Error code 1008 means "An attempt was made to reference a token that does not exist". This is the case
          // in particular in the server process because a Windows service does not have a standard output or standard error stream.
          // This is expected behaviour - not an error.
          if (error != 0 && error != 1008)
            _debugLogger.Error("AsyncImpersonationProcess ({0}): GetStdHandle input failed. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
        }
        else
        {
          readHandle = new SafeFileHandle(IntPtr.Zero, false);
          writeHandle = GetStdHandle(standardHandle);
          var error = Marshal.GetLastWin32Error();
          // Error code 1008 means "An attempt was made to reference a token that does not exist". This is the case
          // in particular in the server process because a Windows service does not have a standard output or standard error stream.
          // This is expected behaviour - not an error.
          if (error != 0 && error != 1008)
            _debugLogger.Error("AsyncImpersonationProcess ({0}): GetStdHandle output failed. ErrorCode: {1} ({2})", StartInfo.FileName, error, new Win32Exception(error).Message);
        }
      }
    }

    private string GetCommandLine()
    {
      var result = new StringBuilder();
      var applicationName = StartInfo.FileName.Trim();
      var arguments = StartInfo.Arguments;

      var applicationNameIsQuoted = applicationName.StartsWith("\"") && applicationName.EndsWith("\"");
      if (!applicationNameIsQuoted)
        result.Append("\"");
      result.Append(applicationName);
      if (!applicationNameIsQuoted)
        result.Append("\"");

      if (arguments.Length > 0)
      {
        result.Append(" ");
        result.Append(arguments);
      }

      return result.ToString();
    }

    private void InvokeMethod(string member, params object[] args)
    {
      typeof(Process).InvokeMember(member, BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, this, args);
    }

    private void SetField(string member, params object[] args)
    {
      typeof(Process).InvokeMember(member, BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance, null, this, args);
    }

#endregion
  }
}
