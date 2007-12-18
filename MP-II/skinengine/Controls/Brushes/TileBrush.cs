using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Brushes
{
  public enum AlignmentX { Left, Center, Right };
  public enum AlignmentY { Top, Center, Bottom };

  public enum Stretch
  {
    StretchNone,
    StretchFill,
    StretchUniform,
    StretchUniformToFill
  };

  public class TileBrush : Brush
  {
    Property _alignmentXProperty;
    Property _alignmentYProperty;
    Property _stretchProperty;
    public TileBrush()
    {
      _alignmentXProperty = new Property(AlignmentX.Center);
      _alignmentYProperty = new Property(AlignmentY.Center);
      _stretchProperty = new Property(Stretch.StretchFill);
    }

    public Property AlignmentXProperty
    {
      get
      {
        return _alignmentXProperty;
      }
      set
      {
        _alignmentXProperty = value;
      }
    }

    public AlignmentX AlignmentX
    {
      get
      {
        return (AlignmentX)_alignmentXProperty.GetValue();
      }
      set
      {
        _alignmentXProperty.SetValue(value);
      }
    }

    public Property AlignmentYProperty
    {
      get
      {
        return _alignmentYProperty;
      }
      set
      {
        _alignmentYProperty = value;
      }
    }

    public AlignmentY AlignmentY
    {
      get
      {
        return (AlignmentY)_alignmentYProperty.GetValue();
      }
      set
      {
        _alignmentYProperty.SetValue(value);
      }
    }

    public Property StretchProperty
    {
      get
      {
        return _stretchProperty;
      }
      set
      {
        _stretchProperty = value;
      }
    }
    public Stretch Stretch
    {
      get
      {
        return (Stretch)_stretchProperty.GetValue();
      }
      set
      {
        _stretchProperty.SetValue(value);
      }
    }
  }
}
