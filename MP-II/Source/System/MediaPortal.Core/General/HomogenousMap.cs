#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
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
  public class HomogenousMap : ICollection<KeyValuePair<object, object>>
  {
    public static readonly object NULL = new object();

    internal class Enumerator : IEnumerator<KeyValuePair<object, object>>
    {
      protected IEnumerator<KeyValuePair<object, object>> _underlayingEnumerator;

      public Enumerator(IEnumerator<KeyValuePair<object, object>> underlayingEnumerator)
      {
        _underlayingEnumerator = underlayingEnumerator;
      }

      #region IDisposable implementation

      public void Dispose()
      {
        _underlayingEnumerator.Dispose();
      }

      #endregion

      #region Implementation of IEnumerator

      public bool MoveNext()
      {
        return _underlayingEnumerator.MoveNext();
      }

      public void Reset()
      {
        _underlayingEnumerator.Reset();
      }

      public KeyValuePair<object, object> Current
      {
        get
        {
          KeyValuePair<object, object> current = _underlayingEnumerator.Current;
          if (current.Key == NULL)
            return new KeyValuePair<object, object>(null, current.Value);
          return current;
        }
      }

      object IEnumerator.Current
      {
        get { return ((IEnumerator<KeyValuePair<object, object>>) this).Current; }
      }

      #endregion
    }

    #region Protected fields

    protected Type _keyType;
    protected Type _valueType;
    protected IDictionary<object, object> _values = new Dictionary<object, object>();

    #endregion

    public HomogenousMap(Type keyType, Type valueType)
    {
      _keyType = keyType;
      _valueType = valueType;
    }

    public Type KeyType
    {
      get { return _keyType; }
    }

    public Type ValueType
    {
      get { return _valueType; }
    }

    protected void CheckKey(object key)
    {
      if (key != null && key.GetType() != _keyType)
        throw new ArgumentException(string.Format("HomogenousMap with key type '{0}' cannot add an item with a key of type '{1}'",
            _keyType.Name, key.GetType().Name));
    }

    protected void CheckValue(object value)
    {
      if (value != null && value.GetType() != _valueType)
        throw new ArgumentException(string.Format("HomogenousMap with value type '{0}' cannot add an item with a value of type '{1}'",
            _valueType.Name, value.GetType().Name));
    }

    public bool ContainsKey(object key)
    {
      return _values.ContainsKey(key);
    }

    public void Add(object key, object value)
    {
      CheckKey(key);
      CheckValue(value);
      _values.Add(key ?? NULL, value);
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
      set
      {
        CheckKey(key);
        CheckValue(value);
        _values[key] = value;
      }
    }

    public ICollection<object> Keys
    {
      get { return _values.Keys; }
    }

    public ICollection<object> Values
    {
      get { return _values.Values; }
    }

    #region ICollection<KeyValuePair<object, object>> implementation

    public bool Remove(KeyValuePair<object, object> item)
    {
      return _values.Remove(item);
    }

    public int Count
    {
      get { return _values.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    public void Add(KeyValuePair<object, object> item)
    {
      Add(item.Key, item.Value);
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

    #endregion

    #region IEnumerable<KeyValuePair<object, object>> implementation

    public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
    {
      return new Enumerator(_values.GetEnumerator());
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