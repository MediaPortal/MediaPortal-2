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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Threading;

namespace MediaPortal.Common.Services.Threading
{
  #region Delegates

  /// <summary>
  /// General delegate for delegating informational, warning, error and debug logging to a different class
  /// </summary>
  /// <param name="format">Message to log.</param>
  /// <param name="args">Objects to format into the message.</param>
  public delegate void LoggerDelegate(string format, params object[] args);

  #endregion

  public class ThreadPool : IThreadPool
  {
    #region Consts

    protected int THREAD_JOIN_TIMEOUT = 2000;

    #endregion

    #region Variables

    /// <summary>
    /// Synchronization object for our internal data.
    /// </summary>
    private readonly object _syncObj = new object();

    /// <summary>
    /// Holds all the necessary parameters for this ThreadPool.
    /// </summary>
    private readonly ThreadPoolStartInfo _startInfo;

    /// <summary>
    /// Dictionary of threads from this pool, with the Thread object being the key and the last activity time as value.
    /// </summary>
    private readonly ConcurrentDictionary<Thread, DateTime> _threads = new ConcurrentDictionary<Thread, DateTime>();

    /// <summary>
    /// List of objects that want to perform work in a fixed interval, with IIntervalWork and the last runtime as value.
    /// </summary>
    private readonly List<IIntervalWork> _intervalBasedWork = new List<IIntervalWork>();

    /// <summary>
    /// Last time we checked whether interval based work should be run.
    /// </summary>
    private DateTime _lastIntervalCheck = DateTime.Now;

    /// <summary>
    /// Priority-based queue which will hold all work when all threads are busy.
    /// </summary>
    private readonly WorkQueue _workQueue = new WorkQueue();

    /// <summary>
    /// WaitHandle which is set when the ThreadPool wants to cancel, so all idle threads stop waiting for work.
    /// </summary>
    private readonly AutoResetEvent _cancelWaitHandle = new AutoResetEvent(false);

    /// <summary>
    /// Amount of work items processed by the queue (only work with FINISHED WorkState is counted).
    /// </summary>
    private long _itemsProcessed = 0;

    /// <summary>
    /// Amount of threads which are currently busy processsing work.
    /// </summary>
    private int _inUseThreads = 0;

    /// <summary>
    /// Private indicator whether the pool is in running state.
    /// </summary>
    private bool _run = true;

    // Decoupled logging delegates

    #endregion

    #region Constructors and destructor

    public ThreadPool() : this(new ThreadPoolStartInfo()) { }

    public ThreadPool(int minThreads, int maxThreads) : this(new ThreadPoolStartInfo(minThreads, maxThreads)) { }

    public ThreadPool(ThreadPoolStartInfo startInfo)
    {
      _startInfo = startInfo;
      if (!_startInfo.DelayedInit)
        Init();
    }

    ~ThreadPool()
    {
      // If the thread pool was not stopped correctly, we'll at least make our worker threads stop
      _run = false;
      _cancelWaitHandle.Set();
      _cancelWaitHandle.Close();
    }

    #endregion

    #region Public methods

    public IWork Add(DoWorkHandler work)
    {
      IWork w = new Work(work, _startInfo.DefaultThreadPriority);
      return Add(w) ? w : null;
    }

    public IWork Add(DoWorkHandler work, QueuePriority queuePriority)
    {
      IWork w = new Work(work, _startInfo.DefaultThreadPriority);
      return Add(w, queuePriority) ? w : null;
    }

    public IWork Add(DoWorkHandler work, string description)
    {
      IWork w = new Work(work, description, _startInfo.DefaultThreadPriority);
      return Add(w) ? w : null;
    }

    public IWork Add(DoWorkHandler work, ThreadPriority threadPriority)
    {
      IWork w = new Work(work, threadPriority);
      return Add(w) ? w : null;
    }

    public IWork Add(DoWorkHandler work, WorkEventHandler workCompletedHandler)
    {
      IWork w = new Work(work, workCompletedHandler);
      return Add(w) ? w : null;
    }

