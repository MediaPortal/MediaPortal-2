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

namespace SkinEngine.Controls.Transforms
{
  public class ScaleTransform : Transform
  {
    Property _centerXProperty;
    Property _centerYProperty;
    Property _scaleXProperty;
    Property _scaleYProperty;
    public ScaleTransform()
    {
      _centerYProperty = new Property((double)0.0);
      _centerXProperty = new Property((double)0.0);
      _scaleXProperty = new Property((double)0.0);
      _scaleYProperty = new Property((double)0.0);
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
        OnPropertyChanged();
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




    public Property ScaleXProperty
    {
      get
      {
        return _scaleXProperty;
      }
      set
      {
        _scaleXProperty = value;
      }
    }

    public double ScaleX
    {
      get
      {
        return (double)_scaleXProperty.GetValue();
      }
      set
      {
        _scaleXProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property ScaleYProperty
    {
      get
      {
        return _scaleYProperty;
      }
      set
      {
        _scaleYProperty = value;
      }
    }

    public double ScaleY
    {
      get
      {
        return (double)_scaleYProperty.GetValue();
      }
      set
      {
        _scaleYProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public override void UpdateTransform()
    {
      double sx = ScaleX;
      double sy = ScaleY;

      if (sx == 0.0) sx = 0.00002;
      if (sy == 0.0) sy = 0.00002;

      double cx = CenterX;
      double cy = CenterY;

      if (cx == 0.0 && cy == 0.0)
      {
        _matrix.Scale((float)sx, (float)sy, 1.0f);
      }
      else
      {
        _matrix.Translate((float)cx, (float)cy, 0);
        _matrix.Scale((float)sx, (float)sy, 1.0f);
        _matrix.Translate((float)-cx, (float)-cy, 0);
      }
    }

  }
}
