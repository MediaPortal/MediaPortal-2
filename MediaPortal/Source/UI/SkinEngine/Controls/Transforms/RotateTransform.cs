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
  public class RotateTransform : Transform
  {
    #region Protected fields

    protected AbstractProperty _centerXProperty;
    protected AbstractProperty _centerYProperty;
    protected AbstractProperty _angleProperty;

    #endregion

    #region Ctor

    public RotateTransform()
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
      _angleProperty = new SProperty(typeof(double), 0.0);
    }

    void Attach()
    {
      _centerYProperty.Attach(OnPropertyChanged);
      _centerXProperty.Attach(OnPropertyChanged);
      _angleProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _centerYProperty.Detach(OnPropertyChanged);
      _centerXProperty.Detach(OnPropertyChanged);
      _angleProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      RotateTransform t = (RotateTransform) source;
      CenterX = t.CenterX;
      CenterY = t.CenterY;
      Angle = t.Angle;
      Attach();
    }

    #endregion

    #region Public properties

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

    public AbstractProperty AngleProperty
    {
      get { return _angleProperty; }
    }

    public double Angle
    {
      get { return (double) _angleProperty.GetValue(); }
      set { _angleProperty.SetValue(value); }
    }

    #endregion

    public override void UpdateTransform()
    {
      base.UpdateTransform();
      double radians = Angle / 180.0 * Math.PI;

      if (CenterX == 0.0 && CenterY == 0.0)
        _matrix = Matrix.RotationZ((float) radians);
      else
      {
        _matrix = Matrix.Translation((float) -CenterX, (float) -CenterY, 0);
        _matrix *= Matrix.RotationZ((float) radians);
        _matrix *= Matrix.Translation((float) CenterX, (float) CenterY, 0);
      }
    }
  }
}
