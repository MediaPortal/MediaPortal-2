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

using MediaPortal.Presentation.DataObjects;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Animations
{
  public class PointAnimation: PropertyAnimationTimeline
  {
    #region Private fields

    Property _fromProperty;
    Property _toProperty;
    Property _byProperty;

    #endregion

    #region Ctor

    public PointAnimation()
    {
      Init();
    }

    void Init()
    {
      _fromProperty = new Property(typeof(Vector2?), null);
      _toProperty = new Property(typeof(Vector2?), null);
      _byProperty = new Property(typeof(Vector2?), null);

    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      PointAnimation a = (PointAnimation) source;
      From = copyManager.GetCopy(a.From);
      To = copyManager.GetCopy(a.To);
      By = copyManager.GetCopy(a.By);
    }

    #endregion

    #region Public properties

    public Property FromProperty
    {
      get { return _fromProperty; }
    }

    public Vector2? From
    {
      get { return (Vector2?) _fromProperty.GetValue(); }
      set { _fromProperty.SetValue(value); }
    }

    public Property ToProperty
    {
      get { return _toProperty; }
    }

    public Vector2? To
    {
      get { return (Vector2?) _toProperty.GetValue(); }
      set { _toProperty.SetValue(value); }
    }

    public Property ByProperty
    {
      get { return _byProperty; }
    }

    public Vector2? By
    {
      get { return (Vector2?) _byProperty.GetValue(); }
      set { _byProperty.SetValue(value); }
    }

    #endregion

    #region Animation methods

    internal override void DoAnimation(TimelineContext context, uint timepassed)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return;

      Vector2 from = From ?? (Vector2) patc.StartValue;
      Vector2 to = To ?? (By.HasValue ? new Vector2(from.X + By.Value.X, from.Y + By.Value.Y) : (Vector2) patc.OriginalValue);

      double distx = (to.X - from.X) / Duration.TotalMilliseconds;
      distx *= timepassed;
      distx += from.X;

      double disty = (to.X - from.Y) / Duration.TotalMilliseconds;
      disty *= timepassed;
      disty += from.Y;

      SetValue(context, new Vector2((float) distx, (float) disty));
    }

    Vector2 GetValue(TimelineContext context)
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

    void SetValue(TimelineContext context,Vector2 vector)
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
