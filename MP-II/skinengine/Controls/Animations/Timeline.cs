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

  public class Timeline
  {
    Property _beginTimeProperty;
    Property _accellerationProperty;
    Property _autoReverseProperty;
    Property _decelerationRatioProperty;
    Property _durationProperty;
    Property _repeatBehaviourProperty;

    public Timeline()
    {
      _beginTimeProperty = new Property(new TimeSpan(0, 0, 0));
      _accellerationProperty = new Property(1.0);
      _autoReverseProperty = new Property(false);
      _decelerationRatioProperty = new Property(1.0);
      _durationProperty = new Property(new TimeSpan(0, 0, 1));
      _repeatBehaviourProperty = new Property(RepeatBehaviour.None);
    }

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


    public virtual void Animate(uint timePassed)
    {
    }

    public virtual void Start(uint timePassed)
    {
    }

    public virtual void Stop()
    {
    }
  }
}
