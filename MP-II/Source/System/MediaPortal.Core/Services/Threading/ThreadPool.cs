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

#region Usings

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Core.Threading;

#endregion

namespace MediaPortal.Core.Services.Threading
{
  #region Delegates
  /// <summary>
  /// General delegate for delegating informational, warning, error and debug logging to a different class
  /// </summary>
  /// <param name="format">message to log</param>
  /// <param name="args">objects to format into the message</param>
  public delegate void LoggerDelegate(string format, params object[] args);
  #endregion

  public class ThreadPool : IThreadPool
  {
    #region Variables

    /// <summary>
    /// Holds all the necessary parameters for this ThreadPool
    /// </summary>
    private readonly ThreadPoolStartInfo _startInfo = new ThreadPoolStartInfo();

    /// <summary>
    /// List of threads from this pool, with the Thread object being the key and the last activity time as value
    /// </summary>
    /// TODO: switch to HashSet, synchronize with sync object
    private readonly Hashtable _threads = Hashtable.Synchronized(new Hashtable());

    /// <summary>
    /// List of objects that want to perform work in a fixed interval, with IWorkInterval and the last runtime as value
    /// </summary>
    private readonly List<IWorkInterval> _intervalBasedWork = new List<IWorkInterval>();

    /// <summary>
    /// Last time we checked whether interval based work should be run
    /// </summary>
    private DateTime _lastIntervalCheck = DateTime.Now;

    /// <summary>
    /// Priority-based queue which will hold all work when all threads are busy
    /// </summary>
    private readonly WorkQueue _workQueue = new WorkQueue();

    /// <summary>
    /// WaitHandle which is set when the ThreadPool wants to cancel, so all idle threads stop waiting for work
    /// </summary>
    private readonly AutoResetEvent _cancelWaitHandle = new AutoResetEvent(false);

    /// <summary>
    /// Amount of work items processed by the queue (only work with FINISHED WorkState is counted)
    /// </summary>
    private long _itemsProcessed = 0;

    /// <summary>
    /// Amount of threads which are currently busy processsing work
    /// </summary>
    private int _inUseThreads = 0;

    /// <summary>
    /// Private indicator whether the pool is in running state
    /// </summary>
    private bool _run = true;

    // decoupled logging delegates
    /// <summary>
    /// Logging delegate for information log messages
    /// </summary>
    public LoggerDelegate InfoLog;

    /// <summary>
    /// Logging delegate for warning log messages
    /// </summary>
    public LoggerDelegate WarnLog;
    
    /// <summary>
    /// Logging delegate for error log messages
    /// </summary>
    public LoggerDelegate ErrorLog;
    
    /// <summary>
    /// Logging delegate for debug log messages
    /// </summary>
    public LoggerDelegate DebugLog;

    #endregion

    #region Constructors

    public ThreadPool()
    {
      Init();
    }

    public ThreadPool(int minThreads, int maxThreads)
    {
      _startInfo = new ThreadPoolStartInfo(minThreads, maxThreads);
      Init();
    }

