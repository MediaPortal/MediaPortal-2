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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace MediaPortal.Mock
{
  class MockDataParameterCollection : IDataParameterCollection
  {
    private IList<IDbDataParameter> _parameters = new List<IDbDataParameter>();

    public bool Contains(string parameterName)
    {
      throw new NotImplementedException();
    }

    public int IndexOf(string parameterName)
    {
      throw new NotImplementedException();
    }

    public void RemoveAt(string parameterName)
    {
      throw new NotImplementedException();
    }

    public object this[string parameterName]
    {
      get
      {
        throw new NotImplementedException();
      }
      set
      {
        throw new NotImplementedException();
      }
    }

    public int Add(object value)
    {
      IDbDataParameter parameter = (IDbDataParameter)value;
      _parameters.Add(parameter);
      return _parameters.Count;
    }

    public void Clear()
    {
      throw new NotImplementedException();
    }

    public bool Contains(object value)
    {
      throw new NotImplementedException();
    }

    public int IndexOf(object value)
    {
      throw new NotImplementedException();
    }

    public void Insert(int index, object value)
    {
      throw new NotImplementedException();
    }

    public bool IsFixedSize
    {
      get { throw new NotImplementedException(); }
    }

    public bool IsReadOnly
    {
      get { throw new NotImplementedException(); }
    }

    public void Remove(object value)
    {
      throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
      throw new NotImplementedException();
    }

    public object this[int index]
    {
      get
      {
        return _parameters[index];
      }
      set
      {
        throw new NotImplementedException();
      }
    }

    public void CopyTo(Array array, int index)
    {
      throw new NotImplementedException();
    }

    public int Count
    {
      get { return _parameters.Count; }
    }

    public bool IsSynchronized
    {
      get { throw new NotImplementedException(); }
    }

    public object SyncRoot
    {
      get { throw new NotImplementedException(); }
    }

    public IEnumerator GetEnumerator()
    {
      return _parameters.GetEnumerator();
    }
  }
}