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
using SlimDX;
using SlimDX.Direct3D9;
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
      Init();
    }

    public ScaleTransform(ScaleTransform r)
      : base(r)
    {
      Init();
      CenterX = r.CenterX;
      CenterY = r.CenterY;
      ScaleX = r.ScaleX;
      ScaleY = r.ScaleY;
    }
    void Init()
    {
      _centerYProperty = new Property((double)0.0);
      _centerXProperty = new Property((double)0.0);
      _scaleXProperty = new Property((double)0.0);
      _scaleYProperty = new Property((double)0.0);
      _centerYProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _centerXProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _scaleXProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _scaleYProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    public override object Clone()
    {
      return new ScaleTransform(this);
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
    /// Gets or sets the scale X property.
    /// </summary>
    /// <value>The scale X property.</value>
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

    /// <summary>
    /// Gets or sets the scale X.
    /// </summary>
    /// <value>The scale X.</value>
    public double ScaleX
    {
      get
      {
        return (double)_scaleXProperty.GetValue();
      }
      set
      {
        _scaleXProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the scale Y property.
    /// </summary>
    /// <value>The scale Y property.</value>
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

    /// <summary>
    /// Gets or sets the scale Y.
    /// </summary>
    /// <value>The scale Y.</value>
    public double ScaleY
    {
      get
      {
        return (double)_scaleYProperty.GetValue();
      }
      set
      {
        _scaleYProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Updates the transform.
    /// </summary>
    public override void UpdateTransform()
    {
      base.UpdateTransform();
      double sx = ScaleX;
      double sy = ScaleY;

      if (sx == 0.0) sx = 0.00002;
      if (sy == 0.0) sy = 0.00002;

      double cx = CenterX;
      double cy = CenterY;

      if (cx == 0.0 && cy == 0.0)
      {
        _matrix=Matrix.Scaling((float)sx, (float)sy, 1.0f);
      }
      else
      {
        _matrix=Matrix.Translation((float)-cx, (float)-cy, 0);
        _matrix *= Matrix.Scaling((float)sx, (float)sy, 1.0f);
        _matrix *= Matrix.Translation((float)cx, (float)cy, 0);
      }
    }

  }
}
