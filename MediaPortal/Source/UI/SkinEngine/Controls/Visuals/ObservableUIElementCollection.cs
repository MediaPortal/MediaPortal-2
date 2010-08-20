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
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public delegate void CollectionChangedDlgt<T>(ObservableUIElementCollection<T> collection) where T : UIElement;

  public class ObservableUIElementCollection<T> : ICollection<T>, IDisposable, IAddChild<T> where T : UIElement
  {
    protected UIElement _parent;
    protected IList<T> _elements;

    public ObservableUIElementCollection(UIElement parent)
    {
      _parent = parent;
      _elements = new List<T>();
    }

    public CollectionChangedDlgt<T> CollectionChanged;

    public void Dispose()
    {
      Clear();
    }

    public T this[int index]
    {
      get { return _elements[index]; }
      set
      {
        if (value != _elements[index])
        {
          _elements[index] = value;
          FireCollectionChanged();
        }
      }
    }

    protected void FireCollectionChanged()
    {
      CollectionChangedDlgt<T> dlgt = CollectionChanged;
      if (dlgt != null)
        dlgt(this);
    }

    #region ICollection implementation

    public void Add(T element)
    {
      element.VisualParent = _parent;
      element.SetScreen(_parent == null ? null : _parent.Screen);
      _elements.Add(element);
      FireCollectionChanged();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      _elements.CopyTo(array, arrayIndex);
    }

    public bool Remove(T element)
    {
      bool result = _elements.Remove(element);
      FireCollectionChanged();
      return result;
    }

    public void Clear()
    {
      foreach (T element in _elements)
      {
        element.Deallocate();
        element.Dispose();
      }
      _elements.Clear();
      FireCollectionChanged();
    }

    public bool Contains(T item)
    {
      return _elements.Contains(item);
    }

    public int Count
    {
      get { return _elements.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    #endregion

    #region IEnumerable<UIElement> implementation

    public IEnumerator<T> GetEnumerator()
    {
      return _elements.GetEnumerator();
    }

    #endregion

    #region IEnumerable implementation

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _elements.GetEnumerator();
    }

    #endregion

    #region IAddChild<T> implementation

    public void AddChild(T child)
    {
      _elements.Add(child);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0}: Count={1}", typeof(ObservableUIElementCollection<T>).Name, Count);
    }

    #endregion
  }
}
