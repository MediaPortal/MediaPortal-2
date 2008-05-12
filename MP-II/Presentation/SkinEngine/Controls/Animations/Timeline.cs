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

namespace Presentation.SkinEngine.Controls.Animations
{
  public enum RepeatBehavior { None, Forever };
  public enum FillBehaviour { HoldEnd, Stop };

  public class Timeline: DependencyObject, ICloneable, IInitializable
  {
    Property _beginTimeProperty;
    Property _accellerationProperty;
    Property _autoReverseProperty;
    Property _decelerationRatioProperty;
    Property _durationProperty;
    Property _repeatBehaviourProperty;
    Property _fillBehaviourProperty;
    Property _visualParentProperty;
    protected PathExpression _propertyExpression = null;
    protected object OriginalValue;

    #region Ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="Timeline"/> class.
    /// </summary>
    public Timeline(): base()
    {
      Init();
    }

    public Timeline(Timeline a): base(a)
    {
      Init();
      BeginTime = a.BeginTime;
      Accelleration = a.Accelleration;
      AutoReverse = a.AutoReverse;
      DecelerationRatio = a.DecelerationRatio;
      Duration = a.Duration;
      FillBehaviour = a.FillBehaviour;
      RepeatBehavior = a.RepeatBehavior;
      VisualParent = a.VisualParent;
    }

    public virtual object Clone()
    {
      return new Timeline(this);
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
      _visualParentProperty = new Property(typeof(UIElement), null);
    }
    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the begin time property.
    /// </summary>
    /// <value>The begin time property.</value>
    public Property BeginTimeProperty
    {
      get
      {
        return _beginTimeProperty;
      }
      set
      {
        _beginTimeProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the begin time.
    /// </summary>
    /// <value>The begin time.</value>
    public TimeSpan BeginTime
    {
      get
      {
        return (TimeSpan)_beginTimeProperty.GetValue();
      }
      set
      {
        _beginTimeProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the accelleration property.
    /// </summary>
    /// <value>The accelleration property.</value>
    public Property AccellerationProperty
    {
      get
      {
        return _accellerationProperty;
      }
      set
      {
        _accellerationProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the accelleration.
    /// </summary>
    /// <value>The accelleration.</value>
    public double Accelleration
    {
      get
      {
        return (double)_accellerationProperty.GetValue();
      }
      set
      {
        _accellerationProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the auto reverse property.
    /// </summary>
    /// <value>The auto reverse property.</value>
    public Property AutoReverseProperty
    {
      get
      {
        return _autoReverseProperty;
      }
      set
      {
        _autoReverseProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether [auto reverse].
    /// </summary>
    /// <value><c>true</c> if [auto reverse]; otherwise, <c>false</c>.</value>
    public bool AutoReverse
    {
      get
      {
        return (bool)_autoReverseProperty.GetValue();
      }
      set
      {
        _autoReverseProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the deceleration ratio property.
    /// </summary>
    /// <value>The deceleration ratio property.</value>
    public Property DecelerationRatioProperty
    {
      get
      {
        return _decelerationRatioProperty;
      }
      set
      {
        _decelerationRatioProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the deceleration ratio.
    /// </summary>
    /// <value>The deceleration ratio.</value>
    public double DecelerationRatio
    {
      get
      {
        return (double)_decelerationRatioProperty.GetValue();
      }
      set
      {
        _decelerationRatioProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the duration property.
    /// </summary>
    /// <value>The duration property.</value>
    public Property DurationProperty
    {
      get
      {
        return _durationProperty;
      }
      set
      {
        _durationProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    /// <value>The duration.</value>
    public TimeSpan Duration
    {
      get
      {
        return (TimeSpan)_durationProperty.GetValue();
      }
      set
      {
        _durationProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the repeat behaviour property.
    /// </summary>
    /// <value>The repeat behaviour property.</value>
    public Property RepeatBehaviorProperty
    {
      get
      {
        return _repeatBehaviourProperty;
      }
      set
      {
        _repeatBehaviourProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the repeat behaviour.
    /// </summary>
    /// <value>The repeat behaviour.</value>
    public RepeatBehavior RepeatBehavior
    {
      get
      {
        return (RepeatBehavior)_repeatBehaviourProperty.GetValue();
      }
      set
      {
        _repeatBehaviourProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the fill behaviour property.
    /// </summary>
    /// <value>The fill behaviour property.</value>
    public Property FillBehaviourProperty
    {
      get
      {
        return _fillBehaviourProperty;
      }
      set
      {
        _fillBehaviourProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the fill behaviour.
    /// </summary>
    /// <value>The fill behaviour.</value>
    public FillBehaviour FillBehaviour
    {
      get
      {
        return (FillBehaviour)_fillBehaviourProperty.GetValue();
      }
      set
      {
        _fillBehaviourProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the visual parent property.
    /// FIXME Albert78: Still needed? Don't we always use <see cref="AnimationContext.VisualParent"/>?
    /// </summary>
    /// <value>The visual parent property.</value>
    public Property VisualParentProperty
    {
      get
      {
        return _visualParentProperty;
      }
      set
      {
        _visualParentProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the visual parent.
    /// FIXME Albert78: Still needed? Don't we always use <see cref="AnimationContext.VisualParent"/>?
    /// </summary>
    /// <value>The visual parent.</value>
    public UIElement VisualParent
    {
      get
      {
        return (UIElement)_visualParentProperty.GetValue();
      }
      set
      {
        _visualParentProperty.SetValue(value);
      }
    }

    #endregion

    #region Animation
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
      object targetObject = element.FindElement(targetName);
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

    public void Initialize(IParserContext context)
    {
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
