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
using MediaPortal.Core.Threading;

namespace MediaPortal.Core.Threading
{
  /// <summary>
  /// Default implementation of an <see cref="IWorkInterval"/>.
  /// </summary>
  public class IntervalWork : Work, IWorkInterval
  {
    #region Variables

    TimeSpan _interval;
    DateTime _lastRun = DateTime.Now;
    bool _running = false;

    #endregion

    #region Contructor

    public IntervalWork(DoWorkHandler work, TimeSpan interval)
    {
      WorkLoad = work;
      _interval = interval;
    }

    #endregion

    #region IWorkInterval implementation

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
      if (WorkLoad != null)
        WorkLoad();

      // mark work as finished and fire work completion delegate
      State = WorkState.FINISHED;
      if (WorkCompleted != null)
        WorkCompleted(EventArgs);
      _running = false;
    }

    #endregion
  }
}