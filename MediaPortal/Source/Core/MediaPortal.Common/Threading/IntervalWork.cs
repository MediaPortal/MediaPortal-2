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

namespace MediaPortal.Common.Threading
{
  /// <summary>
  /// Default implementation of an <see cref="IIntervalWork"/>.
  /// </summary>
  public class IntervalWork : Work, IIntervalWork
  {
    #region Variables

    protected readonly TimeSpan _interval;
    protected DateTime _lastRun = DateTime.Now;
    protected bool _running = false;

    #endregion

    #region Contructor

    public IntervalWork(DoWorkHandler work, TimeSpan interval)
    {
      WorkLoad = work;
      _interval = interval;
    }

    #endregion

    #region IIntervalWork implementation

    public IWork Work
    {
      get { return this; }
    }

    public TimeSpan WorkInterval
    {
      get { return _interval; }
    }

    public DateTime LastRun
    {
      get { return _lastRun; }
      set { _lastRun = value; }
    }

    public bool Running
    {
      get { return _running; }
      set { _running = value; }
    }

    public void OnThreadPoolStopped()
    {
    }

    public void ResetWorkState()
    {
      _running = false;
      State = WorkState.INIT;
    }

    #endregion

    #region Work overrides

    public override void Process()
    {
      // don't perform canceled work
      if (State == WorkState.CANCELED)
        return;
      // don't perform work which is in an invalid state
      if (State != WorkState.INQUEUE)
        throw new InvalidOperationException(String.Format("WorkState for work {0} not INQUEUE, but {1}", Description, State));
      State = WorkState.INPROGRESS;

      // perform work 
      DoWorkHandler workLoad = WorkLoad;
      if (workLoad != null)
        workLoad();

      // mark work as finished and fire work completion delegate
      State = WorkState.FINISHED;
      if (WorkCompleted != null)
        WorkCompleted(EventArgs);
      _running = false;
    }

    #endregion
  }
}
