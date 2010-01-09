#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Transforms
{
  public class TransformCollection : DependencyObject, IObservable, IEnumerable<Transform>
  {
    public class TransformEnumerator : IEnumerator<Transform>
    {
      protected int index = -1;
      protected readonly IList<Transform> _elements;

      public TransformEnumerator(IList<Transform> elements)
      {
        _elements = elements;
      }

      public Transform Current
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

    protected readonly IList<Transform> _elements = new List<Transform>();

    public event ObjectChangedHandler ObjectChanged;

    public override void Dispose()
    {
      base.Dispose();
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

    /// <summary>
    /// Removes the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    public void Remove(Transform element)
    {
      if (_elements.Contains(element))
      {
        _elements.Remove(element);
        element.ObjectChanged -= OnChildChanged;
        element.Dispose();
      }
    }

    /// <summary>
    /// Clears this instance.
    /// </summary>
    public void Clear()
    {
      foreach (Transform element in _elements)
      {
        element.ObjectChanged -= OnChildChanged;
        element.Dispose();
      }
      _elements.Clear();
    }

    public int Count
    {
      get { return _elements.Count; }
    }

    /// <summary>
    /// Gets or sets the <see cref="Transform"/> at the specified index.
    /// </summary>
    /// <value></value>
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
      if (ObjectChanged != null)
        ObjectChanged(this);
    }

    #region IEnumerable<Transform> Members

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<Transform> GetEnumerator()
    {
      return new TransformEnumerator(_elements);
    }

    #endregion

    #region IEnumerable Members

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return new TransformEnumerator(_elements);
    }

    #endregion
  }
}
