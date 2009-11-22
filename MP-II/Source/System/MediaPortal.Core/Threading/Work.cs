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

using System;
using System.Threading;
using MediaPortal.Core.Threading;

namespace MediaPortal.Core.Threading
{
  /// <summary>
  /// Default implementation of an <see cref="IWork"/>.
  /// </summary>
  public class Work : IWork
  {
    #region Variables
    private WorkState _state;
    private string _description;
    private ThreadPriority _priority;
    private Exception _exception;
    private WorkEventArgs _eventArgs;
    public DoWorkHandler WorkLoad;
    public WorkEventHandler WorkCompleted;
    private bool _simpleWork = false;
    #endregion

    #region Constructors
    public Work()
    {
      _state = WorkState.INIT;
      _description = string.Empty;
      _priority = ThreadPriority.Normal;
      _eventArgs = new WorkEventArgs(this);
    }

    public Work(DoWorkHandler work)
        : this(work, string.Empty, ThreadPriority.Normal, null) { }

    public Work(DoWorkHandler work, ThreadPriority threadPriority)
        : this(work, string.Empty, threadPriority, null) { }

    public Work(DoWorkHandler work, WorkEventHandler workCompletedHandler)
        : this(work, string.Empty, ThreadPriority.Normal, workCompletedHandler) { }

    public Work(DoWorkHandler work, string description, ThreadPriority threadPriority)
        : this(work, description, threadPriority, null) { }

    public Work(DoWorkHandler work, ThreadPriority threadPriority, WorkEventHandler workCompletedHandler)
        : this(work, string.Empty, threadPriority, workCompletedHandler) { }

    public Work(DoWorkHandler work, string description, ThreadPriority threadPriority, WorkEventHandler workCompletedHandler)
    {
      WorkLoad = work;
      _description = description;
      _priority = threadPriority;
      WorkCompleted = workCompletedHandler;
      _eventArgs = new WorkEventArgs(this);
      _simpleWork = true;
      _state = WorkState.INIT;
    }

    #endregion

    #region IWork implementation

    public virtual void Process()
    {
      // don't perform canceled work
      if (_state == WorkState.CANCELED)
        return;
      // don't perform work which is in an invalid state
      if (_state != WorkState.INQUEUE)
        throw new InvalidOperationException(String.Format("WorkState for work {0} not INQUEUE, but {1}", _description, _state));

      // mark work as in progress
      if (_simpleWork)
        State = WorkState.INPROGRESS;

      // perform work 
      if (WorkLoad != null)
        WorkLoad();
      else
        throw new NotImplementedException();

      // mark work as finished and fire work completion delegate
      if (_simpleWork)
      {
        State = WorkState.FINISHED;
        if (WorkCompleted != null)
          WorkCompleted(_eventArgs);
      }
    }

    public WorkState State
    {
      get { return _state; }
      set { _state = value; }
    }

    public string Description
    {
      get { return _description; }
      set { _description = value; }
    }

    public Exception Exception
    {
      get { return _exception; }
      set { _exception = value; }
    }

    public ThreadPriority ThreadPriority
    {
      get { return _priority; }
      set { _priority = value; }
    }

    public WorkEventArgs EventArgs
    {
      get { return _eventArgs; }
      set { _eventArgs = value; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Cancels processing of this work
    /// </summary>
    /// <returns></returns>
    public bool Cancel()
    {
      if (_state == WorkState.INIT || _state == WorkState.INQUEUE)
      {
        _state = WorkState.CANCELED;
        return true;
      }
      else
        return false;
    }

    #endregion
  }
}