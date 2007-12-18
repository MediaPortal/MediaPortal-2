using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Properties;

using Skinengine.Controls.Transforms;

namespace Skinengine.Controls.Brushes
{
  public class Brush
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
      }
    }
  }
}
