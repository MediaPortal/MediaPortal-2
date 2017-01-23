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
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Process
{
  public class ProcessUtils
  {
    public static readonly Encoding CONSOLE_ENCODING = Encoding.UTF8;
    [Obsolete("This field will be removed. The functionality is contained in ImpersonationService")]
    private static readonly string CONSOLE_ENCODING_PREAMBLE = CONSOLE_ENCODING.GetString(CONSOLE_ENCODING.GetPreamble());

    public const int INFINITE = -1;
    public const int DEFAULT_TIMEOUT = 10000;

    /// <summary>
    /// Executes the <paramref name="executable"/> asynchronously and waits a maximum time of <paramref name="maxWaitMs"/> for completion.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns>> <see cref="ProcessExecutionResult"/> object that respresents the result of executing the Program</returns>
    /// <remarks>
    /// This method throws an exception only if process.Start() fails (in partiular, if the <paramref name="executable"/> doesn't exist).
    /// Any other error in managed code is signaled by the returned task being set to Faulted state.
    /// If the program itself does not result in an ExitCode of 0, the returned task ends in RanToCompletion state;
    /// the ExitCode of the program will be contained in the returned <see cref="ProcessExecutionResult"/>.
    /// This method is nearly identical to ImpersonationService.ExecuteWithResourceAccessAsync; it is necessary to have this code duplicated
    /// because AsyncImpersonationProcess hides several methods of the Process class and executing these methods on the base class does
    /// therefore not work. If this method is changed it is likely that ImpersonationService.ExecuteWithResourceAccessAsync also
    /// needs to be changed.
    /// </remarks>
    public static Task<ProcessExecutionResult> ExecuteAsync(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
    {
      var tcs = new TaskCompletionSource<ProcessExecutionResult>();
      var process = new System.Diagnostics.Process
      {
        StartInfo = new ProcessStartInfo(executable, arguments)
        {
          UseShellExecute = false,
          CreateNoWindow = true,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          StandardOutputEncoding = CONSOLE_ENCODING,
          StandardErrorEncoding = CONSOLE_ENCODING
        },
        EnableRaisingEvents = true
      };

      // We need to read standardOutput and standardError asynchronously to avoid a deadlock
      // when the buffer is not big enough to receive all the respective output. Otherwise the
      // process may block because the buffer is full and the Exited event below is never raised.
      Task<string> standardOutputTask = null;
      Task<string> standardErrorTask = null;
      var standardStreamTasksReady = new ManualResetEventSlim();

      // The Exited event is raised in any case when the process has finished, i.e. when it gracefully
      // finished (ExitCode = 0), finished with an error (ExitCode != 0) and when it was killed below.
      // That ensures disposal of the process object.
      process.Exited += (sender, args) =>
      {
        try
        {
          // standardStreamTasksReady is only disposed when starting the process was not successful,
          // in which case the Exited event is never raised.
          // ReSharper disable once AccessToDisposedClosure
          standardStreamTasksReady.Wait();
          tcs.TrySetResult(new ProcessExecutionResult
          {
            ExitCode = process.ExitCode,
            // standardStreamTasksReady makes sure that we do not access the standard stream tasks before they are initialized.
            // For the same reason it is intended that these tasks (as closures) are modified (i.e. initialized).
            // We need to take this cumbersome way because it is not possible to access the standard streams before the process
            // is started. If on the other hand the Exited event is raised before the tasks are initialized, we need to make
            // sure that this method waits until the tasks are initialized before they are accessed.
            // ReSharper disable PossibleNullReferenceException
            // ReSharper disable AccessToModifiedClosure
            StandardOutput = standardOutputTask.Result,
            StandardError = standardErrorTask.Result
            // ReSharper restore AccessToModifiedClosure
            // ReSharper restore PossibleNullReferenceException
          });
        }
        catch (Exception e)
        {
          tcs.TrySetException(e);
        }
        finally
        {
          process.Dispose();
        }
      };

      process.Start();
      try
      {
        // This call may throw an exception if the process has already exited when we get here.
        // In that case the Exited event has already set tcs to RanToCompletion state so that
        // the TrySetException call below does not change the state of tcs anymore. This is correct
        // as it doesn't make sense to change the priority of the process if it is already finished.
        // Any other "real" error sets the state of tcs to Faulted below.
        process.PriorityClass = priorityClass;
      }
      catch (Exception e)
      {
        tcs.TrySetException(e);
      }

      standardOutputTask = process.StandardOutput.ReadToEndAsync();
      standardErrorTask = process.StandardError.ReadToEndAsync();
      standardStreamTasksReady.Set();

      // Here we take care of the maximum time to wait for the process if such was requested.
      if (maxWaitMs != INFINITE)
        Task.Delay(maxWaitMs).ContinueWith(task =>
        {
          try
          {
            // We only kill the process if the state of tcs was not set to Faulted or
            // RanToCompletion before.
            if (tcs.TrySetCanceled())
              process.Kill();
          }
          // ReSharper disable once EmptyGeneralCatchClause
          // An exception is thrown in process.Kill() when the external process exits
          // while we set tcs to canceled. In that case there is nothing to do anymore.
          // This is not an error.
          catch
          { }
        });
      return tcs.Task;
    }

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum time of <paramref name="maxWaitMs"/> for completion. If the process doesn't end in 
    /// this time, it gets aborted.
    /// </summary>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns><c>true</c> if process was executed and finished correctly</returns>
    [Obsolete("Use ExecuteAsync instead.")]
    public static bool TryExecute(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
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
    [Obsolete("Use IImpersonationService.ExecuteWithResourceAccessAsync instead.")]
    public static bool TryExecute_AutoImpersonate(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
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
    [Obsolete("Use IImpersonationService.ExecuteWithResourceAccessAsync instead.")]
    public static bool TryExecute_Impersonated(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
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
    [Obsolete("Use ExecuteAsync instead.")]
    public static bool TryExecuteReadString(string executable, string arguments, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
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
    [Obsolete("Use IImpersonationService.ExecuteWithResourceAccessAsync instead.")]
    public static bool TryExecuteReadString_AutoImpersonate(string executable, string arguments, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
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
    [Obsolete("Use IImpersonationService.ExecuteWithResourceAccessAsync instead.")]
    public static bool TryExecuteReadString_Impersonated(string executable, string arguments, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
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
    [Obsolete("This method will be removed. The functionality is contained in ImpersonationService")]
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
    [Obsolete("This method will be removed. The functionality is contained in ImpersonationService")]
    private static bool TryExecute(string executable, string arguments, bool redirectInputOutput, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
    {
      StringBuilder outputBuilder = new StringBuilder();
      using (System.Diagnostics.Process process = new System.Diagnostics.Process())
      using (AutoResetEvent outputWaitHandle = new AutoResetEvent(!redirectInputOutput))
      {
        PrepareProcess(executable, arguments, redirectInputOutput, process, outputWaitHandle, outputBuilder);

        process.Start();
        // Additional check if process is still active, could happen during debugging
        if (!process.HasExited)
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
    [Obsolete("This method will be removed. The functionality is contained in ImpersonationService")]
    private static bool TryExecute_Impersonated(string executable, string arguments, IntPtr token, bool redirectInputOutput, out string result, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
    {
      // Note: Althought the code is nearly identical as TryExecute, it cannot be easily refactored, as the ImpersonationProcess implements many methods and properties with "new".
      // If such an instance is assigned to "Process" base class, any access will fail here. 
      StringBuilder outputBuilder = new StringBuilder();
      using (ImpersonationProcess process = new ImpersonationProcess())
      using (AutoResetEvent outputWaitHandle = new AutoResetEvent(!redirectInputOutput))
      {
        PrepareProcess(executable, arguments, redirectInputOutput, process, outputWaitHandle, outputBuilder);
        process.StartAsUser(token);
        if (!process.HasExited)
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

    [Obsolete("This method will be removed. The functionality is contained in ImpersonationService")]
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
            outputBuilder.AppendLine(e.Data);
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
    [Obsolete("This method will be removed. The functionality is contained in ImpersonationService")]
    private static string RemoveEncodingPreamble(string rawString)
    {
      if (!string.IsNullOrWhiteSpace(rawString) && rawString.StartsWith(CONSOLE_ENCODING_PREAMBLE))
        return rawString.Substring(CONSOLE_ENCODING_PREAMBLE.Length);
      return rawString;
    }

    #endregion
  }
}
