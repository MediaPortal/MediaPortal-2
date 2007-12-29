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
