#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
  public class SmallLRUCache<TKey, TValue>
  {
    #region Protected fields

    protected object _syncObj = new object();
    protected LinkedList<KeyValuePair<TKey, TValue>> _data = new LinkedList<KeyValuePair<TKey, TValue>>();
    protected int _cacheSize;

    #endregion

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
    /// Gets or sets the cache size. The cache will be truncated if its size is bigger than
    /// the value set to this property.
    /// The size should be small, see the class docs.
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

    /// <summary>
    /// Returns a collection with all available values.
    /// </summary>
    public ICollection<TValue> Values
    {
      get
      {
        lock (_syncObj)
          return _data.Select(entry => entry.Value).ToList();
      }
    }

    /// <summary>
    /// Returns a collection with all available keys.
    /// </summary>
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

    /// <summary>
    /// Adds the given key; value pair to this cache.
    /// </summary>
    /// <remarks>
    /// If the given <paramref name="key"/> already exists, the new entry will replace the old entry.
    /// </remarks>
    /// <param name="key">Key of the new entry.</param>
    /// <param name="value">Value of the new entry.</param>
    public void Add(TKey key, TValue value)
    {
      lock (_syncObj)
      {
        LinkedListNode<KeyValuePair<TKey, TValue>> current = FindEntry(key);
        if (current != null)
          _data.Remove(current);
        _data.AddFirst(new KeyValuePair<TKey, TValue>(key, value));
      }
    }

    /// <summary>
    /// Removes the entry with the given key.
    /// </summary>
    /// <param name="key">Key of the entry to remove.</param>
    public void Remove(TKey key)
    {
      lock (_syncObj)
      {
        LinkedListNode<KeyValuePair<TKey, TValue>> current = FindEntry(key);
        if (current != null)
          _data.Remove(current);
      }
    }

    /// <summary>
    /// Returns the value which is stored in the cache for the specified <paramref name="key"/> if
    /// it is present in the cache.
    /// </summary>
    /// <param name="key">Key to retrieve the value for.</param>
    /// <param name="value">Contained value if the key is found in the cache.</param>
    /// <returns><c>true</c>, if the given <paramref name="key"/> is found in the cache, else <c>false</c>.</returns>
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

    /// <summary>
    /// This method has to be called when a cache entry is used. It will set the entry to the beginning of the backing LRU list.
    /// </summary>
    /// <param name="key">Key of the used entry.</param>
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

    /// <summary>
    /// Removes all entries from this cache.
    /// </summary>
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
        while (current != null && !current.Value.Value.Equals(key))
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
          _data.RemoveLast();
    }
  }
}
