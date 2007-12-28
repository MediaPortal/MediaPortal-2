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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Animations
{
  public enum RepeatBehaviour { None, Forever }

  public class Timeline : ICloneable
  {
    protected enum State
    {
      Idle,
      WaitBegin,
      Running,
      Reverse
    };
    Property _keyProperty;
    Property _beginTimeProperty;
    Property _accellerationProperty;
    Property _autoReverseProperty;
    Property _decelerationRatioProperty;
    Property _durationProperty;
    Property _repeatBehaviourProperty;

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
      _repeatBehaviourProperty = new Property(RepeatBehaviour.None);
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
    public Property RepeatBehaviourProperty
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
    public RepeatBehaviour RepeatBehaviour
    {
      get
      {
        return (RepeatBehaviour)_repeatBehaviourProperty.GetValue();
      }
      set
      {
        _repeatBehaviourProperty.SetValue(value);
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
            else if (RepeatBehaviour == RepeatBehaviour.Forever)
            {
              _timeStarted = timePassed;
              AnimateProperty(timePassed - _timeStarted);
            }
            else
            {
              _state = State.Idle;
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
            if (RepeatBehaviour == RepeatBehaviour.Forever)
            {
              _state = State.Running;
              _timeStarted = timePassed;
              AnimateProperty(timePassed - _timeStarted);
            }
            else
            {
              _state = State.Idle;
            }
          }
          else
          {
            AnimateProperty((uint)(Duration.TotalMilliseconds - (passed)));
          }
          break;
      }
    }

    /// <summary>
    /// Starts the animation
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public virtual void Start(uint timePassed)
    {
      _timeStarted = timePassed;
      _state = State.WaitBegin;
    }

    /// <summary>
    /// Stops the animation.
    /// </summary>
    public virtual void Stop()
    {
      _state = State.Idle;
    }
  }
}
