#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common.General;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UI.Presentation.DataObjects
{
  /// <summary>
  /// A thread safe collection of key/value pairs that can be accessed by multiple threads concurrently and implements <see cref="IObservable"/>.
  /// </summary>
  public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IObservable
  {
    protected object _syncRoot = new object();
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();
    protected ConcurrentDictionary<TKey, TValue> _backingDictionary = new ConcurrentDictionary<TKey, TValue>();

    protected Func<TKey, TValue> _valueFactory;

    public ObservableDictionary(Func<TKey, TValue> valueFactory)
    {
      _valueFactory = valueFactory;
    }

    /// <summary>
    /// Event which gets fired when the collection changes.
    /// </summary>
    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    public void FireChange()
    {
      _objectChanged.Fire(new object[] { this });
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// If getting, and the key doesn't exist, creates a new value using the valueFactory
    /// passed in the constructor of this instance and returns it.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    public TValue this[TKey key]
    {
      get { return _backingDictionary.GetOrAdd(key, _valueFactory); }
      set { Add(key, value); }
    }

    public ICollection<TKey> Keys
    {
      get { return _backingDictionary.Keys; }
    }

    public ICollection<TValue> Values
    {
      get { return _backingDictionary.Values; }
    }

    public int Count
    {
      get { return _backingDictionary.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    public void Add(TKey key, TValue value)
    {
      _backingDictionary.AddOrUpdate(key, value, (k, v) => v);
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
      ((IDictionary<TKey, TValue>)_backingDictionary).Add(item);
    }

    public void Clear()
    {
      _backingDictionary.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
      return _backingDictionary.Contains(item);
    }

    public bool ContainsKey(TKey key)
    {
      return _backingDictionary.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
      ((IDictionary<TKey, TValue>)_backingDictionary).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
      return _backingDictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _backingDictionary.GetEnumerator();
    }

    public bool Remove(TKey key)
    {
      return _backingDictionary.TryRemove(key, out _);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
      return ((IDictionary<TKey, TValue>)_backingDictionary).Remove(item);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      return _backingDictionary.TryGetValue(key, out value);
    }
  }
}
