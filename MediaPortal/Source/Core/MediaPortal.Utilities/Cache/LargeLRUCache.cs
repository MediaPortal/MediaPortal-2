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
  /// This cache implementation is performant for a high value of entries. An LRU cache with more than
  /// about 100 entries should use this implementation.
  /// </para>
  /// <para>
  /// For performance reasons this implementation is NOT multithreading-safe.
  /// </para>
  /// </remarks>
  /// <typeparam name="TKey">Key type parameter.</typeparam>
  /// <typeparam name="TValue">Value type parameter.</typeparam>
  public class LargeLRUCache<TKey, TValue> : ILRUCache<TKey, TValue>
  {
    protected int _cacheSize;
    protected IDictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _dataDictionary;
    protected LinkedList<KeyValuePair<TKey, TValue>> _dataList;

    public LargeLRUCache(int cacheSize)
    {
      _cacheSize = cacheSize;
      _dataDictionary = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
      _dataList = new LinkedList<KeyValuePair<TKey, TValue>>();
    }

    public int CacheSize
    {
      get { return _cacheSize; }
      set
      {
        _cacheSize = value;
        Clip();
      }
    }

    public ICollection<TValue> Values
    {
      get { return new List<TValue>(_dataList.Select(n => n.Value)); }
    }

    public ICollection<TKey> Keys
    {
      get { return new List<TKey>(_dataDictionary.Keys); }
    }

    public event ObjectPrunedDlgt<TKey, TValue> ObjectPruned;

    protected virtual void OnObjectPruned(TKey key, TValue value)
    {
      ObjectPruned?.Invoke(this, key, value);
    }

    public void Add(TKey key, TValue value)
    {
      LinkedListNode<KeyValuePair<TKey, TValue>> node;
      if (_dataDictionary.TryGetValue(key, out node))
      {
        if (node != _dataList.Last)
          Touch(node);
      }
      else
      {
        KeyValuePair<TKey, TValue> cacheValue = new KeyValuePair<TKey, TValue>(key, value);
        node = new LinkedListNode<KeyValuePair<TKey, TValue>>(cacheValue);
        _dataDictionary.Add(key, node);
        _dataList.AddLast(node);
        Clip();
      }
    }

    public void Clear()
    {
      _dataDictionary.Clear();
      _dataList.Clear();
    }

    public bool Contains(TKey key)
    {
      return _dataDictionary.ContainsKey(key);
    }

    public void Remove(TKey key)
    {
      LinkedListNode<KeyValuePair<TKey, TValue>> node;
      if (!_dataDictionary.TryGetValue(key, out node))
        return;
      _dataDictionary.Remove(key);
      _dataList.Remove(node);
    }

    public void Touch(TKey key)
    {
      LinkedListNode<KeyValuePair<TKey, TValue>> node;
      if (_dataDictionary.TryGetValue(key, out node))
        Touch(node);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      LinkedListNode<KeyValuePair<TKey, TValue>> node;
      if (_dataDictionary.TryGetValue(key, out node))
      {
        value = node.Value.Value;
        Touch(node);
        return true;
      }
      value = default(TValue);
      return false;
    }

    protected void Touch(LinkedListNode<KeyValuePair<TKey, TValue>> node)
    {
      if (node == _dataList.Last)
        return;
      _dataList.Remove(node);
      _dataList.AddLast(node);
    }

    protected void Clip()
    {
      while (_dataList.Count >= _cacheSize)
      {
        LinkedListNode<KeyValuePair<TKey, TValue>> node = _dataList.First;
        _dataList.RemoveFirst();
        _dataDictionary.Remove(node.Value.Key);
        OnObjectPruned(node.Value.Key, node.Value.Value);
      }
    }
  }
}
