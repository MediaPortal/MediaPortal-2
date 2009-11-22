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

#region Usings

using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using MediaPortal.Core.Threading;
#endregion

namespace MediaPortal.Core.Services.Threading
{

  public class WorkQueue
  {
    #region Variables

    private object _queueMutex = new object();
    private Queue<IWork> _lowQueue = new Queue<IWork>();
    private Queue<IWork> _normalQueue = new Queue<IWork>();
    private Queue<IWork> _highQueue = new Queue<IWork>();
    private List<WorkWaiter> _workWaiters = new List<WorkWaiter>();
    private int _queueItemCount = 0;

    // Thread-specific variables

    [ThreadStatic]
    private static WorkWaiter _workWaiter;

    #endregion

    #region Constructors

    public WorkQueue()
    {
    }

    #endregion

    #region Public methods

    public void Add(IWork work)
    {
      Add(work, QueuePriority.Normal);
    }

    public void Add(IWork work, QueuePriority priority)
    {
      lock (_queueMutex)
      {
        bool mustQueue = true;
        while (_workWaiters.Count > 0)
        {
          WorkWaiter waiter = _workWaiters[0];
          _workWaiters.Remove(waiter);
          if (waiter.Signal(work))
          {
            mustQueue = false;
            break;
          }
        }
        if (mustQueue)
        {
          Queue<IWork> queue = GetQueue(priority);
          queue.Enqueue(work);
          _queueItemCount++;
        }
      }
    }

    /// <summary>
    /// Dequeue work from the work queue
    /// </summary>
    /// <param name="timeoutMilliSeconds">maximum time to wait for </param>
    /// <param name="cancelHandle"></param>
    /// <returns>IWork work to process</returns>
    public IWork Dequeue(int timeoutMilliSeconds, WaitHandle cancelHandle)
    {
      IWork work = null;
      lock (_queueMutex)
      {
        // if queue already contains items, return work immediately
        if (_queueItemCount > 0)
        {
          Queue<IWork> queue = GetFirstQueueWithWork();
          if (queue != null)
          {
            work = queue.Dequeue();
            _queueItemCount--;
            return work;
          }
        }
        // block the call to this method for the current thread until we receive work,
        // or until the given timeout has been reached
        else
        {
          if (_workWaiter == null)
            _workWaiter = new WorkWaiter();
          _workWaiter.Reset();
          _workWaiters.Add(_workWaiter);
        }
      }
      // create array of waiters, and invoke waitany so we wait until any of the handles is signaled
      WaitHandle[] waiters = new WaitHandle[] { _workWaiter.WaitHandle, cancelHandle };
      int i = WaitHandle.WaitAny(waiters, timeoutMilliSeconds, true);
      lock (_queueMutex)
      {
        // if first WaitHandle was signaled, then this means we received some work
        if (i == 0)
        {
          work = _workWaiter.Work;
        }
        else
        {
          // waiting has either been canceled, or timeout has been reached
          if (_workWaiters.Contains(_workWaiter))
            _workWaiters.Remove(_workWaiter);
        }
      }
      return work;
    }

    #endregion

    #region Private methods

    private Queue<IWork> GetQueue(QueuePriority priority)
    {
      switch (priority)
      {
        case QueuePriority.High:
          return _highQueue;
        case QueuePriority.Normal:
          return _lowQueue;
        case QueuePriority.Low:
          return _lowQueue;
        default:
          return _normalQueue;
      }
    }

    private Queue<IWork> GetFirstQueueWithWork()
    {
      if (_highQueue.Count > 0)
        return _highQueue;
      else if (_normalQueue.Count > 0)
        return _normalQueue;
      else if (_lowQueue.Count > 0)
        return _lowQueue;
      else
        return null;
    }

    #endregion

    #region Properties

    public int Count
    {
      get { return _queueItemCount; }
    }

    #endregion
  }

  public class WorkWaiter
  {
    #region Variables
    private AutoResetEvent _waitHandle = new AutoResetEvent(false);
    private IWork _work = null;
    private bool _signaled = false;
    private bool _timedout = false;
    #endregion

    #region Constructor
    public WorkWaiter()
    {
      Reset();
    }
    #endregion

    #region Properties
    public WaitHandle WaitHandle
    {
      get { return _waitHandle; }
    }

    public IWork Work
    {
      get { return _work; }
    }
    #endregion

    #region Public methods
    public bool Signal(IWork work)
    {
      lock (this)
      {
        if (!_timedout)
        {
          _work = work;
          _signaled = true;
          _waitHandle.Set();
          return true;
        }
      }
      return false;
    }
    public bool Timeout()
    {
      lock (this)
      {
        if (!_signaled)
        {
          _timedout = true;
          return true;
        }
      }
      return false;
    }
    public void Reset()
    {
      _work = null;
      _timedout = false;
      _signaled = false;
      _waitHandle.Reset();
    }
    public void Close()
    {
      if (_waitHandle != null)
      {
        _waitHandle.Close();
        _waitHandle = null;
      }
    }
    #endregion
  }
}
