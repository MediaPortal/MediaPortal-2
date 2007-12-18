using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Brushes
{

  public enum BrushMappingMode
  {
    BrushMappingModeAbsolute,
    BrushMappingModeRelativeToBoundingBox
  };

  public enum ColorInterpolationMode
  {
    ColorInterpolationModeScRgbLinearInterpolation,
    ColorInterpolationModeSRgbLinearInterpolation
  };

  public enum GradientSpreadMethod
  {
    GradientSpreadMethodPad,
    GradientSpreadMethodReflect,
    GradientSpreadMethodRepeat
  };


  public class GradientBrush : Brush
  {
    Property _colorInterpolationModeProperty;
    Property _gradientStopsProperty;
    Property _spreadMethodProperty;
    Property _mappingModeProperty;

    public GradientBrush()
    {
      _gradientStopsProperty = new Property(new GradientStopCollection());
      _colorInterpolationModeProperty = new Property(ColorInterpolationMode.ColorInterpolationModeScRgbLinearInterpolation);
      _spreadMethodProperty = new Property(GradientSpreadMethod.GradientSpreadMethodPad);
      _mappingModeProperty = new Property(BrushMappingMode.BrushMappingModeRelativeToBoundingBox);
    }

    public Property ColorInterpolationModeProperty
    {
      get
      {
        return _colorInterpolationModeProperty;
      }
      set
      {
        _colorInterpolationModeProperty = value;
      }
    }

    public ColorInterpolationMode ColorInterpolationMode
    {
      get
      {
        return (ColorInterpolationMode)_colorInterpolationModeProperty.GetValue();
      }
      set
      {
        _colorInterpolationModeProperty.SetValue(value);
      }
    }

    public Property GradientStopsProperty
    {
      get
      {
        return _gradientStopsProperty;
      }
      set
      {
        _gradientStopsProperty = value;
      }
    }

    public GradientStopCollection GradientStops
    {
      get
      {
        return (GradientStopCollection)_gradientStopsProperty.GetValue();
      }
      set
      {
        _gradientStopsProperty.SetValue(value);
      }
    }

    public Property MappingModeProperty
    {
      get
      {
        return _mappingModeProperty;
      }
      set
      {
        _mappingModeProperty = value;
      }
    }
    public BrushMappingMode MappingMode
    {
      get
      {
        return (BrushMappingMode)_mappingModeProperty.GetValue();
      }
      set
      {
        _mappingModeProperty.SetValue(value);
      }
    }

    public Property SpreadMethodProperty
    {
      get
      {
        return _spreadMethodProperty;
      }
      set
      {
        _spreadMethodProperty = value;
      }
    }

    public GradientSpreadMethod SpreadMethod
    {
      get
      {
        return (GradientSpreadMethod)_spreadMethodProperty.GetValue();
      }
      set
      {
        _spreadMethodProperty.SetValue(value);
      }
    }
  }
}
