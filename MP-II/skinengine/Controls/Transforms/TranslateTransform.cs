using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Transforms
{
  public class TranslateTransform : Transform
  {
    Property _XProperty;
    Property _YProperty;
    public TranslateTransform()
    {
      _YProperty = new Property((double)0.0);
      _XProperty = new Property((double)0.0);
    }

    public Property XProperty
    {
      get
      {
        return _XProperty;
      }
      set
      {
        _XProperty = value;
      }
    }

    public double X
    {
      get
      {
        return (double)_XProperty.GetValue();
      }
      set
      {
        _XProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property YProperty
    {
      get
      {
        return _YProperty;
      }
      set
      {
        _YProperty = value;
      }
    }

    public double Y
    {
      get
      {
        return (double)_YProperty.GetValue();
      }
      set
      {
        _YProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public override void UpdateTransform()
    {
      _matrix.Translate((float)X, (float)Y, 0);
    }


  }
}
