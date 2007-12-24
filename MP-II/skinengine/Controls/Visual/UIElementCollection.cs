using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Visuals
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

    public UIElementCollection(UIElement parent)
    {
      _parent = parent;
      _elements = new List<UIElement>();
    }

    public void Add(UIElement element)
    {
      element.VisualParent = _parent;
      _elements.Add(element);
      _parent.Invalidate();
    }

    public void Remove(UIElement element)
    {
      _elements.Remove(element);
      _parent.Invalidate();
    }

    public void Clear()
    {
      _elements.Clear();
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
          _elements[index].VisualParent = _parent;
          _parent.Invalidate();
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
