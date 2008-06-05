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
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.XamlParser;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Animations
{
  public enum RepeatBehavior { None, Forever };
  public enum FillBehaviour { HoldEnd, Stop };

  public class Timeline: DependencyObject
  {
    Property _beginTimeProperty;
    Property _accellerationProperty;
    Property _autoReverseProperty;
    Property _decelerationRatioProperty;
    Property _durationProperty;
    Property _repeatBehaviourProperty;
    Property _fillBehaviourProperty;
    protected PathExpression _propertyExpression = null;
    protected object OriginalValue;

    #region Ctor

    public Timeline()
    {
      Init();
    }

    void Init()
    {
      _beginTimeProperty = new Property(typeof(TimeSpan), new TimeSpan(0, 0, 0));
      _accellerationProperty = new Property(typeof(double), 1.0);
      _autoReverseProperty = new Property(typeof(bool), false);
      _decelerationRatioProperty = new Property(typeof(double), 1.0);
      _durationProperty = new Property(typeof(TimeSpan), new TimeSpan(0, 0, 1));
      _repeatBehaviourProperty = new Property(typeof(RepeatBehavior), RepeatBehavior.None);
      _fillBehaviourProperty = new Property(typeof(FillBehaviour), FillBehaviour.HoldEnd);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Timeline t = source as Timeline;
      BeginTime = copyManager.GetCopy(t.BeginTime);
      Accelleration = copyManager.GetCopy(t.Accelleration);
      AutoReverse = copyManager.GetCopy(t.AutoReverse);
      DecelerationRatio = copyManager.GetCopy(t.DecelerationRatio);
      Duration = copyManager.GetCopy(t.Duration);
      FillBehaviour = copyManager.GetCopy(t.FillBehaviour);
      RepeatBehavior = copyManager.GetCopy(t.RepeatBehavior);
      _propertyExpression = copyManager.GetCopy(t._propertyExpression);
    }

    #endregion

    #region Public properties

    public Property BeginTimeProperty
    {
      get { return _beginTimeProperty; }
    }

    public TimeSpan BeginTime
    {
      get { return (TimeSpan)_beginTimeProperty.GetValue(); }
      set { _beginTimeProperty.SetValue(value); }
    }

    public Property AccellerationProperty
    {
      get { return _accellerationProperty; }
    }

    public double Accelleration
    {
      get { return (double)_accellerationProperty.GetValue(); }
      set { _accellerationProperty.SetValue(value); }
    }

    public Property AutoReverseProperty
    {
      get { return _autoReverseProperty; }
    }

    public bool AutoReverse
    {
      get { return (bool)_autoReverseProperty.GetValue(); }
      set { _autoReverseProperty.SetValue(value); }
    }

    public Property DecelerationRatioProperty
    {
      get { return _decelerationRatioProperty; }
      set { _decelerationRatioProperty = value; }
    }

    public double DecelerationRatio
    {
      get { return (double)_decelerationRatioProperty.GetValue(); }
      set { _decelerationRatioProperty.SetValue(value); }
    }

    public Property DurationProperty
    {
      get { return _durationProperty; }
    }

    public TimeSpan Duration
    {
      get { return (TimeSpan)_durationProperty.GetValue(); }
      set { _durationProperty.SetValue(value); }
    }

    public Property RepeatBehaviorProperty
    {
      get { return _repeatBehaviourProperty; }
    }

    public RepeatBehavior RepeatBehavior
    {
      get { return (RepeatBehavior) _repeatBehaviourProperty.GetValue(); }
      set { _repeatBehaviourProperty.SetValue(value); }
    }

    public Property FillBehaviourProperty
    {
      get { return _fillBehaviourProperty; }
    }

    public FillBehaviour FillBehaviour
    {
      get { return (FillBehaviour)_fillBehaviourProperty.GetValue(); }
      set { _fillBehaviourProperty.SetValue(value); }
    }

    #endregion

    #region Animation methods
    /// <summary>
    /// Animates the property.
    /// </summary>
    /// <param name="timepassed">The timepassed.</param>
    protected virtual void AnimateProperty(AnimationContext context, uint timepassed)
    {
    }

    /// <summary>
    /// Animate
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public virtual void Animate(AnimationContext context, uint timePassed)
    {
      if (context.State == State.Starting) return;
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
          {
            AnimateProperty(context, passed);
          }
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
              AnimateProperty(context, (uint)Duration.TotalMilliseconds);
              Ended(context);
              context.State = State.Ended;
            }
          }
          else
          {
            AnimateProperty(context, (uint)(Duration.TotalMilliseconds - (passed)));
          }
          break;
      }
    }

    public virtual void Ended(AnimationContext context)
    {
      if (IsStopped(context)) return;
      if (context.DataDescriptor != null)
        if (FillBehaviour != FillBehaviour.HoldEnd)
          context.DataDescriptor.Value = OriginalValue;
    }

    /// <summary>
    /// Starts the animation
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public virtual void Start(AnimationContext context, uint timePassed)
    {
      if (!IsStopped(context))
        Stop(context);
      context.TimeStarted = timePassed;
      context.State = State.WaitBegin;
    }

    /// <summary>
    /// Stops the animation.
    /// </summary>
    public virtual void Stop(AnimationContext context)
    {
      if (IsStopped(context)) return;
      if (FillBehaviour == FillBehaviour.Stop)
      {
        AnimateProperty(context, 0);
      }
      context.State = State.Idle;
    }

    /// <summary>
    /// Gets a value indicating whether this timeline is stopped.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this timeline is stopped; otherwise, <c>false</c>.
    /// </value>
    public virtual bool IsStopped(AnimationContext context)
    {
      return (context.State == State.Idle);
    }

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

    public virtual void Setup(AnimationContext context)
    {
      context.DataDescriptor = GetDataDescriptor(context.VisualParent);
    }

    public virtual void Initialize(UIElement element)
    {
      IDataDescriptor dd = GetDataDescriptor(element);
      OriginalValue = dd == null ? null : dd.Value;
    }

    #endregion

    #region IInitializable implementation

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
