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

using System.Collections;
using System.Collections.Generic;
using MediaPortal.SkinEngine.Controls.Panels;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class UIElementCollection : IEnumerable<UIElement>
  {
    public class UIElementEnumerator : IEnumerator<UIElement>
    {
      int index = -1;
      List<UIElement> _elements;
      public UIElementEnumerator(List<UIElement> elements)
      {
        _elements = elements;
      }
      public UIElement Current
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

    UIElement _parent;
    List<UIElement> _elements;
    bool _zIndexFixed = false;

    public UIElementCollection(UIElement parent)
    {
      _parent = parent;
      _elements = new List<UIElement>();
    }

    public void FixZIndex()
    {
      if (MediaPortal.SkinEngine.SkinManagement.SkinContext.UseBatching == true)
        return;
      if (_zIndexFixed) return;
      _zIndexFixed = true;
      double zindex1 = 0;
      foreach (UIElement element in _elements)
      {
        if (Panel.GetZIndex(element) < 0)
        {
          Panel.SetZIndex(element, zindex1);
          zindex1 += 0.0001;
        }
        else
        {
          Panel.SetZIndex(element, Panel.GetZIndex(element)+zindex1);
          zindex1 += 0.0001;
        }
      }
    }

    public void SetParent(UIElement parent)
    {
      _parent = parent;
      foreach (UIElement element in _elements)
      {
        element.VisualParent = _parent;
      }
    }

    public void Add(UIElement element)
    {
      element.VisualParent = _parent;
      _elements.Add(element);
      if (_parent != null)
        _parent.Invalidate();
    }

    public void Remove(UIElement element)
    {
      _elements.Remove(element);
      if (_parent != null)
        _parent.Invalidate();
    }

    public void Clear()
    {
      foreach (UIElement element in _elements)
      {
        element.Deallocate();
      }
      _elements.Clear();
      if (_parent != null)
        _parent.Invalidate();
    }

    public int Count
    {
      get
      {
        return _elements.Count;
      }
    }

    public UIElement this[int index]
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
          if (_parent != null)
          {
            _elements[index].VisualParent = _parent;
            _parent.Invalidate();
          }
        }
      }
    }

    #region IEnumerable<UIElement> Members

    public IEnumerator<UIElement> GetEnumerator()
    {
      return new UIElementEnumerator(_elements);
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
      return new UIElementEnumerator(_elements);
    }

    #endregion
  }
}
