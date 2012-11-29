#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaPortal.Utilities.Process
{
  public class ProcessUtils
  {
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
        process.PriorityClass = priorityClass;
        process.Start();
        if (process.WaitForExit(maxWaitMs))
          return process.ExitCode == 0;
      }
      return false;
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
        process.PriorityClass = priorityClass;
        process.Start();
        using (process.StandardOutput)
        {
          result = process.StandardOutput.ReadToEnd();
          if (process.WaitForExit(maxWaitMs))
            return process.ExitCode == 0;
        }
        if (!process.HasExited)
          process.Close();
      }
      return false;
    }

  }
}
