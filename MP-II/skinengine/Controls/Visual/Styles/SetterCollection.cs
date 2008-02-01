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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Visuals.Styles
{
  public class SetterCollection : IEnumerable<Setter>
  {
    public class SetterEnumerator : IEnumerator<Setter>
    {
      int index = -1;
      List<Setter> _elements;
      public SetterEnumerator(List<Setter> elements)
      {
        _elements = elements;
      }
      public Setter Current
      {
        get
        {
          return _elements[index];
        }
      }

      public void Dispose()
      {
      }

      object System.Collections.IEnumerator.Current
      {
        get
        {
          return _elements[index];
        }
      }

      public bool MoveNext()
      {
        index++;
        return (index < _elements.Count);
      }

      public void Reset()
      {
        index = -1;
      }
    }

    List<Setter> _elements;

    public SetterCollection()
    {
      _elements = new List<Setter>();
    }

    public void Add(Setter element)
    {
      _elements.Add(element);
    }

    public void Remove(Setter element)
    {
      _elements.Remove(element);
    }

    public void Clear()
    {
      _elements.Clear();
    }

    public int Count
    {
      get
      {
        return _elements.Count;
      }
    }

    public Setter this[int index]
    {
      get
      {
        return _elements[index];
      }
      set
      {
        if (value != _elements[index])
        {
          _elements[index] = value;
        }
      }
    }


    #region IEnumerable<Setter> Members

    public IEnumerator<Setter> GetEnumerator()
    {
      return new SetterEnumerator(_elements);
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
      return new SetterEnumerator(_elements);
    }

    #endregion
  }
}
