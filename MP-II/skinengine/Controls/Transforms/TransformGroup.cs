using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;

namespace Skinengine.Controls.Transforms
{
  public class TransformGroup : Transform
  {
    Property _childrenProperty;
    public TransformGroup()
    {
      _childrenProperty = new Property(new TransformCollection());
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

    public TransformCollection Children
    {
      get
      {
        return (TransformCollection)_childrenProperty.GetValue();
      }
      set
      {
        _childrenProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public override void UpdateTransform()
    {
      _matrix = Matrix.Identity;
      foreach (Transform t in Children)
      {
        Matrix m;
        t.GetTransform(out m);
        _matrix.Multiply(m);
      }
    }
  }
}
