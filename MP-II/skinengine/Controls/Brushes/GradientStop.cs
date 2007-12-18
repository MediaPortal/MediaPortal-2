using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Brushes
{
  public class GradientStop : Property
  {
    Property _colorProperty;
    Property _offsetProperty;
    public GradientStop()
    {
      _colorProperty = new Property(Color.White);
      _offsetProperty = new Property((double)0.0f);
    }
    public void OnPropertyChanged()
    {
      Fire();
    }

    public Property ColorProperty
    {
      get
      {
        return _colorProperty;
      }
      set
      {
        _colorProperty = value;
      }
    }

    public Color Color
    {
      get
      {
        return (Color)_colorProperty.GetValue();
      }
      set
      {
        _colorProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property OffsetProperty
    {
      get
      {
        return _offsetProperty;
      }
      set
      {
        _offsetProperty = value;
      }
    }

    public double Offset
    {
      get
      {
        return (double)_offsetProperty.GetValue();
      }
      set
      {
        _offsetProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
  }
}
