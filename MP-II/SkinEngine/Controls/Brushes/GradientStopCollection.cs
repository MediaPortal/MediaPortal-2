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
using MediaPortal.Presentation.DataObjects;

namespace MediaPortal.SkinEngine.Controls.Brushes
{
  public class GradientStopCollection : IEnumerable<GradientStop>
  {
    public class GradientStopEnumerator : IEnumerator<GradientStop>
    {
      int index = -1;
      List<GradientStop> _elements;
      public GradientStopEnumerator(List<GradientStop> elements)
      {
        _elements = elements;
      }

      public GradientStop Current
      {
        get
        {
          return _elements[index];
        }
      }

      public void Dispose()
      {
      }

      object IEnumerator.Current
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

    #region Private fields

    GradientBrush _parent;
    List<GradientStop> _elements;
    PropertyChangedHandler _handler;

    #endregion

    #region Ctor

    public GradientStopCollection(GradientBrush parent)
    {
      _parent = parent;
      Init();
    }

    public GradientStopCollection(GradientStopCollection source)
    {
      _parent = null;
      Init();
      foreach (GradientStop s in source)
        Add(new GradientStop(s.Offset, s.Color));
    }

    void Init()
    {
      _elements = new List<GradientStop>();
      _handler = OnStopChanged;
    }

    #endregion

    void OnStopChanged(Property prop)
    {
      if (_parent != null)
      _parent.OnGradientsChanged();
    }

    public void Add(GradientStop element)
    {
      element.Attach(_handler);
      _elements.Add(element);
      if (_parent != null)
      _parent.OnGradientsChanged();
    }

    public void Remove(GradientStop element)
    {
      if (_elements.Contains(element))
      {
        _elements.Remove(element);
        element.Detach(_handler);
      }
      if (_parent != null)
      _parent.OnGradientsChanged();
    }

    public void Clear()
    {
      foreach (GradientStop stop in _elements)
        stop.Detach(_handler);
      _elements.Clear();
      if (_parent != null)
      _parent.OnGradientsChanged();
    }

    public int Count
    {
      get { return _elements.Count; }
    }

    public GradientStop this[int index]
    {
      get { return _elements[index]; }
      set
      {
        if (value != _elements[index])
        {
          _elements[index].Detach(_handler);
          _elements[index] = value;
          _elements[index].Attach(_handler);
          if (_parent != null)
          _parent.OnGradientsChanged();
        }
      }
    }


    #region IEnumerable<GradientStop> Members

    public IEnumerator<GradientStop> GetEnumerator()
    {
      return new GradientStopEnumerator(_elements);
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
      return new GradientStopEnumerator(_elements);
    }

    #endregion

    public override string ToString()
    {
      string[] stops = new string[_elements.Count];
      int i = 0;
      foreach (GradientStop stop in _elements)
        stops[i++] = stop.ToString();
      return string.Join(", ", stops);
    }
  }
}
