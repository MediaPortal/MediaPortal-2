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
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Animations
{
  public class TimelineGroup : Timeline, IList<Timeline>
  {
    #region Private fields

    Property _childrenProperty;

    #endregion

    #region Ctor

    public TimelineGroup()
    {
      Init();
    }

    void Init()
    {
      _childrenProperty = new Property(typeof(IList<Timeline>), new List<Timeline>());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      TimelineGroup grp = source as TimelineGroup;
      foreach (Timeline t in grp.Children)
        Children.Add(copyManager.GetCopy(t));
    }

    #endregion

    #region Public properties

    public Property ChildrenProperty
    {
      get { return _childrenProperty; }
    }

    public IList<Timeline> Children
    {
      get { return (IList<Timeline>) _childrenProperty.GetValue(); }
    }

    #endregion


    public override void Initialize(UIElement element)
    {
      foreach (Timeline line in Children)
        line.Initialize(element);
    }

    #region IList<Timeline> Members

    public void Add(Timeline value)
    {
      Children.Add(value);
    }

    public void Clear()
    {
      Children.Clear();
    }

    public bool Contains(Timeline item)
    {
      return Children.Contains(item);
    }

    public int IndexOf(Timeline item)
    {
      return Children.IndexOf(item);
    }

    public void Insert(int index, Timeline value)
    {
      Children.Insert(index, value);
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    public bool Remove(Timeline value)
    {
      return Children.Remove(value);
    }

    public void RemoveAt(int index)
    {
      Children.RemoveAt(index);
    }

    public Timeline this[int index]
    {
      get { return Children[index]; }
      set { Children[index] = value; }
    }

    #endregion

    #region ICollection<Timeline> Members

    public void CopyTo(Timeline[] array, int index)
    {
      Children.CopyTo(array, index);
    }

    public int Count
    {
      get { return Children.Count; }
    }

    #endregion

    #region IEnumerable<Timeline> Members

    IEnumerator<Timeline> IEnumerable<Timeline>.GetEnumerator()
    {
      return Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return Children.GetEnumerator();
    }

    #endregion

  }
}
