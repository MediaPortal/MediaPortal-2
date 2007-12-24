#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Brushes
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

    GradientBrush _parent;
    List<GradientStop> _elements;
    PropertyChangedHandler _handler;

    public GradientStopCollection(GradientBrush parent)
    {
      _parent = parent;
      _elements = new List<GradientStop>();
      _handler = new PropertyChangedHandler(OnStopChanged);
    }

    void OnStopChanged(Property prop)
    {
      _parent.OnGradientsChanged();
    }

    public void Add(GradientStop element)
    {
      element.Attach(_handler);
      _elements.Add(element);
      _parent.OnGradientsChanged();
    }

    public void Remove(GradientStop element)
    {
      if (_elements.Contains(element))
      {
        _elements.Remove(element);
        element.Detach(_handler);
      }
      _parent.OnGradientsChanged();
    }

    public void Clear()
    {
      foreach (GradientStop stop in _elements)
      {
        stop.Detach(_handler);
      }
      _elements.Clear();
      _parent.OnGradientsChanged();
    }

    public int Count
    {
      get
      {
        return _elements.Count;
      }
    }

    public GradientStop this[int index]
    {
      get
      {
        return _elements[index];
      }
      set
      {
        if (value != _elements[index])
        {
          _elements[index].Detach(_handler);
          _elements[index] = value;
          _elements[index].Attach(_handler);
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
  }
}
