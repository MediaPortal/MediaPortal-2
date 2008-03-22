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
using System.Drawing;
using MediaPortal.Presentation.Properties;
using SlimDX;
using SlimDX.Direct3D9;
namespace Presentation.SkinEngine.Controls.Animations
{
  public class SplineColorKeyFrame : ColorKeyFrame
  {
    KeySpline _spline;
    Property _keySplineProperty;
    #region ctor
    public SplineColorKeyFrame()
    {
      Init();
      Attach();
    }

    public SplineColorKeyFrame(SplineColorKeyFrame k)
      : base(k)
    {
      Init();
      this.KeySpline = k.KeySpline;
      OnSplineChanged(null);
      Attach();
    }

    void Init()
    {
      _spline = new KeySpline();
      _keySplineProperty = new Property(new Vector4());
      _keySplineProperty.Attach(new PropertyChangedHandler(OnSplineChanged));
    }
    void Attach()
    {
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
      return new SplineColorKeyFrame(this);
    }
    #endregion

    #region properties
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
    #endregion


    public override Color Interpolate(Color start, double keyframe)
    {
      if (keyframe <= 0.0) return start;
      if (keyframe >= 1.0) return Value;
      if (double.IsNaN(keyframe)) return start;
      double v = _spline.GetSplineProgress(keyframe);

      double a = start.A + ((Value.A - start.A) * v);
      double r = start.R + ((Value.R - start.R) * v);
      double g = start.G + ((Value.G - start.G) * v);
      double b = start.B + ((Value.B - start.B) * v);
      return Color.FromArgb((int)a, (int)r, (int)g, (int)b);
    }
  }
}

