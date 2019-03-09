#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common.General;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Controls.Animations.EasingFunctions;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class PointAnimation: PropertyAnimationTimeline
  {
    #region Protected fields

    protected AbstractProperty _fromProperty;
    protected AbstractProperty _toProperty;
    protected AbstractProperty _byProperty;
    protected AbstractProperty _easingFunctionProperty;

    #endregion

    #region Ctor

    public PointAnimation()
    {
      Init();
    }

    void Init()
    {
      _fromProperty = new SProperty(typeof(Vector2?), null);
      _toProperty = new SProperty(typeof(Vector2?), null);
      _byProperty = new SProperty(typeof(Vector2?), null);
      _easingFunctionProperty = new SProperty(typeof(IEasingFunction), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      PointAnimation a = (PointAnimation) source;
      From = copyManager.GetCopy(a.From);
      To = copyManager.GetCopy(a.To);
      By = copyManager.GetCopy(a.By);
      EasingFunction = copyManager.GetCopy(a.EasingFunction);
    }

    #endregion

    #region Public properties

    public AbstractProperty FromProperty
    {
      get { return _fromProperty; }
    }

    public Vector2? From
    {
      get { return (Vector2?) _fromProperty.GetValue(); }
      set { _fromProperty.SetValue(value); }
    }

    public AbstractProperty ToProperty
    {
      get { return _toProperty; }
    }

    public Vector2? To
    {
      get { return (Vector2?) _toProperty.GetValue(); }
      set { _toProperty.SetValue(value); }
    }

    public AbstractProperty ByProperty
    {
      get { return _byProperty; }
    }

    public Vector2? By
    {
      get { return (Vector2?) _byProperty.GetValue(); }
      set { _byProperty.SetValue(value); }
    }

    public AbstractProperty EasingFunctionProperty
    {
      get { return _easingFunctionProperty; }
    }

    public IEasingFunction EasingFunction
    {
      get { return (IEasingFunction)_easingFunctionProperty.GetValue(); }
      set { _easingFunctionProperty.SetValue(value); }
    }

    #endregion

    #region Animation methods

    internal override void DoAnimation(TimelineContext context, uint timepassed)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return;

      Vector2 from = From ?? (Vector2) patc.StartValue;
      Vector2 to = To ?? (By.HasValue ? new Vector2(from.X + By.Value.X, from.Y + By.Value.Y) : (Vector2) patc.OriginalValue);

      double duration = Duration.TotalMilliseconds;
      if (timepassed > duration)
      {
        patc.DataDescriptor.Value = to;
        return;
      }

      double progress = timepassed / duration;

      IEasingFunction easingFunction = EasingFunction;
      if (easingFunction != null)
        progress = easingFunction.Ease(progress);

      double distx = to.X - from.X;
      distx *= progress;
      distx += from.X;

      double disty = to.Y - from.Y;
      disty *= progress;
      disty += from.Y;

      SetValue(context, new Vector2((float) distx, (float) disty));
    }

    protected Vector2 GetValue(TimelineContext context)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return new Vector2(0, 0);
      object o = patc.DataDescriptor.Value;
      if (o.GetType() == typeof(Vector2)) return (Vector2) o;
      if (o.GetType() == typeof(Vector3))
      {
        Vector3 v = (Vector3) o;
        return new Vector2(v.X, v.Y);
      }
      return new Vector2(0, 0);

    }

    protected static void SetValue(TimelineContext context,Vector2 vector)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return;
      object o = patc.DataDescriptor.Value;
      if (o.GetType() == typeof(Vector2))
      {
        patc.DataDescriptor.Value = vector;
        return;
      }
      if (o.GetType() == typeof(Vector3))
      {
        Vector3 v = new Vector3(vector.X, vector.Y, ((Vector3)o).Z);
        patc.DataDescriptor.Value = v;
        return;
      }
    }

    #endregion
  }
}
