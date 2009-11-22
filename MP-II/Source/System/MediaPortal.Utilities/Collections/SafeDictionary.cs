#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System.Collections;
using System.Collections.Generic;

namespace MediaPortal.Utilities.Collections
{
  /// <summary>
  /// Implementation of <see cref="IDictionary{TKey,TValue}"/> which completely delegates to <see cref="Dictionary{TKey,TValue}"/>
  /// except the <see cref="this"/> getter, which returns <c>null</c> if no entry of the requested key is present instead of
  /// throwing a <see cref="KeyNotFoundException"/>.
  /// </summary>
  /// <typeparam name="TKey">Type of the keys of this dictionary.</typeparam>
  /// <typeparam name="TValue">Type of the values of this dictionary. This type parameter must be a class type.</typeparam>
  public class SafeDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TValue : class
  {
    public IDictionary<TKey, TValue> _elements = new Dictionary<TKey, TValue>();

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
      return _elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _elements.GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
      _elements.Add(item);
    }

    public void Clear()
    {
      _elements.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
      return _elements.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
      _elements.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
      return _elements.Remove(item);
    }

    public int Count
    {
      get { return _elements.Count; }
    }

    public bool IsReadOnly
    {
      get { return _elements.IsReadOnly; }
    }

    public bool ContainsKey(TKey key)
    {
      return _elements.ContainsKey(key);
    }

    public void Add(TKey key, TValue value)
    {
      _elements.Add(key, value);
    }

    public bool Remove(TKey key)
    {
      return _elements.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      return _elements.TryGetValue(key, out value);
    }

    public TValue this[TKey key]
    {
      get { return _elements.ContainsKey(key) ? _elements[key] : null; }
      set { _elements[key] = value; }
    }

    public ICollection<TKey> Keys
    {
      get { return _elements.Keys; }
    }

    public ICollection<TValue> Values
    {
      get { return _elements.Values; }
    }
  }
}
