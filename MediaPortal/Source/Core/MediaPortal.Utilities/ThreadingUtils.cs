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
using System.Threading;

namespace MediaPortal.Utilities
{
  public static class ThreadingUtils
  {
    /// <summary>
    /// CallWithTimeout executes an action and waits for the given number of milliseconds. If the action will not finish in time,
    /// the thread executing the action gets aborted and a <see cref="TimeoutException"/> is raised.
    /// The given <paramref name="action"/> should be prepared to handle a <see cref="ThreadAbortException"/> during its execution.
    /// </summary>
    /// <param name="action">Action to start.</param>
    /// <param name="timeoutMs">Timeout in ms to wait for the action, until the action's thread is aborted via <see cref="Thread.Abort()"/>.</param>
    /// <exception cref="TimeoutException">If the given <paramref name="action"/> runs longer than the given numer of <paramref name="timeoutMs"/>.</exception>
    public static void CallWithTimeout(Action action, int timeoutMs)
    {
      var thread = new Thread(new ThreadStart(action));
      thread.Start();
      if (thread.Join(timeoutMs))
        return;
      thread.Abort();
      throw new TimeoutException("Action timed out");
    }

    /// <summary>
    /// Starts a new thread using STA apartment state (<see cref="ApartmentState.STA"/>). This is required for accessing some windows features like the clipboard.
    /// </summary>
    /// <param name="threadStart">Thread to start.</param>
    public static Thread RunSTAThreaded(ThreadStart threadStart)
    {
      Thread newThread = new Thread(threadStart);
      newThread.SetApartmentState(ApartmentState.STA);
      newThread.Start();
      return newThread;
    }
  }
}