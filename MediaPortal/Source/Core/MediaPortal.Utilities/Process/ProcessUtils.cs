#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace MediaPortal.Utilities.Process
{
  public class ProcessUtils
  {
    private static readonly Encoding CONSOLE_ENCODING = Encoding.UTF8;
    private static readonly string CONSOLE_ENCODING_PREAMBLE = CONSOLE_ENCODING.GetString(CONSOLE_ENCODING.GetPreamble());

    private const int INFINITE = -1;

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion. If the process doesn't end in 
    /// this time, it gets aborted.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns><c>true</c> if process was executed and finished correctly</returns>
    public static bool TryExecute(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = INFINITE)
    {
      string unused;
      return TryExecute(executable, arguments, false, out unused, priorityClass, maxWaitMs);
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
    public static bool TryExecute_AutoImpersonate(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = INFINITE)
    {
      return IsImpersonated ?
        TryExecute_Impersonated(executable, arguments, priorityClass, maxWaitMs) :
        TryExecute(executable, arguments, priorityClass, maxWaitMs);
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
      if (!ImpersonationHelper.GetTokenByProcess(out userToken, true))
        return false;
      try
      {
        string unused;
        return TryExecute_Impersonated(executable, arguments, userToken, false, out unused, priorityClass, maxWaitMs);
      }
      finally
      {
        ImpersonationHelper.SafeCloseHandle(userToken);
      }
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
      return TryExecute(executable, arguments, true, out result, priorityClass, maxWaitMs);
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
    public static bool TryExecuteReadString_AutoImpersonate(string executable, string arguments, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = 1000)
    {
      return IsImpersonated ?
        TryExecuteReadString_Impersonated(executable, arguments, out result, priorityClass, maxWaitMs) :
        TryExecuteReadString(executable, arguments, out result, priorityClass, maxWaitMs);
    }

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion and returns the contents of
    /// <see cref="Process.StandardOutput"/>. If the process doesn't end in this time, it gets aborted. 
    /// This method tries to impersonate the interactive user and run the process under its identity.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="result">Returns the contents of standard output</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns><c>true</c> if process was executed and finished correctly</returns>
    public static bool TryExecuteReadString_Impersonated(string executable, string arguments, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = INFINITE)
    {
      IntPtr userToken;
      if (!ImpersonationHelper.GetTokenByProcess(out userToken, true))
      {
        result = null;
        return false;
      }
      try
      {
        return TryExecute_Impersonated(executable, arguments, userToken, true, out result, priorityClass, maxWaitMs);
      }
      finally
      {
        ImpersonationHelper.SafeCloseHandle(userToken);
      }
    }

    #region Private methods

    /// <summary>
    /// Indicates if the current <see cref="WindowsIdentity"/> uses impersonation.
    /// </summary>
    private static bool IsImpersonated
    {
      get
      {
        WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
        return windowsIdentity != null && windowsIdentity.ImpersonationLevel == TokenImpersonationLevel.Impersonation;
      }
    }

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion and returns the contents of
    /// <see cref="Process.StandardOutput"/>. If the process doesn't end in this time, it gets aborted.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="redirectInputOutput"><c>true</c> to redirect standard streams.</param>
    /// <param name="result">Returns the contents of standard output</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns></returns>
    private static bool TryExecute(string executable, string arguments, bool redirectInputOutput, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = 1000)
    {
      StringBuilder outputBuilder = new StringBuilder();
      using (System.Diagnostics.Process process = new System.Diagnostics.Process())
      using (AutoResetEvent outputWaitHandle = new AutoResetEvent(!redirectInputOutput))
      {
        PrepareProcess(executable, arguments, redirectInputOutput, process, outputWaitHandle, outputBuilder);

        process.Start();
        process.PriorityClass = priorityClass;

        if (redirectInputOutput)
          process.BeginOutputReadLine();

        if (process.WaitForExit(maxWaitMs) && outputWaitHandle.WaitOne(maxWaitMs))
        {
          result = RemoveEncodingPreamble(outputBuilder.ToString());
          return process.ExitCode == 0;
        }
        if (!process.HasExited)
          process.Kill();
      }
      result = null;
      return false;
    }

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion. If the process doesn't end in 
    /// this time, it gets aborted. This method tries to impersonate the interactive user and run the process under its identity.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="token">User token to run process</param>
    /// <param name="redirectInputOutput"><c>true</c> to redirect standard streams.</param>
    /// <param name="result">Returns the contents of standard output.</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns><c>true</c> if process was executed and finished correctly</returns>
    private static bool TryExecute_Impersonated(string executable, string arguments, IntPtr token, bool redirectInputOutput, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = INFINITE)
    {
      // Note: Althought the code is nearly identical as TryExecute, it cannot be easily refactored, as the ImpersonationProcess implements many methods and properties with "new".
      // If such an instance is assigned to "Process" base class, any access will fail here. 
      StringBuilder outputBuilder = new StringBuilder();
      using (ImpersonationProcess process = new ImpersonationProcess())
      using (AutoResetEvent outputWaitHandle = new AutoResetEvent(!redirectInputOutput))
      {
        PrepareProcess(executable, arguments, redirectInputOutput, process, outputWaitHandle, outputBuilder);
        process.StartAsUser(token);
        process.PriorityClass = priorityClass;

        if (redirectInputOutput)
          process.BeginOutputReadLine();

        if (process.WaitForExit(maxWaitMs) && outputWaitHandle.WaitOne(maxWaitMs))
        {
          if (redirectInputOutput)
            process.CancelOutputRead();
          result = RemoveEncodingPreamble(outputBuilder.ToString());
          return process.ExitCode == 0;
        }
        if (!process.HasExited)
        {
          if (redirectInputOutput)
            process.CancelOutputRead();
          process.Kill();
        }
      }
      result = null;
      return false;
    }

    private static void PrepareProcess(string executable, string arguments, bool redirectInputOutput, System.Diagnostics.Process process, AutoResetEvent outputWaitHandle, StringBuilder outputBuilder)
    {
      process.StartInfo = new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = redirectInputOutput };
      if (!redirectInputOutput)
        return;

      // Set UTF-8 encoding for standard output.
      process.StartInfo.StandardOutputEncoding = CONSOLE_ENCODING;
      // Enable raising events because Process does not raise events by default.
      process.EnableRaisingEvents = true;
      // Attach the event handler for OutputDataReceived before starting the process.
      process.OutputDataReceived += (sender, e) =>
      {
        try
        {
          if (e.Data == null)
            outputWaitHandle.Set();
          else
            outputBuilder.Append(e.Data);
        }
        // Avoid any exceptions in async calls, they lead to immediate crash.
        catch { }
      };
    }

    /// <summary>
    /// Helper method to remove an existing encoding preamble (<see cref="Encoding.GetPreamble"/>) from the given <paramref name="rawString"/>.
    /// </summary>
    /// <param name="rawString">Raw string that might include the preamble (BOM).</param>
    /// <returns>String without preamble.</returns>
    private static string RemoveEncodingPreamble(string rawString)
    {
      if (!string.IsNullOrWhiteSpace(rawString) && rawString.StartsWith(CONSOLE_ENCODING_PREAMBLE))
        return rawString.Substring(CONSOLE_ENCODING_PREAMBLE.Length);
      return rawString;
    }

    #endregion
  }
}
