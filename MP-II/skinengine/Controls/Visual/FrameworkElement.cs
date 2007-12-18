using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Visuals
{
  public class FrameworkElement : UIElement
  {
    Property _widthProperty;
    Property _heightProperty;
    public FrameworkElement()
    {
      _widthProperty = new Property((double)0.0f);
      _heightProperty = new Property((double)1.0f);
    }

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
    public void OnPropertyChanged()
    {
    }
    public virtual void ComputeBounds()
    {
      double x1, x2, y1, y2;

      x1 = y1 = 0.0;
      x2 = Width;
      y2 = Height;

      //if (x2 != 0.0 && y2 != 0.0)
      //bounds = bounding_rect_for_transformed_rect(&absolute_xform, IntersectBoundsWithClipPath(Rect(x1, y1, x2, y2), false));
    }

    public override bool InsideObject(double x, double y)
    {
      double nx = x, ny = y;

      //uielement_transform_point(this, &nx, &ny);
      if (nx < 0 || ny < 0 || nx > Width || ny > Height)
        return false;

      //return base.InsideObject( x, y);
      return false;
    }

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
