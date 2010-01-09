#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.Core.General;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class SplinePointKeyFrame : PointKeyFrame
  {
    #region Private fields

    KeySpline _spline; // Derived property, will be adjusted automatically when the KeySpline property is changed
    AbstractProperty _keySplineProperty;

    #endregion

    #region Ctor

    public SplinePointKeyFrame()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _spline = new KeySpline();
      _keySplineProperty = new SProperty(typeof(Vector4), new Vector4());
    }

    void Attach()
    {
      _keySplineProperty.Attach(OnSplineChanged);
    }

    void Detach()
    {
      _keySplineProperty.Detach(OnSplineChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      SplinePointKeyFrame kf = (SplinePointKeyFrame) source;
      KeySpline = copyManager.GetCopy(kf.KeySpline);
      Attach();
      InvalidateSpline();
    }

    #endregion

    void OnSplineChanged(AbstractProperty prop, object oldValue)
    {
      InvalidateSpline();
    }

    void InvalidateSpline()
    {
      if (KeySpline.X != 0 && KeySpline.Y != 0 && KeySpline.Z != 0 && KeySpline.W != 0)
        _spline = new KeySpline(KeySpline.X, KeySpline.Y, KeySpline.Z, KeySpline.W);
    }

    #region Public properties

    public AbstractProperty KeySplineProperty
    {
      get { return _keySplineProperty; }
    }

    public Vector4 KeySpline
    {
      get { return (Vector4)_keySplineProperty.GetValue(); }
      set { _keySplineProperty.SetValue(value); }
    }

    #endregion

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
