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

namespace SkinEngine.Controls.Visuals
{
  public class FrameworkElement : UIElement
  {
    Property _widthProperty;
    Property _heightProperty;

    Property _acutalWidthProperty;
    Property _actualHeightProperty;
    Property _acutalPositionProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkElement"/> class.
    /// </summary>
    public FrameworkElement()
    {
      _widthProperty = new Property((double)0.0f);
      _heightProperty = new Property((double)0.0f);


      _acutalWidthProperty = new Property((double)0.0f);
      _actualHeightProperty = new Property((double)0.0f);

      _acutalPositionProperty = new Property(new Vector3(0, 0, 1));
    }

    #region properties
    /// <summary>
    /// Gets or sets the width property.
    /// </summary>
    /// <value>The width property.</value>
    public Property WidthProperty
    {
      get
      {
        return _widthProperty;
      }
      set
      {
        _widthProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public double Width
    {
      get
      {
        return (double)_widthProperty.GetValue();
      }
      set
      {
        _widthProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets the height property.
    /// </summary>
    /// <value>The height property.</value>
    public Property HeightProperty
    {
      get
      {
        return _heightProperty;
      }
      set
      {
        _heightProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public double Height
    {
      get
      {
        return (double)_heightProperty.GetValue();
      }
      set
      {
        _heightProperty.SetValue(value);
        OnPropertyChanged();
      }
    }


    /// <summary>
    /// Gets or sets the width property.
    /// </summary>
    /// <value>The width property.</value>
    public Property ActualWidthProperty
    {
      get
      {
        return _acutalWidthProperty;
      }
      set
      {
        _acutalWidthProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public double ActualWidth
    {
      get
      {
        return (double)_acutalWidthProperty.GetValue();
      }
      set
      {
        _acutalWidthProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets the height property.
    /// </summary>
    /// <value>The height property.</value>
    public Property ActualHeightProperty
    {
      get
      {
        return _actualHeightProperty;
      }
      set
      {
        _actualHeightProperty = value;
      }
    }

    public Property ActualPositionProperty
    {
      get
      {
        return _acutalPositionProperty;
      }
      set
      {
        _acutalPositionProperty = value;
      }
    }

    public Vector3 ActualPosition
    {
      get
      {
        return (Vector3)_acutalPositionProperty.GetValue();
      }
      set
      {
        _acutalPositionProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public double ActualHeight
    {
      get
      {
        return (double)_actualHeightProperty.GetValue();
      }
      set
      {
        _actualHeightProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
    #endregion

    /// <summary>
    /// Called when [property changed].
    /// </summary>
    public void OnPropertyChanged()
    {
    }

    /// <summary>
    /// Computes the bounds.
    /// </summary>
    public virtual void ComputeBounds()
    {
      double x1, x2, y1, y2;

      x1 = y1 = 0.0;
      x2 = Width;
      y2 = Height;

      //if (x2 != 0.0 && y2 != 0.0)
      //bounds = bounding_rect_for_transformed_rect(&absolute_xform, IntersectBoundsWithClipPath(Rect(x1, y1, x2, y2), false));
    }

    /// <summary>
    /// Insides the object.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <returns></returns>
    public override bool InsideObject(double x, double y)
    {
      double nx = x, ny = y;

      //uielement_transform_point(this, &nx, &ny);
      if (nx < 0 || ny < 0 || nx > Width || ny > Height)
        return false;

      //return base.InsideObject( x, y);
      return false;
    }

    /// <summary>
    /// Gets the size for brush.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public override void GetSizeForBrush(out double width, out double height)
    {
      double x1, x2, y1, y2;

      x1 = y1 = 0.0;
      x2 = Width;
      y2 = Height;

      //cairo_matrix_transform_point(&absolute_xform, &x1, &y1);
      //cairo_matrix_transform_point(&absolute_xform, &x2, &y2);

      width = x2 - x1;
      height = y2 - y1;
    }


  }
}
