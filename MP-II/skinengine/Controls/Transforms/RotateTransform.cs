using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Transforms
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
        _matrix.Translate((float)CenterX, (float)CenterY, 0);
        _matrix.RotateZ((float)radians);
        _matrix.Translate((float)-CenterX, (float)-CenterY, 0);
      }
    }
  }
}
