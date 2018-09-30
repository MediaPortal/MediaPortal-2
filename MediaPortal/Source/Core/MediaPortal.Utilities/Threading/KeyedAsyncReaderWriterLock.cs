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
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Threading
{
  /// <summary>
  /// A class that maintains <see cref="AsyncReaderWriterLock"/>s for multiple keys.
  /// Allows fine grained asynchronous reader-writer lock semantics.
  /// </summary>
  /// <typeparam name="TKey">The type of the key.</typeparam>
  public class KeyedAsyncReaderWriterLock<TKey>
  {
    #region Internal classes

    /// <summary>
    /// Wraps an <see cref="AsyncReaderWriterLock"/> and maintains a reference count to the lock.
    /// </summary>
    protected class AsyncReaderWriterWrapper
    {
      private AsyncReaderWriterLock _lock;
      private int _referenceCount;

      internal AsyncReaderWriterWrapper()
      {
        _lock = new AsyncReaderWriterLock();
      }
      
      public AsyncReaderWriterLock Lock
      {
        get { return _lock; }
      }

      public int ReferenceCount
      {
        get { return _referenceCount; }
        set { _referenceCount = value; }
      }
    }
    
    /// <summary>
    /// Class that represents a lock for a key.
    /// This class must be disposed to release the lock.
    /// </summary>
    public class Releaser : IDisposable
    {
      private TKey _key;
      private KeyedAsyncReaderWriterLock<TKey> _parent;
      private AsyncReaderWriterLock.Releaser _child;

      public Releaser(TKey key, KeyedAsyncReaderWriterLock<TKey> parent, AsyncReaderWriterLock.Releaser child)
      {
        _key = key;
        _parent = parent;
        _child = child;
      }

      public void Dispose()
      {
        //release the lock held by the child AsyncReaderWriterLock
        _child.Dispose();
        //Decrement the reference count for this key
        _parent.ReleaseLock(_key);
      }
    }

    #endregion

    protected readonly object _syncObj;
    protected IDictionary<TKey, AsyncReaderWriterWrapper> _locks;
    protected IList<TKey> _unusedKeys; 
    protected int _capacity;
    protected int _maxCapacity;

    public KeyedAsyncReaderWriterLock(int capacity = 50)
    {
      _syncObj = new object();
      _locks = new Dictionary<TKey, AsyncReaderWriterWrapper>();
      _unusedKeys = new List<TKey>();
      _capacity = capacity;
      _maxCapacity = capacity * 2;
    }

    /// <summary>
    /// Aquires a reader lock for the specified <paramref name="key"/>.
    /// The aquired Releaser must be disposed to release the read lock.
    /// </summary>
    /// <returns></returns>
    public Releaser ReaderLock(TKey key)
    {
      return ReaderLockAsync(key).Result;
    }

    /// <summary>
    /// Returns a task that completes when the read lock for the specified <paramref name="key"/> has been aquired.
    /// The aquired Releaser must be disposed to release the read lock.
    /// </summary>
    /// <returns></returns>
    public Task<Releaser> ReaderLockAsync(TKey key)
    {
      //Get or create the lock for this key
      AsyncReaderWriterWrapper wrapper = GetLockForKeyAndIncrement(key);
      //Obtain the read lock
      return wrapper.Lock.ReaderLockAsync().ContinueWith(t => new Releaser(key, this, t.Result));
    }

    /// <summary>
    /// Aquires a writer lock for the specified <paramref name="key"/>.
    /// The aquired Releaser must be disposed to release the write lock.
    /// </summary>
    /// <returns></returns>
    public Releaser WriterLock(TKey key)
    {
      return WriterLockAsync(key).Result;
    }

    /// <summary>
    /// Returns a task that completes when the write lock for the specified <paramref name="key"/> has been aquired.
    /// The aquired Releaser must be disposed to release the write lock.
    /// </summary>
    /// <returns></returns>
    public Task<Releaser> WriterLockAsync(TKey key)
    {
      //Get or create the lock for this key
      AsyncReaderWriterWrapper wrapper = GetLockForKeyAndIncrement(key);
      //Obtain the write lock
      return wrapper.Lock.WriterLockAsync().ContinueWith(t => new Releaser(key, this, t.Result));
    }

    /// <summary>
    /// Gets or creates the <see cref="AsyncReaderWriterWrapper"/> for the specified <paramref name="key"/> and increments the number
    /// of references to the <see cref="AsyncReaderWriterWrapper"/>.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected AsyncReaderWriterWrapper GetLockForKeyAndIncrement(TKey key)
    {
      lock (_syncObj)
      {
        AsyncReaderWriterWrapper wrapper;
        if (!_locks.TryGetValue(key, out wrapper))
          _locks[key] = wrapper = new AsyncReaderWriterWrapper();
        else if(wrapper.ReferenceCount == 0)
          _unusedKeys.Remove(key);

        wrapper.ReferenceCount++;
        return wrapper;
      }
    }

    /// <summary>
    /// Releases the <see cref="AsyncReaderWriterWrapper"/> for the specified <paramref name="key"/> and decrements
    /// the number of references to the <see cref="AsyncReaderWriterWrapper"/>.
    /// </summary>
    /// <param name="key"></param>
    protected void ReleaseLock(TKey key)
    {
      lock (_syncObj)
      {
        AsyncReaderWriterWrapper wrapper;
        if (!_locks.TryGetValue(key, out wrapper))
          throw new InvalidOperationException($"{GetType().Name}: Tried to release a non existant lock with key '{key}'");
        if(wrapper.ReferenceCount < 1)
          throw new InvalidOperationException($"{GetType().Name}: Tried to release a lock with key '{key}' that has already been released");

        wrapper.ReferenceCount--;
        if (wrapper.ReferenceCount == 0)
          _unusedKeys.Add(key);
        Cleanup();
      }
    }

    protected void Cleanup()
    {
      if (_unusedKeys.Count == 0)
        return;
      int currentCount = _locks.Count;
      if (currentCount < _maxCapacity)
        return;
      var keysToRemove = _unusedKeys.Take(currentCount - _capacity).ToArray();
      foreach (TKey key in keysToRemove)
      {
        _locks.Remove(key);
        _unusedKeys.Remove(key);
      }
    }
  }
}
