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

namespace SkinEngine.Controls.Animations
{
  public class TimelineGroup : Timeline, IList
  {
    Property _childrenProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineGroup"/> class.
    /// </summary>
    public TimelineGroup()
    {
      Init();
    }

    public TimelineGroup(TimelineGroup grp)
      : base(grp)
    {
      Init();
      foreach (Timeline t in grp.Children)
      {
        Children.Add((Timeline)t.Clone());
      }
    }
    public override object Clone()
    {
      return new TimelineGroup(this);
    }

    void Init()
    {
      _childrenProperty = new Property(new TimelineCollection());
    }

    /// <summary>
    /// Gets or sets the children property.
    /// </summary>
    /// <value>The children property.</value>
    public Property ChildrenProperty
    {
      get
      {
        return _childrenProperty;
      }
      set
      {
        _childrenProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the children.
    /// </summary>
    /// <value>The children.</value>
    public TimelineCollection Children
    {
      get
      {
        return (TimelineCollection)_childrenProperty.GetValue();
      }
    }

    /// <summary>
    /// Animate
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public override void Animate(uint timePassed)
    {
      foreach (Timeline child in Children)
      {
        child.Animate(timePassed);
      }
    }

    /// <summary>
    /// Starts the animation
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public override void Start(uint timePassed)
    {
      foreach (Timeline child in Children)
      {
        child.VisualParent = VisualParent;
        child.Start(timePassed);
      }
    }

    /// <summary>
    /// Stops the animation.
    /// </summary>
    public override void Stop()
    {
      foreach (Timeline child in Children)
      {
        child.Stop();
      }
    }

    /// <summary>
    /// Gets a value indicating whether this timeline is stopped.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this timeline is stopped; otherwise, <c>false</c>.
    /// </value>
    public override bool IsStopped
    {
      get
      {
        foreach (Timeline child in Children)
        {
          if (!child.IsStopped) return false;
        }
        return true;
      }
    }
    

    #region IList Members

    public int Add(object value)
    {
      Children.Add((Timeline)value);
      return Children.Count;
    }

    public void Clear()
    {
      Children.Clear();
    }

    public bool Contains(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int IndexOf(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Insert(int index, object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public bool IsFixedSize
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsReadOnly
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public void Remove(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void RemoveAt(int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public object this[int index]
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    #region ICollection Members

    public void CopyTo(Array array, int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int Count
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsSynchronized
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public object SyncRoot
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }
}
