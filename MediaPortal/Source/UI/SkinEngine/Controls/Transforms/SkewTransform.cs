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

using System;
using MediaPortal.Common.General;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Transforms
{
  public class SkewTransform : Transform
  {
    #region Protected fields

    protected AbstractProperty _centerXProperty;
    protected AbstractProperty _centerYProperty;
    protected AbstractProperty _angleXProperty;
    protected AbstractProperty _angleYProperty;

    #endregion

    #region Ctor

    public SkewTransform()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    void Init()
    {
      _centerYProperty = new SProperty(typeof(double), 0.0);
      _centerXProperty = new SProperty(typeof(double), 0.0);
      _angleXProperty = new SProperty(typeof(double), 0.0);
      _angleYProperty = new SProperty(typeof(double), 0.0);
    }

    void Attach()
    {
      _centerYProperty.Attach(OnPropertyChanged);
      _centerXProperty.Attach(OnPropertyChanged);
      _angleXProperty.Attach(OnPropertyChanged);
      _angleYProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _centerYProperty.Detach(OnPropertyChanged);
      _centerXProperty.Detach(OnPropertyChanged);
      _angleXProperty.Detach(OnPropertyChanged);
      _angleYProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      SkewTransform t = (SkewTransform) source;
      CenterX = t.CenterX;
      CenterY = t.CenterY;
      AngleX = t.AngleX;
      AngleY = t.AngleY;
      Attach();
    }

    #endregion

    public AbstractProperty CenterXProperty
    {
      get { return _centerXProperty; }
    }

    public double CenterX
    {
      get { return (double) _centerXProperty.GetValue(); }
      set { _centerXProperty.SetValue(value); }
    }

    public AbstractProperty CenterYProperty
    {
      get { return _centerYProperty; }
    }

    public double CenterY
    {
      get { return (double) _centerYProperty.GetValue(); }
      set { _centerYProperty.SetValue(value); }
    }

    public AbstractProperty AngleXProperty
    {
      get { return _angleXProperty; }
    }

    public double AngleX
    {
      get { return (double) _angleXProperty.GetValue(); }
      set { _angleXProperty.SetValue(value); }
    }

    public AbstractProperty AngleYProperty
    {
      get { return _angleYProperty; }
    }

    public double AngleY
    {
      get { return (double) _angleYProperty.GetValue(); }
      set { _angleYProperty.SetValue(value); }
    }

    public override void UpdateTransform()
    {
      base.UpdateTransform();
      _matrix = Matrix.Identity;

      float centerX = (float) CenterX;
      float centerY = (float) CenterY;

      bool centerSet = (centerX != 0.0 || centerY != 0.0);
      if (centerSet)
        _matrix = Matrix.Translation(-centerX, -centerY, 0);

      double skewX = AngleX % 360.0;
      double skewY = AngleY % 360.0;
      _matrix *= CreateSkewRadians(skewX * Math.PI / 180.0, skewY * Math.PI / 180.0);


      if (centerSet)
        _matrix *= Matrix.Translation(centerX, centerY, 0);
    }

    static Matrix CreateSkewRadians(double skewX, double skewY)
    {
      Matrix matrix = Matrix.Identity;
      SetMatrix(ref matrix, 1.0, Math.Tan(skewY), Math.Tan(skewX), 1.0);
      return matrix;
    }

    static void SetMatrix(ref Matrix m, double m11, double m12, double m21, double m22)
    {
      m.M11 = (float)m11;
      m.M12 = (float)m12;
      m.M21 = (float)m21;
      m.M22 = (float)m22;
    }
  }
}
