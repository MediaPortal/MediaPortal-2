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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Threading
{
  /// <summary>
  /// A class that acts like a priority access handler. Multiple priority locks can be held
  /// at the same time while low priority locks can only be held when no priority lock is held.
  /// </summary>
  public class AsyncPriorityLock
  {
    public struct Releaser : IDisposable
    {
      private AsyncPriorityLock _parent;
      private readonly bool _isPriority;

      internal Releaser(AsyncPriorityLock toRelease, bool isPriority)
      {
        _parent = toRelease;
        _isPriority = isPriority;
      }

      public void Dispose()
      {
        if (_parent != null)
        {
          if (_isPriority)
            _parent.PriorityRelease();
          else
            _parent.LowPriorityRelease();
        }
      }
    }

    private readonly object _syncObj = new object();

    //Tasks that complete immediately for fast path when there is no need to wait.
    private readonly Task<Releaser> _priorityReleaser;
    private readonly Task<Releaser> _lowPriorityReleaser;

    //Queue of waiting low priority lock requesters
    private readonly ConcurrentQueue<TaskCompletionSource<Releaser>> _waitingLowPriorities = new ConcurrentQueue<TaskCompletionSource<Releaser>>();

    //Current number of priority locks.
    private int _priorityLocks;

    public AsyncPriorityLock()
    {
      _priorityReleaser = Task.FromResult(new Releaser(this, true));
      _lowPriorityReleaser = Task.FromResult(new Releaser(this, false));
    }

    /// <summary>
    /// Acquires a low priority lock.
    /// The acquired Releaser must be disposed to release the low priority lock.
    /// </summary>
    /// <returns></returns>
    public Releaser LowPriorityLock()
    {
      return LowPriorityLockAsync().Result;
    }

    /// <summary>
    /// Returns a task that completes when the low priority lock has been acquired.
    /// The acquired Releaser must be disposed to release the low priority lock.
    /// </summary>
    /// <returns></returns>
    public Task<Releaser> LowPriorityLockAsync()
    {
      // Checking the priority lock count and the queuing of a low priority task
      // must be atomic to avoid a race condition where the priority lock is
      // decremented between the check and queuing, which would lead to the low
      // priority task being queued without a corresponding priority lock to
      // dequeue it.
      lock (_syncObj)
      {
        if (_priorityLocks == 0)
          return _lowPriorityReleaser;
        var waiter = new TaskCompletionSource<Releaser>();
        _waitingLowPriorities.Enqueue(waiter);
        return waiter.Task;
      }
    }

    /// <summary>
    /// Acquires a priority lock.
    /// The acquired Releaser must be disposed to release the priority lock.
    /// </summary>
    /// <returns></returns>
    public Releaser PriorityLock()
    {
      return PriorityLockAsync().Result;
    }

    /// <summary>
    /// Returns a task that completes when the priority lock has been acquired.
    /// The acquired Releaser must be disposed to release the priority lock.
    /// </summary>
    /// <returns></returns>
    public Task<Releaser> PriorityLockAsync()
    {
      // Technically we could use Interlocked.Increment here. However, in all other places
      // we have to lock to ensure that the priority lock count isn't decremented whilst a
      // low priority lock is being queued. If we used Interlocked here then we'd also have
      // to use it inside those locks to ensure the value remains consistent so we use a
      // lock here as well to avoid having to nest different synchronization methods.
      lock (_syncObj)
        _priorityLocks++;
      return _priorityReleaser;
    }

    private void LowPriorityRelease()
    {
    }

    private void PriorityRelease()
    {
      // We can't use Interlocked.Decrement here because we need to block
      // whilst any low priority locks are being queued.
      lock (_syncObj)
        _priorityLocks--;

      // Don't complete the TCSs inside the lock otherwise we'd need to allow the priority lock count
      // to be incremented outside the lock using the Interlocked methods (see comment in PriorityLockAsync()).
      // Additionally, the completions may run on the current thread which could lead to a deadlock if
      // it synchronoulsy waits on a low priority lock and another thread has since obtained a priority
      // lock, causing the low priority lock to be queued. We'd then be blocked here waiting for the other
      // thread to dequeue it which it couldn't do because we are still holding the lock. We could avoid
      // the second issue by creating the TCS with TaskCreationOptions.RunContinuationsAsynchronously
      // however this wouldn't solve the first issue so we avoid the additional resource usage of running
      // the completions asynchronously.
      while (Volatile.Read(ref _priorityLocks) == 0 && _waitingLowPriorities.TryDequeue(out TaskCompletionSource<Releaser> toWake))
        toWake.SetResult(new Releaser(this, false));
    }
  }
}
