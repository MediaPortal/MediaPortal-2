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
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;

namespace SkinEngine.Controls.Animations
{
  public enum RepeatBehavior { None, Forever };
  public enum FillBehaviour { HoldEnd, Stop };

  public class Timeline : ICloneable
  {
    protected enum State
    {
      Idle,
      Starting,
      WaitBegin,
      Running,
      Reverse,
      Ended
    };
    Property _keyProperty;
    Property _beginTimeProperty;
    Property _accellerationProperty;
    Property _autoReverseProperty;
    Property _decelerationRatioProperty;
    Property _durationProperty;
    Property _repeatBehaviourProperty;
    Property _fillBehaviourProperty;
    Property _visualParentProperty;

    protected uint _timeStarted;
    protected State _state = State.Idle;

    /// <summary>
    /// Initializes a new instance of the <see cref="Timeline"/> class.
    /// </summary>
    public Timeline()
    {
      Init();
    }

    public Timeline(Timeline a)
    {
      Init();
      BeginTime = a.BeginTime;
      Accelleration = a.Accelleration;
      AutoReverse = a.AutoReverse;
      DecelerationRatio = a.DecelerationRatio;
      Duration = a.Duration;
      Key = a.Key;
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
      _keyProperty = new Property("");
      _beginTimeProperty = new Property(new TimeSpan(0, 0, 0));
      _accellerationProperty = new Property(1.0);
      _autoReverseProperty = new Property(false);
      _decelerationRatioProperty = new Property(1.0);
      _durationProperty = new Property(new TimeSpan(0, 0, 1));
      _repeatBehaviourProperty = new Property(RepeatBehavior.None);
      _fillBehaviourProperty = new Property(FillBehaviour.HoldEnd);
      _visualParentProperty = new Property(null);
    }

    /// <summary>
    /// Gets or sets the key property.
    /// </summary>
    /// <value>The key property.</value>
    public Property KeyProperty
    {
      get
      {
        return _keyProperty;
      }
      set
      {
        _keyProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    public string Key
    {
      get
      {
        return _keyProperty.GetValue() as string;
      }
      set
      {
        _keyProperty.SetValue(value);
      }
    }
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
    /// <summary>
    /// Animates the property.
    /// </summary>
    /// <param name="timepassed">The timepassed.</param>
    protected virtual void AnimateProperty(uint timepassed)
    {
    }

    /// <summary>
    /// Animate
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public virtual void Animate(uint timePassed)
    {
      if (_state == State.Starting) return;
      uint passed = (timePassed - _timeStarted);

      switch (_state)
      {
        case State.WaitBegin:
          if (passed >= BeginTime.TotalMilliseconds)
          {
            passed = 0;
            _timeStarted = timePassed;
            _state = State.Running;
            goto case State.Running;
          }
          break;

        case State.Running:
          if (passed >= Duration.TotalMilliseconds)
          {
            if (AutoReverse)
            {
              _state = State.Reverse;
              _timeStarted = timePassed;
              passed = 0;
              goto case State.Reverse;
            }
            else if (RepeatBehavior == RepeatBehavior.Forever)
            {
              _timeStarted = timePassed;
              AnimateProperty(timePassed - _timeStarted);
            }
            else
            {
              AnimateProperty((uint)Duration.TotalMilliseconds);
              Ended();
              _state = State.Ended;
            }
          }
          else
          {
            AnimateProperty(passed);
          }
          break;

        case State.Reverse:

          if (passed >= Duration.TotalMilliseconds)
          {
            if (RepeatBehavior == RepeatBehavior.Forever)
            {
              _state = State.Running;
              _timeStarted = timePassed;
              AnimateProperty(timePassed - _timeStarted);
            }
            else
            {
              AnimateProperty((uint)Duration.TotalMilliseconds);
              Ended();
              _state = State.Ended;
            }
          }
          else
          {
            AnimateProperty((uint)(Duration.TotalMilliseconds - (passed)));
          }
          break;
      }
    }

    public virtual void Ended()
    {
    }
    /// <summary>
    /// Starts the animation
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public virtual void Start(uint timePassed)
    {
      if (!IsStopped)
        Stop();
      _timeStarted = timePassed;
      _state = State.WaitBegin;
    }

    /// <summary>
    /// Stops the animation.
    /// </summary>
    public virtual void Stop()
    {
      if (IsStopped) return;
      if (FillBehaviour == FillBehaviour.Stop)
      {
        AnimateProperty(0);
      }
      _state = State.Idle;
    }

    /// <summary>
    /// Gets a value indicating whether this timeline is stopped.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this timeline is stopped; otherwise, <c>false</c>.
    /// </value>
    public virtual bool IsStopped
    {
      get
      {
        return (_state == State.Idle);
      }
    }

    protected Property GetProperty(string targetName, string targetProperty)
    {
      string propertyname = "";
      Regex regex = new Regex(@"\([^\.]+\.[^\.]+");
      MatchCollection matches = regex.Matches(targetProperty);
      for (int i = 0; i < matches.Count; ++i)
      {
        string part = matches[i].Value;
        string part1 = part;
        int p1 = part.IndexOf("(");
        if (p1 >= 0)
        {
          part1 = part.Substring(p1 + 1);
          p1 = part1.IndexOf(")");
          part1 = (part1.Substring(0, p1) + part1.Substring(p1 + 1));
        }
        int pos = part1.IndexOf(".");
        if (pos > 0)
        {
          part1 = part1.Substring(pos + 1);
        }
        if (propertyname.Length > 0) propertyname += ".";
        propertyname += part1;
      }

      string propertyName = String.Format("{0}.{1}", targetName, propertyname);
      int posPoint = propertyName.LastIndexOf('.');
      string left = propertyName.Substring(0, posPoint);
      string right = propertyName.Substring(posPoint + 1);

      object element = VisualTreeHelper.Instance.FindElement(VisualParent, left);
      if (element == null)
        element = VisualTreeHelper.Instance.FindElement(left);
      if (element == null) return null;
      Type t = element.GetType();
      PropertyInfo pinfo = t.GetProperty(right + "Property");
      if (pinfo == null)
        return null;
      MethodInfo minfo = pinfo.GetGetMethod();
      return minfo.Invoke(element, null) as Property;
    }

    public virtual void Setup(UIElement element)
    {
    }
  }
}
