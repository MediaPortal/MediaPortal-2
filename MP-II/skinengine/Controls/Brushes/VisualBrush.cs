using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using Skinengine.Controls.Visuals;

namespace Skinengine.Controls.Brushes
{
  public class VisualBrush : TileBrush
  {
    Property _visualProperty;
    public VisualBrush()
    {
      _visualProperty = new Property(null);
    }

    public Property VisualProperty
    {
      get
      {
        return _visualProperty;
      }
      set
      {
        _visualProperty = value;
      }
    }

    public Visual Visual
    {
      get
      {
        return (Visual)_visualProperty.GetValue();
      }
      set
      {
        _visualProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
  }
}
