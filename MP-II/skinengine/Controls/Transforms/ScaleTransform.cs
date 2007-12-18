using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Transforms
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
