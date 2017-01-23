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

namespace MediaPortal.Common.Threading
{
  public enum WorkState
  {
    INIT,
    INQUEUE,
    CANCELED,
    INPROGRESS,
    FINISHED,
    ERROR
  }

  public delegate void DoWorkHandler();
  public delegate void WorkEventHandler(WorkEventArgs args);

  public interface IWork
  {
    /// <summary>
    /// The current state the work is in (set by the threadpool and used to cancel work which is still in the queue).
    /// </summary>
    WorkState State { get; set; }

    /// <summary>
    /// Description for this work (optional).
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Placeholder for any exception thrown by this the workload code.
    /// </summary>
    Exception Exception { get; set; }

    /// <summary>
    /// Specifies the scheduling priority for this work.
    /// </summary>
    ThreadPriority ThreadPriority { get; set; }

    /// <summary>
    /// Method which contains the work that should be performed by the ThreadPool.
    /// </summary>
    void Process();
  }
}
