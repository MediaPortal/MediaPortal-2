using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Properties;
using System.Drawing;
namespace Skinengine.Controls.Brushes
{
  public class SolidColorBrush : Brush
  {
    Property _colorProperty;
    Color _color;

    public SolidColorBrush()
    {
      _colorProperty = new Property(Color.White);
    }

    public Property ColorProperty
    {
      get
      {
        return _colorProperty;
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
      }
    }
  }
}
