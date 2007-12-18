using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Brushes
{
  public class RadialGradientBrush : GradientBrush
  {
    Property _centerProperty;
    Property _gradientOriginProperty;
    Property _radiusXProperty;
    Property _radiusYProperty;
    public RadialGradientBrush()
    {
      _centerProperty = new Property(new Point(0, 0));
      _gradientOriginProperty = new Property(new Point(0, 0));
      _radiusXProperty = new Property((double)0.5f);
      _radiusYProperty = new Property((double)0.5f);
    }

    public Property CenterProperty
    {
      get
      {
        return _centerProperty;
      }
      set
      {
        _centerProperty = value;
      }
    }

    public Point Center
    {
      get
      {
        return (Point)_centerProperty.GetValue();
      }
      set
      {
        _centerProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property GradientOriginProperty
    {
      get
      {
        return _gradientOriginProperty;
      }
      set
      {
        _gradientOriginProperty = value;
      }
    }

    public Point GradientOrigin
    {
      get
      {
        return (Point)_gradientOriginProperty.GetValue();
      }
      set
      {
        _gradientOriginProperty.SetValue(value);
        OnPropertyChanged();
      }
    }


    public Property RadiusXProperty
    {
      get
      {
        return _radiusXProperty;
      }
      set
      {
        _radiusXProperty = value;
      }
    }

    public double RadiusX
    {
      get
      {
        return (double)_radiusXProperty.GetValue();
      }
      set
      {
        _radiusXProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property RadiusYProperty
    {
      get
      {
        return _radiusYProperty;
      }
      set
      {
        _radiusYProperty = value;
      }
    }

    public double RadiusY
    {
      get
      {
        return (double)_radiusYProperty.GetValue();
      }
      set
      {
        _radiusYProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

  }
}
