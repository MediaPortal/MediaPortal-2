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
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Animations
{
  public class SplinePointKeyFrame : PointKeyFrame
  {
    KeySpline _spline;
    Property _keySplineProperty;

    public SplinePointKeyFrame()
    {
      Init();
    }

    public SplinePointKeyFrame(SplinePointKeyFrame k)
      : base(k)
    {
      Init();
      this.KeySpline = k.KeySpline;
      OnSplineChanged(null);
    }

    void Init()
    {
      _spline = new KeySpline();
      _keySplineProperty = new Property(new Vector4());
      _keySplineProperty.Attach(new PropertyChangedHandler(OnSplineChanged));
    }


    void OnSplineChanged(Property prop)
    {
      if (this.KeySpline.X != 0 && this.KeySpline.Y != 0 && this.KeySpline.Z != 0 && this.KeySpline.W != 0)
      {
        _spline = new KeySpline(this.KeySpline.X, this.KeySpline.Y, this.KeySpline.Z, this.KeySpline.W);
      }
    }


    public override object Clone()
    {
      return new SplinePointKeyFrame(this);
    }

    public Property KeySplineProperty
    {
      get
      {
        return _keySplineProperty;
      }
      set
      {
        _keySplineProperty = value;
      }
    }

    public Vector4 KeySpline
    {
      get
      {
        return (Vector4)_keySplineProperty.GetValue();
      }
      set
      {
        _keySplineProperty.SetValue(value);
      }
    }


    public override Vector2 Interpolate(Vector2 start, double keyframe)
    {
      if (keyframe <= 0.0) return start;
      if (keyframe >= 1.0) return Value;
      if (double.IsNaN(keyframe)) return start;
      double v = _spline.GetSplineProgress(keyframe);

      double x = (start.X + ((Value.X - start.X) * v));
      double y = (start.Y + ((Value.Y - start.Y) * v));
      return new Vector2((float)x, (float)y);
    }
  }
}