    public IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority)
    {
      IWork w = new Work(work, description, _startInfo.DefaultThreadPriority);
      return Add(w, queuePriority) ? w : null;
    }

    public IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority, ThreadPriority threadPriority)
    {
      IWork w = new Work(work, description, threadPriority);
      return Add(w, queuePriority) ? w : null;
    }

    public IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority, ThreadPriority threadPriority, WorkEventHandler workCompletedHandler)
    {
      IWork w = new Work(work, description, threadPriority, workCompletedHandler);
      return Add(w, queuePriority) ? w : null;
    }

    public bool Add(IWork work)
    {
      return Add(work, QueuePriority.Normal);
    }

    public bool Add(IWork work, QueuePriority queuePriority)
    {
      if (!_run)
        return false;
      if (_startInfo.DelayedInit)
      {
        _startInfo.DelayedInit = false;
        Init();
      }
      if (work == null)
        throw new ArgumentNullException("work", "cannot be null");
      if (work.State != WorkState.INIT)
        throw new InvalidOperationException(String.Format("WorkState must be {0}", WorkState.INIT));
      if (!_run)
        throw new InvalidOperationException("Threadpool is already (being) stopped");
      work.State = WorkState.INQUEUE;
      _workQueue.Add(work, queuePriority);
      CheckThreadIncrementRequired();
      return true;
    }

    public void Stop()
    {
      _run = false;
      _cancelWaitHandle.Set();
      foreach (IIntervalWork iWrk in _intervalBasedWork)
        iWrk.OnThreadPoolStopped();
    }

    public void Shutdown()
    {
      Stop();
      bool allTerminated = _threads.Keys.Aggregate(true, (current, thread) => current & thread.Join(THREAD_JOIN_TIMEOUT));
      if (!allTerminated)
        ServiceRegistration.Get<ILogger>().Warn("ThreadPool: Not all worker threads terminated");
    }

    public void AddIntervalWork(IIntervalWork intervalWork, bool runNow)
    {
      if (_startInfo.DelayedInit)
      {
        _startInfo.DelayedInit = false;
        Init();
      }
      if (intervalWork == null)
        throw new ArgumentNullException("intervalWork", "cannot be null");
      lock (_intervalBasedWork)
      {
        _intervalBasedWork.Add(intervalWork);
        if (runNow)
          RunIntervalBasedWork(intervalWork);
      }
    }

    public void RemoveIntervalWork(IIntervalWork intervalWork)
    {
      lock (_intervalBasedWork)
      {
        if (_intervalBasedWork.Contains(intervalWork))
          _intervalBasedWork.Remove(intervalWork);
      }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the ThreadPool. Note: if the threadpool is configured for delayed
    /// initialization then threads aren't created until the the first work is added
    /// to the threadpool queue.
    /// </summary>
    private void Init()
    {
      ServiceRegistration.Get<ILogger>().Info("ThreadPool.Init()");
      _cancelWaitHandle.Reset();
      ThreadPoolStartInfo.Validate(_startInfo);
      _inUseThreads = 0;
      _itemsProcessed = 0;
      if (!_startInfo.DelayedInit)
        StartThreads(GetOptimalThreadCount());
    }

    #endregion

    #region Thread handling methods

    /// <summary>
    /// Determines the optimal number of threads to initialize the threadpool with.
    /// </summary>
    /// <returns></returns>
    private int GetOptimalThreadCount()
    {
      return Math.Min(Math.Max(_workQueue.Count, _startInfo.MinimumThreads), _startInfo.MaximumThreads);
    }

    private void IncInUseThreadCount()
    {
      Interlocked.Increment(ref _inUseThreads);
    }

    private void DecInUseThreadCount()
    {
      Interlocked.Decrement(ref _inUseThreads);
    }

    /// <summary>
    /// Starts a given number of threads. Increases the number of threads in the threadpool.
    /// Note: shutdown of idle threads is handled by the threads themselves in ProcessQueue().
    /// </summary>
    /// <param name="count">number of threads to start</param>
    private void StartThreads(int count)
    {
      // Don't start threads on shutdown
      if (!_run)
        return;

      lock (_syncObj)
      {
        for (int i = 0; i < count; i++)
        {
          // Make sure maximum thread count never exceeds
          if (_threads.Count >= _startInfo.MaximumThreads)
            return;

          Thread t = new Thread(ProcessQueue) {IsBackground = true};
          t.Name = "Thread" + t.GetHashCode();
          t.Priority = _startInfo.DefaultThreadPriority;
          t.Start();
          ServiceRegistration.Get<ILogger>().Debug("ThreadPool.StartThreads(): Thread {0} started", t.Name);

          // Add thread as key to the Hashtable with creation time as value
          _threads[t] = DateTime.Now;
        }
      }
    }

    /// <summary>
    /// Determines if the number of threads in the pool can and should be increased. If so, StartThreads(1) is called to
    /// increase the thread count with 1.
    /// </summary>
    private void CheckThreadIncrementRequired()
    {
      if (!_run)
        return;
      bool incrementRequired = false;
      lock (_syncObj)
      {
        // check if all threads are in use
        if (_inUseThreads == _threads.Count)
          // check if maximum number of threads hasn't been reached
          if (_threads.Count < _startInfo.MaximumThreads)
            incrementRequired = true;
      }
      if (incrementRequired)
      {
        ServiceRegistration.Get<ILogger>().Debug("ThreadPool.CheckThreadIncrementRequired(): Incrementing thread count {0} with 1", _threads.Count);
        StartThreads(1);
      }
    }

    #endregion

    #region Thread main entry

    protected void ProcessQueue()
    {
      try
      {
        while (_run)
        {
          // Update time this thread was last alive
          _threads[Thread.CurrentThread] = DateTime.Now;

          // Check if increase of number of threads is desired
          CheckThreadIncrementRequired();

          // Wait until we receive a work item
          IWork work = _workQueue.Dequeue(_startInfo.ThreadIdleTimeout, _cancelWaitHandle);

          if (work == null)
          {
            // Dequeue has returned null or we should shutdown
            if (_threads.Count > _startInfo.MinimumThreads)
            {
              lock (_syncObj)
              {
                if (_threads.Count > _startInfo.MinimumThreads)
                {
                  ServiceRegistration.Get<ILogger>().Debug("ThreadPool.ProcessQueue(): Quitting (inUse: {0}, total: {1})", _inUseThreads, _threads.Count);
                  // remove thread from the pool
                  if (_threads.ContainsKey(Thread.CurrentThread))
                  {
                    DateTime time;
                    _threads.TryRemove(Thread.CurrentThread, out time);
                  }
                  break;
                }
              }
            }
            CheckForIntervalBasedWork();
            // Skip this iteration if we got here and work is null
            continue;
          }

//          ServiceRegistration.Get<ILogger>().Debug("ThreadPool.ProcessQueue(): Received valid work: {1}", work.State);
          try
          {
            // Only process items which have status INQUEUE (don't process CANCEL'ed items)
            if (work.State == WorkState.INQUEUE)
            {
              Thread.CurrentThread.Priority = work.ThreadPriority;
              IncInUseThreadCount();
//              ServiceRegistration.Get<ILogger>().Debug("ThreadPool.ProcessQueue(): Processing work {1}", work.Description);
              work.Process();
//              ServiceRegistration.Get<ILogger>().Debug("ThreadPool.ProcessQueue(): Finished processing work {0}", work.Description);
            }
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("ThreadPool.ProcessQueue(): Exception during processing work '{0}'", e, work.Description);
            work.State = WorkState.ERROR;
            work.Exception = e;
          }
          finally
          {
            if (work.State == WorkState.FINISHED || work.State == WorkState.ERROR)
            {
              Interlocked.Increment(ref _itemsProcessed);
//              ServiceRegistration.Get<ILogger>().Debug("ThreadPool.ProcessQueue(): {0} items processed", _itemsProcessed);
              DecInUseThreadCount();
//              ServiceRegistration.Get<ILogger>().Debug("ThreadPool.ProcessQueue(): Finished processing work {0}", work.Description);
            }
            Thread.CurrentThread.Priority = _startInfo.DefaultThreadPriority;
          }
          CheckForIntervalBasedWork();
        }
      }
      catch (ThreadAbortException)
      {
        ServiceRegistration.Get<ILogger>().Debug("ThreadPool.ProcessQueue(): Thread aborted");
        Thread.ResetAbort();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("ThreadPool.ProcessQueue(): Error executing work", e);
      }
      finally
      {
        if (_threads.ContainsKey(Thread.CurrentThread))
        {
          DateTime time;
          _threads.TryRemove(Thread.CurrentThread, out time);
        }
      }
    }

    protected void CheckForIntervalBasedWork()
    {
      // Check if last check was at least 1 second ago
      if (DateTime.Now.AddSeconds(-1) < _lastIntervalCheck)
        return;
      lock (_intervalBasedWork)
      {
        // Double check
        if (DateTime.Now.AddSeconds(-1) < _lastIntervalCheck)
          return;
//        ServiceRegistration.Get<ILogger>().Debug("ThreadPool.CheckForIntervalBasedWork()");
        // Search for any interval which is due and not running already
        foreach (IIntervalWork iWrk in _intervalBasedWork)
        {
          if (iWrk.LastRun.AddTicks(iWrk.WorkInterval.Ticks) <= DateTime.Now)
            if (!iWrk.Running)
              RunIntervalBasedWork(iWrk);
        }
        _lastIntervalCheck = DateTime.Now;
      }
    }

    /// <summary>
    /// Run the given <paramref name="intervalWork"/>.
    /// </summary>
    /// <param name="intervalWork">IIntervalWork to run.</param>
    protected void RunIntervalBasedWork(IIntervalWork intervalWork)
    {
//      ServiceRegistration.Get<ILogger>().Debug("ThreadPool.RunIntervalBasedWork(): Running interval based work '{0}' (interval: {1})",
//          intervalWork.Work.Description, intervalWork.WorkInterval);
      intervalWork.ResetWorkState();
      intervalWork.LastRun = DateTime.Now;
      intervalWork.Running = true;
      Add(intervalWork.Work, QueuePriority.Low);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Total number of threads in the ThreadPool.
    /// </summary>
    public int ThreadCount
    {
      get { return _threads.Count; }
    }

    /// <summary>
    /// Total number of busy threads in the ThreadPool.
    /// </summary>
    public int BusyThreadCount
    {
      get { return _inUseThreads; }
    }

    /// <summary>
    /// Total amount of work which has been processed by the ThreadPool.
    /// </summary>
    public long WorkItemsProcessed
    {
      get { return _itemsProcessed; }
    }

    /// <summary>
    /// Number of work items waiting to be processed by a thread.
    /// </summary>
    public int QueueLength
    {
      get { return _workQueue.Count; }
    }

    /// <summary>
    /// Dynamically change the minimum number of threads in the pool.
    /// Note that the number of threads in the pool doesn't decrease until idle threads have reached their idle timeout.
    /// </summary>
    public int MinimumThreads
    {
      get { return _startInfo.MinimumThreads; }
      set
      {
        ThreadPoolStartInfo tpsi = new ThreadPoolStartInfo
          {
              MinimumThreads = value,
              MaximumThreads = _startInfo.MaximumThreads
          };
        ThreadPoolStartInfo.Validate(tpsi);
        if (value > _startInfo.MinimumThreads)
          StartThreads(value - _startInfo.MinimumThreads);
        _startInfo.MinimumThreads = value;
      }
    }

    /// <summary>
    /// Dynamically change the maximum allowable number of threads in the pool.
    /// Note that the number of threads in the pool doesn't decrease until idle threads have reached their idle timeout.
    /// </summary>
    public int MaximumThreads
    {
      get { return _startInfo.MaximumThreads; }
      set
      {
        ThreadPoolStartInfo tpsi = new ThreadPoolStartInfo
          {
              MinimumThreads = _startInfo.MinimumThreads,
              MaximumThreads = value
          };
        ThreadPoolStartInfo.Validate(tpsi);
        _startInfo.MaximumThreads = value;
        CheckThreadIncrementRequired();
      }
    }

    #endregion
  }
}
