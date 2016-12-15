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

using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Utilities.Cache
{
  /// <summary>
  /// Cache class which stores a fixed maximum amount of (key; value) mapping entries. An entry which
  /// is not among the last used entries will be discarded from the cache.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This cache implementation is performant for a low value of entries. An LRU cache with more than
  /// about 100 entries shouldn't use this implementation.
  /// </para>
  /// <para>
  /// This implementation is multithreading-safe.
  /// </para>
  /// </remarks>
  /// <typeparam name="TKey">Key type parameter.</typeparam>
  /// <typeparam name="TValue">Value type parameter.</typeparam>
  public class SmallLRUCache<TKey, TValue> : ILRUCache<TKey, TValue>
  {
    #region Protected fields

    protected object _syncObj = new object();
    protected LinkedList<KeyValuePair<TKey, TValue>> _data = new LinkedList<KeyValuePair<TKey, TValue>>();
    protected int _cacheSize;

    #endregion

    public event ObjectPrunedDlgt<TKey, TValue> ObjectPruned;

    /// <summary>
    /// Creates a new instance of the <see cref="SmallLRUCache{TKey,TValue}"/> with the specified
    /// <paramref name="cacheSize"/>.
    /// </summary>
    /// <param name="cacheSize">Maximum size of this LRU cache. The size should be small, see the
    /// class docs.</param>
    public SmallLRUCache(int cacheSize)
    {
      _cacheSize = cacheSize;
    }

    /// <summary>
    /// Gets or sets the cache size.
    /// The size should be small for this implementation, see the class docs.
    /// </summary>
    public int CacheSize
    {
      get { return _cacheSize; }
      set
      {
        lock (_syncObj)
        {
          _cacheSize = value;
          Clip();
        }
      }
    }

    public ICollection<TValue> Values
    {
      get
      {
        lock (_syncObj)
          return _data.Select(entry => entry.Value).ToList();
      }
    }

    public ICollection<TKey> Keys
    {
      get
      {
        lock (_syncObj)
          return _data.Select(entry => entry.Key).ToList();
      }
    }

    /// <summary>
    /// Returns the synchronization object which is used to lock the critical sections of this cache.
    /// </summary>
    public object SyncObj
    {
      get { return _syncObj; }
    }

    public void Add(TKey key, TValue value)
    {
      lock (_syncObj)
      {
        LinkedListNode<KeyValuePair<TKey, TValue>> current = FindEntry(key);
        if (current != null)
          _data.Remove(current);
        _data.AddFirst(new KeyValuePair<TKey, TValue>(key, value));
        Clip();
      }
    }

    public void Remove(TKey key)
    {
      lock (_syncObj)
      {
        LinkedListNode<KeyValuePair<TKey, TValue>> current = FindEntry(key);
        if (current != null)
          _data.Remove(current);
      }
    }

    public bool Contains(TKey key)
    {
      lock (_syncObj)
        return FindEntry(key) != null;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      lock (_syncObj)
      {
        LinkedListNode<KeyValuePair<TKey, TValue>> current = FindEntry(key);
        if (current != null)
        {
          _data.Remove(current);
          _data.AddFirst(current);
          value = current.Value.Value;
          return true;
        }
      }
      value = default(TValue);
      return false;
    }

    public void Touch(TKey key)
    {
      lock (_syncObj)
      {
        LinkedListNode<KeyValuePair<TKey, TValue>> current = FindEntry(key);
        if (current == null)
          return;
        _data.Remove(current);
        _data.AddFirst(current);
        Clip();
      }
    }

    public void Clear()
    {
      lock (_syncObj)
        _data.Clear();
    }

    protected LinkedListNode<KeyValuePair<TKey, TValue>> FindEntry(TKey key)
    {
      lock (_syncObj)
      {
        LinkedListNode<KeyValuePair<TKey, TValue>> current = _data.First;
        while (current != null && !current.Value.Key.Equals(key))
          current = current.Next;
        return current;
      }
    }

    /// <summary>
    /// Clips the backing LRU cache list if it is bigger than the specified <see cref="CacheSize"/>.
    /// </summary>
    protected void Clip()
    {
      lock (_syncObj)
        while (_data.Count >= _cacheSize)
        {
          LinkedListNode<KeyValuePair<TKey, TValue>> element = _data.Last;
          _data.Remove(element);
          FireObjectPruned(element.Value.Key, element.Value.Value);
        }
    }

    protected void FireObjectPruned(TKey key, TValue value)
    {
      ObjectPrunedDlgt<TKey, TValue> dlgt = ObjectPruned;
      if (dlgt != null)
        dlgt(this, key, value);
    }
  }
}
