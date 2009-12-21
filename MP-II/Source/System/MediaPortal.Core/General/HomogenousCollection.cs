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

using System;
using System.Collections;
using System.Collections.Generic;

namespace MediaPortal.Core.General
{
  /// <summary>
  /// Simple container for data objects of the same type.
  /// </summary>
  public class HomogenousCollection : ICollection<object>
  {
    #region Protected fields

    protected Type _type;
    protected ICollection<object> _values = new List<object>();

    #endregion

    public HomogenousCollection(Type type)
    {
      _type = type;
    }

    public Type DataType
    {
      get { return _type; }
    }

    #region ICollection<object> implementation

    public void Add(object item)
    {
      if (item != null && item.GetType() != _type)
        throw new ArgumentException(string.Format("HomogenousCollection for objects of type '{0}' cannot add an item of type '{1}'",
            _type.Name, item.GetType().Name));
      _values.Add(item);
    }

    public void Clear()
    {
      _values.Clear();
    }

    public bool Contains(object item)
    {
      return _values.Contains(item);
    }

    public void CopyTo(object[] array, int arrayIndex)
    {
      _values.CopyTo(array, arrayIndex);
    }

    public bool Remove(object item)
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

    #endregion

    #region IEnumerable<object> implementation

    public IEnumerator<object> GetEnumerator()
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