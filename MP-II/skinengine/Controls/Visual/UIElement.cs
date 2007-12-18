using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Visuals
{
  public class UIElement : Visual
  {
    double _total_opacity;
    Property _opacityProperty;
    public Rectangle bounds;

    public UIElement()
    {
      _opacityProperty = new Property((double)1.0f);
    }
    public Property OpacityProperty
    {
      get
      {
        return _opacityProperty;
      }
      set
      {
        _opacityProperty = value;
      }
    }

    public double Opacity
    {
      get
      {
        return (double)_opacityProperty.GetValue();
      }
      set
      {
        _opacityProperty.SetValue(value);
      }
    }

    // GetTotalOpacity
    //   Get the cumulative opacity of this element, including all it's parents
    public double GetTotalOpacity()
    {
      return _total_opacity;
    }

    public void ComputeTotalOpacity()
    {
      if (VisualParent != null)
        VisualParent.ComputeTotalOpacity();

      double local_opacity = Opacity;
      _total_opacity = local_opacity * ((VisualParent != null) ? VisualParent.GetTotalOpacity() : 1.0);
    }


    //
    // GetSizeForBrush:
    //   Gets the size of the area to be painted by a Brush (needed for image/video scaling)
    public virtual void GetSizeForBrush(out double width, out double height)
    {
      width = 0.0;
      height = 0.0;
    }
  }
}
