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

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  /// <summary>
  /// Timeline context class for <see cref="PropertyAnimationTimeline"/>s.
  /// </summary>
  internal class PropertyAnimationTimelineContext : TimelineContext
  {
    protected IDataDescriptor _dataDescriptor;
    protected object _startValue = null;
    protected object _originalValue = null;

    public PropertyAnimationTimelineContext(UIElement element)
      : base(element)
    { }

    public IDataDescriptor DataDescriptor
    {
      get { return _dataDescriptor; }
      set { _dataDescriptor = value; }
    }

    public object StartValue
    {
      get { return _startValue; }
      set { _startValue = value; }
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
    public PropertyAnimationTimeline()
    {
      Duration = new TimeSpan(0, 0, 1);
    }

    /// <summary>
    /// Creates a new <see cref="PropertyAnimationTimeline"/> for use in Code.
    /// </summary>
    public PropertyAnimationTimeline(PathExpression animatePropertyExpression): this()
    {
      _propertyExpression = animatePropertyExpression;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      PropertyAnimationTimeline t = (PropertyAnimationTimeline) source;
      _propertyExpression = copyManager.GetCopy(t._propertyExpression);
    }

    #endregion

    #region Animation methods

    public override void Start(TimelineContext context, uint timePassed)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null)
        return;
      patc.State = State.Idle;
      base.Start(context, timePassed);
    }

    protected IDataDescriptor GetDataDescriptor(UIElement element)
    {
      string targetName = Storyboard.GetTargetName(this);
      object targetObject = element.FindElement(new NameFinder(targetName));
      if (targetObject == null)
        return null;
      try
      {
        IDataDescriptor result = new ValueDataDescriptor(targetObject);
        if (_propertyExpression != null && _propertyExpression.Evaluate(result, out result))
          return result;
      }
      catch (XamlBindingException e)
      {
        ServiceScope.Get<ILogger>().Warn("PropertyAnimationTimeline: Error evaluating expression '{0}' on target object '{1}'", e, _propertyExpression, targetObject);
      }
      return null;
    }

    public override TimelineContext CreateTimelineContext(UIElement element)
    {
      PropertyAnimationTimelineContext result = new PropertyAnimationTimelineContext(element)
        {DataDescriptor = GetDataDescriptor(element)};
      return result;
    }

    public override void AddAllAnimatedProperties(TimelineContext context,
        IDictionary<IDataDescriptor, object> result)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null)
        return;
      result.Add(patc.DataDescriptor, patc.OriginalValue);
    }

    public override void Setup(TimelineContext context,
        IDictionary<IDataDescriptor, object> propertyConfigurations)
    {
      base.Setup(context, propertyConfigurations);
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null)
        return;
      object value;
      patc.OriginalValue = propertyConfigurations.TryGetValue(patc.DataDescriptor, out value) ? value :
          patc.DataDescriptor.Value;
      patc.StartValue = patc.DataDescriptor.Value;
    }

    public override void Reset(TimelineContext context)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null)
        return;
      patc.DataDescriptor.Value = patc.OriginalValue;
    }

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
