#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.General;
using MediaPortal.Utilities;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class GradientStopCollection : IEnumerable<GradientStop>
  {
    #region Protected fields

    protected GradientBrush _parent;
    protected IList<GradientStop> _elements;
    protected IList<GradientStop> _orderedGradientStopList = null; // Caches gradient stops in a list ordered by offset

    #endregion

    #region Ctor

    public GradientStopCollection(GradientBrush parent)
    {
      _parent = parent;
      Init();
    }

    public void Dispose()
    {
      _parent = null; // To avoid OnGradientsChanged callback
      Clear();
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
    }

    #endregion

    void OnStopChanged(IObservable observable)
    {
      _orderedGradientStopList = null;
      if (_parent != null)
        _parent.OnGradientsChanged();
    }

    protected static int CompareByOffset(GradientStop x, GradientStop y)
    {
      return x.Offset.CompareTo(y.Offset);
    }

    public void Add(GradientStop element)
    {
      _orderedGradientStopList = null;
      element.ObjectChanged += OnStopChanged;
      _elements.Add(element);
      if (_parent != null)
        _parent.OnGradientsChanged();
    }

    public void Remove(GradientStop element)
    {
      if (_elements.Contains(element))
      {
        _orderedGradientStopList = null;
        _elements.Remove(element);
        element.ObjectChanged -= OnStopChanged;
        element.Dispose();
      }
      if (_parent != null)
        _parent.OnGradientsChanged();
    }

    public void Clear()
    {
      _orderedGradientStopList = null;
      foreach (GradientStop stop in _elements)
      {
        stop.ObjectChanged -= OnStopChanged;
        stop.Dispose();
      }
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
          _orderedGradientStopList = null;
          _elements[index].ObjectChanged -= OnStopChanged;
          _elements[index].Dispose();
          _elements[index] = value;
          _elements[index].ObjectChanged += OnStopChanged;
          if (_parent != null)
            _parent.OnGradientsChanged();
        }
      }
    }

    public IList<GradientStop> OrderedGradientStopList
    {
      get
      {
        if (_orderedGradientStopList == null)
        {
          List<GradientStop> result = new List<GradientStop>(this);
          result.Sort(CompareByOffset);
          _orderedGradientStopList = result;
        }
        return _orderedGradientStopList;
      }
    }

    #region IEnumerable<GradientStop> Members

    public IEnumerator<GradientStop> GetEnumerator()
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

    public override string ToString()
    {
      return StringUtils.Join(", ", _elements);
    }
  }
}
