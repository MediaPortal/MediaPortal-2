#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Threading
{
  /// <summary>
  /// A class that acts like a priority access handler. Multiple priority locks can be held
  /// at the same time while inferior locks can only be held when no priority lock is held.
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
            _parent.InferiorRelease();
        }
      }
    }

    private readonly object _syncObj = new object();

    //Tasks that complete immediately for fast path when there is no need to wait.
    private readonly Task<Releaser> _priorityReleaser;
    private readonly Task<Releaser> _inferiorReleaser;

    //Queue of waiting inferiors lock requesters
    private readonly Queue<TaskCompletionSource<Releaser>> _waitingInferiors = new Queue<TaskCompletionSource<Releaser>>();

    //Current number of priority locks.
    private int _priorityLocks;

    public AsyncPriorityLock()
    {
      _priorityReleaser = Task.FromResult(new Releaser(this, true));
      _inferiorReleaser = Task.FromResult(new Releaser(this, false));
    }

    /// <summary>
    /// Acquires an inferior lock.
    /// The acquired Releaser must be disposed to release the inferior lock.
    /// </summary>
    /// <returns></returns>
    public Releaser InferiorLock()
    {
      return InferiorLockAsync().Result;
    }

    /// <summary>
    /// Returns a task that completes when the inferior lock has been acquired.
    /// The acquired Releaser must be disposed to release the inferior lock.
    /// </summary>
    /// <returns></returns>
    public Task<Releaser> InferiorLockAsync()
    {
      lock (_syncObj)
      {
        if (_priorityLocks == 0)
        {
          return _inferiorReleaser;
        }
        else
        {
          var waiter = new TaskCompletionSource<Releaser>();
          _waitingInferiors.Enqueue(waiter);
          return waiter.Task;
        }
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
      lock (_syncObj)
      {
        ++_priorityLocks;
        return _priorityReleaser;
      }
    }

    private void InferiorRelease()
    {
      lock (_syncObj)
      {
        while (_priorityLocks == 0 && _waitingInferiors.Count > 0)
        {
          TaskCompletionSource<Releaser> toWake = _waitingInferiors.Dequeue();
          toWake.SetResult(new Releaser(this, false));
        }
      }
    }

    private void PriorityRelease()
    {
      lock (_syncObj)
      {
        --_priorityLocks;
        while (_priorityLocks == 0 && _waitingInferiors.Count > 0)
        {
          TaskCompletionSource<Releaser> toWake = _waitingInferiors.Dequeue();
          toWake.SetResult(new Releaser(this, false));
        }
      }
    }
  }
}
