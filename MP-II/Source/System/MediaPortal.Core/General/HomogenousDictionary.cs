#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using System.Collections;
using System.Collections.Generic;

namespace MediaPortal.Core.General
{
  /// <summary>
  /// Simple container for data objects of the same type.
  /// </summary>
  public class HomogenousDictionary : IDictionary<object, object>
  {
    #region Protected fields

    protected Type _keyType;
    protected Type _valueType;
    protected IDictionary<object, object> _values = new Dictionary<object, object>();

    #endregion

    public HomogenousDictionary(Type keyType, Type valueType)
    {
      _keyType = keyType;
      _valueType = valueType;
    }

    public static HomogenousDictionary Create<TKey, TValue>(IDictionary<TKey, TValue> other)
    {
      HomogenousDictionary result = new HomogenousDictionary(typeof(TKey), typeof(TValue));
      foreach (KeyValuePair<TKey, TValue> pair in other)
        result.Add(pair.Key, pair.Value);
      return result;
    }

    public Type KeyType
    {
      get { return _keyType; }
    }

    public Type ValueType
    {
      get { return _valueType; }
    }

    #region IDictionary<object, object> implementation

    public bool ContainsKey(object key)
    {
      return _values.ContainsKey(key);
    }

    public void Add(object key, object value)
    {
      if (key == null)
        throw new NullReferenceException("Key must not be null");
      if (key.GetType() != _keyType)
        throw new ArgumentException(string.Format("HomogenousDictionary with key type '{0}' cannot add an item with a key of type '{1}'",
            _keyType.Name, key.GetType().Name));
      if (value.GetType() != _valueType)
        throw new ArgumentException(string.Format("HomogenousDictionary with value type '{0}' cannot add an item with a value of type '{1}'",
            _valueType.Name, value.GetType().Name));
      _values.Add(key, value);
    }

    public bool Remove(object item)
    {
      return _values.Remove(item);
    }

    public bool TryGetValue(object key, out object value)
    {
      return _values.TryGetValue(key, out value);
    }

    public object this[object key]
    {
      get { return _values[key]; }
      set { _values[key] = value; }
    }

    public ICollection<object> Keys
    {
      get { return _values.Keys; }
    }

    public ICollection<object> Values
    {
      get { return _values.Values; }
    }

    #endregion

    #region ICollection<KeyValuePair<object, object>> implementation

    public void Add(KeyValuePair<object, object> item)
    {
      if (item.Key == null)
        throw new NullReferenceException("Key must not be null");
      if (item.Key.GetType() != _keyType)
        throw new ArgumentException(string.Format("HomogenousDictionary with key type '{0}' cannot add an item with a key of type '{1}'",
            _keyType.Name, item.Key.GetType().Name));
      if (item.Value.GetType() != _valueType)
        throw new ArgumentException(string.Format("HomogenousDictionary with value type '{0}' cannot add an item with a value of type '{1}'",
            _valueType.Name, item.Value.GetType().Name));
      _values.Add(item);
    }

    public void Clear()
    {
      _values.Clear();
    }

    public bool Contains(KeyValuePair<object, object> item)
    {
      return _values.Contains(item);
    }

    public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
    {
      _values.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<object, object> item)
    {
      throw new NotImplementedException();
    }

    public int Count
    {
      get { return _values.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    #endregion

    #region IEnumerable<KeyValuePair<object, object>> implementation

    public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
    {
      return _values.GetEnumerator();
    }

    #endregion

    #region IEnumerable implementation

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion
  }
}