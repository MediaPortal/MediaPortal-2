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
using SlimDX;
using SlimDX.Direct3D9;
using SkinEngine.Controls;

namespace SkinEngine.Animations
{
  public class SlideAnimation : IAnimation
  {
    private ITrigger _trigger;
    private uint _startTime;
    private TimeSpan _duration;
    private Vector3 _from;
    private Vector3 _to;
    private Tweener _tweener;
    private bool _isAnimating = false;
    private TimeSpan _delay;

    public SlideAnimation(ITrigger trigger, Vector3 from, Vector3 to, TimeSpan duration)
    {
      _from = from;
      _to = to;
      _duration = duration;
      _trigger = trigger;
      _tweener = new LinearTweener();
      _delay = new TimeSpan();
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

    #region IAnimation Members

    public void Reset()
    {
      _isAnimating = false;
      _trigger.Reset();
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
        _isAnimating = true;
        _startTime = timePassed;
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
      float stepX = _tweener.Tween((float) passed, _from.X, (_to.X - _from.X), (float) _duration.TotalMilliseconds);
      float stepY = _tweener.Tween((float) passed, _from.Y, (_to.Y - _from.Y), (float) _duration.TotalMilliseconds);
      float stepZ = _tweener.Tween((float) passed, _from.Z, (_to.Z - _from.Z), (float) _duration.TotalMilliseconds);

      matrix.Matrix *= Matrix.Translation(new Vector3(stepX, stepY, stepZ));
    }

    #endregion
  }
}
