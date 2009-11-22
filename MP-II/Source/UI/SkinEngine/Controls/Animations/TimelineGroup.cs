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
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  /// <summary>
  /// Timeline context class for <see cref="TimelineGroup"/>s.
  /// </summary>
  class TimelineGroupContext: TimelineContext, IList<TimelineContext>
  {
    #region Protected properties

    protected IList<TimelineContext> _children = new List<TimelineContext>();

    public TimelineGroupContext(UIElement visualParent) : base(visualParent) { }

    #endregion

    #region IList<TimelineContext> Members

    public int IndexOf(TimelineContext item)
    {
      return _children.IndexOf(item);
    }

    public void Insert(int index, TimelineContext item)
    {
      _children.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
      _children.RemoveAt(index);
    }

    public TimelineContext this[int index]
    {
      get { return _children[index]; }
      set { _children[index] = value; }
    }

    #endregion

    #region ICollection<TimelineContext> Members

    public void Add(TimelineContext item)
    {
      _children.Add(item);
    }

    public void Clear()
    {
      _children.Clear();
    }

    public bool Contains(TimelineContext item)
    {
      return _children.Contains(item);
    }

    public void CopyTo(TimelineContext[] array, int arrayIndex)
    {
      _children.CopyTo(array, arrayIndex);
    }

    public int Count
    {
      get { return _children.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    public bool Remove(TimelineContext item)
    {
      return _children.Remove(item);
    }

    #endregion

    #region IEnumerable<TimelineContext> Members

    public IEnumerator<TimelineContext> GetEnumerator()
    {
      return _children.GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _children.GetEnumerator();
    }

    #endregion
  }

  /// <summary>
  /// Represents a Timeline which consists of child timelines.
  /// </summary>
  public abstract class TimelineGroup : Timeline, IList<Timeline>
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
      TimelineGroup grp = (TimelineGroup) source;
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

    // Method Start(TimelineContext) has to be overridden in subclasses, as we don't
    // know here when to start the child timelines.

    #region Animation control methods

    public override void Stop(TimelineContext context)
    {
      base.Stop(context);
      TimelineGroupContext tgc = (TimelineGroupContext) context;
      for (int i = 0; i < Children.Count; i++)
        Children[i].Stop(tgc[i]);
    }

    public override TimelineContext CreateTimelineContext(UIElement element)
    {
      TimelineGroupContext result = new TimelineGroupContext(element);
      foreach (Timeline line in Children)
      {
        TimelineContext childContext = line.CreateTimelineContext(element);
        result.Add(childContext);
      }
      return result;
    }

    public override void AddAllAnimatedProperties(TimelineContext context,
        IDictionary<IDataDescriptor, object> result)
    {
      TimelineGroupContext tgc = (TimelineGroupContext) context;
      for (int i = 0; i < Children.Count; i++)
        Children[i].AddAllAnimatedProperties(tgc[i], result);
    }

    public override void Setup(TimelineContext context,
        IDictionary<IDataDescriptor, object> propertyConfigurations)
    {
      base.Setup(context, propertyConfigurations);
      TimelineGroupContext tgc = (TimelineGroupContext) context;
      for (int i = 0; i < Children.Count; i++)
        Children[i].Setup(tgc[i], propertyConfigurations);
    }

    #endregion

    #region Animation state methods

    internal override void Ended(TimelineContext context)
    {
      base.Ended(context);
      TimelineGroupContext tgc = (TimelineGroupContext) context;
      for (int i = 0; i < Children.Count; i++)
      {
        Timeline child = Children[i];
        TimelineContext childContext = tgc[i];
        if (child.FillBehavior == Animations.FillBehavior.Stop)
          child.Stop(childContext);
        else
          child.Ended(childContext);
      }
    }

    #endregion

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
