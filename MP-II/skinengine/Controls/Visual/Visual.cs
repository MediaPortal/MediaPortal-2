using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties; 

namespace Skinengine.Controls.Visuals
{
  public class Visual
  {
    Property _surfaceProperty;
    Property _visualParentProperty;

    public Visual()
    {
      _surfaceProperty = new Property(null);
      _visualParentProperty = new Property(null);
    }

    public Property SurfaceProperty
    {
      get
      {
        return _surfaceProperty;
      }
      set
      {
        _surfaceProperty = value;
      }
    }

    ///@todo: surface returns a surface,not an uri
    public Uri Surface
    {
      get
      {
        return (Uri)_surfaceProperty.GetValue();
      }
      set
      {
        _surfaceProperty.SetValue(value);
      }
    }
    public Property VisualParentProperty
    {
      get
      {
        return _visualParentProperty;
      }
      set
      {
        _visualParentProperty = value;
      }
    }

    public UIElement VisualParent
    {
      get
      {
        return (UIElement)_visualParentProperty.GetValue();
      }
      set
      {
        _visualParentProperty.SetValue(value);
      }
    }

    public virtual bool InsideObject(double x, double y)
    {
      return false;
    }
  }
}

