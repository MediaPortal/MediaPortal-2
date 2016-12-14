#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Transforms
{
  public class TransformCollection : DependencyObject, IObservable, IEnumerable<Transform>, ICollection
  {
    public class TransformEnumerator : IEnumerator<Transform>
    {
      protected int _index = -1;
      protected readonly IList<Transform> _elements;

      public TransformEnumerator(IList<Transform> elements)
      {
        _elements = elements;
      }

      public Transform Current
      {
        get
        {
          return _elements[_index];
        }
      }

      public void Dispose()
      {
      }

      object IEnumerator.Current
      {
        get
        {
          return _elements[_index];
        }
      }

      public bool MoveNext()
      {
        _index++;
        return (_index < _elements.Count);
      }

      public void Reset()
      {
        _index = -1;
      }
    }

    protected readonly IList<Transform> _elements = new List<Transform>();
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();

    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    public override void Dispose()
    {
      base.Dispose();
      _objectChanged.ClearAttachedHandlers();
      Clear();
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      TransformCollection tc = (TransformCollection) source;
      foreach (Transform element in tc._elements)
        Add(copyManager.GetCopy(element));
    }

    protected void OnChildChanged(IObservable observable)
    {
      Fire();
    }

    public void Add(Transform element)
    {
      _elements.Add(element);
      element.ObjectChanged += OnChildChanged;
    }

    public void Remove(Transform element)
    {
      if (_elements.Contains(element))
      {
        _elements.Remove(element);
        element.ObjectChanged -= OnChildChanged;
        element.Dispose();
      }
    }

    public void Clear()
    {
      foreach (Transform element in _elements)
      {
        element.ObjectChanged -= OnChildChanged;
        element.Dispose();
      }
      _elements.Clear();
      Fire();
    }

    public void CopyTo(Array array, int index)
    {
      _elements.ToArray().CopyTo(array, index);
    }

    public int Count
    {
      get { return _elements.Count; }
    }

    public object SyncRoot { get { return this; } }
    public bool IsSynchronized { get { return false; } }

    public Transform this[int index]
    {
      get { return _elements[index]; }
      set
      {
        if (value != _elements[index])
        {
          _elements[index].ObjectChanged -= OnChildChanged;
          _elements[index].Dispose();
          _elements[index] = value;
          _elements[index].ObjectChanged += OnChildChanged;
        }
      }
    }

    protected void Fire()
    {
      _objectChanged.Fire(new object[] {this});
    }

    #region IEnumerable<Transform> Members

    public IEnumerator<Transform> GetEnumerator()
    {
      return new TransformEnumerator(_elements);
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
      return new TransformEnumerator(_elements);
    }

    #endregion
  }
}
