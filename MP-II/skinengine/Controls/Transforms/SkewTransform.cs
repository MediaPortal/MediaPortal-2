using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
namespace Skinengine.Controls.Transforms
{
  public class SkewTransform : Transform
  {
    Property _centerXProperty;
    Property _centerYProperty;
    Property _angleXProperty;
    Property _angleYProperty;
    public SkewTransform()
    {
      _centerYProperty = new Property((double)0.0);
      _centerXProperty = new Property((double)0.0);
      _angleXProperty = new Property((double)0.0);
      _angleYProperty = new Property((double)0.0);
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

    public double AngleX
    {
      get
      {
        return (double)_angleXProperty.GetValue();
      }
      set
      {
        _angleXProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

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

    public double AngleY
    {
      get
      {
        return (double)_angleYProperty.GetValue();
      }
      set
      {
        _angleYProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public override void UpdateTransform()
    {
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
