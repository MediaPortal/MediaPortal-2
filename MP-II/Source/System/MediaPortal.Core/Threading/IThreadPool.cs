#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *	Copyright (C) 2007-2009 Team MediaPortal
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

using System.Threading;

namespace MediaPortal.Core.Threading
{
  #region enums

  public enum QueuePriority
  {
    Low,
    Normal,
    High
  }

  #endregion

  public interface IThreadPool
  {
    #region Methods to add work

    IWork Add(DoWorkHandler work);
    IWork Add(DoWorkHandler work, QueuePriority queuePriority);
    IWork Add(DoWorkHandler work, string description);
    IWork Add(DoWorkHandler work, ThreadPriority threadPriority);
    IWork Add(DoWorkHandler work, WorkEventHandler workCompletedHandler);
    IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority);
    IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority, ThreadPriority threadPriority);
    IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority, ThreadPriority threadPriority, WorkEventHandler workCompletedHandler);
    void Add(IWork work);
    void Add(IWork work, QueuePriority queuePriority);

    #endregion

    #region Methods to manage interval-based work

    void AddIntervalWork(IWorkInterval intervalWork, bool runNow);
    void RemoveIntervalWork(IWorkInterval intervalWork);

    #endregion

    #region Methods to control the threadpool

    void Stop();

    #endregion

    #region Threadpool status properties

    int ThreadCount { get; }
    int BusyThreadCount { get; }
    long WorkItemsProcessed { get; }
    int QueueLength { get; }
    int MinimumThreads { get; }
    int MaximumThreads { get; }

    #endregion
  }
}
