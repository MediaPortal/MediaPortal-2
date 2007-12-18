using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Brushes
{
  public class LinearGradientBrush : GradientBrush
  {
    Property _startPointProperty;
    Property _endPointProperty;
    public LinearGradientBrush()
    {
      _startPointProperty = new Property((double)0.0f);
      _endPointProperty = new Property((double)1.0f);
    }

    public Property StartPointProperty
    {
      get
      {
        return _startPointProperty;
      }
      set
      {
        _startPointProperty = value;
      }
    }

    public Point StartPoint
    {
      get
      {
        return (Point)_startPointProperty.GetValue();
      }
      set
      {
        _startPointProperty.SetValue(value);
      }
    }
    public Property EndPointProperty
    {
      get
      {
        return _endPointProperty;
      }
      set
      {
        _endPointProperty = value;
      }
    }

    public Point EndPoint
    {
      get
      {
        return (Point)_endPointProperty.GetValue();
      }
      set
      {
        _endPointProperty.SetValue(value);
      }
    }
  }
}
