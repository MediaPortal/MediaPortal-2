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
using Microsoft.DirectX;
using SkinEngine.Controls;

namespace SkinEngine.Animations
{
  public enum Axis
  {
    X,
    Y,
    Z
  }

  public class RotateAnimation : IAnimation
  {
    private const float DEGREE_TO_RADIAN = 0.01745329f;
    private ITrigger _trigger;
    private uint _startTime;
    private TimeSpan _duration;
    private float _from;
    private float _to;
    private Axis _axis;
    private Tweener _tweener;
    private Vector3 _center;
    private bool _isAnimating = false;
    private Control _control;
    private TimeSpan _delay;

    public RotateAnimation(ITrigger trigger, Axis axis, float from, float to, TimeSpan duration, Vector3 center)
    {
      _axis = axis;
      _from = from;
      _to = to;
      _duration = duration;
      _trigger = trigger;
      _tweener = new LinearTweener();
      _center = center;
      _delay = new TimeSpan();
    }

    public RotateAnimation(ITrigger trigger, Axis axis, float from, float to, TimeSpan duration, Control control)
    {
      _axis = axis;
      _from = from;
      _to = to;
      _duration = duration;
      _trigger = trigger;
      _tweener = new LinearTweener();
      _control = control;
      _delay = new TimeSpan();
    }

    public bool IsAnimating
    {
      get
      {
        if (_isAnimating)
        {
          return true;
        }
        if (Trigger.CanTrigger)
        {
          return true;
        }
        return false;
      }
    }

    public TimeSpan Duration
    {
      get { return _duration; }
      set { _duration = value; }
    }

    public TimeSpan Delay
    {
      get { return _delay; }
      set { _delay = value; }
    }

    public Axis Axis
    {
      get { return _axis; }
      set { _axis = value; }
    }

    #region IAnimation Members

    public void Reset()
    {
      _isAnimating = false;
      _trigger.Reset();
    }

    public Tweener Tweener
    {
      get { return _tweener; }
      set { _tweener = value; }
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
        _startTime = timePassed;
        _isAnimating = true;
      }


      if (!Trigger.Condition)
      {
        _isAnimating = false;
        return;
      }
      uint passed = timePassed - _startTime;
      if (passed < _delay.TotalMilliseconds)
      {
        passed = 0;
      }
      else
      {
        passed -= (uint) _delay.TotalMilliseconds;
      }
      if (passed >= _duration.TotalMilliseconds)
      {
        passed = (uint) _duration.TotalMilliseconds;
        _isAnimating = false;
      }
      float step = _tweener.Tween((float) passed, _from, (_to - _from), (float) _duration.TotalMilliseconds);

      Vector3 center = _center;
      if (_control != null)
      {
        center = new Vector3(_control.Position.X + _control.Width/2, _control.Position.Y + _control.Height/2, 0);
      }
      else
      {
        center =
          new Vector3((_center.X + control.Position.X), (_center.Y + control.Position.Y),
                      (_center.Z + control.Position.Z));
      }
      Matrix m = Matrix.Identity;
      m *= Matrix.Translation(new Vector3(-center.X, -center.Y, _center.Z));

      switch (Axis)
      {
        case Axis.X:
          m *= Matrix.RotationAxis(new Vector3(1, 0, 0), step*DEGREE_TO_RADIAN);
          break;
        case Axis.Y:
          m *= Matrix.RotationAxis(new Vector3(0, 1, 0), step*DEGREE_TO_RADIAN);
          break;
        case Axis.Z:
          m *= Matrix.RotationAxis(new Vector3(0, 0, 1), step*DEGREE_TO_RADIAN);
          break;
      }
      m *= Matrix.Translation(center);
      matrix.Matrix *= m;
    }

    #endregion
  }
}
