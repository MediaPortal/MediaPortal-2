#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public delegate void CollectionChangedDlgt<T>(ObservableUIElementCollection<T> collection) where T : FrameworkElement;

  public class ObservableUIElementCollection<T> : ICollection<T>, IDisposable, IAddChild<T>, ISynchronizable where T : FrameworkElement
  {
    protected FrameworkElement _parent;
    protected IList<T> _elements;
    protected object _syncObj = new object();

    public ObservableUIElementCollection(FrameworkElement parent)
    {
      _parent = parent;
      _elements = new List<T>();
    }

    public CollectionChangedDlgt<T> CollectionChanged;

    public void Dispose()
    {
      _parent = null;
      CollectionChanged = null;
      Clear();
    }

    public object SyncRoot
    {
      get { return _syncObj; }
    }

    public T this[int index]
    {
      get { return _elements[index]; }
      set
      {
        lock (_syncObj)
          if (value != _elements[index])
          {
            _elements[index] = value;
            FireCollectionChanged();
          }
      }
    }

    public void SetParent(FrameworkElement parent)
    {
      _parent = parent;
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
      lock (_syncObj)
      {
        element.VisualParent = _parent;
        if (_parent != null)
        {
          element.SetScreen(_parent.Screen);
          element.SetElementState(_parent.ElementState);
        }
        else
          element.SetElementState(ElementState.Available);
        _elements.Add(element);
        FireCollectionChanged();
      }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      lock (_syncObj)
        _elements.CopyTo(array, arrayIndex);
    }

    public bool Remove(T element)
    {
      lock (_syncObj)
      {
        bool result = _elements.Remove(element);
        element.CleanupAndDispose();
        FireCollectionChanged();
        return result;
      }
    }

    public void Clear()
    {
      lock (_syncObj)
      {
        foreach (T element in _elements)
          element.CleanupAndDispose();
        _elements.Clear();
        FireCollectionChanged();
      }
    }

    public bool Contains(T item)
    {
      lock (_syncObj)
        return _elements.Contains(item);
    }

    public int Count
    {
      get
      {
        lock (_syncObj)
          return _elements.Count;
      }
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
      lock (_syncObj)
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
