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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Presentation.SkinEngine.Controls.Animations
{
  public class PointKeyFrameCollection : IEnumerable<PointKeyFrame>
  {
    public class PointKeyFrameEnumerator : IEnumerator<PointKeyFrame>
    {
      int index = -1;
      List<PointKeyFrame> _elements;
      public PointKeyFrameEnumerator(List<PointKeyFrame> elements)
      {
        _elements = elements;
      }
      public PointKeyFrame Current
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

    //PointKeyFrame _parent;
    List<PointKeyFrame> _elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="PointKeyFrameCollection"/> class.
    /// </summary>
    public PointKeyFrameCollection(/*PointKeyFrame parent*/)
    {
      //_parent = parent;
      _elements = new List<PointKeyFrame>();
    }

    /// <summary>
    /// Adds the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    public void Add(PointKeyFrame element)
    {
      //element.VisualParent = _parent;
      _elements.Add(element);
      //_parent.Invalidate();
    }

    /// <summary>
    /// Removes the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    public void Remove(PointKeyFrame element)
    {
      _elements.Remove(element);
      //_parent.Invalidate();
    }

    /// <summary>
    /// Clears this instance.
    /// </summary>
    public void Clear()
    {
      _elements.Clear();
      //_parent.Invalidate();
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <value>The count.</value>
    public int Count
    {
      get
      {
        return _elements.Count;
      }
    }

    /// <summary>
    /// Gets or sets the <see cref="SkinEngine.Controls.Animations.PointKeyFrame"/> at the specified index.
    /// </summary>
    /// <value></value>
    public PointKeyFrame this[int index]
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
          //_elements[index].VisualParent = _parent;
          //_parent.Invalidate();
        }
      }
    }


    #region IEnumerable<PointKeyFrame> Members

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<PointKeyFrame> GetEnumerator()
    {
      return new PointKeyFrameEnumerator(_elements);
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
      return new PointKeyFrameEnumerator(_elements);
    }

    #endregion
  }
}
