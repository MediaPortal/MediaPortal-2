#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  /// <summary>
  /// Default implementation of an <see cref="IWork"/>.
  /// </summary>
  public class Work : IWork
  {
    #region Consts

    public static string UNKNOWN_WORK = "<Unknown work>";

    #endregion

    #region Variables

    private WorkState _state;
    private string _description = UNKNOWN_WORK;
    private ThreadPriority _priority;
    private Exception _exception;
    private WorkEventArgs _eventArgs;
    public DoWorkHandler WorkLoad;
    public WorkEventHandler WorkCompleted;
    private readonly bool _simpleWork = false;

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
      // Don't perform canceled work
      if (_state == WorkState.CANCELED)
        return;
      // Don't perform work which is in an invalid state
      if (_state != WorkState.INQUEUE)
        throw new InvalidOperationException(String.Format("WorkState for work '{0}' is {1} but should be {2}", _description, _state, WorkState.INQUEUE));

      // Mark work as in progress
      if (_simpleWork)
        State = WorkState.INPROGRESS;

      // Perform work 
      DoWorkHandler workLoad = WorkLoad;
      if (workLoad != null)
        workLoad();
      else
        throw new NotImplementedException();

      // Mark work as finished and fire work completion delegate
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
    /// Cancels processing of this work.
    /// </summary>
    /// <returns><c>true</c>, if the work was cancelled, else <c>false</c>.</returns>
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
