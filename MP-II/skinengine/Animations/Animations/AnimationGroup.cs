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
using SkinEngine.Controls;
namespace SkinEngine.Animations
{
  public class AnimationGroup : IAnimation
  {
    private ITrigger _trigger;
    private uint _startTime;
    private TimeSpan _duration;
    private TimeSpan _delay;
    private int _repeatMax = 0;
    private int _currentRepeat = 0;
    List<IAnimation> _animations;
    private bool _isAnimating = false;

    public AnimationGroup(ITrigger trigger, TimeSpan duration)
    {
      _animations = new List<IAnimation>();
      _duration = duration;
      _trigger = trigger;
      _delay = new TimeSpan();
    }
    #region IAnimation Members

    public List<IAnimation> Animations
    {
      get
      {
        return _animations;
      }
    }

    public int Repeat
    {
      get { return _repeatMax; }
      set { _repeatMax = value; }
    }


    public TimeSpan Delay
    {
      get { return _delay; }
      set { _delay = value; }
    }

    public TimeSpan Duration
    {
      get { return _duration; }
      set { _duration = value; }
    }

    public ITrigger Trigger
    {
      get { return _trigger; }
      set { _trigger = value; }
    }

    public void Animate(uint timePassed, Control control, ref ExtendedMatrix matrix)
    {
      if (Trigger.IsTriggered)
      {
        _isAnimating = true;
        _startTime = timePassed;
        _currentRepeat = 0;
      }
      if (!Trigger.Condition)
      {
        _isAnimating = false;
        _currentRepeat = 0;
        return;
      }

      uint passed = timePassed - _startTime;
      if (passed < _delay.TotalMilliseconds)
      {
        passed = 0;
      }
      else
      {
        passed -= (uint)_delay.TotalMilliseconds;
      }

      if (passed >= _duration.TotalMilliseconds)
      {
        passed = (uint)_duration.TotalMilliseconds;
        _isAnimating = false;
        if (_repeatMax == -1)
        {
          Reset();
        }
        else if (_repeatMax > 0)
        {
          if (_currentRepeat < _repeatMax)
          {
            _currentRepeat++;
            Reset();
          }
        }
      }

      foreach (IAnimation animation in _animations)
      {
        animation.Animate(timePassed, control, ref matrix);
      }

    }

    public void Reset()
    {
      _trigger.Reset();
      foreach (IAnimation animation in _animations)
      {
        animation.Reset();
      }
    }

    public bool IsAnimating
    {
      get
      {
        if (_repeatMax < 0)
          return false;
        if (_isAnimating)
        {
          return true;
        }
        if (Trigger.CanTrigger)
        {
          return true;
        }
        foreach (IAnimation animation in _animations)
        {
          if (animation.IsAnimating) return true;
        }
        return false;
      }
    }

    #endregion
  }
}
