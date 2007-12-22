#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls.Transforms
{
  public class RotateTransform : Transform
  {
    Property _centerXProperty;
    Property _centerYProperty;
    Property _angleProperty;

    public RotateTransform()
    {
      _centerYProperty = new Property((double)0.0);
      _centerXProperty = new Property((double)0.0);
      _angleProperty = new Property((double)0.0);
    }

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

    public double CenterX
    {
      get
      {
        return (double)_centerXProperty.GetValue();
      }
      set
      {
        _centerXProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

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

    public double CenterY
    {
      get
      {
        return (double)_centerYProperty.GetValue();
      }
      set
      {
        _centerYProperty.SetValue(value);
        OnPropertyChanged();
      }
    }




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

    public double Angle
    {
      get
      {
        return (double)_angleProperty.GetValue();
      }
      set
      {
        _angleProperty.SetValue(value);
        OnPropertyChanged();
      }
    }


    public override void UpdateTransform()
    {
      double radians = Angle / 180.0 * Math.PI;

      if (CenterX == 0.0 && CenterY == 0.0)
      {
        _matrix.RotateZ((float)radians);
      }
      else
      {
        _matrix.Translate((float)-CenterX, (float)-CenterY, 0);
        _matrix *= Matrix.RotationZ((float)radians);
        _matrix *= Matrix.Translation((float)CenterX, (float)CenterY, 0);
      }
    }
  }
}
