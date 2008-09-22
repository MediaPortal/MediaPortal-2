#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Threading;

namespace MediaPortal.Core.Threading
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
    /// The current state the work is in
    /// (set by the threadpool and used to cancel work which is still in the queue)
    /// </summary>
    WorkState State { get; set; }

    /// <summary>
    /// Description for this work (optional)
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Placeholder for any exception thrown by this the workload code
    /// </summary>
    Exception Exception { get; set; }

    /// <summary>
    /// Specifies the scheduling priority for this work
    /// </summary>
    ThreadPriority ThreadPriority { get; set; }

    /// <summary>
    /// Method which contains the work that should be performed by the ThreadPool
    /// </summary>
    void Process();
  }
}
