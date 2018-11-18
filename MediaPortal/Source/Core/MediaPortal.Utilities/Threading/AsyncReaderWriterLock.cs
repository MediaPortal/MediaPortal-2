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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Threading
{
  /// <summary>
  /// A class that acts like a <see cref="System.Threading.ReaderWriterLock"/>, but additionally can be "awaited" asynchronously.
  /// </summary>
  public class AsyncReaderWriterLock
  {
    public struct Releaser : IDisposable
    {
      private AsyncReaderWriterLock _parent;
      private readonly bool _isWriter;

      internal Releaser(AsyncReaderWriterLock toRelease, bool isWriter)
      {
        _parent = toRelease;
        _isWriter = isWriter;
      }

      public void Dispose()
      {
        if (_parent != null)
        {
          if (_isWriter)
            _parent.WriterRelease();
          else
            _parent.ReaderRelease();
        }
      }
    }

    private readonly object _syncObj = new object();

    //Tasks that complete immediately for fast path when there is no need to wait.
    private readonly Task<Releaser> _readerReleaser;
    private readonly Task<Releaser> _writerReleaser;

    //Queue of waiting writers
    private readonly Queue<TaskCompletionSource<Releaser>> _waitingWriters = new Queue<TaskCompletionSource<Releaser>>();

    //Task that completes when waiting readers can read.
    private TaskCompletionSource<Releaser> _waitingReader = new TaskCompletionSource<Releaser>();

    //Number of waiting readers
    private int _readersWaiting;

    //Current status of lock. -1 if currently writing, otherwise number of active readers.
    private int _status;

    public AsyncReaderWriterLock()
    {
      _readerReleaser = Task.FromResult(new Releaser(this, false));
      _writerReleaser = Task.FromResult(new Releaser(this, true));
    }

    /// <summary>
    /// Aquires a reader lock.
    /// The aquired Releaser must be disposed to release the read lock.
    /// </summary>
    /// <returns></returns>
    public Releaser ReaderLock()
    {
      return ReaderLockAsync().Result;
    }

    /// <summary>
    /// Returns a task that completes when the read lock has been aquired.
    /// The aquired Releaser must be disposed to release the read lock.
    /// </summary>
    /// <returns></returns>
    public Task<Releaser> ReaderLockAsync()
    {
      lock (_syncObj)
      {
        if (_status >= 0 && _waitingWriters.Count == 0)
        {
          ++_status;
          return _readerReleaser;
        }
        else
        {
          ++_readersWaiting;
          return _waitingReader.Task.ContinueWith(t => t.Result);
        }
      }
    }

    /// <summary>
    /// Aquires a writer lock.
    /// The aquired Releaser must be disposed to release the write lock.
    /// </summary>
    /// <returns></returns>
    public Releaser WriterLock()
    {
      return WriterLockAsync().Result;
    }

    /// <summary>
    /// Returns a task that completes when the write lock has been aquired.
    /// The aquired Releaser must be disposed to release the write lock.
    /// </summary>
    /// <returns></returns>
    public Task<Releaser> WriterLockAsync()
    {
      lock (_syncObj)
      {
        if (_status == 0)
        {
          _status = -1;
          return _writerReleaser;
        }
        else
        {
          var waiter = new TaskCompletionSource<Releaser>();
          _waitingWriters.Enqueue(waiter);
          return waiter.Task;
        }
      }
    }

    private void ReaderRelease()
    {
      TaskCompletionSource<Releaser> toWake = null;
      lock (_syncObj)
      {
        if (_status > 0)
          --_status;

        if (_status == 0 && _waitingWriters.Count > 0)
        {
          _status = -1;
          toWake = _waitingWriters.Dequeue();
        }
      }
      if (toWake != null)
        toWake.SetResult(new Releaser(this, true));
    }

    private void WriterRelease()
    {
      TaskCompletionSource<Releaser> toWake = null;
      bool toWakeIsWriter = false;
      lock (_syncObj)
      {
        if (_waitingWriters.Count > 0)
        {
          toWake = _waitingWriters.Dequeue();
          toWakeIsWriter = true;
        }
        else if (_readersWaiting > 0)
        {
          toWake = _waitingReader;
          _status = _readersWaiting;
          _readersWaiting = 0;
          _waitingReader = new TaskCompletionSource<Releaser>();
        }
        else _status = 0;
      }

      if (toWake != null)
        toWake.SetResult(new Releaser(this, toWakeIsWriter));
    }
  }
}