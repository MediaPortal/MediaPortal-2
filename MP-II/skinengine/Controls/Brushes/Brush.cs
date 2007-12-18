using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Properties;

using Skinengine.Controls.Transforms;
using Skinengine.Controls.Visuals;

namespace Skinengine.Controls.Brushes
{
  public class Brush : Property
  {
    Property _opacityProperty;
    Property _relativeTransformProperty;
    Property _transformProperty;

    public Brush()
    {
      _opacityProperty = new Property((double)1.0f);
      _relativeTransformProperty = new Property(new TransformGroup());
      _transformProperty = new Property(new TransformGroup());
    }
    public void OnPropertyChanged()
    {
      Fire();
    }
    public Property OpacityProperty
    {
      get
      {
        return _opacityProperty;
      }
      set
      {
        _opacityProperty = value;
      }
    }

    public double Opacity
    {
      get
      {
        return (double)_opacityProperty.GetValue();
      }
      set
      {
        _opacityProperty.SetValue(value);
        OnPropertyChanged();
      }
    }


    public Property RelativeTransformProperty
    {
      get
      {
        return _relativeTransformProperty;
      }
      set
      {
        _relativeTransformProperty = value;
        OnPropertyChanged();
      }
    }

    public TransformGroup RelativeTransform
    {
      get
      {
        return (TransformGroup)_relativeTransformProperty.GetValue();
      }
      set
      {
        _relativeTransformProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property TransformProperty
    {
      get
      {
        return _transformProperty;
      }
      set
      {
        _transformProperty = value;
      }
    }

    public TransformGroup Transform
    {
      get
      {
        return (TransformGroup)_transformProperty.GetValue();
      }
      set
      {
        _transformProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public double GetTotalOpacity(UIElement uielement)
    {
      double opacity = (uielement != null) ? uielement.GetTotalOpacity() : 1.0;

      double brush_opacity = Opacity;
      if (brush_opacity < 1.0)
        opacity *= brush_opacity;

      return opacity;
    }
  }
}
