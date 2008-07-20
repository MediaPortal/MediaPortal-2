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
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Xaml;
using Presentation.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Animations
{
  /// <summary>
  /// Timeline context class for <see cref="PropertyAnimationTimeline"/>s.
  /// </summary>
  internal class PropertyAnimationTimelineContext : TimelineContext
  {
    protected IDataDescriptor _dataDescriptor;
    protected object _originalValue = null;

    public PropertyAnimationTimelineContext(UIElement element)
      : base(element)
    { }

    public IDataDescriptor DataDescriptor
    {
      get { return _dataDescriptor; }
      set { _dataDescriptor = value; }
    }

    public object OriginalValue
    {
      get { return _originalValue; }
      set { _originalValue = value; }
    }
  }

  /// <summary>
  /// Base class for all property animations.
  /// </summary>
  public class PropertyAnimationTimeline: Timeline
  {
    #region Protected fields

    protected PathExpression _propertyExpression = null;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new <see cref="PropertyAnimationTimeline"/> for use in XAML.
    /// Method <see cref="Initialize(IParserContext)"/> will have to be called to complete the
    /// initialization.
    /// </summary>
    public PropertyAnimationTimeline() { }

    /// <summary>
    /// Creates a new <see cref="PropertyAnimationTimeline"/> for use in Code.
    /// </summary>
    public PropertyAnimationTimeline(PathExpression animatePropertyExpression)
    {
      _propertyExpression = animatePropertyExpression;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      PropertyAnimationTimeline t = source as PropertyAnimationTimeline;
      _propertyExpression = copyManager.GetCopy(t._propertyExpression);
    }

    #endregion

    #region Animation methods

    protected IDataDescriptor GetDataDescriptor(UIElement element)
    {
      string targetName = Storyboard.GetTargetName(this);
      object targetObject = VisualTreeHelper.FindElement(element, targetName);
      if (targetObject == null)
        return null;
      IDataDescriptor result = new ValueDataDescriptor(targetObject);
      if (_propertyExpression == null || !_propertyExpression.Evaluate(result, out result))
        return null;
      return result;
    }

    public override TimelineContext CreateTimelineContext(UIElement element)
    {
      return new PropertyAnimationTimelineContext(element);
    }

    public override void Setup(TimelineContext context)
    {
      PropertyAnimationTimelineContext patc = context as PropertyAnimationTimelineContext;
      patc.DataDescriptor = GetDataDescriptor(context.VisualParent);
      patc.OriginalValue = patc.DataDescriptor.Value;
    }

    public override void Reset(TimelineContext context)
    {
      PropertyAnimationTimelineContext patc = context as PropertyAnimationTimelineContext;
      patc.DataDescriptor.Value = patc.OriginalValue;
    }

    /// <summary>
    /// Starting method to animate the underlaying property. This method will
    /// calculate a value for the internal time counter and delegate to method
    /// <see cref="AnimateProperty(TimelineContext,uint)"/>.
    /// </summary>
    public override void Animate(TimelineContext context, uint timePassed)
    {
      uint passed = (timePassed - context.TimeStarted);

      switch (context.State)
      {
        case State.WaitBegin:
          if (passed >= BeginTime.TotalMilliseconds)
          {
            passed = 0;
            context.TimeStarted = timePassed;
            context.State = State.Running;
            goto case State.Running;
          }
          break;

        case State.Running:
          if (passed >= Duration.TotalMilliseconds)
          {
            if (AutoReverse)
            {
              context.State = State.Reverse;
              context.TimeStarted = timePassed;
              passed = 0;
              goto case State.Reverse;
            }
            else if (RepeatBehavior == RepeatBehavior.Forever)
            {
              context.TimeStarted = timePassed;
              AnimateProperty(context, timePassed - context.TimeStarted);
            }
            else
            {
              AnimateProperty(context, (uint)Duration.TotalMilliseconds);
              Ended(context);
              context.State = State.Ended;
            }
          }
          else
            AnimateProperty(context, passed);
          break;

        case State.Reverse:
          if (passed >= Duration.TotalMilliseconds)
          {
            if (RepeatBehavior == RepeatBehavior.Forever)
            {
              context.State = State.Running;
              context.TimeStarted = timePassed;
              AnimateProperty(context, timePassed - context.TimeStarted);
            }
            else
            {
              AnimateProperty(context, 0);
              Ended(context);
              context.State = State.Ended;
            }
          }
          else
            AnimateProperty(context, (uint)(Duration.TotalMilliseconds - passed));
          break;
      }
    }

    /// <summary>
    /// Will do the real work of animating the underlaying property.
    /// </summary>
    /// <remarks>
    /// The animation progress should be calculated to the specified relative
    /// time. It is not necessary to evaluate time overflows or to revert
    /// the animation depending on properties; these calculations have been
    /// done before this method will be called and will be reflected in the
    /// <paramref name="reltime"/> parameter. 
    /// </remarks>
    /// <param name="reltime">This parameter holds the relative animation time in
    /// milliseconds from the <see cref="BeginTime"/> on, up to a maximum value
    /// of Duration.Milliseconds.</param>
    protected virtual void AnimateProperty(TimelineContext context, uint reltime)
    { }

    #endregion

    #region Base overrides

    public override void Initialize(IParserContext context)
    {
      base.Initialize(context);
      if (String.IsNullOrEmpty(Storyboard.GetTargetName(this)) || String.IsNullOrEmpty(Storyboard.GetTargetProperty(this)))
      {
        _propertyExpression = null;
        return;
      }
      string targetProperty = Storyboard.GetTargetProperty(this);
      _propertyExpression = PathExpression.Compile(context, targetProperty);
    }

    #endregion
  }
}
