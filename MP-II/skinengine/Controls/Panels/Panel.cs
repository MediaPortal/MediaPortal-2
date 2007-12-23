using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Panels
{
  public class Panel : FrameworkElement
  {
    Property _alignmentXProperty;
    Property _alignmentYProperty;
    Property _childrenProperty;

    public Panel()
    {
      _childrenProperty = new Property(new UIElementCollection());
      _alignmentXProperty = new Property(AlignmentX.Center);
      _alignmentYProperty = new Property(AlignmentY.Center);
    }

    public Property ChildrenProperty
    {
      get
      {
        return _childrenProperty;
      }
      set
      {
        _childrenProperty = value;
      }
    }

    public UIElementCollection Children
    {
      get
      {
        return _childrenProperty.GetValue() as UIElementCollection;
      }
      set
      {
        _childrenProperty.SetValue(value);
        OnPropertyChanged();
      }
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
        OnPropertyChanged();
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
        OnPropertyChanged();
      }
    }

    public override void DoRender()
    {
      foreach (UIElement element in Children)
      {
        if (element.IsVisible)
        {
          element.DoRender();
        }
      }
    }
  }
}