    public ThreadPool(ThreadPoolStartInfo startInfo)
    {
      _startInfo = startInfo;
      Init();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">DoWorkHandler which contains the work to perform</param>
    /// <returns>IWork reference to work object</returns>
    public IWork Add(DoWorkHandler work)
    {
      IWork w = new Work(work, _startInfo.DefaultThreadPriority);
      Add(w);
      return w;
    }

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">DoWorkHandler which contains the work to perform</param>
    /// <param name="queuePriority">QueuePriority for this work</param>
    /// <returns>IWork reference to work object</returns>
    public IWork Add(DoWorkHandler work, QueuePriority queuePriority)
    {
      IWork w = new Work(work, _startInfo.DefaultThreadPriority);
      Add(w, queuePriority);
      return w;
    }

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">DoWorkHandler which contains the work to perform</param>
    /// <param name="description">description for this work</param>
    /// <returns>IWork reference to work object</returns>
    public IWork Add(DoWorkHandler work, string description)
    {
      IWork w = new Work(work, description, _startInfo.DefaultThreadPriority);
      Add(w);
      return w;
    }

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">DoWorkHandler which contains the work to perform</param>
    /// <param name="threadPriority">System.Threading.ThreadPriority for this work</param>
    /// <returns>IWork reference to work object</returns>
    public IWork Add(DoWorkHandler work, ThreadPriority threadPriority)
    {
      IWork w = new Work(work, threadPriority);
      Add(w);
      return w;
    }

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">DoWorkHandler which contains the work to perform</param>
    /// <param name="workCompletedHandler">WorkEventHandler to be called on completion</param>
    /// <returns>IWork reference to work object</returns>
    public IWork Add(DoWorkHandler work, WorkEventHandler workCompletedHandler)
    {
      IWork w = new Work(work, workCompletedHandler);
      Add(w);
      return w;
    }

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">DoWorkHandler which contains the work to perform</param>
    /// <param name="description">description for this work</param>
    /// <param name="queuePriority">QueuePriority for this work</param>
    /// <returns>IWork reference to work object</returns>
    public IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority)
    {
      IWork w = new Work(work, description, _startInfo.DefaultThreadPriority);
      Add(w, queuePriority);
      return w;
    }

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">DoWorkHandler which contains the work to perform</param>
    /// <param name="description">description for this work</param>
    /// <param name="queuePriority">QueuePriority for this work</param>
    /// <param name="threadPriority">System.Threading.ThreadPriority for this work</param>
    /// <returns>IWork reference to work object</returns>
    public IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority, ThreadPriority threadPriority)
    {
      IWork w = new Work(work, description, threadPriority);
      Add(w, queuePriority);
      return w;
    }

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">DoWorkHandler which contains the work to perform</param>
    /// <param name="description">description for this work</param>
    /// <param name="queuePriority">QueuePriority for this work</param>
    /// <param name="threadPriority">System.Threading.ThreadPriority for this work</param>
    /// <param name="workCompletedHandler">WorkEventHandler to be called on completion</param>
    /// <returns>IWork reference to work object</returns>
    public IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority, ThreadPriority threadPriority, WorkEventHandler workCompletedHandler)
    {
      IWork w = new Work(work, description, threadPriority, workCompletedHandler);
      Add(w, queuePriority);
      return w;
    }

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">Add work to be performed by the threadpool</param>
    public void Add(IWork work)
    {
      Add(work, QueuePriority.Normal);
    }

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// Can throw ArgumentNullException and InvalidOperationException.
    /// </summary>
    /// <param name="work">Add work to be performed by the threadpool</param>
    /// <param name="queuePriority">QueuePriority for this work</param>
    public void Add(IWork work, QueuePriority queuePriority)
    {
      if (_startInfo.DelayedInit)
      {
        _startInfo.DelayedInit = false;
        Init();
      }
      if (work == null)
      {
        LogError("ThreadPool.Add(): work cannot be null");
        throw new ArgumentNullException("work", "cannot be null");
      }
      if (work.State != WorkState.INIT)
      {
        LogError("ThreadPool.Add(): WorkState must be {0}", WorkState.INIT);
        throw new InvalidOperationException(String.Format("WorkState must be {0}", WorkState.INIT));
      }
      if (!_run)
      {
        LogError("ThreadPool.Add(): Threadpool is already (being) stopped");
        throw new InvalidOperationException("Threadpool is already (being) stopped");
      }
      work.State = WorkState.INQUEUE;
      _workQueue.Add(work, queuePriority);
      CheckThreadIncrementRequired();
    }

    /// <summary>
    /// Shuts down the ThreadPool. Active threads will eventually stop; idle threads
    /// will be shutdown and queue will not accept new work anymore.
    /// </summary>
    public void Stop()
    {
      _run = false;
      _cancelWaitHandle.Set();
      foreach (IWorkInterval iWrk in _intervalBasedWork)
        iWrk.OnThreadPoolStopped();
    }

    public void AddIntervalWork(IWorkInterval intervalWork, bool runNow)
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

    public void RemoveIntervalWork(IWorkInterval intervalWork)
    {
      lock (_intervalBasedWork)
      {
        if (_intervalBasedWork.Contains(intervalWork))
          _intervalBasedWork.Remove(intervalWork);
      }
    }

    #endregion
    
    #region Private methods

    #region Initialization

    /// <summary>
    /// Initializes the ThreadPool. Note: if the threadpool is configured for delayed
    /// initialization then threads aren't created until the the first work is added
    /// to the threadpool queue.
    /// </summary>
    private void Init()
    {
      LogInfo("ThreadPool.Init()");
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

    /// <summary>
    /// Manages the in-use thread counter
    /// </summary>
    /// <param name="increment"></param>
    private void HandleInUseThreadCount(bool increment)
    {
      int inUse;
      if (increment)
        inUse = Interlocked.Increment(ref _inUseThreads);
      else
        inUse = Interlocked.Decrement(ref _inUseThreads);
      //LogDebug("ThreadPool.HandleInUseThreadCount() : in use threads: {0} max: {1} increment: {2}", inUse, _startInfo.MaximumThreads, increment);
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

      lock (_threads.SyncRoot)
      {
        for (int i = 0; i < count; i++)
        {
          // Make sure maximum thread count never exceeds
          if (_threads.Count >= _startInfo.MaximumThreads)
            return;

          Thread t = new Thread(ProcessQueue) {IsBackground = true};
          t.Name = "PoolThread" + t.GetHashCode();
          t.Priority = _startInfo.DefaultThreadPriority;
          t.Start();
          LogDebug("ThreadPool.StartThreads() : Thread {0} started", t.Name);

          // Add thread as key to the Hashtable with creation time as value
          _threads[t] = DateTime.Now;
        }
      }
    }

    /// <summary>
    /// Determines if the number of threads in the pool can and should be increased. If so,
    /// StartThreads(1) is called to increase the thread count with 1.
    /// </summary>
    private void CheckThreadIncrementRequired()
    {
      if (!_run)
        return;
      bool incrementRequired = false;
      lock (_threads.SyncRoot)
      {
        // check if all threads are in use
        if (_inUseThreads == _threads.Count)
          // check if maximum number of threads hasn't been reached
          if (_threads.Count < _startInfo.MaximumThreads)
            incrementRequired = true;
      }
      if (incrementRequired)
      {
        LogDebug("ThreadPool.CheckThreadIncrementRequired() : incrementing thread count {0} with 1", _threads.Count);
        StartThreads(1);
      }
    }

    #endregion

    #region Thread main entry

    private void ProcessQueue()
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
//          LogDebug("ThreadPool.ProcessQueue() {0} : entering work dequeue...", Thread.CurrentThread.Name);
          IWork work = _workQueue.Dequeue(_startInfo.ThreadIdleTimeout, _cancelWaitHandle);

          // Update time this thread was last alive
          _threads[Thread.CurrentThread] = DateTime.Now;

          if (work == null)
          {
            // LogDebug("ThreadPool.ProcessQueue() : received NULL work");
            // Dequeue has returned null or we should shutdown
            if (_threads.Count > _startInfo.MinimumThreads)
            {
              lock (_threads.SyncRoot)
              {
                if (_threads.Count > _startInfo.MinimumThreads)
                {
                  LogDebug("ThreadPool.ProcessQueue() : quitting (inUse:{0}, total:{1})", _inUseThreads, _threads.Count);
                  // remove thread from the pool
                  if (_threads.Contains(Thread.CurrentThread))
                    _threads.Remove(Thread.CurrentThread);
                  break;
                }
              }
            }
          }
          if (work == null)
          {
            CheckForIntervalBasedWork();
            // skip this iteration if we got here and work is null
            continue;
          }

//          LogDebug("ThreadPool.ProcessQueue() {0} : received valid work: {1}", Thread.CurrentThread.Name, work.State);
          try
          {
            // Only process items which have status INQUEUE (don't process CANCEL'ed items)
            if (work.State == WorkState.INQUEUE)
            {
              Thread.CurrentThread.Priority = work.ThreadPriority;
              HandleInUseThreadCount(true);
//              LogDebug("ThreadPool.ProcessQueue() {0} : processing work {1}", Thread.CurrentThread.Name, work.Description);
              work.Process();
//              LogDebug("ThreadPool.ProcessQueue() : finished processing work {0}", work.Description);
            }
          }
          catch (Exception e)
          {
            LogWarn("ThreadPool.ProcessQueue() {0} : exception during processing work {1}: {2}", Thread.CurrentThread.Name, work.Description, e.Message);
            work.State = WorkState.ERROR;
            work.Exception = e;
          }
          finally
          {
            if (work.State == WorkState.FINISHED || work.State == WorkState.ERROR)
            {
              Interlocked.Increment(ref _itemsProcessed);
              LogDebug("ThreadPool.ProcessQueue() : total items processed: {0}", _itemsProcessed);
              HandleInUseThreadCount(false);
//              LogDebug("ThreadPool.ProcessQueue() : finished processing work {0}", work.Description);
            }
            Thread.CurrentThread.Priority = _startInfo.DefaultThreadPriority;
          }
          CheckForIntervalBasedWork();
        }
      }
      catch (ThreadAbortException)
      {
        LogDebug("ThreadPool.ProcessQueue() {0} : thread aborted", Thread.CurrentThread.Name);
        Thread.ResetAbort();
      }
      catch (Exception e)
      {
        LogError("ThreadPool.ProcessQueue() Error: {0} {1} {2}", Thread.CurrentThread.Name, e.Message, e.StackTrace);
      }
      finally
      {
        if (_threads.Contains(Thread.CurrentThread))
          _threads.Remove(Thread.CurrentThread);
      }
    }

    private void CheckForIntervalBasedWork()
    {
      // check if last check was at least 1 second ago
      if (DateTime.Now.AddSeconds(-1) < _lastIntervalCheck)
        return;
      lock (_intervalBasedWork)
      {
        // doublecheck
        if (DateTime.Now.AddSeconds(-1) < _lastIntervalCheck)
          return;
//        LogDebug("ThreadPool.CheckForIntervalBasedWork()");
        // search for any interval which is due and not running already
        foreach (IWorkInterval iWrk in _intervalBasedWork)
        {
          if (iWrk.LastRun.AddTicks(iWrk.WorkInterval.Ticks) <= DateTime.Now)
            if (!iWrk.Running)
              RunIntervalBasedWork(iWrk);
        }
        _lastIntervalCheck = DateTime.Now;
      }
    }

    /// <summary>
    /// Run the given IWorkInterval
    /// </summary>
    /// <param name="intervalWork">IWorkInterval to run</param>
    private void RunIntervalBasedWork(IWorkInterval intervalWork)
    {
      LogDebug("ThreadPool.RunIntervalBasedWork() : running interval based work ({0}) interval:{1}", intervalWork.Work.Description, intervalWork.WorkInterval);
      intervalWork.ResetWorkState();
      intervalWork.LastRun = DateTime.Now;
      intervalWork.Running = true;
      Add(intervalWork.Work, QueuePriority.Low);
    }

    #endregion

    #region Logging methods

    private void LogInfo(string format, params object[] args)
    {
      if (InfoLog != null)
        InfoLog(format, args);
    }

    private void LogWarn(string format, params object[] args)
    {
      if (WarnLog != null)
        WarnLog(format, args);
    }

    private void LogError(string format, params object[] args)
    {
      if (ErrorLog != null)
        ErrorLog(format, args);
    }

    private void LogDebug(string format, params object[] args)
    {
      if (DebugLog != null)
        DebugLog(format, args);
    }

    #endregion

    #endregion
    
    #region Properties

    /// <summary>
    /// Total number of threads in the ThreadPool
    /// </summary>
    public int ThreadCount
    {
      get { return _threads.Count; }
    }

    /// <summary>
    /// Total number of busy threads in the ThreadPool
    /// </summary>
    public int BusyThreadCount
    {
      get { return _inUseThreads; }
    }

    /// <summary>
    /// Total amount of work which has been processed by the ThreadPool
    /// </summary>
    public long WorkItemsProcessed
    {
      get { return _itemsProcessed; }
    }

    /// <summary>
    /// Number of work items waiting to be processed by a thread
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
