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
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
namespace SkinEngine.Controls.Transforms
{
  public class SkewTransform : Transform
  {
    Property _centerXProperty;
    Property _centerYProperty;
    Property _angleXProperty;
    Property _angleYProperty;
    /// <summary>
    /// Initializes a new instance of the <see cref="SkewTransform"/> class.
    /// </summary>
    public SkewTransform()
    {
      Init();
    }
    public SkewTransform(SkewTransform r)
      : base(r)
    {
      Init();
      CenterX = r.CenterX;
      CenterY = r.CenterY;
      AngleX = r.AngleX;
      AngleY = r.AngleY;
    }
    void Init()
    {
      _centerYProperty = new Property((double)0.0);
      _centerXProperty = new Property((double)0.0);
      _angleXProperty = new Property((double)0.0);
      _angleYProperty = new Property((double)0.0);

      _centerYProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _centerXProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _angleXProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _angleYProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }
    public override object Clone()
    {
      return new SkewTransform(this);
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
    /// Gets or sets the angle X property.
    /// </summary>
    /// <value>The angle X property.</value>
    public Property AngleXProperty
    {
      get
      {
        return _angleXProperty;
      }
      set
      {
        _angleXProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the angle X.
    /// </summary>
    /// <value>The angle X.</value>
    public double AngleX
    {
      get
      {
        return (double)_angleXProperty.GetValue();
      }
      set
      {
        _angleXProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the angle Y property.
    /// </summary>
    /// <value>The angle Y property.</value>
    public Property AngleYProperty
    {
      get
      {
        return _angleYProperty;
      }
      set
      {
        _angleYProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the angle Y.
    /// </summary>
    /// <value>The angle Y.</value>
    public double AngleY
    {
      get
      {
        return (double)_angleYProperty.GetValue();
      }
      set
      {
        _angleYProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Updates the transform.
    /// </summary>
    public override void UpdateTransform()
    {
      _matrix = Matrix.Identity;
      return;
      ///@todo: fix skew transform
      double cx = CenterX;
      double cy = CenterY;

      bool translation = ((cx != 0.0) || (cy != 0.0));
      if (translation)
        _matrix.Translate((float)cx, (float)cy, 0);
      else
        _matrix = Matrix.Identity;

      double ax = AngleX;
      //      if (ax != 0.0)
      //        _matrix.xy = Math.Tan(ax * Math.PI / 180);

      double ay = AngleY;
      //if (ay != 0.0)
      //        _matrix.yx = Math.Tan(ay * Math.PI / 180);

      if (translation)
        _matrix.Translate((float)-cx, (float)-cy, 0);

    }

  }
}
