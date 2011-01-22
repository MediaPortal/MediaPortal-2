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

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class FrameworkElementCollection : IEnumerable<FrameworkElement>, IDisposable, ISynchronizable
  {
    protected FrameworkElement _parent;
    protected IList<FrameworkElement> _elements;
    protected object _syncObj = new object();

    public FrameworkElementCollection(FrameworkElement parent)
    {
      _parent = parent;
      _elements = new List<FrameworkElement>();
    }

    public void Dispose()
    {
      _parent = null;
      Clear();
    }

    public object SyncRoot
    {
      get { return _syncObj; }
    }

    protected void InvalidateParent(bool invalidateMeasure, bool invalidateArrange)
    {
      if (_parent != null)
        _parent.InvalidateLayout(invalidateMeasure, invalidateArrange);
    }

    public void SetParent(FrameworkElement parent)
    {
      lock (_syncObj)
      {
        _parent = parent;
        foreach (FrameworkElement element in _elements)
        {
          if (_parent != null)
          {
            element.SetScreen(_parent.Screen);
            element.SetElementState(_parent.ElementState);
          }
          else
            element.SetElementState(ElementState.Available);
          element.VisualParent = _parent;
          element.InvalidateLayout(true, true);
        }
      }
    }

    public void Add(FrameworkElement element)
    {
      lock (_syncObj)
      {
        // TODO: Allocate if we are already allocated
        element.VisualParent = _parent;
        if (_parent != null)
        {
          element.SetScreen(_parent.Screen);
          element.SetElementState(_parent.ElementState);
        }
        else
          element.SetElementState(ElementState.Available);
        _elements.Add(element);
        element.InvalidateLayout(true, true);
      }
    }

    public void AddAll(IEnumerable<FrameworkElement> elements)
    {
      lock (_syncObj)
        foreach (FrameworkElement element in elements)
          Add(element);
    }

    public void Remove(FrameworkElement element)
    {
      lock (_syncObj)
      {
        if (_elements.Remove(element))
          element.CleanupAndDispose();
      }
      InvalidateParent(true, true);
    }

    public void Clear()
    {
      lock (_syncObj)
      {
        IList<FrameworkElement> oldElements = _elements;
        _elements = new List<FrameworkElement>();
        foreach (FrameworkElement element in oldElements)
          element.CleanupAndDispose();
      }
      InvalidateParent(true, true);
    }

    public int Count
    {
      get
      {
        lock (_syncObj)
          return _elements.Count;
      }
    }

    public FrameworkElement this[int index]
    {
      get
      {
        lock (_syncObj)
          return _elements[index];
      }
      set
      {
        lock (_syncObj)
        {
          FrameworkElement oldElement = _elements[index];
          if (value != oldElement)
          {
            oldElement.CleanupAndDispose();
            _elements[index] = value;
            value.VisualParent = _parent;
            if (_parent != null)
            {
              value.SetScreen(_parent.Screen);
              value.SetElementState(_parent.ElementState);
            }
            else
              value.SetElementState(ElementState.Available);
          }
        }
        value.InvalidateLayout(true, true);
      }
    }

    #region IEnumerable<UIElement> Members

    public IEnumerator<FrameworkElement> GetEnumerator()
    {
      return _elements.GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _elements.GetEnumerator();
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0}: Count={1}", typeof(FrameworkElementCollection).Name, Count);
    }

    #endregion
  }
}
