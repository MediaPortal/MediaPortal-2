#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;

namespace MediaPortal.Core.General
{
  /// <summary>
  /// Cache class which stores a fixed maximum amount of (key; value) mapping entries. An entry which
  /// is not among the last used entries will be discarded from the cache.
  /// </summary>
  /// <remarks>
  /// This cache implementation is performant for a low value of entries. An LRU cache with more than
  /// 100 entries shouldn't use this implementation.
  /// </remarks>
  /// <typeparam name="TKey">Key type parameter.</typeparam>
  /// <typeparam name="TValue">Value type parameter.</typeparam>
  public class SmallLRUCache<TKey, TValue>
  {
    #region Protected fields

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
        _cacheSize = value;
        Clip();
      }
    }

    /// <summary>
    /// Returns an enumeration over all available values. Enumerating over the returned
    /// enumeration neither will change the cache order nor will make cache items stay in cache
    /// if they are displaced in the meantime.
    /// </summary>
    public IEnumerable<TValue> Values
    {
      get
      {
        ICollection<TValue> result = new List<TValue>();
        foreach (KeyValuePair<TKey, TValue> entry in _data)
          result.Add(entry.Value);
        return result;
      }
    }

    /// <summary>
    /// Convenience implementation for method <see cref="Get(TKey,TValue)"/>. Will use the default
    /// value of <see cref="TValue"/> for the defVal parameter. This method should be used if <see cref="TValue"/>
    /// is a class.
    /// </summary>
    /// <param name="key">Key to retrieve the value for.</param>
    /// <returns>Value for the specified <paramref name="key"/> if it is present in the cache. Else
    /// the default value of <see cref="TValue"/> is returned.</returns>
    public TValue Get(TKey key)
    {
      return Get(key, default(TValue));
    }

    /// <summary>
    /// Returns the value which is stored in the cache for the specified <paramref name="key"/> if
    /// it is present in the cache.
    /// </summary>
    /// <param name="key">Key to retrieve the value for.</param>
    /// <param name="defVal">Default value to return if the key is not found in the cache.</param>
    /// <returns>Value for the specified <paramref name="key"/> if it is present in the cache. Else
    /// <paramref name="defVal"/> is returned.</returns>
    public TValue Get(TKey key, TValue defVal)
    {
      LinkedListNode<KeyValuePair<TKey, TValue>> current = _data.First;
      while (current != null && !current.Value.Value.Equals(key))
        current = current.Next;
      if (current != null)
      {
        _data.Remove(current);
        _data.AddFirst(current);
      }
      return defVal;
    }

    /// <summary>
    /// This method has to be called when a cache entry is used. It will set the entry to the beginning of
    /// the backing LRU list.
    /// </summary>
    /// <param name="key">Key of the used entry.</param>
    /// <param name="value">Value of the used entry.</param>
    public void NotifyUsage(TKey key, TValue value)
    {
      LinkedListNode<KeyValuePair<TKey, TValue>> current = _data.First;
      while (current != null && !current.Value.Value.Equals(key))
        current = current.Next;
      if (current == null)
        current = new LinkedListNode<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(key, value));
      else
      {
        _data.Remove(current);
        current.Value = new KeyValuePair<TKey, TValue>(key, value);
      }
      _data.AddFirst(current);
      Clip();
    }

    /// <summary>
    /// Clips the backing LRU cache list if it is bigger than the specified <see cref="CacheSize"/>.
    /// </summary>
    protected void Clip()
    {
      while (_data.Count >= _cacheSize)
        _data.RemoveLast();
    }
  }
}
