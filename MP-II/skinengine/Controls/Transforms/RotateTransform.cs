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
using MediaPortal.Presentation.Properties;
using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.Direct3D;

namespace SkinEngine.Controls.Transforms
{
  public class RotateTransform : Transform
  {
    Property _centerXProperty;
    Property _centerYProperty;
    Property _angleProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="RotateTransform"/> class.
    /// </summary>
    public RotateTransform()
    {
      Init();
    }

    public RotateTransform(RotateTransform r)
      : base(r)
    {
      Init();
      CenterX = r.CenterX;
      CenterY = r.CenterY;
      Angle = r.Angle;
    }
    void Init()
    {
      _centerYProperty = new Property((double)0.0);
      _centerXProperty = new Property((double)0.0);
      _angleProperty = new Property((double)0.0);
      _centerYProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _centerXProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _angleProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    public override object Clone()
    {
      return new RotateTransform(this);
    }

    protected void OnPropertyChanged(Property property)
    {
      _needUpdate = true;
      Fire();
    }


    /// <summary>
    /// Gets or sets the center X property.
    /// </summary>
    /// <value>The center X property.</value>
    public Property CenterXProperty
    {
      get
      {
        return _centerXProperty;
      }
      set
      {
        _centerXProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the center X.
    /// </summary>
    /// <value>The center X.</value>
    public double CenterX
    {
      get
      {
        return (double)_centerXProperty.GetValue();
      }
      set
      {
        _centerXProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the center Y property.
    /// </summary>
    /// <value>The center Y property.</value>
    public Property CenterYProperty
    {
      get
      {
        return _centerYProperty;
      }
      set
      {
        _centerYProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the center Y.
    /// </summary>
    /// <value>The center Y.</value>
    public double CenterY
    {
      get
      {
        return (double)_centerYProperty.GetValue();
      }
      set
      {
        _centerYProperty.SetValue(value);
      }
    }




    /// <summary>
    /// Gets or sets the angle property.
    /// </summary>
    /// <value>The angle property.</value>
    public Property AngleProperty
    {
      get
      {
        return _angleProperty;
      }
      set
      {
        _angleProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the angle.
    /// </summary>
    /// <value>The angle.</value>
    public double Angle
    {
      get
      {
        return (double)_angleProperty.GetValue();
      }
      set
      {
        _angleProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Updates the transform.
    /// </summary>
    public override void UpdateTransform()
    {
      base.UpdateTransform();
      double radians = Angle / 180.0 * Math.PI;

      if (CenterX == 0.0 && CenterY == 0.0)
      {
        _matrix = Matrix.RotationZ((float)radians);
      }
      else
      {
        _matrix = Matrix.Translation((float)-CenterX * SkinContext.Zoom.Width, (float)-CenterY * SkinContext.Zoom.Height, 0);
        _matrix *= Matrix.RotationZ((float)radians);
        _matrix *= Matrix.Translation((float)CenterX * SkinContext.Zoom.Width, (float)CenterY * SkinContext.Zoom.Height, 0);
      }
    }

    public override void UpdateTransformRel()
    {
      base.UpdateTransformRel();
      double radians = Angle / 180.0 * Math.PI;

      if (CenterX == 0.0 && CenterY == 0.0)
      {
        _matrixRel = Matrix.RotationZ((float)radians);
      }
      else
      {
        _matrixRel = Matrix.Translation((float)-CenterX, (float)-CenterY, 0);
        _matrixRel *= Matrix.RotationZ((float)radians);
        _matrixRel *= Matrix.Translation((float)CenterX, (float)CenterY, 0);
      }
    }
  }
}
