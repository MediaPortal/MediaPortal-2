#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion
using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Brushes
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
      _gradientStopsProperty = new Property(new GradientStopCollection(this));
      _colorInterpolationModeProperty = new Property(ColorInterpolationMode.ColorInterpolationModeScRgbLinearInterpolation);
      _spreadMethodProperty = new Property(GradientSpreadMethod.GradientSpreadMethodPad);
      _mappingModeProperty = new Property(BrushMappingMode.BrushMappingModeRelativeToBoundingBox);

      _gradientStopsProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _colorInterpolationModeProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _spreadMethodProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _mappingModeProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    public void OnGradientsChanged()
    {
      OnPropertyChanged(GradientStopsProperty);
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
